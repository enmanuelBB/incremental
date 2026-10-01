using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class WeaponSave
{
    public string id;
    public bool owned;
    public int[] upgradeLevels = new int[UpgradeTypeInfo.Count];
}

[Serializable]
public class SaveData
{
    public int money;
    public List<WeaponSave> weapons = new List<WeaponSave>();

    public WeaponSave GetWeapon(string id)
    {
        WeaponSave save = weapons.Find(w => w.id == id);
        if (save == null)
        {
            save = new WeaponSave { id = id };
            weapons.Add(save);
        }
        if (save.upgradeLevels == null || save.upgradeLevels.Length != UpgradeTypeInfo.Count)
            Array.Resize(ref save.upgradeLevels, UpgradeTypeInfo.Count);
        return save;
    }
}

/// <summary>
/// Guarda el progreso en un JSON en persistentDataPath. Los cambios viven en memoria
/// (Data) y se escriben a disco solo en puntos de control, no en cada kill.
/// </summary>
public static class SaveSystem
{
    private static SaveData data;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => data = null;

    public static void Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo leer el guardado, se empieza de cero: " + e.Message);
            }
        }

        if (data == null)
        {
            data = new SaveData();
            ImportLegacyPlayerPrefs(data);
        }
    }

    public static void Save()
    {
        if (data == null) return;

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError("No se pudo guardar el progreso: " + e.Message);
        }
    }

    public static void Delete()
    {
        data = new SaveData();
        if (File.Exists(FilePath)) File.Delete(FilePath);
        PlayerPrefs.DeleteAll();
    }

    // Migra el progreso de la versión anterior (PlayerPrefs) la primera vez que se carga.
    private static void ImportLegacyPlayerPrefs(SaveData target)
    {
        target.money = PlayerPrefs.GetInt("PlayerMoney", 0);

        for (int i = 0; i < 8; i++)
        {
            if (!PlayerPrefs.HasKey("Weapon_" + i + "_Owned") &&
                !PlayerPrefs.HasKey("Weapon_" + i + "_FireRateLevel")) continue;

            WeaponSave save = target.GetWeapon("legacy_" + i);
            save.owned = PlayerPrefs.GetInt("Weapon_" + i + "_Owned", 0) == 1;
            save.upgradeLevels[(int)UpgradeType.FireRate] = PlayerPrefs.GetInt("Weapon_" + i + "_FireRateLevel", 0);
            save.upgradeLevels[(int)UpgradeType.Reload] = PlayerPrefs.GetInt("Weapon_" + i + "_ReloadLevel", 0);
            save.upgradeLevels[(int)UpgradeType.Damage] = PlayerPrefs.GetInt("Weapon_" + i + "_DamageLevel", 0);
        }
    }
}
