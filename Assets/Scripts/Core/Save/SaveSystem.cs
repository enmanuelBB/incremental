using System.IO;
using UnityEngine;

/// <summary>
/// Guarda el progreso en un JSON en persistentDataPath. Los cambios viven en memoria
/// (Data) y se escriben a disco solo en puntos de control, no en cada kill.
/// </summary>
public static class SaveSystem
{
    private static SaveData data;

    // Si el archivo es de una versión más nueva, se juega en memoria sin pisarlo.
    private static bool readOnly;

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
    private static void ResetStatics()
    {
        data = null;
        readOnly = false;
    }

    public static void Load()
    {
        readOnly = false;
        SaveFile.ReadResult result = SaveFile.Read(FilePath);

        switch (result.Status)
        {
            case SaveFile.ReadStatus.Loaded:
            case SaveFile.ReadStatus.Recovered:
                data = result.Data;
                if (result.Status == SaveFile.ReadStatus.Recovered)
                    Debug.LogWarning("El guardado principal estaba dañado, se recuperó la copia de seguridad.");
                if (result.Migrated) Save();
                break;

            case SaveFile.ReadStatus.Newer:
                readOnly = true;
                data = NewSave();
                break;

            default:
                data = NewSave();
                if (result.Status == SaveFile.ReadStatus.Missing) ImportLegacyPlayerPrefs(data);
                break;
        }
    }

    public static void Save()
    {
        if (data == null || readOnly) return;

        try
        {
            SaveFile.Write(FilePath, data);
        }
        catch (System.Exception e)
        {
            Debug.LogError("No se pudo guardar el progreso: " + e.Message);
        }
    }

    public static void Delete()
    {
        data = NewSave();
        readOnly = false;
        SaveFile.Delete(FilePath);
        PlayerPrefs.DeleteAll();
    }

    private static SaveData NewSave()
    {
        var fresh = new SaveData();
        SaveMigrations.Validate(fresh);
        return fresh;
    }

    // Migra el progreso de la versión anterior (PlayerPrefs) la primera vez que se carga.
    private static void ImportLegacyPlayerPrefs(SaveData target)
    {
        target.money = PlayerPrefs.GetInt("PlayerMoney", 0);
        CharacterSave main = target.GetCharacter(SaveData.DefaultCharacterId);

        for (int i = 0; i < 8; i++)
        {
            if (!PlayerPrefs.HasKey("Weapon_" + i + "_Owned") &&
                !PlayerPrefs.HasKey("Weapon_" + i + "_FireRateLevel")) continue;

            WeaponSave save = main.GetWeapon("legacy_" + i);
            save.owned = PlayerPrefs.GetInt("Weapon_" + i + "_Owned", 0) == 1;
            save.upgradeLevels[(int)UpgradeType.FireRate] = PlayerPrefs.GetInt("Weapon_" + i + "_FireRateLevel", 0);
            save.upgradeLevels[(int)UpgradeType.Reload] = PlayerPrefs.GetInt("Weapon_" + i + "_ReloadLevel", 0);
            save.upgradeLevels[(int)UpgradeType.Damage] = PlayerPrefs.GetInt("Weapon_" + i + "_DamageLevel", 0);
        }
    }
}
