using TMPro;
using UnityEngine;

public class UpgradeMenuUI : MenuPanel
{
    public static UpgradeMenuUI Instance { get; private set; }

    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text fireRateText;
    [SerializeField] private TMP_Text reloadText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text panelMoneyText;

    private int selectedWeaponIndex;

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.MoneyChanged += OnMoneyChanged;
    }

    protected override void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        base.OnDisable();
    }

    protected override void OnOpened()
    {
        // Abre mostrando el arma equipada; SetSelectedWeapon permite elegir otra.
        selectedWeaponIndex = Shooting.Instance.CurrentWeaponIndex;
        Refresh();
    }

    public void SetSelectedWeapon(int index)
    {
        selectedWeaponIndex = index;
        Refresh();
    }

    private void OnMoneyChanged(int total)
    {
        if (IsOpen) Refresh();
    }

    private void Refresh()
    {
        WeaponState weapon = Shooting.Instance.GetWeapon(selectedWeaponIndex);

        weaponNameText.text = weapon.Name;
        panelMoneyText.text = "$" + MoneyManager.Instance.Money;

        fireRateText.text = UpgradeLabel(weapon, UpgradeType.FireRate);
        reloadText.text = UpgradeLabel(weapon, UpgradeType.Reload);
        damageText.text = UpgradeLabel(weapon, UpgradeType.Damage);
    }

    // El nombre de cada mejora lo decide el arma (el bastón las llama Cadencia, Maná y Poder).
    private static string UpgradeLabel(WeaponState weapon, UpgradeType type)
    {
        string cost = weapon.IsMaxLevel(type) ? "MAX" : "$" + weapon.GetUpgradeCost(type);
        return weapon.GetUpgradeLabel(type) + "\nNv " + weapon.GetLevel(type) + "/" + weapon.GetMaxLevel(type) + "\n" + cost;
    }

    // Los botones del panel llaman a estos métodos desde el Inspector.
    public void UpgradeFireRate() => Upgrade(UpgradeType.FireRate);
    public void UpgradeReload() => Upgrade(UpgradeType.Reload);
    public void UpgradeDamage() => Upgrade(UpgradeType.Damage);

    private void Upgrade(UpgradeType type)
    {
        Shooting.Instance.BuyUpgrade(selectedWeaponIndex, type);
        Refresh();
    }
}
