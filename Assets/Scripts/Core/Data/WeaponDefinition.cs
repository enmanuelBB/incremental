using UnityEngine;

/// <summary>Datos de un arma. Se crea desde Assets > Create > Game > Weapon.</summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponDefinition : GameDefinition
{
    public string weaponName = "Arma";

    [Header("Disparo")]
    public int magazineSize = 12;
    public float fireRate = 0.2f;
    public float reloadTime = 1.5f;
    public int damage = 10;
    public float range = 50f;
    public bool isAutomatic = true;

    [Range(0f, 1f)]
    [Tooltip("Punto de la recarga en que el cargador se llena")]
    public float ammoFillPoint = 0.7f;

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

    /// <summary>Falso para armas sin cargador (por ejemplo un bastón).</summary>
    public virtual bool UsesAmmo => true;

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
            default: return damageUpgrade;
        }
    }
}
