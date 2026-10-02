using System.Collections.Generic;
using System.Globalization;

public struct StatLine
{
    public string Label;
    public string Value;

    public StatLine(string label, string value)
    {
        Label = label;
        Value = value;
    }
}

/// <summary>Arma las líneas de estadísticas que muestra el menú de personajes, con las mejoras ya compradas.</summary>
public static class CharacterInfo
{
    // Para comparar contra oleadas: cuántos enemigos suponemos en la fila del rayo.
    public const int LineTargets = 4;

    // InvariantCulture: es-CL no existe en todas las builds y no queremos una excepción por un texto.
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <param name="save">Progreso del personaje; puede ser null (todo en nivel 0).</param>
    public static List<StatLine> Describe(CharacterDefinition character, CharacterSave save)
    {
        var lines = new List<StatLine>
        {
            new StatLine("Vida", character.maxHealth.ToString(Culture)),
            new StatLine("Velocidad", character.moveSpeed.ToString("0.#", Culture))
        };

        if (character.startingWeapons == null) return lines;

        foreach (WeaponDefinition weapon in character.startingWeapons)
        {
            if (weapon == null) continue;

            WeaponSave progress = save?.weapons.Find(w => w.id == weapon.Id);
            int fireRate = Level(progress, UpgradeType.FireRate);
            int reload = Level(progress, UpgradeType.Reload);
            int damage = Level(progress, UpgradeType.Damage);

            if (weapon is StaffDefinition staff) AddStaff(lines, staff, fireRate, reload, damage);
            else AddGun(lines, weapon, fireRate, reload, damage);
        }

        return lines;
    }

    private static void AddGun(List<StatLine> lines, WeaponDefinition weapon, int fireRate, int reload, int damage)
    {
        float dps = CombatMath.GunSustainedDps(weapon, fireRate, reload, damage);
        string mode = weapon.isAutomatic ? "automática" : "semiautomática";

        lines.Add(new StatLine(weapon.weaponName,
            $"daño {weapon.DamageAt(damage)} · {weapon.FireRateAt(fireRate).ToString("0.00", Culture)} s · " +
            $"cargador {weapon.magazineSize} · recarga {weapon.ReloadTimeAt(reload).ToString("0.0", Culture)} s · {mode} · {dps.ToString("0", Culture)} DPS"));
    }

    private static void AddStaff(List<StatLine> lines, StaffDefinition staff, int fireRate, int mana, int power)
    {
        float basic = CombatMath.StaffBasicDps(staff, fireRate, power);
        float single = CombatMath.StaffSustainedDps(staff, fireRate, mana, power, 1);
        float line = CombatMath.StaffSustainedDps(staff, fireRate, mana, power, LineTargets);

        lines.Add(new StatLine(staff.weaponName,
            $"disparo básico sin maná: daño {staff.DamageAt(power)} · {staff.FireRateAt(fireRate).ToString("0.00", Culture)} s · {basic.ToString("0", Culture)} DPS"));
        lines.Add(new StatLine("Maná",
            $"{staff.manaMax.ToString("0", Culture)} (+{staff.ManaRegenAt(mana).ToString("0.0", Culture)}/s)"));
        lines.Add(new StatLine(staff.abilityName,
            $"daño {staff.AbilityDamageAt(power)} · {staff.abilityManaCost.ToString("0", Culture)} de maná · " +
            $"enfriamiento {staff.AbilityCooldownAt(mana).ToString("0.0", Culture)} s · atraviesa enemigos"));
        lines.Add(new StatLine("DPS sostenido",
            $"{single.ToString("0", Culture)} (1 enemigo) · {line.ToString("0", Culture)} ({LineTargets} en fila)"));
    }

    private static int Level(WeaponSave progress, UpgradeType type) =>
        progress != null && progress.upgradeLevels != null && (int)type < progress.upgradeLevels.Length
            ? progress.upgradeLevels[(int)type]
            : 0;
}
