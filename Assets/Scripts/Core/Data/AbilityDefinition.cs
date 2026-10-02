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

    /// <summary>Daño de la habilidad a partir del daño de una bala del arma equipada (mínimo 1).</summary>
    public int DamageFor(int bulletDamage) =>
        Mathf.Max(1, Mathf.RoundToInt(bulletDamage * damageMultiplier));
}
