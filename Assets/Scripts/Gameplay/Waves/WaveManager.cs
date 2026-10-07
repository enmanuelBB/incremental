using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ejecuta las oleadas: spawnea enemigos, cuenta cuántos siguen vivos y avisa por GameEvents.
/// Qué enemigos tiene cada oleada lo decide WaveBuilder a partir del WaveSet.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [SerializeField] private WaveSet waveSet;
    [SerializeField, Tooltip("Zona donde aparecen los enemigos, dispersos. Si está vacía se usan los puntos de abajo")]
    private SpawnZone spawnZone;
    [SerializeField, Tooltip("Puntos fijos de aparición (se usan solo si no hay zona)")]
    private Transform[] spawnPoints;

    [Header("Ritmo")]
    [SerializeField] private float timeBetweenWaves = 5f;

    public bool HasStarted { get; private set; }

    private int currentWave;          // índice base 0
    private int enemiesAlive;
    private bool waveActive;
    private bool allSpawned;
    private Coroutine waveRoutine;

    // Orden de aparición de la oleada actual y cuántos ya salieron (si se salta la oleada, el resto pasa a la siguiente)
    private List<EnemyDefinition> spawnOrder;
    private int spawnedCount;
    private readonly List<WaveGroup> pendingCarryOver = new List<WaveGroup>();
    private BaseHealth baseTarget;

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
        bool hasSpawn = spawnZone != null || (spawnPoints != null && spawnPoints.Length > 0);
        if (waveSet == null || waveSet.waves == null || waveSet.waves.Length == 0 || !hasSpawn)
        {
            Debug.LogError("WaveManager necesita un WaveSet con al menos una oleada y una zona o un punto de spawn.", this);
            return;
        }

        HasStarted = true;
        GameEvents.RaiseGameStarted();
        waveRoutine = StartCoroutine(StartWave());
    }

    private void OnGameOver(string message, GameOverCause cause)
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

        // El jefe sale al inicio de su oleada, por el centro de la zona; los enemigos normales son su escolta.
        if (plan.Boss != null) SpawnBoss(plan.Boss, plan.HealthScale);

        // Salen en manadas mixtas: cada intervalo, packRows filas de frente a la base, de packRowMin a packRowMax enemigos cada una.
        spawnOrder = WaveBuilder.Interleave(WaveBuilder.Merge(plan.Groups, pendingCarryOver));
        spawnedCount = 0;
        pendingCarryOver.Clear();

        while (spawnedCount < spawnOrder.Count)
        {
            int[] rows = PackFormation.RowWidths(waveSet.packRows, waveSet.packRowMin, waveSet.packRowMax,
                spawnOrder.Count - spawnedCount, () => Random.value);
            int size = 0;
            foreach (int width in rows) size += width;

            Vector3 center = PackCenter();
            Vector3 forward = TowardBase(center);
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            for (int i = 0; i < size; i++)
            {
                Vector2 slot = PackFormation.Slot(i, rows, waveSet.packSpacing);
                Vector3 position = center + right * slot.x - forward * slot.y;   // y = hacia atrás
                SpawnEnemy(spawnOrder[spawnedCount], plan.HealthScale, position);
                spawnedCount++;
            }

            if (spawnedCount < spawnOrder.Count) yield return new WaitForSeconds(plan.SpawnInterval);
        }

        // Si el jugador mató todo antes de terminar de spawnear, la oleada se cierra recién aquí.
        allSpawned = true;
        TryCompleteWave();
    }

    private void SpawnBoss(EnemyDefinition boss, float healthScale)
    {
        Vector3 position = spawnZone != null ? spawnZone.CenterPoint() : spawnPoints[spawnPoints.Length / 2].position;

        EnemyPool.Instance.Spawn(boss, position, healthScale);
        enemiesAlive++;
    }

    /// <summary>Un enemigo que aparece por una habilidad de jefe (invocar): cuenta para cerrar la oleada.</summary>
    public void RegisterSummoned() => enemiesAlive++;

    // Punto al azar de la zona (o uno de los puntos fijos) alrededor del cual sale la próxima manada.
    private Vector3 PackCenter()
    {
        if (spawnZone != null) return spawnZone.RandomPoint();
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }

    // Dirección (en el plano) desde la manada hacia la base: las filas quedan de frente a ella.
    private Vector3 TowardBase(Vector3 from)
    {
        if (baseTarget == null) baseTarget = FindFirstObjectByType<BaseHealth>();
        Vector3 direction = baseTarget != null ? baseTarget.transform.position - from : Vector3.back;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.back;
    }

    private void SpawnEnemy(EnemyDefinition enemy, float healthScale, Vector3 position)
    {
        if (spawnZone != null) position = spawnZone.OnNavMesh(position);

        EnemyPool.Instance.Spawn(enemy, position, healthScale);
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

    /// <summary>Salta a la siguiente oleada; lo que no alcanzó a spawnear pasa a la siguiente.</summary>
    public void SkipWave()
    {
        if (!HasStarted || GameState.IsGameOver) return;

        if (waveActive && !allSpawned && spawnOrder != null)
            foreach (WaveGroup group in WaveBuilder.CountRemaining(spawnOrder, spawnedCount))
                AddToPending(group.Enemy, group.Count);

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
