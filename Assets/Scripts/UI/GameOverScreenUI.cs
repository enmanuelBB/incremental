using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pantalla de fin de partida: la imagen se oscurece con un tono rojo y una viñeta mientras dura la cámara lenta, y al
/// congelarse el juego entra el título ("HAS MUERTO" / "LA BASE HA CAÍDO"), la causa, la tarjeta con el resumen de la
/// run (cada fila aparece y sus números suben) y el botón de reiniciar. Se arma sola por código. Todo usa tiempo real
/// porque el juego está en cámara lenta o congelado.
/// </summary>
public class GameOverScreenUI : MonoBehaviour
{
    private static readonly Color DimColor = new Color(0.1f, 0f, 0f, 1f);
    private static readonly Color TitleColor = new Color(0.86f, 0.1f, 0.1f, 1f);
    private static readonly Color CardColor = new Color(0.03f, 0.03f, 0.05f, 0.88f);
    private static readonly Color RestartColor = new Color(0.72f, 0.13f, 0.13f, 1f);

    private const float RowHeight = 52f;
    private const float CardWidth = 920f;
    private const float DimTarget = 0.84f;

    private class Row
    {
        public CanvasGroup Group;
        public TMP_Text Value;
        public int Target;
        public Func<int, string> Format;
        public string StaticText;
    }

    private GameObject root;
    private Image dim;
    private RawImage vignette;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private RectTransform card;
    private CanvasGroup cardGroup;
    private Button restartButton;
    private CanvasGroup restartGroup;
    private readonly List<Row> rows = new List<Row>();
    private Coroutine fadeRoutine;
    private Coroutine revealRoutine;
    private Action onRestart;

    private void Awake()
    {
        Transform canvas = UiKit.FindCanvas();
        if (canvas == null) return;

        root = UiKit.Rect("GameOverScreen", canvas).gameObject;
        UiKit.Stretch((RectTransform)root.transform);

        dim = UiKit.Box("Dim", root.transform, new Color(DimColor.r, DimColor.g, DimColor.b, 0f));
        UiKit.Stretch(dim.rectTransform);

        RectTransform vignetteRect = UiKit.Rect("Vignette", root.transform);
        UiKit.Stretch(vignetteRect);
        vignette = vignetteRect.gameObject.AddComponent<RawImage>();
        vignette.texture = CreateVignetteTexture();
        vignette.raycastTarget = false;
        vignette.color = new Color(1f, 1f, 1f, 0f);

        titleText = UiKit.Label("Title", root.transform, "", 140f, TextAlignmentOptions.Center, TitleColor);
        UiKit.Place(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1800f, 180f));
        titleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 14f;
        // Una sola línea siempre: los títulos largos ("LA BASE HA CAÍDO") reducen la letra en vez de partirse.
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 70f;
        titleText.fontSizeMax = 140f;
        titleText.outlineWidth = 0.2f;
        titleText.outlineColor = new Color32(20, 0, 0, 255);

        subtitleText = UiKit.Label("Subtitle", root.transform, "", 40f, TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Place(subtitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 215f), new Vector2(1500f, 56f));
        subtitleText.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image cardImage = UiKit.Box("Card", root.transform, CardColor);
        card = cardImage.rectTransform;
        cardGroup = cardImage.gameObject.AddComponent<CanvasGroup>();

