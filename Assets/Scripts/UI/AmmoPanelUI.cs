using UnityEngine;

/// <summary>
/// Panel de munición de los personajes con armas de fuego: una tarjeta por arma. Las tarjetas sobrantes
/// quedan ocultas, así un personaje con una sola arma muestra una sola.
/// </summary>
public class AmmoPanelUI : MonoBehaviour
{
    [SerializeField] private WeaponCardUI[] cards;

    /// <summary>Oculta todas las tarjetas; las que hagan falta se muestran al llegar su estado.</summary>
    public void ResetSlots()
    {
        foreach (WeaponCardUI card in cards) card.gameObject.SetActive(false);
    }

    public void SetSlot(WeaponSlotInfo info)
    {
        if (info.Index < 0 || info.Index >= cards.Length) return;

        cards[info.Index].gameObject.SetActive(true);
        cards[info.Index].Set(info);
    }
}
