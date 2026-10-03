using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Habilidades del personaje (hasta 3, en las teclas Q, E y F). Lee las AbilityDefinition del personaje,
/// lleva un enfriamiento por casilla y ejecuta cada una según su clase. Solo se lanzan con la partida
/// empezada: antes de la primera oleada la tecla E es "Interactuar".
/// Shooting la crea y la configura, así que no hace falta tocar la escena.
/// </summary>
[RequireComponent(typeof(Shooting))]
public class PlayerAbilities : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color HeavyShotColor = new Color(0.9f, 0.08f, 0.08f, 1f);
    private static readonly Color MistTint = new Color(0.1f, 0.09f, 0.14f);
    private static readonly Color ArmorTint = new Color(0.65f, 0.04f, 0.06f);

    private readonly AbilityDefinition[] slots = new AbilityDefinition[GameInput.AbilitySlots];
    private readonly AbilityCharges[] charges = CreateCharges();
    private readonly float[] nextCastAllowed = new float[GameInput.AbilitySlots];
    private readonly int[] publishedCharges = new int[GameInput.AbilitySlots];
    private readonly HashSet<EnemyAI> mistTouched = new HashSet<EnemyAI>();
    private readonly HashSet<Collider> mistIgnored = new HashSet<Collider>();
    private readonly TimedEffect mist = new TimedEffect();
    private readonly TimedEffect ultimate = new TimedEffect();
    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly List<EnemyAI> nearby = new List<EnemyAI>();
    private readonly List<Renderer> tintTargets = new List<Renderer>();
    private MaterialPropertyBlock block;

    private Shooting shooting;
    private PlayerHealth health;
    private PlayerMovement movement;
    private Collider bodyCollider;
    private ParticleSystem mistVfx;
    private GameObject river;
    private AbilityDefinition ultimateDef;
    private AbilityDefinition mistDef;
    private int ultimateRank = 1;
    private float riverNextTick;
    private bool combatStarted;

    // Pausa mínima entre dos lanzamientos seguidos de una habilidad con varias cargas.
    private const float ConsecutiveCastGap = 0.35f;
    // Separación entre las balas de un disparo pesado con balas extra.
    private const float HeavyBulletSpacing = 0.12f;
    // Niebla: a esta distancia se ignoran las colisiones con los enemigos; a la segunda se les aplica el efecto.
    private const float MistIgnoreRadius = 3f;
    private const float MistTouchRadius = 1.2f;

    private static AbilityCharges[] CreateCharges()
    {
        var result = new AbilityCharges[GameInput.AbilitySlots];
        for (int i = 0; i < result.Length; i++) result[i] = new AbilityCharges();
        return result;
    }

    private static TreeBonuses Bonuses =>
        SkillTreeManager.Instance != null ? SkillTreeManager.Instance.Bonuses : TreeBonuses.None;

    /// <summary>Verdadero mientras es niebla: no puede disparar ni lanzar otras habilidades.</summary>
    public bool IsMist => mist.IsActive(Time.time);

    public bool UltimateActive => ultimate.IsActive(Time.time);

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        bodyCollider = GetComponent<Collider>();
        block = new MaterialPropertyBlock();
    }

    private void OnEnable() => GameEvents.GameStarted += OnGameStarted;

    private void OnDisable()
    {
        GameEvents.GameStarted -= OnGameStarted;
        EndMist();
        EndUltimate();
    }

    private void OnGameStarted() => combatStarted = true;

    /// <summary>Carga las habilidades del personaje, reinicia los enfriamientos y corta los efectos en curso.</summary>
    public void Configure(CharacterDefinition character)
    {
        mist.Cancel();
        ultimate.Cancel();
        EndMist();
        EndUltimate();

        for (int i = 0; i < slots.Length; i++)
        {
            bool hasOne = character != null && character.abilities != null && i < character.abilities.Length;
            slots[i] = hasOne ? character.abilities[i] : null;
        }

        RefreshBuild();
    }

    /// <summary>
    /// Reconfigura las cargas y los enfriamientos de cada casilla con el rango y los bonos del árbol. Se llama al
    /// cambiar de personaje y al comprar o reiniciar algo (siempre antes de la primera oleada), y deja todo listo.
    /// </summary>
    public void RefreshBuild()
    {
        TreeBonuses bonuses = Bonuses;
        float now = Time.time;

        for (int i = 0; i < slots.Length; i++)
        {
            AbilityDefinition ability = slots[i];
            int rank = Mathf.Max(1, RankOf(i));
            int max = ability != null && ability.kind == AbilityKind.HeavyShot ? 1 + bonuses.HeavyShotExtraCharges : 1;
            float cooldown = ability != null ? EffectiveCooldown(ability, rank, bonuses) : 1f;

            charges[i].Configure(max, cooldown, now);
            nextCastAllowed[i] = 0f;
            publishedCharges[i] = max;
            GameEvents.RaiseAbilityChargesChanged(i, max, max);
        }
    }

    // Enfriamiento del rango menos lo que da el árbol; nunca baja de 1 s.
    private static float EffectiveCooldown(AbilityDefinition ability, int rank, TreeBonuses bonuses)
    {
        float reduction = 0f;
        switch (ability.kind)
        {
            case AbilityKind.HeavyShot: reduction = bonuses.HeavyShotCooldownReduction; break;
            case AbilityKind.Mist: reduction = bonuses.MistCooldownReduction; break;
            case AbilityKind.Ultimate: reduction = bonuses.UltCooldownReduction; break;
        }
        return Mathf.Max(1f, ability.CooldownAt(rank) - reduction);
    }

    public bool HasAbility(int slot) => slots[slot] != null;

    /// <summary>Rango que el personaje activo tiene en la habilidad de esa casilla (0 = sin aprender). Sale del guardado.</summary>
    public int RankOf(int slot)
    {
        CharacterDefinition character = shooting.Character;
        if (character == null || slots[slot] == null) return 0;

        return SaveSystem.Data.GetCharacter(character.Id).abilityRanks[slot];
    }

    /// <summary>Lo que el HUD dibuja en cada casilla (vacía si no hay habilidad o todavía no se aprendió).</summary>
    public AbilityHudInfo[] HudInfo()
    {
        var info = new AbilityHudInfo[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || RankOf(i) < 1) continue;
            info[i] = new AbilityHudInfo { Name = slots[i].abilityName, Icon = slots[i].icon };
        }
        return info;
    }

    private void Update()
    {
        float now = Time.time;

        // Los efectos siguen su curso aunque haya un menú abierto; solo el lanzamiento depende del input.
        if (mist.TryFinish(now)) EndMist();
        if (ultimate.TryFinish(now)) EndUltimate();
        if (ultimate.IsActive(now)) TickRiver(now);
        if (mist.IsActive(now)) TickMist();
        PublishChargeChanges(now);

        if (!combatStarted || GameState.InputBlocked || mist.IsActive(now)) return;

        GameInput input = GameInput.Instance;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && input.AbilityPressed(i)) TryCast(i);
        }
    }

    // Con varias cargas, el HUD muestra cuántas quedan y se actualiza solo al recuperar una.
    private void PublishChargeChanges(float now)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (charges[i].Max <= 1) continue;

            int available = charges[i].Available(now);
            if (available == publishedCharges[i]) continue;

            publishedCharges[i] = available;
            GameEvents.RaiseAbilityChargesChanged(i, available, charges[i].Max);
        }
    }

    private void TryCast(int slot)
    {
        AbilityDefinition ability = slots[slot];
        int rank = RankOf(slot);
        float now = Time.time;
        if (rank < 1 || now < nextCastAllowed[slot] || charges[slot].Available(now) <= 0) return;

        bool cast;
        switch (ability.kind)
        {
            case AbilityKind.HeavyShot: cast = CastHeavyShot(ability, rank); break;
            case AbilityKind.Mist: cast = CastMist(ability, rank); break;
            case AbilityKind.Ultimate: cast = CastUltimate(ability, rank); break;
            default: cast = false; break;
        }

        if (!cast) return;

        charges[slot].TryUse(now);

        int left = charges[slot].Available(now);
        bool chained = charges[slot].Max > 1;
        nextCastAllowed[slot] = chained ? now + ConsecutiveCastGap : 0f;
        publishedCharges[slot] = left;

        // Si quedan cargas, el HUD solo muestra la pausa corta entre lanzamientos; si no, lo que falta para recargar una.
        float readyAt = left > 0 ? now + ConsecutiveCastGap : now + charges[slot].RechargeRemaining(now);
        GameEvents.RaiseAbilityUsed(slot, readyAt);
        if (chained) GameEvents.RaiseAbilityChargesChanged(slot, left, charges[slot].Max);
    }

    // --- Disparo pesado ---

    // Una bala enorme contra lo primero que haya en la mira. No gasta munición. Con balas extra del árbol salen
    // varias, una tras otra, cada una con su daño y su sangrado.
    private bool CastHeavyShot(AbilityDefinition ability, int rank)
    {
        FireHeavyBullet(ability, rank);

        int extra = Bonuses.HeavyShotExtraBullets;
        if (extra > 0) StartCoroutine(HeavyBurst(ability, rank, extra));
        return true;
    }

    private IEnumerator HeavyBurst(AbilityDefinition ability, int rank, int extra)
    {
        for (int i = 0; i < extra; i++)
        {
            yield return new WaitForSeconds(HeavyBulletSpacing);
            if (GameState.IsGameOver) yield break;
            FireHeavyBullet(ability, rank);
        }
    }

    private void FireHeavyBullet(AbilityDefinition ability, int rank)
    {
        int bullet = shooting.CurrentWeapon.Damage;

        bool found = shooting.TryGetHit(ability.range, out RaycastHit hit);
        Vector3 end = found ? hit.point : shooting.AimRay().GetPoint(ability.range);
        shooting.ShowBolt(end, HeavyShotColor, 0.2f, 0.18f);

        if (!found) return;

        EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
        if (enemy == null) return;

        int damage = ability.DamageFor(bullet, rank);
        enemy.TakeDamage(damage);
        enemy.ApplyBleed(ability.bleedStacks, shooting.BleedCap, BleedStacks.DamagePerStack(bullet));
        HealFromDamage(damage);
    }

    // --- Niebla ---

    // Invulnerable y más rápido unos segundos; no puede disparar ni usar otras habilidades. Sangra a los cercanos.
    private bool CastMist(AbilityDefinition ability, int rank)
    {
        mistDef = ability;
        mist.Start(Time.time, ability.DurationAt(rank) + Bonuses.MistDurationBonus);
        mistTouched.Clear();

        health.Invulnerable = true;
        movement.SpeedMultiplier = ability.SpeedMultiplierAt(rank);

        if (mistVfx == null) mistVfx = AbilityVfx.CreateMist(transform);
        mistVfx.Play();
        RefreshTint();

        int perStack = BleedStacks.DamagePerStack(shooting.CurrentWeapon.Damage);
        CollectEnemies(transform.position, ability.radius, null);
        foreach (EnemyAI enemy in nearby) enemy.ApplyBleed(ability.bleedStacks, shooting.BleedCap, perStack);

        return true;
    }

    // Mientras es niebla atraviesa a los enemigos cercanos y, la primera vez que toca a cada uno, le deja el sangrado
    // y la ralentización que dé el árbol.
    private void TickMist()
    {
        if (mistDef == null) return;

        TreeBonuses bonuses = Bonuses;
        int perStack = BleedStacks.DamagePerStack(shooting.CurrentWeapon.Damage);

        CollectEnemies(transform.position, MistIgnoreRadius, null);
        foreach (EnemyAI enemy in nearby)
        {
            Collider body = enemy.GetComponent<Collider>();
            if (body != null && bodyCollider != null && mistIgnored.Add(body))
                Physics.IgnoreCollision(bodyCollider, body, true);

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude > MistTouchRadius || !mistTouched.Add(enemy)) continue;

            if (bonuses.MistBleedOnPass > 0) enemy.ApplyBleed(bonuses.MistBleedOnPass, shooting.BleedCap, perStack);
            if (bonuses.MistSlow > 0f) enemy.ApplySlow(bonuses.MistSlow, mistDef.mistSlowSeconds);
        }
    }

    // Devuelve las colisiones con los enemigos. Los que ya murieron y están desactivados se arreglan solos al
    // reaparecer (EnemyAI.Spawn).
    private void RestoreMistCollisions()
    {
        foreach (Collider body in mistIgnored)
        {
            if (body != null && bodyCollider != null && body.gameObject.activeInHierarchy)
                Physics.IgnoreCollision(bodyCollider, body, false);
        }
        mistIgnored.Clear();
        mistTouched.Clear();
    }

    private void EndMist()
    {
        RestoreMistCollisions();
        if (health != null) health.Invulnerable = false;
        if (movement != null) movement.SpeedMultiplier = 1f;
        if (mistVfx != null) mistVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        mistDef = null;
        RefreshTint();
    }

    // --- Definitiva ---

    // Armadura roja, río de sangre, robo de vida y disparos con explosión mientras dura.
    private bool CastUltimate(AbilityDefinition ability, int rank)
    {
        ultimateDef = ability;
        ultimateRank = rank;
        ultimate.Start(Time.time, ability.DurationAt(rank) + Bonuses.UltDurationBonus);
        riverNextTick = Time.time + ability.riverTickSeconds;

        if (river != null) Destroy(river);
        river = AbilityVfx.CreateRiver(ability.radius);
        river.SetActive(true);
        PlaceRiver();
        RefreshTint();
        return true;
    }

    private void EndUltimate()
    {
        if (river != null) Destroy(river);
        river = null;
        ultimateDef = null;
        RefreshTint();
    }

    // El río sigue al jugador pegado al suelo y suma una pila a los enemigos que estén dentro cada pocos segundos.
    private void TickRiver(float now)
    {
        PlaceRiver();
        if (now < riverNextTick || ultimateDef == null) return;

        riverNextTick = now + ultimateDef.riverTickSeconds;
        int perStack = BleedStacks.DamagePerStack(shooting.CurrentWeapon.Damage);

        CollectEnemies(transform.position, ultimateDef.radius, null);
        foreach (EnemyAI enemy in nearby) enemy.ApplyBleed(ultimateDef.bleedStacks, shooting.BleedCap, perStack);
    }

    private void PlaceRiver()
    {
        if (river == null) return;

        Vector3 origin = transform.position;
        float groundY = Physics.Raycast(origin, Vector3.down, out RaycastHit ground, 3f, ~0, QueryTriggerInteraction.Ignore)
            ? ground.point.y
            : origin.y - 1f;

        river.transform.position = new Vector3(origin.x, groundY + 0.03f, origin.z);
    }

    // --- Disparos básicos ---

    /// <summary>
    /// Lo llama Shooting tras dañar a un enemigo con un disparo básico: suma el sangrado del arma y,
    /// con la definitiva activa, lo duplica, hace explotar la bala en área y cura al jugador.
    /// </summary>
    public void AfterBulletHit(EnemyAI enemy, Vector3 point, WeaponState weapon)
    {
        int bullet = weapon.Damage;
        int perStack = BleedStacks.DamagePerStack(bullet);
        int cap = shooting.BleedCap;
        int stacks = weapon.BleedPerHit;

        AbilityDefinition ult = UltimateActive ? ultimateDef : null;
        if (ult == null)
        {
            enemy.ApplyBleed(stacks, cap, perStack);
            return;
        }

        enemy.ApplyBleed(ult.BoostedBleed(stacks), cap, perStack);

        int direct = bullet;
        int splash = ult.ExplosionDamageFor(bullet);

        CollectEnemies(point, ult.explosionRadius, enemy);
        foreach (EnemyAI neighbour in nearby)
        {
            neighbour.TakeDamage(splash);
            neighbour.ApplyBleed(ult.explosionBleedStacks, cap, perStack);
            direct += splash;
        }

        AbilityVfx.ExplosionFlash(point, ult.explosionRadius);
        HealFromDamage(direct);
    }

    // Robo de vida de la definitiva: solo del daño directo (balas y explosiones), no del sangrado.
    private void HealFromDamage(int damage)
    {
        if (!UltimateActive || ultimateDef == null) return;

        float fraction = Mathf.Clamp01(ultimateDef.LifeStealAt(ultimateRank) + Bonuses.UltLifeStealBonus);
        health.Heal(Mathf.Max(0, Mathf.RoundToInt(damage * fraction)));
    }

    // --- Ayudas ---

    // Llena 'nearby' con los enemigos vivos dentro del radio, sin repetir y sin el excluido.
    private void CollectEnemies(Vector3 center, float radius, EnemyAI exclude)
    {
        nearby.Clear();

        int count = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = overlapBuffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy == exclude || enemy.IsDead || nearby.Contains(enemy)) continue;

            nearby.Add(enemy);
        }
    }

    // Color del cuerpo: oscuro en niebla, rojo con la definitiva, normal si no hay ninguno.
    private void RefreshTint()
    {
        bool inMist = mist.IsActive(Time.time);
        bool inUltimate = ultimate.IsActive(Time.time);

        tintTargets.Clear();
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer) continue;
            tintTargets.Add(rend);
        }

        foreach (Renderer rend in tintTargets)
        {
            if (!inMist && !inUltimate)
            {
                rend.SetPropertyBlock(null);
                continue;
            }

            Material material = rend.sharedMaterial;
            Color original = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            Color tint = inMist ? MistTint : ArmorTint;

            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(original, tint, 0.85f));
            rend.SetPropertyBlock(block);
        }
    }
}
