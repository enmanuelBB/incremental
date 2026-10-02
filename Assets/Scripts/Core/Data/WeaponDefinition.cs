using UnityEngine;

/// <summary>Datos de un arma. Se crea desde Assets > Create > Game > Weapon.</summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponDefinition : GameDefinition
{
    public string weaponName = "Arma";

    [Header("Disparo")]
    public int magazineSize = 12;

    [Min(1)]
    [Tooltip("Cañones que se turnan (2 = dos pistolas): cada clic dispara UNA bala, de un cañón y luego del otro. Cada cañón tiene su propio cargador de 'magazineSize'.")]
    public int barrels = 1;
    public float fireRate = 0.2f;
    public float reloadTime = 1.5f;
    public int damage = 10;
    public float range = 50f;
    public bool isAutomatic = true;

    [Range(0f, 1f)]
    [Tooltip("Punto de la recarga en que el cargador se llena")]
    public float ammoFillPoint = 0.7f;

    [Header("Retroceso")]
    [Tooltip("Retroceso de cada cañón en cada disparo (posición 0 = primer cañón/pistola izquierda, 1 = segundo/derecha). Si falta uno, ese cañón no retrocede.")]
    public RecoilSettings[] barrelRecoil = new RecoilSettings[0];

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Tienda")]
    public bool ownedFromStart = true;
    public int price = 400;

    [Header("Mejoras")]
    public UpgradeStat fireRateUpgrade = new UpgradeStat { step = 0.02f, limit = 0.05f, basePrice = 100, priceIncrease = 50, maxLevel = 5 };
    public UpgradeStat reloadUpgrade = new UpgradeStat { step = 0.15f, limit = 0.5f, basePrice = 100, priceIncrease = 50, maxLevel = 5 };
    public UpgradeStat damageUpgrade = new UpgradeStat { step = 1f, limit = 0f, basePrice = 150, priceIncrease = 75, maxLevel = 10 };

    [Tooltip("Aplicación de sangrado: step = pilas extra por impacto y nivel. Precio exponencial (muy cara).")]
    public UpgradeStat bleedUpgrade = new UpgradeStat { step = 1f, limit = 0f, basePrice = 600, priceIncrease = 0, priceGrowth = 1.6f, maxLevel = 10 };

    [Header("Sangrado")]
    [Min(0)]
    [Tooltip("Pilas de sangrado que aplica cada impacto sin mejoras. 0 = el arma no sangra y no muestra la mejora.")]
    public int baseBleedPerHit = 1;

    /// <summary>Retroceso del cañón indicado (sin retroceso si el arma no lo define).</summary>
    public RecoilSettings RecoilOf(int barrel) =>
        barrelRecoil != null && barrel >= 0 && barrel < barrelRecoil.Length ? barrelRecoil[barrel] : RecoilSettings.None;

    /// <summary>Falso para armas sin cargador (por ejemplo un bastón).</summary>
    public virtual bool UsesAmmo => true;

    /// <summary>Verdadero si el arma aplica sangrado (muestra la fila de mejora en la estación).</summary>
    public virtual bool AppliesBleed => baseBleedPerHit > 0;

    /// <summary>Pilas de sangrado por impacto con el nivel de mejora indicado.</summary>
    public int BleedPerHitAt(int level) =>
        AppliesBleed ? baseBleedPerHit + Mathf.RoundToInt(level * bleedUpgrade.step) : 0;

    /// <summary>Nombre de cada mejora para el menú; cada tipo de arma puede nombrarlas distinto.</summary>
    public virtual string UpgradeLabel(UpgradeType type) => UpgradeTypeInfo.DisplayName(type);

    public virtual float FireRateAt(int level) =>
        Mathf.Max(fireRate - level * fireRateUpgrade.step, fireRateUpgrade.limit);

    public virtual int DamageAt(int level) =>
        damage + Mathf.RoundToInt(level * damageUpgrade.step);

    public float ReloadTimeAt(int level) =>
        Mathf.Max(reloadTime - level * reloadUpgrade.step, reloadUpgrade.limit);

    public UpgradeStat GetUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate: return fireRateUpgrade;
            case UpgradeType.Reload: return reloadUpgrade;
            case UpgradeType.Bleed: return bleedUpgrade;
            default: return damageUpgrade;
        }
    }
}
