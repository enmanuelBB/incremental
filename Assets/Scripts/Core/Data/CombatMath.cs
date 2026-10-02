using UnityEngine;

/// <summary>
/// Fórmulas de daño por segundo para comparar personajes y proteger el balance con tests.
/// "Sostenido" incluye recargas (armas) o el límite de maná y enfriamiento (habilidad del bastón).
/// </summary>
public static class CombatMath
{
    /// <summary>
    /// balas × daño ÷ (balas × cadencia + recarga), con balas = cañones × cargador. Los disparos se turnan entre los
    /// cañones (una bala por clic), así que más cañones NO suben el daño por segundo: solo hay más balas por recarga
    /// y por eso se pierde menos tiempo recargando.
    /// </summary>
    public static float GunSustainedDps(WeaponDefinition weapon, int fireRateLevel, int reloadLevel, int damageLevel)
    {
        float bullets = weapon.barrels * weapon.magazineSize;
        float cycle = bullets * weapon.FireRateAt(fireRateLevel) + weapon.ReloadTimeAt(reloadLevel);
        return bullets * weapon.DamageAt(damageLevel) / cycle;
    }

    public static float StaffBasicDps(StaffDefinition staff, int fireRateLevel, int damageLevel) =>
        staff.DamageAt(damageLevel) / staff.FireRateAt(fireRateLevel);

    /// <summary>Segundos entre dos habilidades seguidas: el mayor entre el enfriamiento y lo que tarda en regenerarse el maná.</summary>
    public static float StaffAbilityInterval(StaffDefinition staff, int manaLevel) =>
        Mathf.Max(staff.AbilityCooldownAt(manaLevel), staff.abilityManaCost / staff.ManaRegenAt(manaLevel));

    /// <param name="targetsInLine">Enemigos que atraviesa cada rayo (la habilidad daña a todos).</param>
    public static float StaffSustainedDps(StaffDefinition staff, int fireRateLevel, int manaLevel, int damageLevel, int targetsInLine)
    {
        float basic = StaffBasicDps(staff, fireRateLevel, damageLevel);
        float ability = targetsInLine * staff.AbilityDamageAt(damageLevel) / StaffAbilityInterval(staff, manaLevel);
        return basic + ability;
    }

    /// <summary>Nivel máximo de cada mejora de un arma, en el orden FireRate, Reload, Damage.</summary>
    public static int[] MaxLevels(WeaponDefinition weapon) => new[]
    {
        weapon.GetUpgrade(UpgradeType.FireRate).maxLevel,
        weapon.GetUpgrade(UpgradeType.Reload).maxLevel,
        weapon.GetUpgrade(UpgradeType.Damage).maxLevel
    };
}
