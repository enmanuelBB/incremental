using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Un nodo del árbol en pantalla: avisa a la vista cuál nodo tiene el ratón encima o la selección del teclado o mando.</summary>
public class SkillNodeButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public SkillNode Node { get; set; }
    public Action<SkillNode> Focused { get; set; }

    /// <summary>Solo cuando el nodo se selecciona (teclado, mando o clic), no al pasar el ratón: la vista lo trae a la pantalla.</summary>
    public Action<SkillNode> Selected { get; set; }

    public void OnPointerEnter(PointerEventData eventData) => Focused?.Invoke(Node);

    public void OnSelect(BaseEventData eventData)
    {
        Focused?.Invoke(Node);
        Selected?.Invoke(Node);
    }
}
