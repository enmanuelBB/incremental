using UnityEngine;

/// <summary>Por qué no se puede subir algo con los puntos de personaje.</summary>
public enum UpgradeBlock
{
    None,
    NoPoints,
    LevelTooLow,
    MaxRank
}

/// <summary>
/// Reglas de experiencia, nivel y puntos de personaje. Lógica pura sobre CharacterSave, sin Unity en juego,
/// para poder probarla. Cada personaje lleva su propia experiencia, nivel, rangos de habilidad y nivel de sangrado.
/// </summary>
public static class Progression
{
    public const int MaxLevel = 30;
    public const int MaxBleedLevel = 15;
    public const int AbilitySlots = 3;
    public const int MaxNormalRank = 5;
    public const int MaxUltimateRank = 3;

    /// <summary>Experiencia que hace falta para pasar del nivel indicado al siguiente: 100 x nivel^1,5.</summary>
    public static int XpForNextLevel(int level) => Mathf.RoundToInt(100f * Mathf.Pow(Mathf.Max(1, level), 1.5f));

    /// <summary>Experiencia extra por completar una oleada.</summary>
    public static int WaveBonusXp(int wave) => 10 + 5 * Mathf.Max(1, wave);

    /// <summary>Suma experiencia al personaje y sube de nivel las veces que haga falta. Devuelve cuántos niveles subió.</summary>
    public static int AddXp(CharacterSave character, int amount)
    {
        if (amount <= 0 || character.level >= MaxLevel) return 0;

        int gained = 0;
        character.xp += amount;

        while (character.level < MaxLevel && character.xp >= XpForNextLevel(character.level))
        {
            character.xp -= XpForNextLevel(character.level);
            character.level++;
            gained++;
        }

        if (character.level >= MaxLevel) character.xp = 0;
        return gained;
    }

    /// <summary>Rango máximo que permite el nivel del personaje. Normales: hasta 5 con tope ceil(nivel/2). Definitiva: niveles 6, 12 y 18.</summary>
    public static int RankCapForLevel(AbilityKind kind, int level)
    {
        if (kind == AbilityKind.Ultimate) return level >= 18 ? 3 : level >= 12 ? 2 : level >= 6 ? 1 : 0;
        return Mathf.Min(MaxNormalRank, (Mathf.Max(1, level) + 1) / 2);
    }

    public static int MaxRank(AbilityKind kind) => kind == AbilityKind.Ultimate ? MaxUltimateRank : MaxNormalRank;

    /// <summary>Puntos ya gastados: uno por rango de habilidad y por cada nivel de sangrado sobre el 1.º (gratis).</summary>
    public static int SpentPoints(CharacterSave character)
    {
        int spent = character.bleedLevel - 1;
        foreach (int rank in character.abilityRanks) spent += rank;
        return spent;
    }

    /// <summary>Un punto por nivel, menos los gastados.</summary>
    public static int PointsAvailable(CharacterSave character) => Mathf.Max(0, character.level - SpentPoints(character));

    public static UpgradeBlock CanUpgradeAbility(CharacterSave character, int slot, AbilityKind kind)
    {
        int rank = character.abilityRanks[slot];

        if (rank >= MaxRank(kind)) return UpgradeBlock.MaxRank;
        if (rank >= RankCapForLevel(kind, character.level)) return UpgradeBlock.LevelTooLow;
        if (PointsAvailable(character) <= 0) return UpgradeBlock.NoPoints;
        return UpgradeBlock.None;
    }

    public static bool TryUpgradeAbility(CharacterSave character, int slot, AbilityKind kind)
    {
        if (CanUpgradeAbility(character, slot, kind) != UpgradeBlock.None) return false;

        character.abilityRanks[slot]++;
        return true;
    }

    public static UpgradeBlock CanUpgradeBleed(CharacterSave character)
    {
        if (character.bleedLevel >= MaxBleedLevel) return UpgradeBlock.MaxRank;
        if (PointsAvailable(character) <= 0) return UpgradeBlock.NoPoints;
        return UpgradeBlock.None;
    }

    public static bool TryUpgradeBleed(CharacterSave character)
    {
        if (CanUpgradeBleed(character) != UpgradeBlock.None) return false;

        character.bleedLevel++;
        return true;
    }

    /// <summary>Nivel mínimo del personaje para tener el rango indicado (para el aviso "Requiere nivel N").</summary>
    public static int LevelRequiredForRank(AbilityKind kind, int rank)
    {
        for (int level = 1; level <= MaxLevel; level++)
            if (RankCapForLevel(kind, level) >= rank) return level;
        return MaxLevel;
    }
}
