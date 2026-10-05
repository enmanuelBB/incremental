using UnityEngine;

public enum AbilityKind
{
    [Tooltip("Una bala enorme contra un solo enemigo")]
    HeavyShot,
    [Tooltip("Se vuelve niebla un rato: invulnerable y más rápido")]
    Mist,
    [Tooltip("Transformación de unos segundos con robo de vida y disparos en área")]
    Ultimate,
    [Tooltip("Cono de fuego frente al jugador que daña y deja quemados a los enemigos (Guts)")]
    FlameBurst,
    [Tooltip("Dash con giro que termina disparando al enemigo más cercano (Guts)")]
    Dash,
    [Tooltip("Armadura Berserker: interruptor con bonos fuertes que drena vida (Guts)")]
    Berserk,
    [Tooltip("Rayo de maná masivo: línea que atraviesa a todos los enemigos (Frieren). Daño, maná y enfriamiento salen del bastón")]
    ManaBeam,
    [Tooltip("Campo de flores: zona colocada con la mira que cura a Frieren y ralentiza a los enemigos (Frieren)")]
    FlowerField,
    [Tooltip("Pulso de maná: definitiva que aturde, ralentiza, vuelve instantáneo el Zoltraak y acelera las otras habilidades (Frieren)")]
    ManaPulse
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
    [Tooltip("Niebla: segundos que dura la ralentización que deja a los enemigos que atraviesa (si el árbol la da)")]
    public float mistSlowSeconds = 2f;

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

    [Header("Llamarada (Guts)")]
    [Tooltip("Apertura del cono de fuego frente al jugador, en grados. El alcance es 'range'; los segundos de quemadura son 'duration'")]
    public float coneDegrees = 90f;
    [Tooltip("Quemadura: fracción del daño de la espada que hace por segundo")]
    public float burnDamageFractionPerSecond = 0.6f;
    [Min(0.1f), Tooltip("Quemadura: cada cuántos segundos hace daño")]
    public float burnTickSeconds = 0.5f;

    [Header("Embestida (Guts)")]
    [Tooltip("Metros que recorre el dash")]
    public float dashDistance = 6f;
    [Min(0.05f), Tooltip("Segundos que dura el dash")]
    public float dashSeconds = 0.35f;
    [Min(0f), Tooltip("Segundos que tarda en levantarse antes de disparar (sigue invulnerable)")]
    public float riseSeconds = 0.25f;
    [Tooltip("Alcance del disparo al enemigo más cercano, en metros")]
    public float shotRange = 25f;

    [Header("Armadura Berserker (Guts)")]
    [Range(0f, 1f), Tooltip("Fracción de la vida máxima que pierde por segundo mientras la lleva puesta (el daño usa 'damageMultiplier')")]
    public float drainFractionPerSecond = 0.02f;
    [Tooltip("Cuánto baja el drenaje por cada rango extra (0,005 = 0,5 puntos)")]
    public float drainPerRank = 0f;
    [Tooltip("Radio del rugido al activarla, en metros")]
    public float roarRadius = 4f;
    [Tooltip("Segundos que aturde el rugido (los jefes son inmunes)")]
    public float roarStunSeconds = 1.5f;
    [Range(0f, 1f), Tooltip("Multiplicador del daño que recibe mientras la lleva (0,5 = la mitad)")]
    public float damageTakenMultiplier = 0.5f;
    [Range(0.1f, 1f), Tooltip("Multiplicador del tiempo entre golpes de espada (0,5 = el doble de rápido)")]
    public float cadenceMultiplier = 0.5f;
    [Min(1f), Tooltip("Multiplicador de la Furia que carga con cada golpe")]
    public float furyGainMultiplier = 2f;
    [Range(0f, 360f), Tooltip("Apertura del golpe de espada mientras la lleva (360 = alrededor)")]
    public float swingArcDegrees = 360f;

    [Header("Maná")]
    [Min(0f), Tooltip("Maná que gasta al lanzarla (Campo de flores y Pulso de maná; el rayo usa el del bastón)")]
    public float manaCost = 0f;

    [Header("Lanzamiento")]
    [Min(0f), Tooltip("Segundos que tarda en lanzarse: el gesto sube hasta su máximo en ese tiempo y el efecto sale al llegar (0 = al instante). Rayo y Pulso de maná de Frieren")]
    public float castSeconds = 0f;

