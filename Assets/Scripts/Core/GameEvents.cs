using System;
using UnityEngine;

/// <summary>
/// Canal de eventos del juego. Quien produce un dato lo publica aquí y quien lo
/// necesita se suscribe (OnEnable) y se desuscribe (OnDisable), sin conocerse entre sí.
/// </summary>
public static class GameEvents
{
    public static event Action<int, int> PlayerHealthChanged;   // actual, máximo
    public static event Action<int, int> BaseHealthChanged;     // actual, máximo
    public static event Action<int> MoneyChanged;               // total
    public static event Action<int> MoneyGained;                // cantidad ganada
    public static event Action<int> WaveStarted;                // número de horda (desde 1)
    public static event Action<int> WaveCompleted;              // número de horda (desde 1)
    public static event Action<int, int, bool> AmmoChanged;     // actual, máximo, recargando
    public static event Action<int> EnemyKilled;                // recompensa en dinero
    public static event Action GameStarted;
    public static event Action<string> GameOver;
    public static event Action<string> PromptChanged;           // texto del aviso; null o vacío lo oculta
    public static event Action<CharacterDefinition> CharacterChanged;
    public static event Action<bool, string, Sprite> ResourceModeChanged; // usa maná (en vez de munición), nombre e icono de la habilidad (null si no tiene)
    public static event Action<float, float> ManaChanged;       // actual, máximo
    public static event Action<string, float> AbilityUsed;      // nombre, momento (Time.time) en que vuelve a estar lista

    public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
    public static void RaiseBaseHealthChanged(int current, int max) => BaseHealthChanged?.Invoke(current, max);
    public static void RaiseMoneyChanged(int total) => MoneyChanged?.Invoke(total);
    public static void RaiseMoneyGained(int amount) => MoneyGained?.Invoke(amount);
    public static void RaiseWaveStarted(int wave) => WaveStarted?.Invoke(wave);
    public static void RaiseWaveCompleted(int wave) => WaveCompleted?.Invoke(wave);
    public static void RaiseAmmoChanged(int current, int max, bool reloading) => AmmoChanged?.Invoke(current, max, reloading);
    public static void RaiseEnemyKilled(int reward) => EnemyKilled?.Invoke(reward);
    public static void RaiseGameStarted() => GameStarted?.Invoke();
    public static void RaiseGameOver(string message) => GameOver?.Invoke(message);
    public static void RaisePromptChanged(string message) => PromptChanged?.Invoke(message);
    public static void RaiseCharacterChanged(CharacterDefinition character) => CharacterChanged?.Invoke(character);
    public static void RaiseResourceModeChanged(bool usesMana, string abilityName, Sprite abilityIcon) => ResourceModeChanged?.Invoke(usesMana, abilityName, abilityIcon);
    public static void RaiseManaChanged(float current, float max) => ManaChanged?.Invoke(current, max);
    public static void RaiseAbilityUsed(string abilityName, float readyAt) => AbilityUsed?.Invoke(abilityName, readyAt);
}
