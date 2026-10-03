using UnityEngine;

/// <summary>
/// Va anotando lo que pasa durante la run (oleada, bajas, dinero, experiencia, puntos del árbol) escuchando los
/// eventos del juego, y entrega el resumen al terminar la partida. La cuenta vive en RunStats (lógica pura).
/// </summary>
public class RunSummaryTracker : MonoBehaviour
{
    public static RunSummaryTracker Instance { get; private set; }

    public RunStats Stats { get; } = new RunStats();

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
        GameEvents.GameStarted += OnGameStarted;
        GameEvents.WaveStarted += OnWaveStarted;
        GameEvents.EnemyKilled += OnEnemyKilled;
        GameEvents.MoneyGained += OnMoneyGained;
        GameEvents.XpEarned += OnXpEarned;
        GameEvents.SkillPointsGained += OnSkillPointsGained;
    }

    private void OnDisable()
    {
        GameEvents.GameStarted -= OnGameStarted;
        GameEvents.WaveStarted -= OnWaveStarted;
        GameEvents.EnemyKilled -= OnEnemyKilled;
        GameEvents.MoneyGained -= OnMoneyGained;
        GameEvents.XpEarned -= OnXpEarned;
        GameEvents.SkillPointsGained -= OnSkillPointsGained;
    }

    private static CharacterSave ActiveSave => ProgressionManager.Instance != null ? ProgressionManager.Instance.ActiveSave : null;

    private void OnGameStarted()
    {
        CharacterSave save = ActiveSave;
        Stats.Begin(save != null ? save.level : 1, Time.time);
    }

    private void OnWaveStarted(int wave) => Stats.WaveStarted(wave);
    private void OnEnemyKilled(int reward) => Stats.EnemyKilled();
    private void OnMoneyGained(int amount) => Stats.MoneyEarned(amount);
    private void OnXpEarned(int amount) => Stats.XpEarned(amount);
    private void OnSkillPointsGained(string characterName, int amount) => Stats.SkillPointsEarned(amount);

    /// <summary>Cierra la run con el estado actual del personaje y devuelve el resumen.</summary>
    public RunStats Finish()
    {
        CharacterSave save = ActiveSave;
        Stats.Finish(save != null ? save.level : 1, save != null ? Progression.PointsAvailable(save) : 0, Time.time);
        return Stats;
    }
}