    [Header("Campo de flores (Frieren)")]
    [Tooltip("Cura a Frieren, mientras esté dentro, esta fracción de su vida máxima por segundo (0,03 = 3%). 'radius' es el radio y 'duration' los segundos que dura")]
    public float fieldHealFractionPerSecond = 0.03f;
    [Range(0f, 0.9f), Tooltip("Fracción de velocidad que quita a los enemigos dentro (0,4 = -40%)")]
    public float fieldSlowFraction = 0.4f;
    [Min(0.05f), Tooltip("Cada cuántos segundos cura y ralentiza")]
    public float fieldTickSeconds = 0.25f;
    [Min(1f), Tooltip("Hasta cuántos metros se puede colocar el campo")]
    public float fieldPlaceRange = 25f;

    [Header("Pulso de maná (Frieren)")]
    [Min(0f), Tooltip("Segundos que aturde a los enemigos del radio al activarlo (los jefes son inmunes). El radio es 'radius' y la duración 'duration'")]
    public float pulseStunSeconds = 1.5f;
    [Range(0f, 0.9f), Tooltip("Fracción de velocidad que quita a los enemigos del radio mientras dura (0,4 = -40%)")]
    public float pulseSlowFraction = 0.4f;
    [Min(1f), Tooltip("Mientras dura, las otras dos habilidades se recargan este número de veces más rápido (2 = el doble)")]
    public float pulseCooldownBoost = 2f;

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
            case AbilityKind.Dash:
                return "Daño x" + DamageMultiplierAt(rank).ToString("0.#") + " al más cercano · " + cd;
            case AbilityKind.Berserk:
                return "Daño x" + BerserkDamageAt(rank).ToString("0.0") + " · drena " + (BerserkDrainAt(rank) * 100f).ToString("0.#") + "% por s · " + cd;
            case AbilityKind.FlameBurst:
                return "Daño x" + DamageMultiplierAt(rank).ToString("0.#") + " · quema " + DurationAt(rank).ToString("0.#") + " s · " + cd;
            case AbilityKind.ManaBeam:
                return "Rayo que atraviesa enemigos · daño, maná y enfriamiento del bastón";
            case AbilityKind.FlowerField:
                return "Radio " + radius.ToString("0.#") + " m · " + DurationAt(rank).ToString("0.#") + " s · cura " + (fieldHealFractionPerSecond * 100f).ToString("0.#")
                    + "% por s · enemigos -" + Mathf.RoundToInt(fieldSlowFraction * 100f) + "% · " + cd;
            case AbilityKind.ManaPulse:
                return DurationAt(rank).ToString("0.#") + " s · aturde " + pulseStunSeconds.ToString("0.#") + " s · Zoltraak instantáneo · " + cd;
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

    /// <summary>Daño de una habilidad que escala con la espada: daño de la espada x multiplicador del rango (x más con Furia). Mínimo 1.</summary>
    public int SwordScaledDamageFor(int swordDamage, int rank, float empowerMultiplier = 1f) =>
        Mathf.Max(1, Mathf.RoundToInt(swordDamage * DamageMultiplierAt(rank) * empowerMultiplier));

    /// <summary>Daño inicial de la llamarada: daño de la espada x multiplicador del rango (x2 más con Furia). Mínimo 1.</summary>
    public int FlameDamageFor(int swordDamage, int rank, float empowerMultiplier = 1f) =>
        SwordScaledDamageFor(swordDamage, rank, empowerMultiplier);

    /// <summary>Multiplicador de daño de la armadura Berserker en un rango (1,4 / 1,5 / 1,6).</summary>
    public float BerserkDamageAt(int rank) => DamageMultiplierAt(rank);

    /// <summary>Fracción de vida máxima que drena por segundo en un rango (nunca negativa).</summary>
    public float BerserkDrainAt(int rank) => Mathf.Max(0f, drainFractionPerSecond - Steps(rank) * drainPerRank);

    /// <summary>Daño de cada tick de la quemadura: fracción por segundo del daño de la espada x el intervalo. Mínimo 1.</summary>
    public int BurnTickDamageFor(int swordDamage, float empowerMultiplier = 1f) =>
        Mathf.Max(1, Mathf.RoundToInt(swordDamage * burnDamageFractionPerSecond * burnTickSeconds * empowerMultiplier));

    /// <summary>Segundos que dura la quemadura en un rango.</summary>
    public float BurnSecondsAt(int rank) => DurationAt(rank);

    /// <summary>Daño de la habilidad a partir del daño de una bala del arma equipada (mínimo 1).</summary>
    public int DamageFor(int bulletDamage, int rank = 1) =>
        Mathf.Max(1, Mathf.RoundToInt(bulletDamage * DamageMultiplierAt(rank)));
}
