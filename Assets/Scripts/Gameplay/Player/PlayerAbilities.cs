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
    private readonly AbilityCooldown[] cooldowns = CreateCooldowns();
    private readonly TimedEffect mist = new TimedEffect();
    private readonly TimedEffect ultimate = new TimedEffect();
    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly List<EnemyAI> nearby = new List<EnemyAI>();
    private readonly List<Renderer> tintTargets = new List<Renderer>();
    private MaterialPropertyBlock block;

    private Shooting shooting;
    private PlayerHealth health;
    private PlayerMovement movement;
    private ParticleSystem mistVfx;
    private GameObject river;
    private AbilityDefinition ultimateDef;
    private AbilityDefinition mistDef;
    private float riverNextTick;
    private bool combatStarted;

    private static AbilityCooldown[] CreateCooldowns()
    {
        var result = new AbilityCooldown[GameInput.AbilitySlots];
        for (int i = 0; i < result.Length; i++) result[i] = new AbilityCooldown();
        return result;
    }

    /// <summary>Verdadero mientras es niebla: no puede disparar ni lanzar otras habilidades.</summary>
    public bool IsMist => mist.IsActive(Time.time);

    public bool UltimateActive => ultimate.IsActive(Time.time);

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
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
            cooldowns[i].Reset();
        }
    }

    public bool HasAbility(int slot) => slots[slot] != null;

    /// <summary>Lo que el HUD dibuja en cada casilla (vacía si el personaje no tiene esa habilidad).</summary>
    public AbilityHudInfo[] HudInfo()
    {
        var info = new AbilityHudInfo[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
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

        if (!combatStarted || GameState.InputBlocked || mist.IsActive(now)) return;

        GameInput input = GameInput.Instance;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && input.AbilityPressed(i)) TryCast(i);
        }
    }

    private void TryCast(int slot)
    {
        AbilityDefinition ability = slots[slot];
        if (!cooldowns[slot].IsReady(Time.time)) return;

        bool cast;
        switch (ability.kind)
        {
            case AbilityKind.HeavyShot: cast = CastHeavyShot(ability); break;
            case AbilityKind.Mist: cast = CastMist(ability); break;
            case AbilityKind.Ultimate: cast = CastUltimate(ability); break;
            default: cast = false; break;
        }

        if (!cast) return;

        cooldowns[slot].Start(Time.time, ability.cooldown);
        GameEvents.RaiseAbilityUsed(slot, Time.time + ability.cooldown);
    }

    // --- Disparo pesado ---

    // Una bala enorme contra lo primero que haya en la mira. No gasta munición.
    private bool CastHeavyShot(AbilityDefinition ability)
    {
        int bullet = shooting.CurrentWeapon.Damage;

        bool found = shooting.TryGetHit(ability.range, out RaycastHit hit);
        Vector3 end = found ? hit.point : shooting.AimRay().GetPoint(ability.range);
        shooting.ShowBolt(end, HeavyShotColor, 0.2f, 0.18f);

        if (!found) return true;

        EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
        if (enemy == null) return true;

        int damage = ability.DamageFor(bullet);
        enemy.TakeDamage(damage);
        enemy.ApplyBleed(ability.bleedStacks, shooting.BleedCap, BleedStacks.DamagePerStack(bullet));
        HealFromDamage(damage);
        return true;
    }

    // --- Niebla ---

    // Invulnerable y más rápido unos segundos; no puede disparar ni usar otras habilidades. Sangra a los cercanos.
    private bool CastMist(AbilityDefinition ability)
    {
        mistDef = ability;
        mist.Start(Time.time, ability.duration);

        health.Invulnerable = true;
        movement.SpeedMultiplier = ability.speedMultiplier;

        if (mistVfx == null) mistVfx = AbilityVfx.CreateMist(transform);
        mistVfx.Play();
        RefreshTint();

        int perStack = BleedStacks.DamagePerStack(shooting.CurrentWeapon.Damage);
        CollectEnemies(transform.position, ability.radius, null);
        foreach (EnemyAI enemy in nearby) enemy.ApplyBleed(ability.bleedStacks, shooting.BleedCap, perStack);

        return true;
    }

    private void EndMist()
    {
        if (health != null) health.Invulnerable = false;
        if (movement != null) movement.SpeedMultiplier = 1f;
        if (mistVfx != null) mistVfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        mistDef = null;
        RefreshTint();
    }

    // --- Definitiva ---

    // Armadura roja, río de sangre, robo de vida y disparos con explosión mientras dura.
    private bool CastUltimate(AbilityDefinition ability)
    {
        ultimateDef = ability;
        ultimate.Start(Time.time, ability.duration);
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

        health.Heal(ultimateDef.LifeStealFor(damage));
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
