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

    /// <summary>Vida que se recupera por un daño directo (50% con la definitiva).</summary>
    public int LifeStealFor(int damage) => Mathf.Max(0, Mathf.RoundToInt(damage * lifeSteal));

    /// <summary>Daño que reciben los vecinos de la explosión (mínimo 1).</summary>
    public int ExplosionDamageFor(int bulletDamage) => Mathf.Max(1, Mathf.RoundToInt(bulletDamage * explosionFraction));

    /// <summary>Pilas de sangrado de un disparo básico mientras dura la definitiva.</summary>
    public int BoostedBleed(int stacksPerHit) => Mathf.Max(0, Mathf.RoundToInt(stacksPerHit * bleedMultiplier));

    /// <summary>Daño de la habilidad a partir del daño de una bala del arma equipada (mínimo 1).</summary>
    public int DamageFor(int bulletDamage) =>
        Mathf.Max(1, Mathf.RoundToInt(bulletDamage * damageMultiplier));
}
