using UnityEngine;

/// <summary>
/// Veneno de un enemigo (campo de flores de Frieren): aplica el daño de cada tick, avisa a la UI (número verde) y tiñe al enemigo.
/// Misma lógica que la quemadura (BurnState: renovar y conservar el mayor daño). EnemyAI lo agrega la primera vez que hace falta.
/// Nota: usa el mismo MaterialPropertyBlock de color que EnemyBurn y EnemyBleed; Frieren no quema ni hace sangrar, así que no chocan.
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class EnemyPoison : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color PoisonColor = new Color(0.35f, 0.9f, 0.2f);

    [SerializeField, Range(0f, 1f), Tooltip("Cuánto se acerca al verde mientras está envenenado")]
    private float tintAmount = 0.6f;

    private readonly BurnState poison = new BurnState();
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

    public bool IsPoisoned => poison.IsBurning;

    /// <summary>Envenena o renueva el veneno (conserva el mayor daño por tick).</summary>
    public void Apply(float seconds, int damagePerTick, float tickSeconds)
    {
        poison.Apply(seconds, damagePerTick, tickSeconds);
        if (poison.IsBurning) SetTint(true);
    }

    /// <summary>Quita el veneno y devuelve el color original (al morir o al reutilizar del pool).</summary>
    public void Clear()
    {
        poison.Clear();
        SetTint(false);
    }

    private void Update()
    {
        if (!poison.IsBurning || GameState.IsGameOver) return;

        int ticks = poison.Advance(Time.deltaTime);
        int damage = poison.DamagePerTick;
        for (int i = 0; i < ticks && !enemy.IsDead; i++)
        {
            // El número se publica ANTES de dañar: si el tick mata, el enemigo vuelve al pool y ya no habría dónde mostrarlo.
            GameEvents.RaisePoisonTick(HeadPosition(), damage);
            enemy.TakeTickDamage(damage);
        }

        if (!poison.IsBurning) SetTint(false);
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
            block.SetColor(BaseColorId, Color.Lerp(baseColors[i], PoisonColor, tintAmount));
            renderers[i].SetPropertyBlock(block);
        }
    }
}
