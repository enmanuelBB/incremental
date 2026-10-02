using System.Collections.Generic;

public enum CharacterStatus
{
    Unlocked,
    Locked,
    ComingSoon
}

/// <summary>
/// Reglas de desbloqueo y selección de personajes sobre SaveData. Lógica pura: el dinero se descuenta
/// de data.money y quien llama avisa a la UI y guarda.
/// </summary>
public static class CharacterRules
{
    public static CharacterStatus StatusOf(CharacterDefinition def, SaveData data)
    {
        if (def.availability == CharacterAvailability.ComingSoon) return CharacterStatus.ComingSoon;
        if (def.unlockKind == CharacterUnlockKind.Free) return CharacterStatus.Unlocked;

        CharacterSave save = data.characters.Find(c => c.id == def.Id);
        return save != null && save.unlocked ? CharacterStatus.Unlocked : CharacterStatus.Locked;
    }

    public static bool CanUnlockWithMoney(CharacterDefinition def, SaveData data) =>
        StatusOf(def, data) == CharacterStatus.Locked &&
        def.unlockKind == CharacterUnlockKind.Money &&
        data.money >= def.unlockPrice;

    /// <summary>Desbloquea con dinero. Devuelve false (sin cobrar) si no corresponde o no alcanza.</summary>
    public static bool TryUnlockWithMoney(CharacterDefinition def, SaveData data)
    {
        if (!CanUnlockWithMoney(def, data)) return false;

        data.money -= def.unlockPrice;
        data.GetCharacter(def.Id).unlocked = true;
        return true;
    }

    /// <summary>Gancho para misiones y eventos: desbloquea sin cobrar. No hace nada con los "próximamente".</summary>
    public static void Unlock(CharacterDefinition def, SaveData data)
    {
        if (def.availability == CharacterAvailability.ComingSoon) return;
        data.GetCharacter(def.Id).unlocked = true;
    }

    /// <summary>Marca el personaje como el elegido. Devuelve false si está bloqueado o es un hueco reservado.</summary>
    public static bool Select(CharacterDefinition def, SaveData data)
    {
        if (StatusOf(def, data) != CharacterStatus.Unlocked) return false;

        data.selectedCharacterId = def.Id;
        data.GetCharacter(def.Id);
        return true;
    }

    /// <summary>
    /// El personaje elegido en el guardado; si ya no existe o está bloqueado, el primero disponible por orden del menú.
    /// </summary>
    public static CharacterDefinition ResolveSelected(IReadOnlyList<CharacterDefinition> roster, SaveData data)
    {
        CharacterDefinition fallback = null;

        foreach (CharacterDefinition def in roster)
        {
            if (def == null || StatusOf(def, data) != CharacterStatus.Unlocked) continue;
            if (def.Id == data.selectedCharacterId) return def;
            if (fallback == null || def.menuOrder < fallback.menuOrder) fallback = def;
        }

        return fallback;
    }
}
