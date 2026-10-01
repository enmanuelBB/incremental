using System;
using TMPro;
using UnityEngine;

/// <summary>Texto que sube y se desvanece (por ejemplo "+10" al ganar dinero).</summary>
public class FloatingText : MonoBehaviour
{
    [SerializeField, Tooltip("Qué tan rápido sube, en píxeles UI por segundo")] private float moveSpeed = 50f;
    [SerializeField, Tooltip("Cuánto dura antes de desaparecer")] private float duration = 1f;

    private TMP_Text text;
    private RectTransform rectTransform;
    private Color startColor;
    private Vector2 startPosition;
    private float timer;
    private Action<FloatingText> release;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
        startColor = text.color;
    }

    /// <summary>Muestra el texto en la posición dada; al terminar llama a release (devolverlo al pool).</summary>
    public void Show(string message, Vector3 position, Action<FloatingText> releaseCallback)
    {
        release = releaseCallback;
        text.text = message;
        text.color = startColor;
        rectTransform.position = position;
        startPosition = rectTransform.anchoredPosition;
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime; // tiempo real: funciona aunque el juego esté en pausa

        rectTransform.anchoredPosition = startPosition + Vector2.up * (moveSpeed * timer);
        text.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, timer / duration));

        if (timer >= duration) release(this);
    }
}
