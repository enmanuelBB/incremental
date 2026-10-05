using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD propio del bastón de Frieren: una barra fina bajo la mira que muestra la carga del Zoltraak y una línea de pista
/// (por ejemplo al colocar el campo de flores). Se crea sola la primera vez que se usa y se vuelve a crear si el canvas cambió.
/// </summary>
public static class StaffHud
{
    private static readonly Color BackColor = new Color(0.04f, 0.05f, 0.1f, 0.7f);
    private static readonly Color FillColor = new Color(1f, 0.82f, 0.35f, 0.95f);
    private const float BarWidth = 220f;
    private const float BarHeight = 10f;

    private static GameObject root;
    private static RectTransform fill;
    private static GameObject barObject;
    private static TMP_Text hint;

    /// <summary>Muestra la carga (0 a 1) o la oculta con un valor negativo.</summary>
    public static void SetCharge(float fraction)
    {
        if (fraction < 0f)
        {
            if (barObject != null) barObject.SetActive(false);
            return;
        }

        if (!EnsureBuilt()) return;

        barObject.SetActive(true);
        fill.localScale = new Vector3(Mathf.Clamp01(fraction), 1f, 1f);
    }

    /// <summary>Muestra una pista bajo la mira, o la oculta si el texto es nulo o vacío.</summary>
    public static void SetHint(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (hint != null) hint.gameObject.SetActive(false);
            return;
        }

        if (!EnsureBuilt()) return;

        hint.text = text;
        hint.gameObject.SetActive(true);
    }

    private static bool EnsureBuilt()
    {
        if (root != null) return true;

        Transform canvas = UiKit.FindCanvas();
        if (canvas == null) return false;

        RectTransform container = UiKit.Rect("StaffHud", canvas);
        UiKit.Place(container, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(BarWidth, 60f));
        root = container.gameObject;

        Image back = UiKit.Box("ChargeBar", container, BackColor);
        UiKit.Place(back.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(BarWidth, BarHeight));
        back.raycastTarget = false;
        barObject = back.gameObject;

        Image bar = UiKit.Box("Fill", back.transform, FillColor);
        UiKit.Stretch(bar.rectTransform);
        bar.rectTransform.pivot = new Vector2(0f, 0.5f);   // crece desde la izquierda
        bar.raycastTarget = false;
        fill = bar.rectTransform;
        barObject.SetActive(false);

        hint = UiKit.Label("Hint", container, "", 24f, TextAlignmentOptions.Center, Color.white);
        UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 34f));
        hint.outlineWidth = 0.2f;
        hint.outlineColor = new Color32(0, 0, 0, 255);
        hint.gameObject.SetActive(false);
        return true;
    }
}
