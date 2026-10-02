using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ejecuta las hordas: spawnea enemigos, cuenta cuántos siguen vivos y avisa por GameEvents.
/// Qué enemigos tiene cada horda lo decide WaveBuilder a partir del WaveSet.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] private WaveSet waveSet;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Ritmo")]
    [SerializeField] private float timeBetweenWaves = 5f;

    public bool HasStarted { get; private set; }

    private int currentWave;          // índice base 0
    private int enemiesAlive;
    private bool waveActive;
    private bool allSpawned;
    private Coroutine waveRoutine;

    // Para poder calcular lo que falta por spawnear si se salta la horda
    private List<WaveGroup> currentGroups;
    private int currentGroupIndex;
    private int currentSpawnCountInGroup;
    private readonly List<WaveGroup> pendingCarryOver = new List<WaveGroup>();

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
        GameEvents.EnemyKilled += OnEnemyKilled;
        GameEvents.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.EnemyKilled -= OnEnemyKilled;
        GameEvents.GameOver -= OnGameOver;
    }

    public void BeginGame()
    {
        if (HasStarted) return;
        if (waveSet == null || waveSet.waves == null || waveSet.waves.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("WaveManager necesita un WaveSet con al menos una horda y un punto de spawn.", this);
            return;
        }

        HasStarted = true;
        GameEvents.RaiseGameStarted();
        waveRoutine = StartCoroutine(StartWave());
    }

    private void OnGameOver(string message)
    {
        StopAllCoroutines();
        waveActive = false;
    }

    private IEnumerator StartWave()
    {
        waveActive = true;
        allSpawned = false;
        GameEvents.RaiseWaveStarted(currentWave + 1);

        WavePlan plan = WaveBuilder.Build(waveSet, currentWave);

        currentGroups = WaveBuilder.Merge(plan.Groups, pendingCarryOver);
        pendingCarryOver.Clear();

        for (currentGroupIndex = 0; currentGroupIndex < currentGroups.Count; currentGroupIndex++)
        {
            WaveGroup group = currentGroups[currentGroupIndex];

            for (currentSpawnCountInGroup = 0; currentSpawnCountInGroup < group.Count; currentSpawnCountInGroup++)
            {
                SpawnEnemy(group.Enemy, plan.HealthScale);
                yield return new WaitForSeconds(plan.SpawnInterval);
            }
        }

        // Si el jugador mató todo antes de terminar de spawnear, la horda se cierra recién aquí.
        allSpawned = true;
        TryCompleteWave();
    }

    private void SpawnEnemy(EnemyDefinition enemy, float healthScale)
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        EnemyPool.Instance.Spawn(enemy, spawnPoint.position + randomOffset, healthScale);
        enemiesAlive++;
    }

    private void OnEnemyKilled(int reward)
    {
        enemiesAlive--;
        TryCompleteWave();
    }

    private void TryCompleteWave()
    {
        if (!waveActive || !allSpawned || enemiesAlive > 0) return;

        waveActive = false;
        GameEvents.RaiseWaveCompleted(currentWave + 1);
        currentWave++;
        waveRoutine = StartCoroutine(WaitAndStartNextWave());
    }

    private IEnumerator WaitAndStartNextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        waveRoutine = StartCoroutine(StartWave());
    }

    /// <summary>Salta a la siguiente horda; lo que no alcanzó a spawnear pasa a la siguiente.</summary>
    public void SkipWave()
    {
        if (!HasStarted || GameState.IsGameOver) return;

        if (waveActive && !allSpawned && currentGroups != null)
        {
            for (int i = currentGroupIndex; i < currentGroups.Count; i++)
            {
                WaveGroup group = currentGroups[i];
                int spawnedInThisGroup = i == currentGroupIndex ? currentSpawnCountInGroup + 1 : 0;
                int remaining = group.Count - spawnedInThisGroup;
                if (remaining > 0) AddToPending(group.Enemy, remaining);
            }
        }

        StopAllCoroutines();

        if (waveActive) currentWave++;
        waveRoutine = StartCoroutine(StartWave());
    }

    private void AddToPending(EnemyDefinition enemy, int count)
    {
        WaveGroup existing = pendingCarryOver.Find(g => g.Enemy == enemy);
        if (existing != null) existing.Count += count;
        else pendingCarryOver.Add(new WaveGroup { Enemy = enemy, Count = count });
    }
}
