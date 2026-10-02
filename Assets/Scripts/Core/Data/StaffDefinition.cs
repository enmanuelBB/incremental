using UnityEngine;

/// <summary>
/// Bastón de un mago. Se crea desde Assets > Create > Game > Staff.
/// El disparo básico no gasta maná; la habilidad sí, y tiene enfriamiento.
/// Usa los mismos 3 espacios de mejora que las armas, con otro significado:
///   Cadencia (FireRate): velocidad del disparo básico.
///   Maná (Reload): más regeneración de maná y menos enfriamiento de la habilidad.
///   Poder (Damage): más daño del disparo básico y de la habilidad (multiplicador por nivel).
/// </summary>
[CreateAssetMenu(fileName = "NewStaff", menuName = "Game/Staff")]
public class StaffDefinition : WeaponDefinition
{
    [Header("Maná")]
    public float manaMax = 100f;
    [Tooltip("Maná que recupera por segundo (sin mejoras)")]
    public float manaRegen = 4f;

    [Header("Habilidad (rayo penetrante)")]
    public string abilityName = "Rayo arcano";
    [Tooltip("Icono que se muestra en la casilla de la habilidad del HUD")]
    public Sprite abilityIcon;
    public int abilityDamage = 40;
    public float abilityManaCost = 25f;
    public float abilityCooldown = 3f;
    public float abilityRange = 100f;
    [Tooltip("Grosor del rayo: más ancho es más fácil de acertar a varios enemigos")]
    public float abilityBeamRadius = 0.4f;

    [Header("Mejora de Maná: enfriamiento")]
    [Tooltip("Segundos que baja el enfriamiento por nivel de Maná")]
    public float cooldownStep = 0.3f;
    public float cooldownLimit = 1.5f;

    public override bool UsesAmmo => false;

    public override string UpgradeLabel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate: return "Cadencia";
            case UpgradeType.Reload: return "Maná";
            default: return "Poder";
        }
    }

    // Con el bastón, el nivel de Poder multiplica el daño en vez de sumarlo (step = fracción por nivel).
    public override int DamageAt(int level) =>
        Mathf.RoundToInt(damage * (1f + level * damageUpgrade.step));

    public int AbilityDamageAt(int powerLevel) =>
        Mathf.RoundToInt(abilityDamage * (1f + powerLevel * damageUpgrade.step));

    public float ManaRegenAt(int manaLevel) =>
        manaRegen + manaLevel * reloadUpgrade.step;

    public float AbilityCooldownAt(int manaLevel) =>
        Mathf.Max(abilityCooldown - manaLevel * cooldownStep, cooldownLimit);
}
