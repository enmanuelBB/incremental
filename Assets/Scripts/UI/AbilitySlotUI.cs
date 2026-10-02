using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Casilla de una habilidad: el icono, un círculo que se va llenando mientras dura el enfriamiento (lleno = lista),
/// los segundos que faltan en el centro y la tecla debajo. Solo dibuja; el momento en que la habilidad queda lista
/// se lo da UIManager a partir de los eventos del juego.
/// </summary>
public class AbilitySlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField, Tooltip("Imagen circular con 'Image Type = Filled' y 'Fill Method = Radial 360'")]
    private Image cooldownFill;
    [SerializeField] private TMP_Text secondsText;
    [SerializeField] private TMP_Text keyText;

    [SerializeField, Tooltip("Fila que contiene la casilla. Se oculta junto con ella: una fila activa pero vacía descuadra el HUD (el contenedor se colapsa).")]
    private GameObject container;

    [Header("Colores")]
    [SerializeField] private Color readyIconColor = Color.white;
    [SerializeField, Tooltip("Icono oscurecido mientras la habilidad se recarga")]
    private Color coolingIconColor = new Color(0.4f, 0.42f, 0.5f, 1f);
    [SerializeField, Tooltip("Círculo que se va llenando")]
    private Color fillColor = new Color(0.3f, 0.62f, 1f, 0.6f);

    /// <param name="icon">Icono de la habilidad; si es null se deja el que ya tenga la casilla.</param>
    /// <param name="key">Texto de la tecla que se muestra debajo (por ejemplo "Q").</param>
    public void Show(Sprite icon, string key)
    {
        if (icon != null) iconImage.sprite = icon;
        keyText.text = key;
        if (container != null) container.SetActive(true);
        gameObject.SetActive(true);
        SetCooldown(0f, 1f);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (container != null) container.SetActive(false);
    }

    /// <param name="remaining">Segundos que faltan; 0 o menos significa lista.</param>
    /// <param name="total">Duración total del enfriamiento que está corriendo.</param>
    public void SetCooldown(float remaining, float total)
    {
        bool ready = remaining <= 0f;

        iconImage.color = ready ? readyIconColor : coolingIconColor;

        // Mientras se recarga, el círculo avanza de vacío a lleno; al quedar lista desaparece y se ve el icono limpio.
        cooldownFill.enabled = !ready;
        cooldownFill.color = fillColor;
        cooldownFill.fillAmount = ready || total <= 0f ? 1f : Mathf.Clamp01(1f - remaining / total);

        secondsText.enabled = !ready;
        if (!ready) secondsText.text = remaining.ToString("0.0");
    }
}
