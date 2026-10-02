using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarjeta de munición de una arma: número de la tecla, nombre, balas "12 / 12" y una barra que se vacía al disparar.
/// La equipada se ve brillante y con borde dorado; las demás, atenuadas. Solo dibuja lo que le dice WeaponSlotInfo.
/// </summary>
public class WeaponCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField, Tooltip("Imagen con 'Image Type = Filled' y 'Fill Method = Horizontal'")]
    private Image ammoBar;
    [SerializeField, Tooltip("Borde de la tarjeta (componente Outline): dorado si es la equipada")]
    private Outline border;
    [SerializeField] private CanvasGroup group;

    [Header("Colores")]
    [SerializeField] private Color selectedBorder = new Color(1f, 0.78f, 0.25f, 1f);
    [SerializeField] private Color idleBorder = new Color(0.3f, 0.34f, 0.45f, 0.8f);
    [SerializeField] private Color barColor = new Color(1f, 0.78f, 0.25f, 1f);
    [SerializeField, Tooltip("Cuando queda poca munición")] private Color lowAmmoColor = new Color(0.95f, 0.3f, 0.25f, 1f);
    [SerializeField, Range(0f, 1f)] private float lowAmmoFraction = 0.25f;

    [Header("Opacidad")]
    [SerializeField] private float selectedAlpha = 1f;
    [SerializeField] private float idleAlpha = 0.55f;
    [SerializeField] private float lockedAlpha = 0.35f;

    public void Set(WeaponSlotInfo info)
    {
        keyText.text = (info.Index + 1).ToString();
        nameText.text = HudFormat.DisplayName(info.Name);
        border.effectColor = info.Selected ? selectedBorder : idleBorder;
        group.alpha = !info.Owned ? lockedAlpha : info.Selected ? selectedAlpha : idleAlpha;

        if (!info.Owned)
        {
            ammoText.text = "<size=60%>Sin comprar</size>";
            ammoBar.fillAmount = 0f;
            return;
        }

        // La barra muestra todas las balas del arma juntas (24 si son dos pistolas de 12).
        int capacity = info.Magazine * Mathf.Max(1, info.Barrels);
        float fraction = capacity > 0 ? Mathf.Clamp01((float)info.TotalAmmo / capacity) : 0f;
        bool low = fraction <= lowAmmoFraction;

        if (info.Reloading)
        {
            // Más pequeño y en ámbar para que no se monte sobre el nombre del arma.
            ammoText.text = "<size=50%><color=#" + ColorUtility.ToHtmlStringRGB(barColor) + ">RECARGANDO</color></size>";
            ammoBar.fillAmount = 0f;
            return;
        }

        // Un cañón: "12 / 12". Varios (dos pistolas): "11 | 12", una cifra por cargador. Como los disparos se
        // turnan, bajan de a una; cada cifra se pone roja por separado cuando a esa pistola le quedan pocas.
        if (info.Barrels > 1)
        {
            const string separator = "<size=60%><color=#B8C0D0> | </color></size>";
            var text = new System.Text.StringBuilder();

            for (int barrel = 0; barrel < info.Barrels; barrel++)
            {
                bool barrelLow = info.Magazine > 0 && (float)info.BarrelAmmo[barrel] / info.Magazine <= lowAmmoFraction;
                string barrelColor = ColorUtility.ToHtmlStringRGB(barrelLow ? lowAmmoColor : Color.white);

                if (barrel > 0) text.Append(separator);
                text.Append("<color=#").Append(barrelColor).Append('>').Append(info.BarrelAmmo[barrel]).Append("</color>");
            }

            ammoText.text = text.ToString();
        }
        else
        {
            string color = ColorUtility.ToHtmlStringRGB(low ? lowAmmoColor : Color.white);
            ammoText.text = "<color=#" + color + ">" + info.TotalAmmo + "</color><size=55%><color=#B8C0D0> / " + info.Magazine + "</color></size>";
        }
        ammoBar.fillAmount = fraction;
        ammoBar.color = low ? lowAmmoColor : barColor;
    }
}
