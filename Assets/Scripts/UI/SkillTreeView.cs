using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja el árbol de un personaje (la pestaña "Árbol" de la estación de habilidades): un cuadrado por nodo en su
/// posición, líneas entre los conectados, y abajo un panel con el detalle del nodo señalado y el botón de reiniciar.
/// Se arma por código. Clic o Aceptar en un nodo lo compra; los colores indican el estado de cada nodo.
/// </summary>
public class SkillTreeView
{
    private static readonly Color OwnedColor = new Color(0.98f, 0.8f, 0.12f, 1f);
    private static readonly Color BuyableColor = new Color(0.25f, 0.45f, 0.9f, 1f);
    private static readonly Color UnaffordableColor = new Color(0.17f, 0.27f, 0.5f, 1f);
    private static readonly Color LockedColor = new Color(0.22f, 0.24f, 0.3f, 1f);
    private static readonly Color LineIdle = new Color(0.3f, 0.33f, 0.42f, 1f);

    private const float NodeSize = 112f;
    private const float NodeFontSize = 20f;
    private const float NodeFontMin = 13f;
    private const float AreaWidth = 1800f;
    private const float AreaHeight = 640f;
    private const float DetailHeight = 110f;
    private const float DetailGap = 10f;
    private const float TopOffset = 215f;   // debajo del título, la XP y los puntos
    private const float LineThickness = 9f;
    private const float MaxScale = 190f;
    private const float MinZoom = 0.6f;
    private const float MaxZoom = 3f;
    private const float ZoomStep = 1.15f;
    private const float PanMargin = 160f;

    private class NodeUi
    {
        public SkillNode Node;
        public Image Image;
        public Button Button;
        public TMP_Text Label;
    }

    private class LineUi
    {
        public Image Image;
        public string A;
        public string B;
    }

    public RectTransform Root { get; }

    private readonly RectTransform viewport;   // zona visible (recorta lo que sale al acercar)
    private readonly RectTransform content;    // lo que se mueve y se escala: líneas y nodos
    private readonly RectTransform linesLayer;
    private readonly RectTransform nodesLayer;
    private readonly TMP_Text detailText;
    private readonly TMP_Text resetLabel;
    private readonly Action onChanged;
    private readonly Dictionary<string, NodeUi> nodes = new Dictionary<string, NodeUi>();
    private readonly List<LineUi> lines = new List<LineUi>();

    private SkillTreeDefinition builtFor;
    private SkillNode shown;
    private float zoom = 1f;
    private Vector2 pan;

    /// <param name="parent">Panel donde se coloca la vista (misma zona que la lista de habilidades).</param>
    /// <param name="onChanged">Se llama tras comprar o reiniciar, para que la pantalla se redibuje.</param>
    public SkillTreeView(Transform parent, Action onChanged)
    {
        this.onChanged = onChanged;

        Root = UiKit.Rect("SkillTree", parent);
        UiKit.Place(Root, new Vector2(0.5f, 1f), new Vector2(0f, -TopOffset), new Vector2(AreaWidth, AreaHeight + DetailGap + DetailHeight));
        Root.pivot = new Vector2(0.5f, 1f);

        RectTransform area = UiKit.Rect("Area", Root);
        UiKit.Place(area, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(AreaWidth, AreaHeight));
        area.pivot = new Vector2(0.5f, 1f);
        viewport = area;
        area.gameObject.AddComponent<RectMask2D>();
        // Casi transparente pero con 'raycast': recibe el arrastre y la rueda sobre el fondo vacío.
        area.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        area.gameObject.AddComponent<SkillTreePanZoom>().Init(this);

        content = UiKit.Rect("Content", area);
        UiKit.Place(content, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(AreaWidth, AreaHeight));
        content.pivot = new Vector2(0.5f, 0.5f);
        linesLayer = UiKit.Rect("Lines", content);
        UiKit.Stretch(linesLayer);
        nodesLayer = UiKit.Rect("Nodes", content);
        UiKit.Stretch(nodesLayer);

        BuildViewControls();

        Image detail = UiKit.Box("Detail", Root, UiKit.RowColor);
        UiKit.Place(detail.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(AreaWidth, DetailHeight));
        detail.rectTransform.pivot = new Vector2(0.5f, 0f);

        detailText = UiKit.Label("DetailText", detail.transform, "Pasa el ratón o selecciona un nodo para ver qué hace.", 30f,
            TextAlignmentOptions.MidlineLeft, Color.white);
        UiKit.Place(detailText.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(AreaWidth - 470f, DetailHeight - 10f));
        detailText.rectTransform.pivot = new Vector2(0f, 0.5f);

        Button reset = UiKit.TextButton("ResetTree", detail.transform, "Reiniciar árbol", 32f, ResetClicked, out resetLabel);
        UiKit.Place((RectTransform)reset.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(400f, 84f));
        ((RectTransform)reset.transform).pivot = new Vector2(1f, 0.5f);
    }

