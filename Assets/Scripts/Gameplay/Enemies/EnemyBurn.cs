using UnityEngine;

/// <summary>
/// Quemadura de un enemigo: aplica el daño de cada tick, avisa a la UI (número naranja) y tiñe al enemigo.
/// Un solo Update por enemigo que arde, sin corrutinas. EnemyAI lo agrega solo la primera vez que hace falta.
/// Nota: usa el mismo MaterialPropertyBlock de color que EnemyBleed; Guts no sangra, así que no chocan en la práctica.
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class EnemyBurn : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color BurnColor = new Color(1f, 0.45f, 0.05f);

    [SerializeField, Range(0f, 1f), Tooltip("Cuánto se acerca al naranja mientras arde")]
    private float tintAmount = 0.6f;

    private readonly BurnState burn = new BurnState();
    private EnemyAI enemy;
    private Collider body;
    private Renderer[] renderers;
    private Color[] baseColors;
    private MaterialPropertyBlock block;
    private bool tinted;

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

    public bool IsBurning => burn.IsBurning;

    /// <summary>Quema o renueva la quemadura (conserva el mayor daño por tick).</summary>
    public void Apply(float seconds, int damagePerTick, float tickSeconds)
    {
        burn.Apply(seconds, damagePerTick, tickSeconds);
        if (burn.IsBurning) SetTint(true);
    }

    /// <summary>Apaga la quemadura y devuelve el color original (al morir o al reutilizar del pool).</summary>
    public void Clear()
    {
        burn.Clear();
        SetTint(false);
    }

    private void Update()
    {
        if (!burn.IsBurning || GameState.IsGameOver) return;

        int ticks = burn.Advance(Time.deltaTime);
        int damage = burn.DamagePerTick;
        for (int i = 0; i < ticks && !enemy.IsDead; i++)
        {
            // El número se publica ANTES de dañar: si el tick mata, el enemigo vuelve al pool y ya no habría dónde mostrarlo.
            GameEvents.RaiseBurnTick(HeadPosition(), damage);
            enemy.TakeDamage(damage);
        }

        if (!burn.IsBurning) SetTint(false);
    }

    private Vector3 HeadPosition()
    {
        float top = body != null ? body.bounds.max.y : transform.position.y + 1f;
        return new Vector3(transform.position.x, top + 0.25f, transform.position.z);
    }

    private void SetTint(bool on)
    {
        if (renderers == null || tinted == on) return;
        tinted = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!on)
            {
                renderers[i].SetPropertyBlock(null);
                continue;
            }

            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(baseColors[i], BurnColor, tintAmount));
            renderers[i].SetPropertyBlock(block);
        }
    }
}
