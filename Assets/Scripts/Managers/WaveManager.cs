using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [System.Serializable]
    public class EnemyGroup
    {
        public GameObject enemyPrefab;
        public int count;
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName;
        public EnemyGroup[] enemyGroups;
        public float spawnInterval;
    }

    [SerializeField] private Wave[] waves;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Hordas infinitas (después de la última definida)")]
    [SerializeField, Tooltip("Cuánto crece la vida de los enemigos por horda extra (0.15 = +15%)")]
    private float healthMultiplierPerWave = 0.15f;
    [SerializeField] private int extraEnemiesPerWave = 2;
    [SerializeField] private float infiniteSpawnInterval = 1f;

    [Header("Ritmo")]
    [SerializeField] private float timeBetweenWaves = 5f;

    public bool HasStarted { get; private set; }

    private int currentWave;          // índice base 0
    private int enemiesAlive;
    private bool waveActive;
    private bool allSpawned;
    private Coroutine waveRoutine;

    // Para poder calcular lo que falta por spawnear si se salta la horda
    private List<EnemyGroup> currentGroups;
    private int currentGroupIndex;
    private int currentSpawnCountInGroup;
    private readonly List<EnemyGroup> pendingCarryOver = new List<EnemyGroup>();

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
        if (waves == null || waves.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("WaveManager necesita al menos una horda y un punto de spawn.", this);
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

        List<EnemyGroup> baseGroups;
        float interval;
        float healthScale = 1f;

        if (currentWave < waves.Length)
        {
            Wave wave = waves[currentWave];
            baseGroups = new List<EnemyGroup>();
            foreach (EnemyGroup g in wave.enemyGroups)
                baseGroups.Add(new EnemyGroup { enemyPrefab = g.enemyPrefab, count = g.count });
            interval = wave.spawnInterval;
        }
        else
        {
            int wavesPastEnd = currentWave - waves.Length + 1;
            baseGroups = GenerateInfiniteWave(wavesPastEnd);
            interval = infiniteSpawnInterval;
            healthScale = 1f + healthMultiplierPerWave * wavesPastEnd;
        }

        currentGroups = MergeGroups(baseGroups, pendingCarryOver);
        pendingCarryOver.Clear();

        for (currentGroupIndex = 0; currentGroupIndex < currentGroups.Count; currentGroupIndex++)
        {
            EnemyGroup group = currentGroups[currentGroupIndex];

            for (currentSpawnCountInGroup = 0; currentSpawnCountInGroup < group.count; currentSpawnCountInGroup++)
            {
                SpawnEnemy(group.enemyPrefab, healthScale);
                yield return new WaitForSeconds(interval);
            }
        }

        // Si el jugador mató todo antes de terminar de spawnear, la horda se cierra recién aquí.
        allSpawned = true;
        TryCompleteWave();
    }

    // Usa la última horda definida como plantilla y le suma enemigos.
    private List<EnemyGroup> GenerateInfiniteWave(int wavesPastEnd)
    {
        List<EnemyGroup> generated = new List<EnemyGroup>();
        Wave template = waves[waves.Length - 1];

        foreach (EnemyGroup g in template.enemyGroups)
        {
            generated.Add(new EnemyGroup
            {
                enemyPrefab = g.enemyPrefab,
                count = g.count + extraEnemiesPerWave * wavesPastEnd
            });
        }

        return generated;
    }

    private List<EnemyGroup> MergeGroups(List<EnemyGroup> baseGroups, List<EnemyGroup> extraGroups)
    {
        List<EnemyGroup> result = new List<EnemyGroup>();

        foreach (EnemyGroup g in baseGroups)
            result.Add(new EnemyGroup { enemyPrefab = g.enemyPrefab, count = g.count });

        foreach (EnemyGroup extra in extraGroups)
        {
            EnemyGroup existing = result.Find(r => r.enemyPrefab == extra.enemyPrefab);
            if (existing != null) existing.count += extra.count;
            else result.Add(new EnemyGroup { enemyPrefab = extra.enemyPrefab, count = extra.count });
        }

        return result;
    }

    private void SpawnEnemy(GameObject prefab, float healthScale)
    {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        EnemyPool.Instance.Spawn(prefab.GetComponent<EnemyAI>(), spawnPoint.position + randomOffset, healthScale);
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
                EnemyGroup group = currentGroups[i];
                int spawnedInThisGroup = i == currentGroupIndex ? currentSpawnCountInGroup + 1 : 0;
                int remaining = group.count - spawnedInThisGroup;
                if (remaining > 0) AddToPending(group.enemyPrefab, remaining);
            }
        }

        StopAllCoroutines();

        if (waveActive) currentWave++;
        waveRoutine = StartCoroutine(StartWave());
    }

    private void AddToPending(GameObject prefab, int count)
    {
        EnemyGroup existing = pendingCarryOver.Find(g => g.enemyPrefab == prefab);
        if (existing != null) existing.count += count;
        else pendingCarryOver.Add(new EnemyGroup { enemyPrefab = prefab, count = count });
    }
}
