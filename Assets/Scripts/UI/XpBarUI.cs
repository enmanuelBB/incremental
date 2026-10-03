using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de experiencia del personaje activo (la tienen todos los personajes). Es un clon de la barra de vida
/// colocado dentro del mismo contenedor de barras del HUD (PlayerHud/Bars), debajo de las demás: así tiene el mismo
/// largo y cualquier barra que se agregue allí (maná, furia...) se apila sin taparla.
/// Izquierda: nivel. Centro: XP que falta para subir. Derecha: puntos sin gastar (solo si hay).
/// También muestra un aviso breve cuando el personaje sube de nivel.
/// </summary>
public class XpBarUI : MonoBehaviour
{
    private static readonly Color FillColor = new Color(0.98f, 0.8f, 0.12f, 1f);
    private const float BarHeight = 22f;
    private const float TextSize = 17f;
    private const float SideMargin = 12f;

    [SerializeField, Tooltip("Segundos que se ve el aviso de subida de nivel")] private float toastSeconds = 3.5f;

    private Slider slider;
    private TMP_Text centerText;
    private TMP_Text levelText;
    private TMP_Text pointsText;
    private TMP_Text toast;
    private Coroutine toastRoutine;

    private void Awake()
    {
        Transform canvas = UiKit.FindCanvas();
        if (canvas == null) return;

        BuildBar(canvas);
        BuildToast(canvas);
    }

    private void BuildBar(Transform canvas)
    {
        Transform bars = canvas.Find("PlayerHud/Bars");
        Transform healthBar = bars != null ? bars.Find("PlayerHealthBar") : null;
        if (healthBar == null)
        {
            Debug.LogWarning("XpBarUI: no se encontró PlayerHud/Bars/PlayerHealthBar; no se crea la barra de XP.");
            return;
        }

        GameObject bar = Instantiate(healthBar.gameObject, bars);
        bar.name = "XpBar";
        bar.SetActive(true);
        bar.transform.SetAsLastSibling(); // debajo de la vida y del maná

        slider = bar.GetComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        Transform fill = bar.transform.Find("Fill Area/Fill");
        if (fill != null) fill.GetComponent<Image>().color = FillColor;

        LayoutElement layout = bar.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.minHeight = BarHeight; // la barra de vida trae un mínimo de 30 que se copia con el clon
            layout.preferredHeight = BarHeight;
        }

        Transform original = bar.transform.Find("PlayerHealthText");
        centerText = original.GetComponent<TMP_Text>();
        centerText.name = "XpText";
        StyleText(centerText, TextAlignmentOptions.Center, 0f, 0f);

        levelText = CloneText(centerText, "LevelText", TextAlignmentOptions.MidlineLeft, SideMargin, 0f);
        pointsText = CloneText(centerText, "PointsText", TextAlignmentOptions.MidlineRight, 0f, SideMargin);
    }

    // El texto de la barra de vida es blanco y se lee sobre el verde; sobre el amarillo necesita contorno.
    private static void StyleText(TMP_Text text, TextAlignmentOptions align, float leftMargin, float rightMargin)
    {
        text.fontSize = TextSize;
        text.alignment = align;
        text.outlineWidth = 0.22f;
        text.outlineColor = new Color32(20, 16, 4, 255);
        text.raycastTarget = false;
        text.rectTransform.offsetMin = new Vector2(leftMargin, 0f);
        text.rectTransform.offsetMax = new Vector2(-rightMargin, 0f);
    }

    private static TMP_Text CloneText(TMP_Text source, string name, TextAlignmentOptions align, float leftMargin, float rightMargin)
    {
        TMP_Text copy = Instantiate(source, source.transform.parent);
        copy.name = name;
        StyleText(copy, align, leftMargin, rightMargin);
        return copy;
    }

    private void BuildToast(Transform canvas)
    {
        toast = UiKit.Label("LevelUpToast", canvas, "", 38f, TextAlignmentOptions.Center, UiKit.Gold);
        UiKit.Place(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1400f, 60f));
        toast.transform.SetAsFirstSibling(); // detrás de los menús: no debe tapar los textos de la estación
        toast.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.XpChanged += OnXpChanged;
        GameEvents.LevelUp += OnLevelUp;
    }

    private void OnDisable()
    {
        GameEvents.XpChanged -= OnXpChanged;
        GameEvents.LevelUp -= OnLevelUp;
    }

    private void OnXpChanged(XpInfo info)
    {
        if (slider == null) return;

        slider.value = info.Fraction;
        levelText.text = "Nv " + info.Level;
        centerText.text = info.IsMaxLevel ? "MAX" : "Faltan " + (info.XpNeeded - info.Xp) + " XP";
        pointsText.text = info.Points > 0 ? info.Points + (info.Points == 1 ? " punto" : " puntos") : "";
    }

    private void OnLevelUp(string characterName, int level, int points)
    {
        if (toast == null) return;

        toast.text = "¡" + characterName + " sube al nivel " + level + "!  " + points
            + (points == 1 ? " punto para gastar" : " puntos para gastar");

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ShowToast());
    }

    private IEnumerator ShowToast()
    {
        toast.gameObject.SetActive(true);
        toast.alpha = 1f;
        yield return new WaitForSecondsRealtime(toastSeconds);

        float fade = 0.6f;
        for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
        {
            toast.alpha = 1f - t / fade;
            yield return null;
        }

        toast.gameObject.SetActive(false);
        toastRoutine = null;
    }
}
