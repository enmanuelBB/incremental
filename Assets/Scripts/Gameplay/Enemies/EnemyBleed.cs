using UnityEngine;

/// <summary>
/// Sangrado de un enemigo: aplica el daño de cada tick, avisa a la UI (número rojo) y tiñe al enemigo.
/// Un solo Update por enemigo que sangra, sin corrutinas. EnemyAI lo agrega solo la primera vez que hace falta.
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class EnemyBleed : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color BleedColor = new Color(0.85f, 0.04f, 0.04f);

    [SerializeField, Range(0f, 1f), Tooltip("Cuánto se acerca al rojo con las pilas al tope")]
    private float maxTint = 0.7f;

    private readonly BleedStacks stacks = new BleedStacks();
    private EnemyAI enemy;
    private Collider body;
    private Renderer[] renderers;
    private Color[] baseColors;
    private MaterialPropertyBlock block;
    private int damagePerStack = 1;

    public int Stacks => stacks.Stacks;

    private void Awake()
    {
        enemy = GetComponent<EnemyAI>();
        body = GetComponent<Collider>();
        block = new MaterialPropertyBlock();

        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            baseColors[i] = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
        }
    }

    /// <summary>Suma pilas (hasta el tope). El daño por pila es el mayor que haya recibido este enemigo.</summary>
    public void Apply(int amount, int cap, int perStack)
    {
        int added = stacks.Add(amount, cap);
        int previous = damagePerStack;
        damagePerStack = Mathf.Max(damagePerStack, perStack);

        if (added > 0 || damagePerStack != previous) UpdateTint();
    }

    /// <summary>Quita todo el sangrado y devuelve el color original (al morir o al reutilizar del pool).</summary>
    public void Clear()
    {
        stacks.Clear();
        damagePerStack = 1;
        if (renderers == null) return;

        foreach (Renderer rend in renderers) rend.SetPropertyBlock(null);
    }

    private void Update()
    {
        if (!stacks.IsBleeding || GameState.IsGameOver) return;

        int ticks = stacks.Advance(Time.deltaTime);
        for (int i = 0; i < ticks && stacks.IsBleeding; i++)
        {
            int damage = stacks.TickDamage(damagePerStack);

            // El número se publica ANTES de dañar: si el tick mata, el enemigo vuelve al pool y ya no habría dónde mostrarlo.
            GameEvents.RaiseBleedTick(HeadPosition(), damage);
            enemy.TakeDamage(damage);
        }
    }

    private Vector3 HeadPosition()
    {
        float top = body != null ? body.bounds.max.y : transform.position.y + 1f;
        return new Vector3(transform.position.x, top + 0.25f, transform.position.z);
    }

    private void UpdateTint()
    {
        float amount = stacks.Intensity * maxTint;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(baseColors[i], BleedColor, amount));
            renderers[i].SetPropertyBlock(block);
        }
    }
}
