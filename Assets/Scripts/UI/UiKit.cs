using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Piezas mínimas para armar interfaz por código (paneles, textos, botones) con el estilo del juego.</summary>
public static class UiKit
{
    public static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.1f, 0.97f);
    public static readonly Color RowColor = new Color(0.1f, 0.12f, 0.2f, 1f);
    public static readonly Color ButtonColor = new Color(0.25f, 0.45f, 0.9f, 1f);
    public static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    public static readonly Color Muted = new Color(0.7f, 0.74f, 0.82f);

    /// <summary>Canvas principal del HUD (el de la escena, no los que crean otros sistemas).</summary>
    public static Transform FindCanvas()
    {
        GameObject named = GameObject.Find("Canvas");
        if (named != null) return named.transform;

        Canvas any = Object.FindFirstObjectByType<Canvas>();
        return any != null ? any.transform : null;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>Ancla el rectángulo a un punto de su padre con un tamaño y una posición fijos.</summary>
    public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static Image Box(string name, Transform parent, Color color)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static TMP_Text Label(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
    {
        RectTransform rect = Rect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = align;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    public static Button TextButton(string name, Transform parent, string label, float fontSize, UnityAction onClick, out TMP_Text text)
    {
        Image image = Box(name, parent, ButtonColor);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.85f, 0.92f, 1f),
            pressedColor = new Color(0.7f, 0.72f, 0.85f),
            selectedColor = new Color(0.85f, 0.92f, 1f),
            disabledColor = new Color(0.45f, 0.47f, 0.55f, 0.8f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        button.onClick.AddListener(onClick);

        text = Label("Text", image.transform, label, fontSize, TextAlignmentOptions.Center, Color.white);
        Stretch(text.rectTransform);
        return button;
    }
}
