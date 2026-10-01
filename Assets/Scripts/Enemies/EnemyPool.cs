using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Reutiliza enemigos en lugar de instanciar y destruir uno por cada spawn.</summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    private readonly Dictionary<EnemyAI, ObjectPool<EnemyAI>> pools = new Dictionary<EnemyAI, ObjectPool<EnemyAI>>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public EnemyAI Spawn(EnemyAI prefab, Vector3 position, float healthScale = 1f)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool<EnemyAI> pool))
        {
            pool = CreatePool(prefab);
            pools[prefab] = pool;
        }

        EnemyAI enemy = pool.Get();
        enemy.Spawn(position, healthScale);
        return enemy;
    }

    private ObjectPool<EnemyAI> CreatePool(EnemyAI prefab)
    {
        ObjectPool<EnemyAI> pool = null;
        pool = new ObjectPool<EnemyAI>(
            createFunc: () =>
            {
                EnemyAI enemy = Instantiate(prefab, transform);
                enemy.Init(pool);
                return enemy;
            },
            actionOnGet: enemy => enemy.gameObject.SetActive(true),
            actionOnRelease: enemy => enemy.gameObject.SetActive(false),
            actionOnDestroy: enemy => Destroy(enemy.gameObject));
        return pool;
    }
}
