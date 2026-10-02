using UnityEngine;

/// <summary>
/// Habilidades del personaje (hasta 3, en las teclas Q, E y F). Lee las AbilityDefinition del personaje,
/// lleva un enfriamiento por casilla y ejecuta cada una según su clase. Solo actúan con la partida
/// empezada: antes de la primera oleada la tecla E es "Interactuar".
/// Shooting la crea y la configura, así que no hace falta tocar la escena.
/// </summary>
[RequireComponent(typeof(Shooting))]
public class PlayerAbilities : MonoBehaviour
{
    private static readonly Color HeavyShotColor = new Color(0.9f, 0.08f, 0.08f, 1f);

    private readonly AbilityDefinition[] slots = new AbilityDefinition[GameInput.AbilitySlots];
    private readonly AbilityCooldown[] cooldowns = CreateCooldowns();

    private Shooting shooting;
    private bool combatStarted;

    private static AbilityCooldown[] CreateCooldowns()
    {
        var result = new AbilityCooldown[GameInput.AbilitySlots];
        for (int i = 0; i < result.Length; i++) result[i] = new AbilityCooldown();
        return result;
    }

    private void Awake() => shooting = GetComponent<Shooting>();

    private void OnEnable() => GameEvents.GameStarted += OnGameStarted;
    private void OnDisable() => GameEvents.GameStarted -= OnGameStarted;

    private void OnGameStarted() => combatStarted = true;

    /// <summary>Carga las habilidades del personaje y reinicia los enfriamientos.</summary>
    public void Configure(CharacterDefinition character)
    {
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
        if (!combatStarted || GameState.InputBlocked) return;

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
            default: cast = false; break; // niebla y definitiva: pasos siguientes
        }

        if (!cast) return;

        cooldowns[slot].Start(Time.time, ability.cooldown);
        GameEvents.RaiseAbilityUsed(slot, Time.time + ability.cooldown);
    }

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

        enemy.TakeDamage(ability.DamageFor(bullet));
        enemy.ApplyBleed(ability.bleedStacks, shooting.BleedCap, BleedStacks.DamagePerStack(bullet));
        return true;
    }
}
