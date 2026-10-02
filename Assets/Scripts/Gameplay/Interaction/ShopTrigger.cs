using UnityEngine;

/// <summary>Tienda de un arma. El nombre y el precio salen del WeaponDefinition.</summary>
public class ShopTrigger : InteractableStation
{
    [SerializeField] private int weaponIndex;

    // El arma que vende es del arsenal de un personaje; con otro personaje puede no existir.
    private bool Available => weaponIndex < Shooting.Instance.WeaponCount;

    private WeaponState Weapon => Shooting.Instance.GetWeapon(weaponIndex);

    protected override string PromptText
    {
        get
        {
            if (!Available) return "No disponible para este personaje";

            return Weapon.Owned
                ? Weapon.Name + " ya comprada"
                : "Presiona E para comprar " + Weapon.Name + " - $" + Weapon.Price;
        }
    }

    protected override void Interact()
    {
        if (!Available || Weapon.Owned) return;

        bool bought = Shooting.Instance.BuyWeapon(weaponIndex);
        ShowPrompt(bought ? PromptText : "Fondos insuficientes");
    }
}
