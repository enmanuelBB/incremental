using System.Collections.Generic;
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

    // A esta distancia (más el alcance) de su objetivo deja de esquivar a los otros enemigos y empuja para trepar.
    private const float PressDistance = 6f;

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
    private EnemyBurn burn;
    private EnemyPoison poison;
    private Collider bodyCollider;
    private float slowMultiplier = 1f;
    private float slowUntil;
    private float damageTakenBonus;
    private float damageTakenUntil;
    private readonly StunTimer stun = new StunTimer();
    private BossController boss;
    private int maxHealthScaled;
    private float enrageSpeed = 1f;
    private float enrageDamage = 1f;
    private float invulnerableUntil;

    // Altura del cuerpo: la de apoyo en el suelo, más la de vuelo, más la del montón en que está subido.
    private float groundOffset;
    private float heightScale = 1f;
    private float bodyHeight;
    private float bobPhase;
    private ObstacleAvoidanceType defaultAvoidance;
    private float distanceToGoal;
    private float climbBlockedUntil;

    // Enemigos activos, para el apilado (EnemyCrowd).
    private static readonly List<EnemyAI> active = new List<EnemyAI>();
    public static IReadOnlyList<EnemyAI> Active => active;

    /// <summary>Altura a la que está subido sobre otros enemigos (0 = en el suelo). La mueve EnemyCrowd.</summary>
    public float StackLift { get; set; }
    public float BodyHeight => bodyHeight;
    /// <summary>Se sube encima de otros al amontonarse (ni los jefes ni los voladores trepan).</summary>
    public bool CanClimb => def != null && def.climbsOthers && !def.Flies && !IsBoss && !isDead;
    /// <summary>Sirve de apoyo a los que trepan (los voladores no).</summary>
    public bool CanSupport => def != null && !def.Flies && !isDead;
    /// <summary>Distancia en planta hasta el borde de su objetivo: en un montón, el más cercano queda abajo.</summary>
    public float DistanceToGoal => distanceToGoal;

    /// <summary>
    /// Llegó a lo más alto del montón: unos segundos vuelve a esquivar a los demás, así se corre a un lado y el montón
    /// se ensancha en vez de seguir creciendo en una sola columna.
    /// </summary>
    public void BlockClimb(float seconds) => climbBlockedUntil = Time.time + seconds;

    public bool IsDead => isDead;
    public bool IsStunned => stun.IsStunned(Time.time);

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

        defaultAvoidance = agent.obstacleAvoidanceType;
        bobPhase = Random.value * 10f;

        // El agente multiplica radio, alto y altura de apoyo por la escala del objeto: se le pasan en unidades locales
        // (si no, un enemigo de escala 1,6 flota en el aire y ocupa más espacio del que se ve).
        Vector3 scale = transform.lossyScale;
        float flatScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z), 0.0001f);
        heightScale = Mathf.Max(Mathf.Abs(scale.y), 0.0001f);

        Collider body = GetComponent<Collider>();
        bodyCollider = body;
        if (body != null)
        {
            Vector3 extents = body.bounds.extents;
            bodyRadius = Mathf.Max(extents.x, extents.z);
            bodyHeight = extents.y * 2f;
            agent.radius = bodyRadius / flatScale;
            agent.height = bodyHeight / heightScale;
            groundOffset = extents.y > 0f ? extents.y : 0.5f;
            agent.baseOffset = groundOffset / heightScale;
        }
    }

    private void OnEnable() => active.Add(this);

    private void OnDisable() => active.Remove(this);

    // Vuelo (con un vaivén suave) o altura del montón en que está subido.
    private void ApplyHeight()
    {
        float lift = def.Flies ? def.flyHeight + Mathf.Sin(Time.time * 2f + bobPhase) * 0.15f : StackLift;
        agent.baseOffset = (groundOffset + lift) / heightScale;
    }

    /// <summary>Coloca y activa al enemigo. healthScale permite oleadas más resistentes.</summary>
    public void Spawn(Vector3 position, float healthScale = 1f)
    {
        isDead = false;
        if (bleed != null) bleed.Clear(); // viene del pool: sin sangrado ni tinte de su vida anterior
        if (burn != null) burn.Clear();   // ni quemadura
        if (poison != null) poison.Clear(); // ni veneno
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
        damageTakenBonus = 0f;
        damageTakenUntil = 0f;
        stun.Clear();
        StackLift = 0f;
        distanceToGoal = 0f;
        climbBlockedUntil = 0f;
        agent.obstacleAvoidanceType = defaultAvoidance;
        ApplySpeed();
        ApplyHeight();

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

    /// <summary>Golpe directo (balas, espada, habilidades): muestra el número de daño sobre el enemigo.</summary>
    public void TakeDamage(int damage) => ApplyDamage(damage, true);

    /// <summary>Daño de sangrado, quemadura o veneno: esos publican su propio número (de su color), así que aquí no.</summary>
    public void TakeTickDamage(int damage) => ApplyDamage(damage, false);

    private void ApplyDamage(int damage, bool showNumber)
    {
        if (isDead || Time.time < invulnerableUntil) return;
        if (Time.time < damageTakenUntil) damage = FrierenTreeMath.Scale(damage, damageTakenBonus);   // Marca de maná

        // El número se publica antes de restar la vida: si el golpe mata, el enemigo vuelve al pool.
        if (showNumber && damage > 0) GameEvents.RaiseEnemyHit(HeadPosition(), damage);

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
        if (burn != null) burn.Clear();
        if (poison != null) poison.Clear();
        GameEvents.RaiseEnemyKilled(def.moneyReward);
        GameEvents.RaiseEnemyDied(transform.position, IsBoss);
        GameEvents.RaiseXpGained(def.xpReward);
        if (def.treePointsReward > 0) GameEvents.RaiseBossDefeated(def.treePointsReward);

        if (pool != null) pool.Release(this);
        else gameObject.SetActive(false);
    }

    // Justo encima del cuerpo (donde salen los números de daño).
    private Vector3 HeadPosition()
    {
        float top = bodyCollider != null ? bodyCollider.bounds.max.y : transform.position.y + 1f;
        return new Vector3(transform.position.x, top + 0.25f, transform.position.z);
    }

    /// <summary>Le quita una fracción de velocidad durante unos segundos (0,3 = -30%). Mientras una dura, solo la reemplaza otra igual o más fuerte.</summary>
    public void ApplySlow(float fraction, float seconds)
    {
        if (isDead || fraction <= 0f || seconds <= 0f) return;
        bool active = Time.time < slowUntil;
        if (!SlowRules.ShouldReplace(slowMultiplier, active, fraction)) return;

        slowUntil = SlowRules.EndTime(slowMultiplier, active, slowUntil, fraction, Time.time, seconds);
        slowMultiplier = Mathf.Clamp(1f - fraction, 0.1f, 1f);
        ApplySpeed();
    }

    /// <summary>Lo deja quieto y sin atacar unos segundos. Los jefes son inmunes. No acorta uno que ya dure más.</summary>
    public void ApplyStun(float seconds)
    {
        if (isDead || seconds <= 0f || IsBoss) return;

        stun.Apply(Time.time, seconds);
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
    }

    /// <summary>Lo deja ardiendo unos segundos (daño cada tick). Renueva y conserva el mayor daño. No hace nada si ya murió.</summary>
    public void ApplyBurn(float seconds, int damagePerTick, float tickSeconds)
    {
        if (isDead || seconds <= 0f || damagePerTick <= 0) return;

        if (burn == null)
        {
            burn = GetComponent<EnemyBurn>();
            if (burn == null) burn = gameObject.AddComponent<EnemyBurn>();
        }

        burn.Apply(seconds, damagePerTick, tickSeconds);
    }

    /// <summary>Lo envenena unos segundos (daño cada tick). Renueva y conserva el mayor daño. No hace nada si ya murió.</summary>
    public void ApplyPoison(float seconds, int damagePerTick, float tickSeconds)
    {
        if (isDead || seconds <= 0f || damagePerTick <= 0) return;

        if (poison == null)
        {
            poison = GetComponent<EnemyPoison>();
            if (poison == null) poison = gameObject.AddComponent<EnemyPoison>();
        }

        poison.Apply(seconds, damagePerTick, tickSeconds);
    }

    public bool IsPoisoned => poison != null && poison.IsPoisoned;

    /// <summary>Recibe más daño de todo durante unos segundos (0,3 = +30%; la Marca de maná de Frieren). Conserva el bono mayor.</summary>
    public void ApplyDamageTakenBonus(float bonus, float seconds)
    {
        if (isDead || bonus <= 0f || seconds <= 0f) return;
        if (Time.time < damageTakenUntil && damageTakenBonus > bonus) return;

        damageTakenBonus = bonus;
        damageTakenUntil = Mathf.Max(damageTakenUntil, Time.time + seconds);
    }

    private bool IsBoss => def != null && def.IsBoss;

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

        ApplyHeight();

        // Termina la ralentización: vuelve a su velocidad normal.
        if (slowUntil > 0f && Time.time >= slowUntil)
        {
            slowUntil = 0f;
            slowMultiplier = 1f;
            ApplySpeed();
        }

        // Aturdido: quieto y sin atacar hasta que pase el tiempo (el movimiento se reanuda más abajo).
        if (stun.IsStunned(Time.time))
        {
            agent.isStopped = true;
            return;
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
        distanceToGoal = DistanceToEdge(target);

        if (playerInReach) Attack(true);

        if (distanceToGoal <= def.attackReach)
        {
            agent.isStopped = true;
            if (!playerInReach) Attack(target == playerCollider);
            return;
        }

        // Cerca de su objetivo deja de esquivar a los demás y se mete entre ellos: así trepa y se forman montones.
        bool pressing = CanClimb && !def.ranged && Time.time >= climbBlockedUntil && distanceToGoal <= def.attackReach + PressDistance;
        agent.obstacleAvoidanceType = pressing ? ObstacleAvoidanceType.NoObstacleAvoidance : defaultAvoidance;

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

        if (def.ranged)
        {
            Shoot(targetIsPlayer ? playerCollider : baseCollider, targetIsPlayer ? def.damageToPlayer : def.damageToBase);
            return;
        }

        if (targetIsPlayer) playerTarget.TakeDamage(Mathf.RoundToInt(def.damageToPlayer * enrageDamage));
        else baseTarget.TakeDamage(Mathf.RoundToInt(def.damageToBase * enrageDamage));
    }

    // El Lanzador mira a su objetivo y le lanza un proyectil desde la parte de arriba del cuerpo.
    private void Shoot(Collider target, int damage)
    {
        if (target == null) return;

        FaceDirection(target.bounds.center - transform.position);
        Vector3 origin = transform.position + Vector3.up * (bodyHeight * 0.35f) + transform.forward * (bodyRadius + 0.2f);
        EnemyProjectile.Launch(origin, target, def.projectileSpeed, Mathf.RoundToInt(damage * enrageDamage), def.projectileColor, def.projectileSize);
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
