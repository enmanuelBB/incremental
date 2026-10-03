using UnityEngine;

/// <summary>
/// Experiencia y nivel del personaje activo. Escucha la experiencia que dan los enemigos y las oleadas completadas,
/// la suma al guardado del personaje (con las reglas de Progression) y avisa a la interfaz.
/// Cada personaje conserva su propio nivel entre partidas.
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    private CharacterDefinition Current => Shooting.Instance != null ? Shooting.Instance.Character : null;

    /// <summary>Guardado del personaje activo (null si todavía no hay ninguno).</summary>
    public CharacterSave ActiveSave => Current != null ? SaveSystem.Data.GetCharacter(Current.Id) : null;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.XpGained += OnXpGained;
        GameEvents.WaveCompleted += OnWaveCompleted;
        GameEvents.CharacterChanged += OnCharacterChanged;
    }

    private void OnDisable()
    {
        GameEvents.XpGained -= OnXpGained;
        GameEvents.WaveCompleted -= OnWaveCompleted;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        SaveSystem.Save();
    }

    private void Start() => Refresh();

    private void OnCharacterChanged(CharacterDefinition character) => Refresh();

    private void OnWaveCompleted(int wave) => AddXp(Progression.WaveBonusXp(wave));

    private void OnXpGained(int amount) => AddXp(amount);

    private void AddXp(int amount)
    {
        CharacterSave save = ActiveSave;
        if (save == null) return;

        int levels = Progression.AddXp(save, amount);
        if (amount > 0) GameEvents.RaiseXpEarned(amount);
        Refresh();

        if (levels > 0)
            GameEvents.RaiseLevelUp(Current.displayName, save.level, Progression.PointsAvailable(save));
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Solo desarrollo: sube el personaje activo exactamente un nivel.</summary>
    public void DebugLevelUp()
    {
        CharacterSave save = ActiveSave;
        if (save == null || save.level >= Progression.MaxLevel) return;

        Progression.AddXp(save, Progression.XpForNextLevel(save.level) - save.xp);
        SaveSystem.Save();
        Refresh();
        GameEvents.RaiseLevelUp(Current.displayName, save.level, Progression.PointsAvailable(save));
    }

    /// <summary>Solo desarrollo: deja al personaje activo en nivel 1, sin XP, sin rangos y con el sangrado en 1.</summary>
    public void DebugResetLevel()
    {
        CharacterSave save = ActiveSave;
        if (save == null) return;

        save.level = 1;
        save.xp = 0;
        save.bleedLevel = 1;
        System.Array.Clear(save.abilityRanks, 0, save.abilityRanks.Length);

        SaveSystem.Save();
        Refresh();
        Shooting.Instance.RefreshAbilityHud();
    }
#endif

    /// <summary>Vuelve a publicar el nivel y la experiencia (al cambiar de personaje o de puntos).</summary>
    public void Refresh()
    {
        CharacterSave save = ActiveSave;
        if (save == null) return;

        bool max = save.level >= Progression.MaxLevel;
        GameEvents.RaiseXpChanged(new XpInfo
        {
            CharacterName = Current.displayName,
            Level = save.level,
            Xp = save.xp,
            XpNeeded = max ? 0 : Progression.XpForNextLevel(save.level),
            Points = Progression.PointsAvailable(save)
        });
    }
}
