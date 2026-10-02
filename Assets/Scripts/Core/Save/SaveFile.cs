using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Lectura y escritura del archivo de guardado, separadas de SaveSystem para poder probarlas
/// con una carpeta temporal. Escribe a un .tmp y reemplaza, dejando el guardado anterior como .bak.
/// </summary>
public static class SaveFile
{
    public enum ReadStatus { Missing, Loaded, Recovered, Unreadable, Newer }

    public struct ReadResult
    {
        public ReadStatus Status;
        public SaveData Data;
        public bool Migrated;
    }

    private static string BackupPath(string path) => path + ".bak";
    private static string TempPath(string path) => path + ".tmp";
    private static string PreMigrationPath(string path) => Path.Combine(Path.GetDirectoryName(path), "save.v1.bak");

    /// <summary>
    /// Lee el guardado. Si el principal está dañado prueba con el .bak. Si viene de un formato viejo,
    /// guarda una única copia previa a la migración (save.v1.bak) que nunca se sobrescribe.
    /// </summary>
    public static ReadResult Read(string path)
    {
        if (!File.Exists(path) && !File.Exists(BackupPath(path)))
            return new ReadResult { Status = ReadStatus.Missing };

        bool damaged = false;

        foreach (string candidate in new[] { path, BackupPath(path) })
        {
            if (!File.Exists(candidate)) continue;

            try
            {
                SaveData data = SaveMigrations.Parse(File.ReadAllText(candidate), out bool migrated);
                if (migrated) KeepPreMigrationCopy(path, candidate);

                return new ReadResult
                {
                    Status = damaged ? ReadStatus.Recovered : ReadStatus.Loaded,
                    Data = data,
                    Migrated = migrated
                };
            }
            catch (NewerSaveVersionException e)
            {
                Debug.LogWarning(e.Message + " No se toca el archivo.");
                return new ReadResult { Status = ReadStatus.Newer };
            }
            catch (Exception e)
            {
                Debug.LogWarning("No se pudo leer " + Path.GetFileName(candidate) + ": " + e.Message);
                damaged = true;
            }
        }

        return new ReadResult { Status = ReadStatus.Unreadable };
    }

    public static void Write(string path, SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        string temp = TempPath(path);
        File.WriteAllText(temp, JsonUtility.ToJson(data, true));

        if (File.Exists(path)) File.Replace(temp, path, BackupPath(path));
        else File.Move(temp, path);
    }

    /// <summary>Borra el guardado y sus copias de seguridad automáticas (no la copia previa a la migración).</summary>
    public static void Delete(string path)
    {
        foreach (string file in new[] { path, BackupPath(path), TempPath(path) })
            if (File.Exists(file)) File.Delete(file);
    }

    private static void KeepPreMigrationCopy(string path, string source)
    {
        string copy = PreMigrationPath(path);
        if (File.Exists(copy)) return;

        try { File.Copy(source, copy); }
        catch (Exception e) { Debug.LogWarning("No se pudo copiar el guardado previo a la migración: " + e.Message); }
    }
}
