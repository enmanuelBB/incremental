using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de Furia de Guts. Es un clon de la barra de vida colocado en el contenedor de barras del HUD
/// (PlayerHud/Bars), debajo de la de XP, como hace XpBarUI. Solo se ve con un personaje que tenga Furia
/// (el evento trae máximo 0 para los demás). Llena, el texto pasa a "¡FURIA!" y el relleno pulsa.
/// </summary>
public class FuryBarUI : MonoBehaviour
{
    private static readonly Color FillColor = new Color(0.8f, 0.08f, 0.08f, 1f);
    private static readonly Color ReadyColor = new Color(1f, 0.38f, 0.28f, 1f);
    private const float BarHeight = 22f;
    private const float TextSize = 17f;
    private const float PulseSpeed = 8f;

    private GameObject bar;
    private Slider slider;
    private Image fill;
    private TMP_Text text;
    private bool ready;

    private void Awake()
    {
        Transform canvas = UiKit.FindCanvas();
        if (canvas == null) return;

        BuildBar(canvas);
    }

    private void Start()
    {
        // Ya existen todas las barras: la de Furia queda debajo de la de XP.
        if (bar == null) return;

        Transform xp = bar.transform.parent.Find("XpBar");
        if (xp != null) xp.SetAsLastSibling();
        bar.transform.SetAsLastSibling();
    }

    private void BuildBar(Transform canvas)
    {
        Transform bars = canvas.Find("PlayerHud/Bars");
        Transform healthBar = bars != null ? bars.Find("PlayerHealthBar") : null;
        if (healthBar == null)
        {
            Debug.LogWarning("FuryBarUI: no se encontró PlayerHud/Bars/PlayerHealthBar; no se crea la barra de Furia.");
            return;
        }

        bar = Instantiate(healthBar.gameObject, bars);
        bar.name = "FuryBar";
        bar.SetActive(false); // se muestra al llegar un máximo mayor que 0
        bar.transform.SetAsLastSibling();

        slider = bar.GetComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        Transform fillTransform = bar.transform.Find("Fill Area/Fill");
        if (fillTransform != null)
        {
            fill = fillTransform.GetComponent<Image>();
            fill.color = FillColor;
        }

        LayoutElement layout = bar.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minHeight = BarHeight; // la barra de vida trae un mínimo de 30 que se copia con el clon
            layout.preferredHeight = BarHeight;
        }

        Transform original = bar.transform.Find("PlayerHealthText");
        text = original.GetComponent<TMP_Text>();
        text.name = "FuryText";
        text.fontSize = TextSize;
        text.alignment = TextAlignmentOptions.Center;
        text.outlineWidth = 0.22f;
        text.outlineColor = new Color32(30, 4, 4, 255);
        text.raycastTarget = false;
    }

    private void OnEnable() => GameEvents.FuryChanged += OnFuryChanged;
    private void OnDisable() => GameEvents.FuryChanged -= OnFuryChanged;

    private void OnFuryChanged(float current, float max)
    {
        if (bar == null) return;

        bool hasFury = max > 0f;
        bar.SetActive(hasFury);
        if (!hasFury)
        {
            ready = false;
            return;
        }

        slider.value = current / max;
        ready = current >= max;
        text.text = ready ? "¡FURIA!" : "FURIA  " + Mathf.FloorToInt(current) + " / " + Mathf.FloorToInt(max);
        if (!ready && fill != null) fill.color = FillColor;
    }

    // Con la barra llena el relleno pulsa entre dos rojos (tiempo real: no se detiene con la cámara lenta).
    private void Update()
    {
        if (!ready || fill == null) return;

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PulseSpeed);
        fill.color = Color.Lerp(FillColor, ReadyColor, wave);
    }
}
