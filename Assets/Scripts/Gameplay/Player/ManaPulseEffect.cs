using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pulso de maná de Frieren (definitiva, tecla F): un pulso dorado aturde a los enemigos de un radio grande y, mientras dura,
/// los ralentiza, vuelve instantáneo (sin carga ni maná) el Zoltraak y acelera las otras dos habilidades. No es un interruptor:
/// dura su tiempo. PlayerAbilities la crea, la activa y le pide la aceleración de las otras casillas.
/// </summary>
public class ManaPulseEffect : MonoBehaviour
{
    private const float TickSeconds = 0.25f;
    private static readonly Color PulseColor = new Color(1f, 0.82f, 0.35f, 0.4f);

    private readonly TimedEffect effect = new TimedEffect();
    private readonly Collider[] buffer = new Collider[512];   // con Dominio entra el mapa entero
    private readonly List<EnemyAI> found = new List<EnemyAI>();
    private AbilityDefinition ability;
    private Shooting owner;
    private float nextTick;
    private float endsAt;
    private float nextRain;

    public bool IsActive => effect.IsActive(Time.time);

    /// <summary>Multiplicador de velocidad de recarga de las otras habilidades (2 = el doble). 1 si el pulso no está activo.</summary>
    public float CooldownBoost => IsActive && ability != null ? ability.pulseCooldownBoost : 1f;

    private void OnEnable()
    {
        GameEvents.GameOver += OnGameOver;
        GameEvents.CharacterChanged += OnCharacterChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= OnGameOver;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        ForceOff();
    }

    private void OnGameOver(string message, GameOverCause cause) => ForceOff();
    private void OnCharacterChanged(CharacterDefinition character) => ForceOff();

    /// <summary>Activa el pulso. False (sin gastar nada) si no alcanza el maná.</summary>
    public bool Activate(AbilityDefinition definition, int rank, Shooting shooter)
    {
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        if (shooter.Mana == null || !shooter.Mana.TrySpend(FrierenTreeMath.ManaCost(definition.manaCost, tree.ManaCostReduction))) return false;

        ability = definition;
        owner = shooter;
        shooter.RefreshMana();

        float duration = definition.DurationAt(rank) + tree.PulseDurationBonus;
        effect.Start(Time.time, duration);
        endsAt = Time.time + duration;
        nextTick = Time.time;
        nextRain = Time.time + tree.PulseRainInterval;

        // Terror: aturde (una sola vez) a todos los enemigos del radio. Los jefes son inmunes (EnemyAI.ApplyStun).
        foreach (EnemyAI enemy in EnemiesInRadius())
        {
            enemy.ApplyStun(definition.pulseStunSeconds + tree.PulseStunBonus);
            if (tree.PulseMarkBonus > 0f) enemy.ApplyDamageTakenBonus(tree.PulseMarkBonus, duration);
        }

        AbilityVfx.ExplosionFlash(transform.position, definition.radius, PulseColor);
        if (shooter.Body != null) shooter.Body.PlayPowerUp();
        return true;
    }

    public void ForceOff()
    {
        effect.Cancel();
        ability = null;
        owner = null;
    }

    private void Update()
    {
        if (ability == null) return;

        if (effect.TryFinish(Time.time))
        {
            FinalBlast();
            ForceOff();
            return;
        }

        TickRain();

        if (Time.time < nextTick) return;
        nextTick = Time.time + TickSeconds;

        // Mientras dura, los enemigos del radio van más lentos (la ralentización caduca sola poco después de salir); con la
        // Marca, los que toca reciben más daño hasta que el pulso termine.
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        float left = Mathf.Max(0f, endsAt - Time.time);
        foreach (EnemyAI enemy in EnemiesInRadius())
        {
            enemy.ApplySlow(ability.pulseSlowFraction, TickSeconds * 2f);
            if (tree.PulseMarkBonus > 0f) enemy.ApplyDamageTakenBonus(tree.PulseMarkBonus, left);
        }
    }

    // Lluvia de Zoltraak: cada intervalo cae uno sobre un enemigo al azar del radio (si no hay ninguno, ese no cae).
    private void TickRain()
    {
        float interval = SkillTreeManager.CurrentBonuses.PulseRainInterval;
        if (interval <= 0f || Time.time < nextRain || owner == null || owner.WeaponCount == 0 || owner.CurrentWeapon.Staff == null) return;
        nextRain = Time.time + interval;

        List<EnemyAI> enemies = EnemiesInRadius();
        if (enemies.Count == 0) return;

        ZoltraakCaster caster = owner.GetComponent<ZoltraakCaster>();
        if (caster == null) caster = owner.gameObject.AddComponent<ZoltraakCaster>();
        caster.DropFromSky(owner.CurrentWeapon, enemies[Random.Range(0, enemies.Count)].transform.position);
    }

    // Explosión final: al terminar (no al cortarse por fin de partida), 3 Zoltraak completos a todos los del radio.
    private void FinalBlast()
    {
        float multiplier = SkillTreeManager.CurrentBonuses.PulseFinalBlastMultiplier;
        if (multiplier <= 0f || owner == null || owner.WeaponCount == 0 || owner.CurrentWeapon.Staff == null) return;

        int damage = Mathf.RoundToInt(owner.CurrentWeapon.ZoltraakDamage(1f) * multiplier);
        foreach (EnemyAI enemy in EnemiesInRadius()) enemy.TakeDamage(damage);
        AbilityVfx.ExplosionFlash(transform.position, ability.radius, PulseColor);
    }

    private List<EnemyAI> EnemiesInRadius()
    {
        found.Clear();
        float radius = SkillTreeManager.CurrentBonuses.PulseWholeMap ? FrierenTreeMath.WholeMapRadius : ability.radius;
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy != null && !enemy.IsDead && !found.Contains(enemy)) found.Add(enemy);
        }
        return found;
    }
}
