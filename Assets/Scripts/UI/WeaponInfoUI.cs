using TMPro;
using UnityEngine;

public class WeaponInfoUI : MenuPanel
{
    public static WeaponInfoUI Instance { get; private set; }

    [SerializeField, Tooltip("Un texto por arma, en el mismo orden que el array de armas de Shooting")]
    private TMP_Text[] weaponInfoTexts;

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    protected override void OnOpened()
    {
        for (int i = 0; i < weaponInfoTexts.Length && i < Shooting.Instance.WeaponCount; i++)
        {
            WeaponState weapon = Shooting.Instance.GetWeapon(i);

            if (!weapon.Owned)
            {
                weaponInfoTexts[i].text = "???\n(no comprada)";
                continue;
            }

            weaponInfoTexts[i].text =
                weapon.Name + "\n" +
                "Daño: " + weapon.Damage + "\n" +
                "Cadencia: " + weapon.FireRate.ToString("F2") + "s\n" +
                "Recarga: " + weapon.ReloadTime.ToString("F2") + "s\n" +
                "Cargador: " + weapon.MagazineSize;
        }
    }
}
