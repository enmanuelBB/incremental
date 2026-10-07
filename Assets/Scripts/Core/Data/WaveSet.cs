using System;
using UnityEngine;

/// <summary>
/// Oleadas de una partida. Las primeras se definen a mano; después se generan solas tomando la
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
        [Tooltip("Segundos entre una manada y la siguiente")]
        public float spawnInterval;
    }

    [Serializable]
    public class BossWave
    {
        [Tooltip("Número de oleada (desde 1) en la que sale este jefe")]
        public int wave;
        public EnemyDefinition boss;
    }

    public Wave[] waves;

    [Header("Manadas")]
    [Tooltip("Filas de cada manada (de frente a la base)")]
    public int packRows = 2;
    [Tooltip("Enemigos por fila, al azar entre mínimo y máximo (cada fila por su cuenta)")]
    public int packRowMin = 3;
    public int packRowMax = 5;
    [Tooltip("Metros entre un enemigo y el de al lado (y entre filas)")]
    public float packSpacing = 2.5f;

    [Header("Jefes")]
    [Tooltip("Jefes y minijefes y la oleada en que salen. Salen al inicio de la oleada, junto con los enemigos normales")]
    public BossWave[] bosses = new BossWave[0];
    [Tooltip("Pasado este número de oleada el ciclo de jefes se repite (20: en la 25 sale el de la 5). 0 = no se repite")]
    public int bossCycleLength = 20;

    [Header("Oleadas infinitas (después de la última definida)")]
    [Tooltip("Cuánto crece la vida de los enemigos por oleada extra (0.15 = +15%)")]
    public float healthMultiplierPerWave = 0.15f;
    public int extraEnemiesPerWave = 2;
    public float infiniteSpawnInterval = 1f;
}
