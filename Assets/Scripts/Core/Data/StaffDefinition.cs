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

    [Header("Zoltraak (disparo cargado, clic izquierdo)")]
    [Tooltip("Daño a carga completa (sin mejoras)")]
    public int zoltraakDamage = 45;
    [Min(0.1f), Tooltip("Segundos que hay que mantener el clic para la carga completa")]
    public float zoltraakChargeSeconds = 1.2f;
    [Range(0f, 1f), Tooltip("Fracción del daño con la carga en 0 (suelta al instante)")]
    public float zoltraakMinDamageFraction = 0.4f;
    [Tooltip("Radio de la explosión con la carga en 0, en metros")]
    public float zoltraakMinRadius = 2.5f;
    [Tooltip("Radio de la explosión a carga completa, en metros")]
    public float zoltraakMaxRadius = 4f;
    [Tooltip("Maná que gasta al dispararlo. 0 = gratis (así lo quiere el usuario: el Zoltraak no gasta maná)")]
    public float zoltraakManaCost = 0f;
    [Tooltip("Pausa tras un Zoltraak antes de poder empezar otro, en segundos")]
    public float zoltraakPause = 0.4f;
    [Tooltip("Pausa entre disparos con la definitiva activa (sin carga)")]
    public float zoltraakInstantPause = 0.35f;
    [Tooltip("Alcance de la mira para colocar la explosión, en metros")]
    public float zoltraakRange = 60f;

    public override bool UsesAmmo => false;

    // El bastón no sangra: no muestra la mejora de sangrado.
    public override bool AppliesBleed => false;

    public override string UpgradeLabel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate: return "Cadencia";
            case UpgradeType.Reload: return "Maná";
            case UpgradeType.Bleed: return "Sangrado";
            default: return "Poder";
        }
    }

    // Con el bastón, el nivel de Poder multiplica el daño en vez de sumarlo (step = fracción por nivel).
    public override int DamageAt(int level) =>
        Mathf.RoundToInt(damage * (1f + level * damageUpgrade.step));

    public int AbilityDamageAt(int powerLevel) =>
        Mathf.RoundToInt(abilityDamage * (1f + powerLevel * damageUpgrade.step));

    /// <summary>Daño del Zoltraak: el de carga completa con el Poder de la tienda (+step por nivel), escalado de 'zoltraakMinDamageFraction' a 1 según la carga (0 a 1).</summary>
    public int ZoltraakDamageAt(int powerLevel, float charge) =>
        Mathf.RoundToInt(zoltraakDamage * (1f + powerLevel * damageUpgrade.step) * Mathf.Lerp(zoltraakMinDamageFraction, 1f, Mathf.Clamp01(charge)));

    public float ZoltraakRadiusAt(float charge) =>
        Mathf.Lerp(zoltraakMinRadius, zoltraakMaxRadius, Mathf.Clamp01(charge));

    public float ManaRegenAt(int manaLevel) =>
        manaRegen + manaLevel * reloadUpgrade.step;

    public float AbilityCooldownAt(int manaLevel) =>
        Mathf.Max(abilityCooldown - manaLevel * cooldownStep, cooldownLimit);
}
