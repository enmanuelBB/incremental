using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Recibe el arrastre y la rueda del ratón sobre el árbol y se los pasa a la vista (SkillTreeView). Va en el área del
/// árbol: los eventos de los nodos suben hasta aquí (los botones no usan arrastre ni rueda), y un clic sin arrastrar
/// sigue comprando el nodo porque Unity cancela el clic en cuanto empieza un arrastre.
/// </summary>
public class SkillTreePanZoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    private SkillTreeView view;
    private Canvas canvas;

    public void Init(SkillTreeView owner)
    {
        view = owner;
        canvas = GetComponentInParent<Canvas>();
    }

    // Hace falta para que Unity empiece el arrastre y no cuente como clic.
    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        if (view == null) return;

        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        view.PanBy(eventData.delta / scale);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (view == null || Mathf.Approximately(eventData.scrollDelta.y, 0f)) return;

        // Un paso de zoom por cada muesca, sin depender de las unidades de la rueda.
        view.ZoomAt(Mathf.Sign(eventData.scrollDelta.y), eventData.position, eventData.pressEventCamera);
    }
}
