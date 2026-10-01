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
    [Header("Stats")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private int health = 30;
    [SerializeField] private int moneyReward = 10;

    [Header("Ataque")]
    [SerializeField] private int damageToBase = 10;
    [SerializeField] private int damageToPlayer = 10;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField, Tooltip("Distancia desde el borde del enemigo al borde del objetivo para poder atacar")]
    private float attackReach = 0.6f;

    [Header("Persecución")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float chaseGiveUpTime = 5f;
    [SerializeField] private float giveUpCooldown = 3f;
    [SerializeField, Tooltip("Si el jugador queda 'detrás' respecto a la base, el enemigo lo ignora")]
    private float directionTolerance = 0f;
    [SerializeField] private float repathInterval = 0.25f;

    // Objetivos compartidos por todos los enemigos; se resuelven una sola vez por escena.
    private static BaseHealth baseTarget;
    private static PlayerHealth playerTarget;
    private static Collider baseCollider;
    private static Collider playerCollider;

    private NavMeshAgent agent;
    private ObjectPool<EnemyAI> pool;
    private float bodyRadius;
    private int currentHealth;
    private float nextAttackTime;
    private float nextRepathTime;
    private float chaseTimer;
    private float ignorePlayerUntil;
    private bool isDead;

    public void Init(ObjectPool<EnemyAI> ownerPool)
    {
        pool = ownerPool;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
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

    /// <summary>Coloca y activa al enemigo. healthScale permite hordas más resistentes.</summary>
    public void Spawn(Vector3 position, float healthScale = 1f)
    {
        isDead = false;
        currentHealth = Mathf.CeilToInt(health * healthScale);
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
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth > 0) return;

        isDead = true;
        GameEvents.RaiseEnemyKilled(moneyReward);

        if (pool != null) pool.Release(this);
        else gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isDead || !agent.isOnNavMesh) return;

        if (GameState.IsGameOver || baseTarget == null || playerTarget == null)
        {
            agent.isStopped = true;
            return;
        }

        Collider target = ChooseTarget();
        bool targetIsPlayer = target == playerCollider;

        if (DistanceToEdge(target) <= attackReach)
        {
            agent.isStopped = true;
            Attack(targetIsPlayer);
            return;
        }

        agent.isStopped = false;
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + repathInterval;
            MoveTo(target.transform.position);
        }
    }

    private Collider ChooseTarget()
    {
        Vector3 position = transform.position;
        Vector3 playerPos = playerTarget.transform.position;
        Vector3 basePos = baseTarget.transform.position;

        bool canChasePlayer = Time.time >= ignorePlayerUntil;
        float distanceToPlayer = Vector3.Distance(position, playerPos);

        Vector3 dirToBase = (basePos - position).normalized;
        Vector3 dirToPlayer = (playerPos - position).normalized;
        bool playerIsBehind = Vector3.Dot(dirToBase, dirToPlayer) < directionTolerance;

        if (canChasePlayer && distanceToPlayer <= detectionRange && !playerIsBehind)
        {
            chaseTimer += Time.deltaTime;
            if (chaseTimer >= chaseGiveUpTime)
            {
                ignorePlayerUntil = Time.time + giveUpCooldown;
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
        nextAttackTime = Time.time + damageInterval;

        if (targetIsPlayer) playerTarget.TakeDamage(damageToPlayer);
        else baseTarget.TakeDamage(damageToBase);
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
