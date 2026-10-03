using UnityEngine;

public enum AbilityKind
{
    [Tooltip("Una bala enorme contra un solo enemigo")]
    HeavyShot,
    [Tooltip("Se vuelve niebla un rato: invulnerable y más rápido")]
    Mist,
    [Tooltip("Transformación de unos segundos con robo de vida y disparos en área")]
    Ultimate
}

/// <summary>
/// Datos de una habilidad de personaje. Se crea desde Assets > Create > Game > Ability.
/// Un solo tipo de asset: cada clase (AbilityKind) usa solo los campos de su sección.
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "Game/Ability")]
public class AbilityDefinition : GameDefinition
{
    public string abilityName = "Habilidad";
    [Tooltip("Icono de la casilla del HUD")]
    public Sprite icon;
    public AbilityKind kind;

    [Min(0.1f)]
    public float cooldown = 8f;

    [Header("Disparo pesado")]
    [Tooltip("Daño = daño de una bala × este número")]
    public float damageMultiplier = 6f;
    public float range = 60f;

    [Header("Sangrado que aplica")]
    [Tooltip("Pilas de sangrado que suma la habilidad a cada enemigo que alcanza")]
    public int bleedStacks = 3;

    [Header("Niebla y definitiva")]
    [Tooltip("Segundos que dura")]
    public float duration = 2f;
    [Tooltip("Multiplicador de velocidad mientras dura (1,6 = +60%)")]
    public float speedMultiplier = 1.6f;
    [Tooltip("Radio de efecto, en metros")]
    public float radius = 3f;

    [Header("Definitiva")]
    [Range(0f, 1f), Tooltip("Fracción del daño directo que se convierte en vida")]
    public float lifeSteal = 0.5f;
    [Tooltip("Mientras dura, las pilas de sangrado de cada disparo básico se multiplican por esto")]
    public float bleedMultiplier = 2f;
    [Tooltip("Radio de la explosión de cada disparo básico, en metros")]
    public float explosionRadius = 2.5f;
    [Range(0f, 1f), Tooltip("Fracción del daño de la bala que reciben los enemigos vecinos")]
    public float explosionFraction = 0.6f;
    [Tooltip("Pilas de sangrado que reciben los enemigos vecinos por cada explosión")]
    public int explosionBleedStacks = 2;
    [Min(0.1f), Tooltip("Cada cuántos segundos el río de sangre suma pilas a los enemigos dentro del radio")]
    public float riverTickSeconds = 2f;

    [Header("Mejoras por rango (cada rango sobre el 1.º)")]
    [Tooltip("Segundos que baja el enfriamiento por cada rango extra")]
    public float cooldownPerRank = 0f;
    [Tooltip("Disparo pesado: cuánto sube el multiplicador de daño por rango")]
    public float damagePerRank = 0f;
    [Tooltip("Niebla y definitiva: segundos extra de duración por rango")]
    public float durationPerRank = 0f;
    [Tooltip("Niebla: cuánto sube el multiplicador de velocidad por rango (0,1 = +10%)")]
    public float speedPerRank = 0f;
    [Tooltip("Definitiva: cuánto sube el robo de vida por rango (0,1 = +10 puntos)")]
    public float lifeStealPerRank = 0f;

    private static int Steps(int rank) => Mathf.Max(0, rank - 1);

    public float CooldownAt(int rank) => Mathf.Max(0.1f, cooldown - Steps(rank) * cooldownPerRank);
    public float DamageMultiplierAt(int rank) => damageMultiplier + Steps(rank) * damagePerRank;
    public float DurationAt(int rank) => duration + Steps(rank) * durationPerRank;
    public float SpeedMultiplierAt(int rank) => speedMultiplier + Steps(rank) * speedPerRank;
    public float LifeStealAt(int rank) => Mathf.Clamp01(lifeSteal + Steps(rank) * lifeStealPerRank);

    /// <summary>Texto corto de lo que hace la habilidad en un rango (para la estación de mejoras).</summary>
    public string DescribeRank(int rank)
    {
        if (rank < 1) return "Sin aprender";

        string cd = CooldownAt(rank).ToString("0.#") + " s de enfriamiento";
        switch (kind)
        {
            case AbilityKind.HeavyShot:
                return "Daño x" + DamageMultiplierAt(rank).ToString("0.#") + " · " + cd;
            case AbilityKind.Mist:
                return DurationAt(rank).ToString("0.#") + " s · velocidad +" + Mathf.RoundToInt((SpeedMultiplierAt(rank) - 1f) * 100f) + "% · " + cd;
            default:
                return DurationAt(rank).ToString("0.#") + " s · robo de vida " + Mathf.RoundToInt(LifeStealAt(rank) * 100f) + "% · " + cd;
        }
    }

    /// <summary>Vida que se recupera por un daño directo (50% con la definitiva en rango 1).</summary>
    public int LifeStealFor(int damage, int rank = 1) => Mathf.Max(0, Mathf.RoundToInt(damage * LifeStealAt(rank)));

    /// <summary>Daño que reciben los vecinos de la explosión (mínimo 1).</summary>
    public int ExplosionDamageFor(int bulletDamage) => Mathf.Max(1, Mathf.RoundToInt(bulletDamage * explosionFraction));

    /// <summary>Pilas de sangrado de un disparo básico mientras dura la definitiva.</summary>
    public int BoostedBleed(int stacksPerHit) => Mathf.Max(0, Mathf.RoundToInt(stacksPerHit * bleedMultiplier));

    /// <summary>Daño de la habilidad a partir del daño de una bala del arma equipada (mínimo 1).</summary>
    public int DamageFor(int bulletDamage, int rank = 1) =>
        Mathf.Max(1, Mathf.RoundToInt(bulletDamage * DamageMultiplierAt(rank)));
}
