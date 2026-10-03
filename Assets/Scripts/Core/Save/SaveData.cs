using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponSave
{
    public string id;
    public bool owned;
    public int[] upgradeLevels = new int[UpgradeTypeInfo.Count];

    /// <summary>Deja upgradeLevels con el tamaño y los valores válidos.</summary>
    public void Normalize()
    {
        if (upgradeLevels == null) upgradeLevels = new int[UpgradeTypeInfo.Count];
        else if (upgradeLevels.Length != UpgradeTypeInfo.Count) Array.Resize(ref upgradeLevels, UpgradeTypeInfo.Count);

        for (int i = 0; i < upgradeLevels.Length; i++)
            if (upgradeLevels[i] < 0) upgradeLevels[i] = 0;
    }
}

[Serializable]
public class CharacterSave
{
    public string id;
    public bool unlocked;
    public int level = 1;
    [Tooltip("Experiencia acumulada hacia el siguiente nivel (no el total histórico)")]
    public int xp;
    [Tooltip("Rango de cada habilidad por casilla (0 = sin aprender)")]
    public int[] abilityRanks = new int[Progression.AbilitySlots];
    [Tooltip("Nivel de sangrado: el 1 es gratis; tope de pilas = 4 + nivel")]
    public int bleedLevel = 1;
    public List<WeaponSave> weapons = new List<WeaponSave>();

    /// <summary>Deja los datos de progresión dentro de rangos válidos (por ejemplo tras leer un guardado viejo o editado a mano).</summary>
    public void NormalizeProgress()
    {
        level = Math.Max(1, Math.Min(Progression.MaxLevel, level));
        xp = level >= Progression.MaxLevel ? 0 : Math.Max(0, Math.Min(xp, Progression.XpForNextLevel(level) - 1));
        bleedLevel = Math.Max(1, Math.Min(Progression.MaxBleedLevel, bleedLevel));

        if (abilityRanks == null) abilityRanks = new int[Progression.AbilitySlots];
        else if (abilityRanks.Length != Progression.AbilitySlots) Array.Resize(ref abilityRanks, Progression.AbilitySlots);
        for (int i = 0; i < abilityRanks.Length; i++)
            abilityRanks[i] = Math.Max(0, Math.Min(Progression.MaxNormalRank, abilityRanks[i]));
    }

    public WeaponSave GetWeapon(string weaponId)
    {
        WeaponSave save = weapons.Find(w => w.id == weaponId);
        if (save == null)
        {
            save = new WeaponSave { id = weaponId };
            weapons.Add(save);
        }
        save.Normalize();
        return save;
    }

    /// <summary>
    /// Busca el guardado de un arma por id. Si no existe, intenta recuperar el progreso de la
    /// versión que guardaba por posición ("legacy_N") y le asigna el id definitivo.
    /// </summary>
    public WeaponSave GetWeapon(string weaponId, int legacyIndex)
    {
        if (!weapons.Exists(w => w.id == weaponId))
        {
            WeaponSave legacy = weapons.Find(w => w.id == "legacy_" + legacyIndex);
            if (legacy != null) legacy.id = weaponId;
        }
        return GetWeapon(weaponId);
    }
}

/// <summary>
/// Datos que se guardan en disco. Cambiar la forma de esta clase exige subir CurrentVersion
/// y agregar el paso correspondiente en SaveMigrations.
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 3;
    public const string DefaultCharacterId = "alucard";

    public int version = CurrentVersion;
    public int money;
    public int prestigeCoins;
    public string selectedCharacterId = DefaultCharacterId;
    public List<CharacterSave> characters = new List<CharacterSave>();

    public CharacterSave GetCharacter(string characterId)
    {
        CharacterSave save = characters.Find(c => c.id == characterId);
        if (save == null)
        {
            save = new CharacterSave { id = characterId, unlocked = characterId == DefaultCharacterId };
            characters.Add(save);
        }
        return save;
    }
}
