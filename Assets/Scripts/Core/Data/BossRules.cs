using System;
using UnityEngine;

public enum EnemyTier
{
    Normal,
    MiniBoss,
    Boss
}

public enum BossAbilityKind
{
    [Tooltip("Se detiene, avisa y se lanza en línea recta")] Charge,
    [Tooltip("Llama a otros enemigos")] Summon,
    [Tooltip("Gana velocidad y daño al bajar de cierta vida (una sola vez)")] Enrage,
    [Tooltip("Al morir se levanta una vez con parte de su vida")] Revive,
    [Tooltip("Dispara proyectiles al jugador")] Shoot
}

/// <summary>
/// Una habilidad de jefe con sus números. Cada habilidad solo se usa mientras la vida del jefe esté en su ventana
/// (<c>minHealthFraction &lt; vida &lt;= maxHealthFraction</c>): dos habilidades seguidas (0,5 a 1 y 0 a 0,5) hacen las dos fases.
/// </summary>
[Serializable]
public class BossAbility
{
    public BossAbilityKind kind;

    [Header("Cuándo")]
    [Range(0f, 1f), Tooltip("Solo se usa si la vida (de 0 a 1) está POR ENCIMA de este valor")]
    public float minHealthFraction = 0f;
    [Range(0f, 1f), Tooltip("Solo se usa si la vida está en este valor o por debajo. 1 = desde el principio; 0,5 = a mitad de vida")]
    public float maxHealthFraction = 1f;
    [Min(0.1f), Tooltip("Segundos entre usos (embestida, invocar y disparar)")]
    public float interval = 8f;

    [Header("Embestida")]
    public float telegraphSeconds = 1f;
    public float chargeSeconds = 1.2f;
    public float chargeSpeed = 14f;
    public int chargeDamage = 25;
    [Tooltip("Si el jugador está más cerca que esto, se lanza hacia él; si no, hacia la base")]
    public float chargeRange = 30f;

    [Header("Invocar")]
    public EnemyDefinition summon;
    [Min(1)] public int summonCount = 3;

    [Header("Enfurecer")]
    [Tooltip("0,4 = +40% de velocidad")] public float speedBonus = 0.4f;
    [Tooltip("0,5 = +50% de daño")] public float damageBonus = 0.5f;

    [Header("Resucitar")]
    [Range(0f, 1f), Tooltip("Fracción de la vida máxima con la que se levanta")]
    public float reviveHealth = 0.5f;
    [Tooltip("Segundos sin recibir daño tras levantarse")]
    public float reviveInvulnerableSeconds = 1f;

    [Header("Disparar")]
    public int projectileDamage = 15;
    public float projectileSpeed = 12f;
    [Min(1), Tooltip("Proyectiles por disparo (ráfaga)")] public int burst = 1;
    public float burstSpacing = 0.18f;
}

/// <summary>Reglas puras de los jefes (sin escena, para poder probarlas).</summary>
public static class BossRules
{
    /// <summary>La habilidad está en su ventana de vida: <c>min &lt; vida &lt;= max</c>. Con vida 0 (muerto) nada está activo.</summary>
    public static bool IsActive(BossAbility ability, float healthFraction) =>
        healthFraction > ability.minHealthFraction && healthFraction <= ability.maxHealthFraction;

    /// <summary>Para lo que se dispara una sola vez (enfurecer): la vida ya bajó hasta el umbral.</summary>
    public static bool Triggered(BossAbility ability, float healthFraction) =>
        healthFraction <= ability.maxHealthFraction;

    /// <summary>Vida con la que se levanta un jefe que resucita (redondeada hacia arriba, mínimo 1).</summary>
    public static int ReviveHealth(int maxHealth, BossAbility ability) =>
        Mathf.Max(1, Mathf.CeilToInt(maxHealth * ability.reviveHealth));

    /// <summary>La embestida apunta al jugador si está dentro del alcance; si no, a la base.</summary>
    public static bool ChargeTargetsPlayer(float distanceToPlayer, float range) => distanceToPlayer <= range;
}

/// <summary>Cuándo le toca a cada habilidad. El tiempo actual se pasa desde fuera para poder probarlo.</summary>
public class AbilityTimers
{
    private readonly float[] readyAt;

    public AbilityTimers(int count)
    {
        readyAt = new float[Mathf.Max(0, count)];
    }

    public void Schedule(int index, float now, float delay)
    {
        if (index >= 0 && index < readyAt.Length) readyAt[index] = now + delay;
    }

    public bool Due(int index, float now) => index >= 0 && index < readyAt.Length && now >= readyAt[index];
}

/// <summary>Lo que ya pasó en la pelea con un jefe: habilidades de un solo disparo ya usadas y si ya resucitó.</summary>
public class BossPhaseState
{
    private readonly System.Collections.Generic.HashSet<int> fired = new System.Collections.Generic.HashSet<int>();

    public bool ReviveUsed { get; set; }

    /// <summary>True la primera vez que se pide para esa habilidad; después false.</summary>
    public bool TryFireOnce(int abilityIndex) => fired.Add(abilityIndex);

    public void Reset()
    {
        fired.Clear();
        ReviveUsed = false;
    }
}
