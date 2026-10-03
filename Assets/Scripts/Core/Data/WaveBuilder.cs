using System.Collections.Generic;

/// <summary>Cuántos enemigos de un tipo faltan por aparecer en una oleada.</summary>
public class WaveGroup
{
    public EnemyDefinition Enemy;
    public int Count;
}

/// <summary>Lo que hay que spawnear en una oleada concreta.</summary>
public class WavePlan
{
    public List<WaveGroup> Groups = new List<WaveGroup>();
    public float SpawnInterval;
    public float HealthScale = 1f;
    /// <summary>Jefe o minijefe que sale al inicio de esta oleada (null si no toca).</summary>
    public EnemyDefinition Boss;
}

/// <summary>
/// Arma la composición de cada oleada a partir de un WaveSet. Es lógica pura (sin MonoBehaviour)
/// para poder probarla sin entrar a Play.
/// </summary>
public static class WaveBuilder
{
    /// <param name="waveIndex">Índice base 0 de la oleada.</param>
    public static WavePlan Build(WaveSet set, int waveIndex)
    {
        var plan = new WavePlan();
        if (set == null || set.waves == null || set.waves.Length == 0) return plan;

        if (waveIndex < set.waves.Length)
        {
            WaveSet.Wave wave = set.waves[waveIndex];
            AddGroups(plan.Groups, wave, 0);
            plan.SpawnInterval = wave.spawnInterval;
        }
        else
        {
            // Pasada la última oleada definida, se usa como plantilla y se le suman enemigos.
            int wavesPastEnd = waveIndex - set.waves.Length + 1;
            AddGroups(plan.Groups, set.waves[set.waves.Length - 1], set.extraEnemiesPerWave * wavesPastEnd);
            plan.SpawnInterval = set.infiniteSpawnInterval;
            plan.HealthScale = 1f + set.healthMultiplierPerWave * wavesPastEnd;
        }

        plan.Boss = BossFor(set, waveIndex + 1);
        return plan;
    }

    /// <summary>El jefe de una oleada (número desde 1), o null. Si hay ciclo, pasada su longitud se repite.</summary>
    public static EnemyDefinition BossFor(WaveSet set, int waveNumber)
    {
        if (set == null || set.bosses == null || waveNumber < 1) return null;

        int number = waveNumber;
        if (set.bossCycleLength > 0 && number > set.bossCycleLength) number = ((number - 1) % set.bossCycleLength) + 1;

        foreach (WaveSet.BossWave entry in set.bosses)
            if (entry != null && entry.wave == number && entry.boss != null) return entry.boss;
        return null;
    }

    /// <summary>Suma los grupos extra a los base, juntando los del mismo tipo de enemigo.</summary>
    public static List<WaveGroup> Merge(List<WaveGroup> baseGroups, List<WaveGroup> extraGroups)
    {
        var result = new List<WaveGroup>();

        foreach (WaveGroup g in baseGroups)
            result.Add(new WaveGroup { Enemy = g.Enemy, Count = g.Count });

        foreach (WaveGroup extra in extraGroups)
        {
            WaveGroup existing = result.Find(r => r.Enemy == extra.Enemy);
            if (existing != null) existing.Count += extra.Count;
            else result.Add(new WaveGroup { Enemy = extra.Enemy, Count = extra.Count });
        }

        return result;
    }

    private static void AddGroups(List<WaveGroup> target, WaveSet.Wave wave, int extraPerGroup)
    {
        if (wave.enemyGroups == null) return;

        foreach (WaveSet.EnemyGroup g in wave.enemyGroups)
            target.Add(new WaveGroup { Enemy = g.enemy, Count = g.count + extraPerGroup });
    }
}
