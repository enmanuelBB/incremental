using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida grande del jefe (o minijefe) en la parte alta de la pantalla, con su nombre y una marca en el 50%
/// (donde cambia de fase). Aparece cuando el jefe recibe vida y desaparece cuando muere o se va. Se arma por código.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    private static readonly Color BackColor = new Color(0.05f, 0.03f, 0.04f, 0.9f);
    private static readonly Color FillColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    private static readonly Color BorderColor = new Color(0.9f, 0.75f, 0.3f, 1f);

    private const float Width = 900f;
    private const float Height = 34f;

    private GameObject root;
    private RectTransform fill;
    private TMP_Text nameText;
    private TMP_Text valueText;

    private void Awake()
    {
        Transform canvas = UiKit.FindCanvas();
        if (canvas == null) return;

        RectTransform rect = UiKit.Rect("BossHealthBar", canvas);
        UiKit.Place(rect, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(Width, Height + 46f));
        rect.pivot = new Vector2(0.5f, 1f);
        root = rect.gameObject;
        rect.SetAsFirstSibling(); // detrás de los menús

        nameText = UiKit.Label("Name", rect, "", 32f, TextAlignmentOptions.Center, UiKit.Gold);
        UiKit.Place(nameText.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(Width, 40f));
        nameText.rectTransform.pivot = new Vector2(0.5f, 1f);
        nameText.fontStyle = FontStyles.Bold;
        nameText.characterSpacing = 4f;
        nameText.outlineWidth = 0.2f;
        nameText.outlineColor = new Color32(20, 10, 4, 255);

        Image border = UiKit.Box("Border", rect, BorderColor);
        UiKit.Place(border.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(Width, Height));
        border.rectTransform.pivot = new Vector2(0.5f, 0f);

        Image back = UiKit.Box("Back", border.transform, BackColor);
        UiKit.Stretch(back.rectTransform);
        back.rectTransform.offsetMin = new Vector2(3f, 3f);
        back.rectTransform.offsetMax = new Vector2(-3f, -3f);

        Image fillImage = UiKit.Box("Fill", back.transform, FillColor);
        fill = fillImage.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        // Marca del 50%: el jefe cambia de fase aquí.
        Image mark = UiKit.Box("PhaseMark", back.transform, new Color(1f, 1f, 1f, 0.55f));
        mark.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        mark.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        mark.rectTransform.sizeDelta = new Vector2(3f, 0f);
        mark.raycastTarget = false;

        valueText = UiKit.Label("Value", back.transform, "", 22f, TextAlignmentOptions.Center, Color.white);
        UiKit.Stretch(valueText.rectTransform);
        valueText.fontStyle = FontStyles.Bold;
        valueText.outlineWidth = 0.2f;
        valueText.outlineColor = new Color32(20, 0, 0, 255);

        root.SetActive(false);
    }

    private void OnEnable() => GameEvents.BossHealthChanged += OnBossHealthChanged;

    private void OnDisable() => GameEvents.BossHealthChanged -= OnBossHealthChanged;

    private void OnBossHealthChanged(string bossName, int current, int max)
    {
        if (root == null) return;

        if (current <= 0)
        {
            root.SetActive(false);
            return;
        }

        root.SetActive(true);
        nameText.text = bossName.ToUpperInvariant();
        valueText.text = current + " / " + max;
        fill.anchorMax = new Vector2(Mathf.Clamp01(max > 0 ? (float)current / max : 0f), 1f);
    }
}