    // Botones de zoom y "Ajustar" (para quien no tiene rueda) y una pista, abajo a la izquierda del área.
    private void BuildViewControls()
    {
        float y = -(AreaHeight - 70f);

        // Fondo oscuro para que los controles se lean sobre los nodos al acercar.
        Image back = UiKit.Box("ViewControlsBack", Root, new Color(0.04f, 0.05f, 0.1f, 0.88f));
        UiKit.Place(back.rectTransform, new Vector2(0f, 1f), new Vector2(6f, y + 6f), new Vector2(760f, 70f));
        back.rectTransform.pivot = new Vector2(0f, 1f);
        back.raycastTarget = false;

        Button zoomIn = UiKit.TextButton("ZoomIn", Root, "+", 36f, () => ZoomStepBy(1f), out _);
        UiKit.Place((RectTransform)zoomIn.transform, new Vector2(0f, 1f), new Vector2(12f, y), new Vector2(58f, 58f));
        ((RectTransform)zoomIn.transform).pivot = new Vector2(0f, 1f);

        Button zoomOut = UiKit.TextButton("ZoomOut", Root, "-", 36f, () => ZoomStepBy(-1f), out _);
        UiKit.Place((RectTransform)zoomOut.transform, new Vector2(0f, 1f), new Vector2(78f, y), new Vector2(58f, 58f));
        ((RectTransform)zoomOut.transform).pivot = new Vector2(0f, 1f);

        Button fit = UiKit.TextButton("FitView", Root, "Ajustar", 26f, ResetView, out _);
        UiKit.Place((RectTransform)fit.transform, new Vector2(0f, 1f), new Vector2(144f, y), new Vector2(150f, 58f));
        ((RectTransform)fit.transform).pivot = new Vector2(0f, 1f);

        TMP_Text hint = UiKit.Label("ViewHint", Root, "Rueda: zoom  ·  Arrastrar: mover", 22f, TextAlignmentOptions.MidlineLeft, UiKit.Muted);
        UiKit.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(308f, y), new Vector2(450f, 58f));
        hint.rectTransform.pivot = new Vector2(0f, 1f);
    }

    // --- Desplazar y zoom ---

    public void PanBy(Vector2 delta)
    {
        pan += delta;
        ApplyView();
    }

    /// <summary>Acerca (+1) o aleja (-1) manteniendo fijo el punto de la pantalla bajo el cursor.</summary>
    public void ZoomAt(float direction, Vector2 screenPoint, Camera eventCamera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, eventCamera, out Vector2 local);
        ZoomAround(local, direction);
    }

    private void ZoomStepBy(float direction) => ZoomAround(Vector2.zero, direction);

    private void ZoomAround(Vector2 viewPoint, float direction)
    {
        float newZoom = Mathf.Clamp(zoom * (direction > 0f ? ZoomStep : 1f / ZoomStep), MinZoom, MaxZoom);
        if (Mathf.Approximately(newZoom, zoom)) return;

        // El punto del contenido que está bajo 'viewPoint' tiene que seguir ahí después del zoom.
        Vector2 contentPoint = (viewPoint - pan) / zoom;
        zoom = newZoom;
        pan = viewPoint - contentPoint * zoom;
        ApplyView();
    }

    /// <summary>Vuelve a ver todo el árbol, centrado y sin zoom.</summary>
    public void ResetView()
    {
        zoom = 1f;
        pan = Vector2.zero;
        ApplyView();
    }

    // Que el árbol no se pueda arrastrar fuera de la pantalla: siempre queda parte a la vista.
    private void ApplyView()
    {
        float limitX = Mathf.Max(0f, (AreaWidth * zoom - AreaWidth) * 0.5f) + PanMargin;
        float limitY = Mathf.Max(0f, (AreaHeight * zoom - AreaHeight) * 0.5f) + PanMargin;
        pan = new Vector2(Mathf.Clamp(pan.x, -limitX, limitX), Mathf.Clamp(pan.y, -limitY, limitY));

        content.localScale = new Vector3(zoom, zoom, 1f);
        content.anchoredPosition = pan;
    }

    // Con teclado o mando, el nodo seleccionado tiene que verse: si queda fuera, se mueve el árbol lo justo.
    private void EnsureVisible(SkillNode node)
    {
        if (!nodes.TryGetValue(node.id, out NodeUi ui)) return;

        Vector2 inView = pan + ((RectTransform)ui.Button.transform).anchoredPosition * zoom;
        float halfX = AreaWidth * 0.5f - NodeSize * 0.7f;
        float halfY = AreaHeight * 0.5f - NodeSize * 0.7f;

        if (inView.x > halfX) pan.x -= inView.x - halfX;
        else if (inView.x < -halfX) pan.x += -halfX - inView.x;
        if (inView.y > halfY) pan.y -= inView.y - halfY;
        else if (inView.y < -halfY) pan.y += -halfY - inView.y;

        ApplyView();
    }

    /// <summary>Dibuja el árbol (lo construye la primera vez o si cambió) y actualiza colores y textos.</summary>
    public void Show(SkillTreeDefinition tree, CharacterSave save)
    {
        if (builtFor != tree) Build(tree);

        Refresh(tree, save);
    }

    /// <summary>El nodo al que conviene dar el foco al abrir la pestaña: uno comprable, y si no, la raíz.</summary>
    public GameObject FirstFocusable(SkillTreeDefinition tree, CharacterSave save)
    {
        NodeUi fallback = null;
        foreach (SkillNode node in tree.nodes)
        {
            if (!nodes.TryGetValue(node.id, out NodeUi ui)) continue;

            if (SkillTreeRules.CanBuy(tree, save, node.id) == SkillBuyBlock.None) return ui.Button.gameObject;
            if (fallback == null || node.isRoot) fallback = ui;
        }
        return fallback != null ? fallback.Button.gameObject : null;
    }

    private void Build(SkillTreeDefinition tree)
    {
        foreach (NodeUi ui in nodes.Values) UnityEngine.Object.Destroy(ui.Button.gameObject);
        foreach (LineUi line in lines) UnityEngine.Object.Destroy(line.Image.gameObject);
        nodes.Clear();
        lines.Clear();
        builtFor = tree;
        shown = null;
        ResetView();

        if (tree == null || tree.nodes == null || tree.nodes.Length == 0) return;

        // Ajusta la escala para que el árbol entre en la zona sin importar cuántos nodos o qué tan separados estén.
        // El ancho y el alto se ajustan por separado: la zona es mucho más ancha que alta y así los nodos no se pegan.
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        foreach (SkillNode node in tree.nodes)
        {
            min = Vector2.Min(min, node.position);
            max = Vector2.Max(max, node.position);
        }
        Vector2 center = (min + max) * 0.5f;
        float width = Mathf.Max(0.01f, max.x - min.x);
        float height = Mathf.Max(0.01f, max.y - min.y);
        float scaleX = Mathf.Min(MaxScale, (AreaWidth - NodeSize) / width);
        float scaleY = Mathf.Min(MaxScale, (AreaHeight - NodeSize) / height);

        Vector2 ToScreen(Vector2 p) => new Vector2((p.x - center.x) * scaleX, (p.y - center.y) * scaleY);

        // Primero las líneas (debajo), una por cada par conectado.
        var drawn = new HashSet<string>();
        foreach (SkillNode node in tree.nodes)
        {
            if (node.connections == null) continue;
            foreach (string other in node.connections)
            {
                SkillNode target = tree.Find(other);
                if (target == null) continue;

                string key = string.CompareOrdinal(node.id, other) < 0 ? node.id + "|" + other : other + "|" + node.id;
                if (!drawn.Add(key)) continue;

                lines.Add(CreateLine(ToScreen(node.position), ToScreen(target.position), node.id, other));
            }
        }

        foreach (SkillNode node in tree.nodes)
        {
            SkillNode captured = node;
            Button button = UiKit.TextButton("Node_" + node.id, nodesLayer, node.displayName, NodeFontSize, () => NodeClicked(captured), out TMP_Text label);
            UiKit.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), ToScreen(node.position), new Vector2(NodeSize, NodeSize));
            ((RectTransform)button.transform).pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.offsetMin = new Vector2(4f, 4f);
            label.rectTransform.offsetMax = new Vector2(-4f, -4f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = NodeFontMin;
            label.fontSizeMax = NodeFontSize;
            label.overflowMode = TextOverflowModes.Ellipsis;

            var focus = button.gameObject.AddComponent<SkillNodeButton>();
            focus.Node = node;
            focus.Focused = ShowDetail;
            focus.Selected = EnsureVisible;

            nodes[node.id] = new NodeUi { Node = node, Image = button.GetComponent<Image>(), Button = button, Label = label };
        }
    }

    private LineUi CreateLine(Vector2 from, Vector2 to, string a, string b)
    {
        Image image = UiKit.Box("Line_" + a + "_" + b, linesLayer, LineIdle);
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        Vector2 delta = to - from;
        UiKit.Place(rect, new Vector2(0.5f, 0.5f), (from + to) * 0.5f, new Vector2(delta.magnitude, LineThickness));
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        return new LineUi { Image = image, A = a, B = b };
    }

    private void Refresh(SkillTreeDefinition tree, CharacterSave save)
    {
        foreach (NodeUi ui in nodes.Values)
        {
            SkillNode node = ui.Node;
            bool owned = save.skillNodes.Contains(node.id);
            bool unlocked = SkillTreeRules.IsUnlocked(tree, node, save.skillNodes);
            bool affordable = save.skillPoints >= node.cost;

            ui.Image.color = owned ? OwnedColor : !unlocked ? LockedColor : affordable ? BuyableColor : UnaffordableColor;
            ui.Label.color = owned ? new Color(0.1f, 0.08f, 0.01f, 1f) : unlocked ? Color.white : new Color(0.8f, 0.82f, 0.9f, 1f);
            ui.Label.fontStyle = FontStyles.Bold;
            ui.Label.text = owned ? node.displayName : node.displayName + "\n" + node.cost + (node.cost == 1 ? " pt" : " pts");
        }

        foreach (LineUi line in lines)
        {
            bool both = save.skillNodes.Contains(line.A) && save.skillNodes.Contains(line.B);
            line.Image.color = both ? OwnedColor : LineIdle;
        }

        int spent = 0;
        foreach (string id in save.skillNodes)
        {
            SkillNode owned = tree.Find(id);
            if (owned != null) spent += owned.cost;
        }
        resetLabel.text = spent > 0 ? "Reiniciar árbol (+" + spent + ")" : "Reiniciar árbol";

        // Mantiene el detalle al día tras comprar (el mismo nodo ahora figura como comprado).
        if (shown != null) ShowDetail(shown);
    }

    private void ShowDetail(SkillNode node)
    {
        shown = node;
        if (node == null || builtFor == null || SkillTreeManager.Instance == null || SkillTreeManager.Instance.ActiveSave == null) return;

        CharacterSave save = SkillTreeManager.Instance.ActiveSave;
        string status;
        switch (SkillTreeRules.CanBuy(builtFor, save, node.id))
        {
            case SkillBuyBlock.None: status = "Clic para comprar"; break;
            case SkillBuyBlock.Owned: status = "Comprado"; break;
            case SkillBuyBlock.Locked: status = "Bloqueado: compra antes un nodo conectado"; break;
            case SkillBuyBlock.NoPoints: status = "Te faltan " + (node.cost - save.skillPoints) + " puntos"; break;
            default: status = ""; break;
        }

        detailText.text = "<b>" + node.displayName + "</b>   ·   Costo: " + node.cost + (node.cost == 1 ? " punto" : " puntos")
            + "   ·   " + status + "\n" + node.description;
    }

    private void NodeClicked(SkillNode node)
    {
        ShowDetail(node);
        if (SkillTreeManager.Instance == null || !SkillTreeManager.Instance.Buy(node.id)) return;

        onChanged?.Invoke();
    }

    private void ResetClicked()
    {
        if (SkillTreeManager.Instance == null) return;

        SkillTreeManager.Instance.ResetTree();
        onChanged?.Invoke();
    }
}
