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
    private readonly Collider[] buffer = new Collider[192];
    private readonly List<EnemyAI> found = new List<EnemyAI>();
    private AbilityDefinition ability;
    private float nextTick;

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
    public bool Activate(AbilityDefinition definition, int rank, Shooting owner)
    {
        if (owner.Mana == null || !owner.Mana.TrySpend(definition.manaCost)) return false;

        ability = definition;
        owner.RefreshMana();

        effect.Start(Time.time, definition.DurationAt(rank));
        nextTick = Time.time;

        // Terror: aturde (una sola vez) a todos los enemigos del radio. Los jefes son inmunes (EnemyAI.ApplyStun).
        foreach (EnemyAI enemy in EnemiesInRadius())
            enemy.ApplyStun(definition.pulseStunSeconds);

        AbilityVfx.ExplosionFlash(transform.position, definition.radius, PulseColor);
        if (owner.Body != null) owner.Body.PlayPowerUp();
        return true;
    }

    public void ForceOff()
    {
        effect.Cancel();
        ability = null;
    }

    private void Update()
    {
        if (ability == null) return;

        if (effect.TryFinish(Time.time))
        {
            ForceOff();
            return;
        }

        if (Time.time < nextTick) return;
        nextTick = Time.time + TickSeconds;

        // Mientras dura, los enemigos del radio van más lentos (la ralentización caduca sola poco después de salir).
        foreach (EnemyAI enemy in EnemiesInRadius())
            enemy.ApplySlow(ability.pulseSlowFraction, TickSeconds * 2f);
    }

    private List<EnemyAI> EnemiesInRadius()
    {
        found.Clear();
        int count = Physics.OverlapSphereNonAlloc(transform.position, ability.radius, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy != null && !enemy.IsDead && !found.Contains(enemy)) found.Add(enemy);
        }
        return found;
    }
}
