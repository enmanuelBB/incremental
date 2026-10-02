using UnityEngine;

/// <summary>
/// Estado en partida de un arma: munición, si está comprada y sus niveles de mejora.
/// Calcula los stats efectivos y se sincroniza con el guardado.
/// </summary>
public class WeaponState
{
    public WeaponDefinition Definition { get; }
    public int Ammo { get; set; }
    public bool Owned { get; private set; }

    private readonly WeaponSave save;

    /// <param name="save">Progreso de esta arma para el personaje que la usa (lo entrega quien la crea).</param>
    public WeaponState(WeaponDefinition definition, WeaponSave save)
    {
        Definition = definition;
        this.save = save;

        // Un guardado editado a mano no puede dejar una mejora por encima de su nivel máximo.
        for (int i = 0; i < UpgradeTypeInfo.Count; i++)
            save.upgradeLevels[i] = Mathf.Min(save.upgradeLevels[i], definition.GetUpgrade((UpgradeType)i).maxLevel);

        Owned = definition.ownedFromStart || save.owned;
        Ammo = definition.magazineSize;
    }

    public string Name => Definition.weaponName;
    public int MagazineSize => Definition.magazineSize;
    public bool IsAutomatic => Definition.isAutomatic;
    public int Price => Definition.price;

    public int GetLevel(UpgradeType type) => save.upgradeLevels[(int)type];
    public int GetMaxLevel(UpgradeType type) => Definition.GetUpgrade(type).maxLevel;
    public bool IsMaxLevel(UpgradeType type) => GetLevel(type) >= GetMaxLevel(type);
    public int GetUpgradeCost(UpgradeType type) => Definition.GetUpgrade(type).PriceAt(GetLevel(type));

    public float FireRate
    {
        get
        {
            UpgradeStat u = Definition.fireRateUpgrade;
            return Mathf.Max(Definition.fireRate - GetLevel(UpgradeType.FireRate) * u.step, u.limit);
        }
    }

    public float ReloadTime
    {
        get
        {
            UpgradeStat u = Definition.reloadUpgrade;
            return Mathf.Max(Definition.reloadTime - GetLevel(UpgradeType.Reload) * u.step, u.limit);
        }
    }

    public int Damage =>
        Definition.damage + Mathf.RoundToInt(GetLevel(UpgradeType.Damage) * Definition.damageUpgrade.step);

    /// <summary>Cobra el arma. Devuelve false si no alcanza el dinero.</summary>
    public bool TryBuy()
    {
        if (Owned) return true;
        if (!MoneyManager.Instance.SpendMoney(Definition.price)) return false;

        Owned = true;
        save.owned = true;
        SaveSystem.Save();
        return true;
    }

    /// <summary>Cobra y sube un nivel de mejora. Devuelve false si está al máximo o no alcanza el dinero.</summary>
    public bool TryUpgrade(UpgradeType type)
    {
        if (IsMaxLevel(type)) return false;
        if (!MoneyManager.Instance.SpendMoney(GetUpgradeCost(type))) return false;

        save.upgradeLevels[(int)type]++;
        SaveSystem.Save();
        return true;
    }
}
