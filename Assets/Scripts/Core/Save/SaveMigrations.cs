using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>El archivo pertenece a una versión más nueva del juego que la que está corriendo.</summary>
public class NewerSaveVersionException : Exception
{
    public int FoundVersion { get; }

    public NewerSaveVersionException(int foundVersion)
        : base("El guardado es de la versión " + foundVersion + " y este juego solo conoce hasta la " + SaveData.CurrentVersion + ".")
    {
        FoundVersion = foundVersion;
    }
}

/// <summary>
/// Convierte el JSON de cualquier versión anterior al formato actual. Son funciones puras
/// (texto entra, SaveData sale) para poder probarlas sin tocar el disco.
/// </summary>
public static class SaveMigrations
{
    // Formato original: sin campo version, un solo conjunto de armas y el dinero.
    [Serializable]
    private class SaveDataV1
    {
        public int money;
        public List<WeaponSave> weapons = new List<WeaponSave>();
    }

    [Serializable]
    private class VersionProbe
    {
        public int version;
    }

    /// <summary>Versión del formato que declara el JSON (0 si no trae el campo: formato original).</summary>
    public static int ReadVersion(string json)
    {
        VersionProbe probe = JsonUtility.FromJson<VersionProbe>(json);
        return probe != null ? probe.version : 0;
    }

    /// <summary>Lee el JSON, lo migra hasta la versión actual y lo valida.</summary>
    public static SaveData Parse(string json, out bool migrated)
    {
        int version = ReadVersion(json);

        if (version > SaveData.CurrentVersion) throw new NewerSaveVersionException(version);

        migrated = version < SaveData.CurrentVersion;

        // v1 (sin versión) tiene otra forma. v2 y v3 comparten forma: la v3 solo agrega campos de progresión,
        // que JsonUtility deja en su valor inicial (nivel 1, sin XP ni rangos) si el archivo no los trae.
        SaveData data = version < 2
            ? FromV1(JsonUtility.FromJson<SaveDataV1>(json))
            : JsonUtility.FromJson<SaveData>(json);

        if (data == null) throw new FormatException("El guardado está vacío o dañado.");

        Validate(data);
        return data;
    }

    private static SaveData FromV1(SaveDataV1 old)
    {
        if (old == null) old = new SaveDataV1();

        var data = new SaveData { money = old.money };
        CharacterSave main = data.GetCharacter(SaveData.DefaultCharacterId);
        main.unlocked = true;
        if (old.weapons != null) main.weapons.AddRange(old.weapons);
        return data;
    }

    /// <summary>Corrige valores imposibles (por ejemplo un guardado editado a mano).</summary>
    public static void Validate(SaveData data)
    {
        data.version = SaveData.CurrentVersion;
        data.money = Mathf.Max(0, data.money);
        data.prestigeCoins = Mathf.Max(0, data.prestigeCoins);

        if (data.characters == null) data.characters = new List<CharacterSave>();
        if (string.IsNullOrEmpty(data.selectedCharacterId)) data.selectedCharacterId = SaveData.DefaultCharacterId;

        data.GetCharacter(SaveData.DefaultCharacterId);
        data.GetCharacter(data.selectedCharacterId);

        foreach (CharacterSave character in data.characters)
        {
            character.NormalizeProgress();
            if (character.weapons == null) character.weapons = new List<WeaponSave>();
            foreach (WeaponSave weapon in character.weapons) weapon.Normalize();
        }

        // El personaje inicial siempre está disponible: sin él no se puede jugar.
        data.GetCharacter(SaveData.DefaultCharacterId).unlocked = true;
    }
}
