using UnityEngine;

/// <summary>
/// Estado en partida de un arma: munición, si está comprada y sus niveles de mejora.
/// Calcula los stats efectivos y se sincroniza con el guardado.
/// </summary>
public class WeaponState
{
    public WeaponDefinition Definition { get; }

    /// <summary>Cargadores del arma: uno por cañón, y los disparos se turnan entre ellos.</summary>
    public BarrelMagazines Magazines { get; }
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
        Magazines = new BarrelMagazines(definition.barrels, definition.magazineSize);
    }

    public string Name => Definition.weaponName;
    public int MagazineSize => Definition.magazineSize;
    public bool IsAutomatic => Definition.isAutomatic;
    public int Price => Definition.price;

    public int GetLevel(UpgradeType type) => save.upgradeLevels[(int)type];
    public int GetMaxLevel(UpgradeType type) => Definition.GetUpgrade(type).maxLevel;
    public bool IsMaxLevel(UpgradeType type) => GetLevel(type) >= GetMaxLevel(type);
    public int GetUpgradeCost(UpgradeType type) => Definition.GetUpgrade(type).PriceAt(GetLevel(type));

    public bool UsesAmmo => Definition.UsesAmmo;
    public string GetUpgradeLabel(UpgradeType type) => Definition.UpgradeLabel(type);

    /// <summary>Datos del bastón si esta arma lo es; null para las armas de fuego.</summary>
    public StaffDefinition Staff => Definition as StaffDefinition;

    /// <summary>Datos de la espada si esta arma lo es; null para armas de fuego y bastones.</summary>
    public SwordDefinition Sword => Definition as SwordDefinition;

    // Las fórmulas viven en las definiciones (Game.Core) para que el balance se pruebe sin abrir Unity.
    public float FireRate => Definition.FireRateAt(GetLevel(UpgradeType.FireRate));
    public float ReloadTime => Definition.ReloadTimeAt(GetLevel(UpgradeType.Reload));
    /// <summary>Daño de una bala: el del arma con sus mejoras de dinero, multiplicado por el bono de daño del árbol.</summary>
    public int Damage
    {
        get
        {
            int baseDamage = Definition.DamageAt(GetLevel(UpgradeType.Damage));
            float multiplier = (SkillTreeManager.Instance != null ? SkillTreeManager.Instance.Bonuses.DamageMultiplier : 1f)
                * BerserkArmor.DamageMultiplier;
            return Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage * multiplier));
        }
    }

    /// <summary>Pilas de sangrado que aplica cada impacto (0 si el arma no sangra).</summary>
    public int BleedPerHit => Definition.BleedPerHitAt(GetLevel(UpgradeType.Bleed));

    /// <summary>Probabilidad de aturdir a cada enemigo golpeado (solo la espada): nivel de Aturdir más el bono del árbol.</summary>
    public float StunChance
    {
        get
        {
            SwordDefinition sword = Sword;
            if (sword == null) return 0f;

            float treeBonus = SkillTreeManager.Instance != null ? SkillTreeManager.Instance.Bonuses.StunChanceBonus : 0f;
            return sword.StunChanceAt(GetLevel(UpgradeType.Reload), treeBonus);
        }
    }

    // Solo bastón
    /// <summary>Daño del rayo de maná: el del bastón con el Poder de la tienda, por el daño del árbol y el del rayo.</summary>
    public int AbilityDamage
    {
        get
        {
            int baseDamage = Staff.AbilityDamageAt(GetLevel(UpgradeType.Damage));
            TreeBonuses tree = SkillTreeManager.CurrentBonuses;
            return Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage * tree.DamageMultiplier * (1f + tree.BeamDamagePercent)));
        }
    }

    /// <summary>Daño de un Zoltraak con esa carga (0 a 1): el del bastón con el Poder de la tienda, por el daño del árbol y el del Zoltraak.</summary>
    public int ZoltraakDamage(float charge)
    {
        int baseDamage = Staff.ZoltraakDamageAt(GetLevel(UpgradeType.Damage), charge);
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        return Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage * tree.DamageMultiplier * (1f + tree.ZoltraakDamagePercent)));
    }

    public float ManaRegen => Staff.ManaRegenAt(GetLevel(UpgradeType.Reload));
    public float AbilityCooldownTime => Staff.AbilityCooldownAt(GetLevel(UpgradeType.Reload));

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
        if (type == UpgradeType.Bleed && !Definition.AppliesBleed) return false;
        if (IsMaxLevel(type)) return false;
        if (!MoneyManager.Instance.SpendMoney(GetUpgradeCost(type))) return false;

        save.upgradeLevels[(int)type]++;
        SaveSystem.Save();
        return true;
    }
}
