using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

/// <summary>
/// Enemigo que avanza por el NavMesh hacia la base y persigue al jugador si lo tiene
/// cerca. Ataca por distancia (sin depender de colisiones). Vive en un pool: Spawn() lo
/// activa y al morir se devuelve solo.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour, IDamageable
{
    // Objetivos compartidos por todos los enemigos; se resuelven una sola vez por escena.
    private static BaseHealth baseTarget;
    private static PlayerHealth playerTarget;
    private static Collider baseCollider;
    private static Collider playerCollider;

    private NavMeshAgent agent;
    private EnemyDefinition def;
    private ObjectPool<EnemyAI> pool;
    private float bodyRadius;
    private int currentHealth;
    private float nextAttackTime;
    private float nextRepathTime;
    private float chaseTimer;
    private float ignorePlayerUntil;
    private bool isDead;
    private EnemyBleed bleed;

    public bool IsDead => isDead;

    /// <summary>Lo llama el pool una sola vez, al crear la instancia.</summary>
    public void Init(ObjectPool<EnemyAI> ownerPool, EnemyDefinition definition)
    {
        pool = ownerPool;
        def = definition;
        agent.speed = def.speed;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.acceleration = 30f;
        agent.stoppingDistance = 0f;

        Collider body = GetComponent<Collider>();
        if (body != null)
        {
            Vector3 extents = body.bounds.extents;
            bodyRadius = Mathf.Max(extents.x, extents.z);
            agent.radius = bodyRadius;
            agent.height = extents.y * 2f;
            agent.baseOffset = extents.y > 0f ? extents.y : 0.5f;
        }
    }

    /// <summary>Coloca y activa al enemigo. healthScale permite oleadas más resistentes.</summary>
    public void Spawn(Vector3 position, float healthScale = 1f)
    {
        isDead = false;
        if (bleed != null) bleed.Clear(); // viene del pool: sin sangrado ni tinte de su vida anterior
        currentHealth = Mathf.CeilToInt(def.maxHealth * healthScale);
        chaseTimer = 0f;
        ignorePlayerUntil = 0f;
        nextAttackTime = 0f;
        nextRepathTime = 0f;

        ResolveTargets();

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            position = hit.position;

        // El agente viene desactivado en el prefab para que no busque NavMesh antes de colocarlo.
        if (agent.enabled)
        {
            agent.Warp(position);
        }
        else
        {
            transform.position = position;
            agent.enabled = true;
        }

        agent.isStopped = false;
        FaceBase();
    }

    // Sin esto el enemigo aparece con la orientación que tenía en el pool (o la del prefab) y el
    // agente tarda ~1,5 s en girar, caminando de espaldas a su destino.
    private void FaceBase()
    {
        if (baseTarget == null) return;

        Vector3 toBase = baseTarget.transform.position - transform.position;
        toBase.y = 0f;
        if (toBase.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(toBase);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth > 0) return;

        isDead = true;
        if (bleed != null) bleed.Clear();
        GameEvents.RaiseEnemyKilled(def.moneyReward);
        GameEvents.RaiseXpGained(def.xpReward);

        if (pool != null) pool.Release(this);
        else gameObject.SetActive(false);
    }

    /// <summary>Suma pilas de sangrado (permanentes hasta que muera). No hace nada si ya está muerto.</summary>
    public void ApplyBleed(int stacks, int cap, int damagePerStack)
    {
        if (isDead || stacks <= 0) return;

        if (bleed == null)
        {
            bleed = GetComponent<EnemyBleed>();
            if (bleed == null) bleed = gameObject.AddComponent<EnemyBleed>();
        }
        bleed.Apply(stacks, cap, damagePerStack);
    }

    private void Update()
    {
        if (isDead || !agent.isOnNavMesh) return;

        if (GameState.IsGameOver || baseTarget == null || playerTarget == null)
        {
            agent.isStopped = true;
            return;
        }

        // Hacia dónde se mueve (a quién persigue) y a quién golpea son decisiones separadas:
        // al jugador que tenga al alcance lo golpea siempre, aunque no sea su destino, y en ese
        // caso sigue caminando hacia la base en vez de detenerse.
        Collider target = ChooseTarget();
        bool playerInReach = DistanceToEdge(playerCollider) <= def.attackReach;

        if (playerInReach) Attack(true);

        if (DistanceToEdge(target) <= def.attackReach)
        {
            agent.isStopped = true;
            if (!playerInReach) Attack(target == playerCollider);
            return;
        }

        agent.isStopped = false;
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + def.repathInterval;
            MoveTo(target.transform.position);
        }
    }

    /// <summary>Decide hacia dónde se mueve el enemigo: al jugador (si lo persigue) o a la base.</summary>
    private Collider ChooseTarget()
    {
        Vector3 position = transform.position;
        Vector3 playerPos = playerTarget.transform.position;
        Vector3 basePos = baseTarget.transform.position;

        bool canChasePlayer = Time.time >= ignorePlayerUntil;
        float distanceToPlayer = Vector3.Distance(position, playerPos);

        Vector3 dirToBase = (basePos - position).normalized;
        Vector3 dirToPlayer = (playerPos - position).normalized;
        bool playerIsBehind = Vector3.Dot(dirToBase, dirToPlayer) < def.directionTolerance;

        if (canChasePlayer && distanceToPlayer <= def.detectionRange && !playerIsBehind)
        {
            chaseTimer += Time.deltaTime;
            if (chaseTimer >= def.chaseGiveUpTime)
            {
                ignorePlayerUntil = Time.time + def.giveUpCooldown;
                chaseTimer = 0f;
                return baseCollider;
            }
            return playerCollider;
        }

        chaseTimer = 0f;
        return baseCollider;
    }

    private float DistanceToEdge(Collider target)
    {
        Vector3 closest = target.ClosestPoint(transform.position);
        Vector3 offset = closest - transform.position;
        offset.y = 0f;
        return offset.magnitude - bodyRadius;
    }

    private void MoveTo(Vector3 destination)
    {
        // El destino (centro de la base o del jugador) puede quedar fuera del NavMesh.
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    private void Attack(bool targetIsPlayer)
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + def.damageInterval;

        if (targetIsPlayer) playerTarget.TakeDamage(def.damageToPlayer);
        else baseTarget.TakeDamage(def.damageToBase);
    }

    private static void ResolveTargets()
    {
        if (baseTarget == null)
        {
            baseTarget = FindFirstObjectByType<BaseHealth>();
            baseCollider = baseTarget != null ? baseTarget.GetComponent<Collider>() : null;
        }

        if (playerTarget == null)
        {
            playerTarget = FindFirstObjectByType<PlayerHealth>();
            playerCollider = playerTarget != null ? playerTarget.GetComponent<Collider>() : null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        baseTarget = null;
        playerTarget = null;
        baseCollider = null;
        playerCollider = null;
    }
}
