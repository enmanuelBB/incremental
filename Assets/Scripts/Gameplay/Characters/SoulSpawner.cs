using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Coloca un alma donde muere cada enemigo mientras el personaje activo suelte almas (Guts). Las almas van en un pool
/// y se retiran todas al cambiar de personaje o al terminar la partida. Aspecto provisional: esfera azul brillante.
/// </summary>
public class SoulSpawner : MonoBehaviour
{
    private const float SoulScale = 0.35f;
    private const float BossSoulScale = 0.7f;
    private static readonly Color SoulColor = new Color(0.45f, 0.8f, 1f, 1f);

    private ObjectPool<SoulPickup> pool;
    private readonly List<SoulPickup> active = new List<SoulPickup>();
    private Material material;

    /// <summary>Almas que hay ahora en el piso (para pruebas).</summary>
    public int ActiveCount => active.Count;

    private void Awake()
    {
        Shader shader = Shader.Find("Sprites/Default");
        material = new Material(shader) { color = SoulColor };

        pool = new ObjectPool<SoulPickup>(
            createFunc: Create,
            actionOnGet: soul => soul.gameObject.SetActive(true),
            actionOnRelease: soul => soul.gameObject.SetActive(false),
            actionOnDestroy: soul => Destroy(soul.gameObject));
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void OnEnable()
    {
        GameEvents.EnemyDied += OnEnemyDied;
        GameEvents.CharacterChanged += OnCharacterChanged;
        GameEvents.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.EnemyDied -= OnEnemyDied;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        GameEvents.GameOver -= OnGameOver;
        ReleaseAll();
    }

    private SoulPickup Create()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Soul";
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(transform);
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        return sphere.AddComponent<SoulPickup>();
    }

    private void OnEnemyDied(Vector3 position, bool isBoss)
    {
        Shooting shooting = Shooting.Instance;
        CharacterDefinition character = shooting != null ? shooting.Character : null;
        if (character == null || character.soulHealFraction <= 0f) return;

        PlayerHealth health = shooting.GetComponent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        // Mejora del árbol "Alma voraz": las almas curan un poco más.
        float fraction = character.soulHealFraction + SkillTreeManager.CurrentBonuses.SoulHealBonus;
        int heal = SoulRules.HealAmount(health.MaxHealth, fraction, isBoss, character.soulBossMultiplier);
        SoulPickup soul = pool.Get();
        soul.Init(position, heal, character.soulLifetime, character.soulPickupRadius,
            isBoss ? BossSoulScale : SoulScale, shooting.transform, health, Release);
        active.Add(soul);
    }

    private void Release(SoulPickup soul)
    {
        if (!active.Remove(soul)) return;   // ya estaba retirada
        pool.Release(soul);
    }

    private void OnCharacterChanged(CharacterDefinition character) => ReleaseAll();
    private void OnGameOver(string message, GameOverCause cause) => ReleaseAll();

    private void ReleaseAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) pool.Release(active[i]);
        active.Clear();
    }
}