        restartButton = UiKit.TextButton("RestartButton", root.transform, "Reiniciar", 42f, () => onRestart?.Invoke(), out _);
        restartButton.GetComponent<Image>().color = RestartColor;
        UiKit.Place((RectTransform)restartButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -385f), new Vector2(440f, 84f));
        ((RectTransform)restartButton.transform).pivot = new Vector2(0.5f, 0.5f);
        restartGroup = restartButton.gameObject.AddComponent<CanvasGroup>();

        root.SetActive(false);
    }

    // Viñeta: transparente en el centro y rojiza oscura hacia los bordes. Se genera una vez, no hace falta ningún archivo.
    private static Texture2D CreateVignetteTexture()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x / (size - 1f)) * 2f - 1f;
                float dy = (y / (size - 1f)) * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                // 0 hasta el 35% del radio y sube suave hacia el borde; el clamp evita que el byte se desborde.
                float edge = Mathf.Clamp01(Mathf.InverseLerp(0.35f, 1.35f, distance));
                float alpha = edge * edge * (3f - 2f * edge) * 0.95f;
                pixels[y * size + x] = new Color32(40, 0, 0, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    /// <summary>Empieza a oscurecer la imagen (durante la cámara lenta), todavía sin texto.</summary>
    public void BeginFade()
    {
        if (root == null) return;

        root.SetActive(true);
        root.transform.SetAsLastSibling();
        titleText.gameObject.SetActive(false);
        subtitleText.gameObject.SetActive(false);
        card.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        const float seconds = 1.6f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / seconds);
            dim.color = new Color(DimColor.r, DimColor.g, DimColor.b, DimTarget * u);
            vignette.color = new Color(1f, 1f, 1f, u);
            yield return null;
        }

        dim.color = new Color(DimColor.r, DimColor.g, DimColor.b, DimTarget);
        vignette.color = Color.white;
    }

    /// <summary>Muestra el título, la causa, el resumen y el botón, con sus animaciones.</summary>
    public void Reveal(GameOverCause cause, RunStats stats, string characterName, bool hasTree, Action restart)
    {
        if (root == null) return;

        onRestart = restart;
        if (!root.activeSelf) BeginFade();

        string wave = stats.WaveReached > 0 ? " en la oleada " + stats.WaveReached : "";
        titleText.text = cause == GameOverCause.PlayerDied ? "HAS MUERTO" : "LA BASE HA CAÍDO";
        subtitleText.text = cause == GameOverCause.PlayerDied
            ? characterName + " cayó" + wave
            : "Los enemigos destruyeron la base" + wave;

        BuildRows(stats, characterName, hasTree);

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(RevealRoutine());
    }

    private void BuildRows(RunStats stats, string characterName, bool hasTree)
    {
        foreach (Transform child in card) Destroy(child.gameObject);
        rows.Clear();

        AddRow("Oleada alcanzada", stats.WaveReached, n => n.ToString());
        AddRow("Enemigos eliminados", stats.EnemiesKilled, n => n.ToString());
        AddRow("Tiempo sobrevivido", RunSummaryFormat.Duration(stats.SecondsSurvived));
        AddRow("Dinero ganado", stats.MoneyGained, n => "$ " + HudFormat.Money(n));
        AddRow("Experiencia ganada", stats.XpGained, n => n + " XP");
        AddRow("Nivel", RunSummaryFormat.Level(characterName, stats.StartLevel, stats.EndLevel), stats.LevelsGained > 0);
        AddRow("Puntos de nivel sin gastar", RunSummaryFormat.Points(stats.PointsAvailable), stats.PointsAvailable > 0);
        if (hasTree) AddRow("Puntos de " + characterName + " (árbol)", "+" + stats.SkillPointsGained, stats.SkillPointsGained > 0);

        float height = rows.Count * RowHeight + 44f;
        UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 165f - height * 0.5f), new Vector2(CardWidth, height));
        card.pivot = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < rows.Count; i++)
        {
            RectTransform rect = (RectTransform)rows[i].Group.transform;
            UiKit.Place(rect, new Vector2(0.5f, 1f), new Vector2(0f, -22f - i * RowHeight), new Vector2(CardWidth - 60f, RowHeight));
            rect.pivot = new Vector2(0.5f, 1f);
        }
    }

    private void AddRow(string label, int target, Func<int, string> format) => CreateRow(label, null, target, format, false);

    private void AddRow(string label, string text, bool highlight = false) => CreateRow(label, text, 0, null, highlight);

    private void CreateRow(string label, string text, int target, Func<int, string> format, bool highlight)
    {
        RectTransform rect = UiKit.Rect("Row" + rows.Count, card);
        var group = rect.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        TMP_Text left = UiKit.Label("Label", rect, label, 34f, TextAlignmentOptions.MidlineLeft, UiKit.Muted);
        UiKit.Stretch(left.rectTransform);

        TMP_Text right = UiKit.Label("Value", rect, text ?? format(0), 38f, TextAlignmentOptions.MidlineRight, highlight ? UiKit.Gold : Color.white);
        right.fontStyle = FontStyles.Bold;
        UiKit.Stretch(right.rectTransform);

        rows.Add(new Row { Group = group, Value = right, Target = target, Format = format, StaticText = text });
    }

    private IEnumerator RevealRoutine()
    {
        // Título: entra grande y se asienta, con fundido.
        titleText.gameObject.SetActive(true);
        subtitleText.gameObject.SetActive(true);
        card.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);
        cardGroup.alpha = 0f;
        restartGroup.alpha = 0f;
        restartButton.interactable = false;
        subtitleText.alpha = 0f;

        RectTransform titleRect = titleText.rectTransform;
        const float pop = 0.6f;
        for (float t = 0f; t < pop; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / pop);
            float eased = 1f - Mathf.Pow(1f - u, 3f);
            titleRect.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, eased);
            titleText.alpha = eased;
            yield return null;
        }
        titleRect.localScale = Vector3.one;
        titleText.alpha = 1f;

        yield return Fade(0.4f, v => subtitleText.alpha = v);
        yield return Fade(0.35f, v => cardGroup.alpha = v);

        // Cada fila aparece y su número sube.
        foreach (Row row in rows)
        {
            yield return new WaitForSecondsRealtime(0.18f);
            StartCoroutine(RevealRow(row));
        }

        yield return new WaitForSecondsRealtime(0.9f);
        yield return Fade(0.4f, v => restartGroup.alpha = v);
        restartButton.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
    }

    private static IEnumerator Fade(float seconds, Action<float> apply)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            apply(Mathf.Clamp01(t / seconds));
            yield return null;
        }
        apply(1f);
    }

    private IEnumerator RevealRow(Row row)
    {
        const float seconds = 0.7f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / seconds);
            row.Group.alpha = Mathf.Clamp01(u * 4f);
            if (row.Format != null) row.Value.text = row.Format(Mathf.RoundToInt(row.Target * (1f - Mathf.Pow(1f - u, 3f))));
            yield return null;
        }

        row.Group.alpha = 1f;
        if (row.Format != null) row.Value.text = row.Format(row.Target);
    }
}
