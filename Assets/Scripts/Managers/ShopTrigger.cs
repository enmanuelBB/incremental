using UnityEngine;

/// <summary>Tienda de un arma. El nombre y el precio salen del WeaponDefinition.</summary>
public class ShopTrigger : InteractableStation
{
    [SerializeField] private int weaponIndex;

    private WeaponState Weapon => Shooting.Instance.GetWeapon(weaponIndex);

    protected override string PromptText =>
        Weapon.Owned
            ? Weapon.Name + " ya comprada"
            : "Presiona E para comprar " + Weapon.Name + " - $" + Weapon.Price;

    protected override void Interact()
    {
        if (Weapon.Owned) return;

        bool bought = Shooting.Instance.BuyWeapon(weaponIndex);
        ShowPrompt(bought ? PromptText : "Fondos insuficientes");
    }
}
