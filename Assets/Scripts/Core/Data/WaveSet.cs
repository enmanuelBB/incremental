using System;
using UnityEngine;

/// <summary>
/// Hordas de una partida. Las primeras se definen a mano; después se generan solas tomando la
/// última como plantilla. Se crea desde Assets > Create > Game > Wave Set.
/// </summary>
[CreateAssetMenu(fileName = "NewWaveSet", menuName = "Game/Wave Set")]
public class WaveSet : GameDefinition
{
    [Serializable]
    public class EnemyGroup
    {
        public EnemyDefinition enemy;
        public int count;
    }

    [Serializable]
    public class Wave
    {
        public string waveName;
        public EnemyGroup[] enemyGroups;
        public float spawnInterval;
    }

    public Wave[] waves;

    [Header("Hordas infinitas (después de la última definida)")]
    [Tooltip("Cuánto crece la vida de los enemigos por horda extra (0.15 = +15%)")]
    public float healthMultiplierPerWave = 0.15f;
    public int extraEnemiesPerWave = 2;
    public float infiniteSpawnInterval = 1f;
}
