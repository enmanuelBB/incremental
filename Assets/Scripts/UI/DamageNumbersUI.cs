using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Números de daño sobre los enemigos. El golpe directo (balas, espada, habilidades) sale grande, con un "pop", rojo
/// que pasa a blanco; los ticks de sangrado, quemadura y veneno salen más chicos y de su color. Cada número queda
/// anclado al punto del golpe (no se desliza al girar la cámara), sube frenando y se desvanece. La animación está en
/// DamageNumberStyle; el aspecto (sombra, relieve, textura; sin borde) en el material asignado.
/// </summary>
public class DamageNumbersUI : MonoBehaviour
{
    private static readonly Color BleedColor = new Color(0.95f, 0.15f, 0.15f);
    private static readonly Color BurnColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color PoisonColor = new Color(0.45f, 1f, 0.3f);

    [SerializeField, Tooltip("Material del texto (sombra, relieve y textura). Vacío = el de la fuente")]
    private Material numberMaterial;
    [SerializeField] private float hitFontSize = 36f;
    [SerializeField] private float tickFontSize = 24f;
    [SerializeField, Tooltip("Cuántos números puede haber a la vez; pasado el tope se reutiliza el más viejo")]
    private int maxNumbers = 90;
    [SerializeField, Tooltip("Cuánto se separan al azar del punto del golpe, en metros (para que no se tapen)")]
    private float jitter = 0.35f;

    private class Number
    {
        public RectTransform Rect;
        public TMP_Text Text;
        public Vector3 World;
        public Vector3 Drift;
        public float Age;
        public float Size;
        public bool IsHit;
        public Color TickColor;
    }

    private readonly List<Number> active = new List<Number>();
    private readonly Stack<Number> free = new Stack<Number>();
    private RectTransform container;

    private void Awake()
    {
        Transform canvas = UiKit.FindCanvas();
        if (canvas == null) return;

        container = UiKit.Rect("DamageNumbers", canvas);
        UiKit.Stretch(container);
        container.SetAsFirstSibling(); // detrás del HUD y de los menús
    }

    private void OnEnable()
    {
        GameEvents.EnemyHit += OnEnemyHit;
        GameEvents.BleedTick += OnBleedTick;
        GameEvents.BurnTick += OnBurnTick;
        GameEvents.PoisonTick += OnPoisonTick;
    }

    private void OnDisable()
    {
        GameEvents.EnemyHit -= OnEnemyHit;
        GameEvents.BleedTick -= OnBleedTick;
        GameEvents.BurnTick -= OnBurnTick;
        GameEvents.PoisonTick -= OnPoisonTick;
    }

    private void OnEnemyHit(Vector3 world, int damage) => Spawn(world, damage, true, Color.white);
    private void OnBleedTick(Vector3 world, int damage) => Spawn(world, damage, false, BleedColor);
    private void OnBurnTick(Vector3 world, int damage) => Spawn(world, damage, false, BurnColor);
    private void OnPoisonTick(Vector3 world, int damage) => Spawn(world, damage, false, PoisonColor);

    private void Spawn(Vector3 world, int damage, bool isHit, Color tickColor)
    {
        if (container == null) return;

        Number number;
        if (active.Count >= maxNumbers)
        {
            number = active[0];
            active.RemoveAt(0);
        }
        else
        {
            number = free.Count > 0 ? free.Pop() : Create();
        }

        Vector2 offset = Random.insideUnitCircle * jitter;
        number.World = world + new Vector3(offset.x, Random.Range(0f, jitter), offset.y);
        number.Drift = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
        number.Age = 0f;
        number.IsHit = isHit;
        number.TickColor = tickColor;
        number.Size = (isHit ? hitFontSize : tickFontSize) * DamageNumberStyle.SizeFor(damage);
        number.Text.text = damage.ToString();
        number.Text.fontSize = number.Size;
        number.Rect.SetAsLastSibling(); // el más nuevo, encima
        number.Rect.gameObject.SetActive(true);
        active.Add(number);
        Place(number, Camera.main);
    }

    private Number Create()
    {
        TMP_Text text = UiKit.Label("Damage", container, "", hitFontSize, TextAlignmentOptions.Center, Color.white);
        if (numberMaterial != null) text.fontSharedMaterial = numberMaterial;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.extraPadding = true;   // la sombra del material no se recorta
        text.rectTransform.sizeDelta = new Vector2(300f, 100f);

        return new Number { Rect = text.rectTransform, Text = text };
    }

    private void LateUpdate()
    {
        if (active.Count == 0) return;

        Camera cam = Camera.main;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Number number = active[i];
            number.Age += Time.deltaTime;

            if (number.Age >= DamageNumberStyle.Duration)
            {
                number.Rect.gameObject.SetActive(false);
                active.RemoveAt(i);
                free.Push(number);
                continue;
            }

            Place(number, cam);
        }
    }

    // El canvas es Screen Space Overlay: la posición del texto es la de pantalla.
    private static void Place(Number number, Camera cam)
    {
        float t = number.Age;
        Color color = number.IsHit ? DamageNumberStyle.HitColor(t) : number.TickColor;
        color.a = DamageNumberStyle.Alpha(t);

        Vector3 world = number.World + Vector3.up * DamageNumberStyle.Rise(t) + number.Drift * (t / DamageNumberStyle.Duration);
        Vector3 screen = cam != null ? cam.WorldToScreenPoint(world) : Vector3.back;
        if (screen.z <= 0f) color.a = 0f; // detrás de la cámara

        number.Rect.position = new Vector3(screen.x, screen.y, 0f);
        number.Rect.localScale = Vector3.one * DamageNumberStyle.Scale(t);
        number.Text.color = color;
    }
}
