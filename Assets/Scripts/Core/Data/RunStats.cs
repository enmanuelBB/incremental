using UnityEngine;

/// <summary>Por qué terminó la partida.</summary>
public enum GameOverCause
{
    PlayerDied,
    BaseDestroyed
}

/// <summary>
/// Lo que pasó durante una run (desde que empieza la primera oleada hasta que se pierde). Se va llenando con los
/// eventos del juego y al terminar alimenta la pantalla de resumen. Lógica pura, sin escena, para poder probarla.
/// </summary>
public class RunStats
{
    private float startedAt;

    public int StartLevel { get; private set; } = 1;
    public int EndLevel { get; private set; } = 1;
    public int WaveReached { get; private set; }
    public int EnemiesKilled { get; private set; }
    public int MoneyGained { get; private set; }
    public int XpGained { get; private set; }
    public int SkillPointsGained { get; private set; }
    public int PointsAvailable { get; private set; }
    public float SecondsSurvived { get; private set; }

    /// <summary>Niveles que subió el personaje en la run (nunca negativo, aunque el nivel se haya reiniciado con un atajo de desarrollo).</summary>
    public int LevelsGained => Mathf.Max(0, EndLevel - StartLevel);

    /// <summary>Empieza una run nueva: borra todo y recuerda el nivel inicial y el momento.</summary>
    public void Begin(int level, float now)
    {
        StartLevel = Mathf.Max(1, level);
        EndLevel = StartLevel;
        startedAt = now;
        WaveReached = 0;
        EnemiesKilled = 0;
        MoneyGained = 0;
        XpGained = 0;
        SkillPointsGained = 0;
        PointsAvailable = 0;
        SecondsSurvived = 0f;
    }

    public void WaveStarted(int wave) => WaveReached = Mathf.Max(WaveReached, wave);

    public void EnemyKilled() => EnemiesKilled++;

    public void MoneyEarned(int amount)
    {
        if (amount > 0) MoneyGained += amount;
    }

    public void XpEarned(int amount)
    {
        if (amount > 0) XpGained += amount;
    }

    public void SkillPointsEarned(int amount)
    {
        if (amount > 0) SkillPointsGained += amount;
    }

    /// <summary>Cierra la run con el nivel y los puntos sin gastar que tiene el personaje en ese momento.</summary>
    public void Finish(int level, int pointsAvailable, float now)
    {
        EndLevel = Mathf.Max(1, level);
        PointsAvailable = Mathf.Max(0, pointsAvailable);
        SecondsSurvived = Mathf.Max(0f, now - startedAt);
    }
}

/// <summary>Textos del resumen de run (separados de la pantalla para poder probarlos).</summary>
public static class RunSummaryFormat
{
    /// <summary>Minutos y segundos: 75 s = "1:15". Más de 60 minutos sigue contando minutos.</summary>
    public static string Duration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return (total / 60) + ":" + (total % 60).ToString("00");
    }

    /// <summary>"Alucard: sube del nivel 4 al 6", o solo "Alucard: nivel 4" si no subió.</summary>
    public static string Level(string characterName, int startLevel, int endLevel) =>
        endLevel > startLevel
            ? characterName + ": sube del nivel " + startLevel + " al " + endLevel
            : characterName + ": nivel " + startLevel;

    public static string Points(int amount) => amount + (amount == 1 ? " punto" : " puntos");
}
