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
    private Collider bodyCollider;
    private float slowMultiplier = 1f;
    private float slowUntil;
    private BossController boss;
    private int maxHealthScaled;
    private float enrageSpeed = 1f;
    private float enrageDamage = 1f;
    private float invulnerableUntil;

    public bool IsDead => isDead;

    // --- Lo que necesita un jefe (BossController) ---
    public EnemyDefinition Definition => def;
    public int CurrentHealth => currentHealth;
    public int MaxHealthScaled => maxHealthScaled;
    public float HealthFraction => maxHealthScaled > 0 ? Mathf.Clamp01((float)currentHealth / maxHealthScaled) : 0f;
    public float HealthScale { get; private set; } = 1f;
    public float BodyRadius => bodyRadius;

    /// <summary>Mientras es true la IA normal no mueve ni ataca (el jefe se mueve por su cuenta, por ejemplo al embestir).</summary>
    public bool IsControlled { get; private set; }

    public void SetControlled(bool controlled)
    {
        IsControlled = controlled;
        if (controlled && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
    }

    /// <summary>Gana velocidad y daño de forma permanente (hasta que muera): 0,4 = +40%.</summary>
    public void Enrage(float speedBonus, float damageBonus)
    {
        enrageSpeed += speedBonus;
        enrageDamage += damageBonus;
        ApplySpeed();
    }

    public void SetInvulnerable(float seconds) => invulnerableUntil = Time.time + seconds;

    /// <summary>Mueve al enemigo respetando el NavMesh (embestida).</summary>
    public void MoveForced(Vector3 delta)
    {
        if (agent.enabled && agent.isOnNavMesh) agent.Move(delta);
        else transform.position += delta;
    }

    public void FaceDirection(Vector3 flatDirection)
    {
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flatDirection);
    }

    // Velocidad = la del tipo x enfurecer x ralentización (la ralentización no acumula con otra: vale la última).
    private void ApplySpeed()
    {
        float slow = Time.time < slowUntil ? slowMultiplier : 1f;
        agent.speed = def.speed * enrageSpeed * slow;
    }

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
        boss = GetComponent<BossController>();
        agent.acceleration = 30f;
        agent.stoppingDistance = 0f;

        Collider body = GetComponent<Collider>();
        bodyCollider = body;
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
        maxHealthScaled = currentHealth;
        HealthScale = healthScale;
        enrageSpeed = 1f;
        enrageDamage = 1f;
        invulnerableUntil = 0f;
        IsControlled = false;
        chaseTimer = 0f;
        ignorePlayerUntil = 0f;
        nextAttackTime = 0f;
        nextRepathTime = 0f;
        slowUntil = 0f;
        slowMultiplier = 1f;
        ApplySpeed();

        ResolveTargets();

        // La niebla de Alucard ignora las colisiones con los enemigos cercanos; un enemigo que vuelve del pool
        // no debe arrastrar ese estado.
        if (playerCollider != null && bodyCollider != null)
            Physics.IgnoreCollision(playerCollider, bodyCollider, false);

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

        if (boss != null) boss.Begin(this, def);
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
        if (isDead || Time.time < invulnerableUntil) return;

        currentHealth -= damage;
        if (currentHealth > 0) return;

        // Un jefe que resucita se levanta en vez de morir.
        if (boss != null && boss.TryRevive(out int revivedHealth))
        {
            currentHealth = revivedHealth;
            return;
        }

        isDead = true;
        if (bleed != null) bleed.Clear();
        GameEvents.RaiseEnemyKilled(def.moneyReward);
        GameEvents.RaiseXpGained(def.xpReward);
        if (def.treePointsReward > 0) GameEvents.RaiseBossDefeated(def.treePointsReward);

        if (pool != null) pool.Release(this);
        else gameObject.SetActive(false);
    }

    /// <summary>Le quita una fracción de velocidad durante unos segundos (0,3 = -30%). No acumula: vale la última.</summary>
    public void ApplySlow(float fraction, float seconds)
    {
        if (isDead || fraction <= 0f || seconds <= 0f) return;

        slowMultiplier = Mathf.Clamp(1f - fraction, 0.1f, 1f);
        slowUntil = Time.time + seconds;
        ApplySpeed();
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

        // Termina la ralentización: vuelve a su velocidad normal.
        if (slowUntil > 0f && Time.time >= slowUntil)
        {
            slowUntil = 0f;
            slowMultiplier = 1f;
            ApplySpeed();
        }

        if (IsControlled) return;

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

        if (targetIsPlayer) playerTarget.TakeDamage(Mathf.RoundToInt(def.damageToPlayer * enrageDamage));
        else baseTarget.TakeDamage(Mathf.RoundToInt(def.damageToBase * enrageDamage));
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
