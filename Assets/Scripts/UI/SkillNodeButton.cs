using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Un nodo del árbol en pantalla: avisa a la vista cuál nodo tiene el ratón encima o la selección del teclado o mando.</summary>
public class SkillNodeButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public SkillNode Node { get; set; }
    public Action<SkillNode> Focused { get; set; }

    public void OnPointerEnter(PointerEventData eventData) => Focused?.Invoke(Node);

    public void OnSelect(BaseEventData eventData) => Focused?.Invoke(Node);
}
