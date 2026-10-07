using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Reutiliza enemigos en lugar de instanciar y destruir uno por cada spawn.</summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    private readonly Dictionary<EnemyDefinition, ObjectPool<EnemyAI>> pools = new Dictionary<EnemyDefinition, ObjectPool<EnemyAI>>();

    private void Awake()
    {
        Instance = this;
        if (GetComponent<EnemyCrowd>() == null) gameObject.AddComponent<EnemyCrowd>();   // apilado de enemigos
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public EnemyAI Spawn(EnemyDefinition definition, Vector3 position, float healthScale = 1f)
    {
        if (!pools.TryGetValue(definition, out ObjectPool<EnemyAI> pool))
        {
            pool = CreatePool(definition);
            pools[definition] = pool;
        }

        EnemyAI enemy = pool.Get();
        enemy.Spawn(position, healthScale);
        return enemy;
    }

    private ObjectPool<EnemyAI> CreatePool(EnemyDefinition definition)
    {
        ObjectPool<EnemyAI> pool = null;
        pool = new ObjectPool<EnemyAI>(
            createFunc: () =>
            {
                GameObject instance = Instantiate(definition.prefab, transform);
                EnemyAI enemy = instance.GetComponent<EnemyAI>();
                enemy.Init(pool, definition);
                return enemy;
            },
            actionOnGet: enemy => enemy.gameObject.SetActive(true),
            actionOnRelease: enemy => enemy.gameObject.SetActive(false),
            actionOnDestroy: enemy => Destroy(enemy.gameObject));
        return pool;
    }
}
