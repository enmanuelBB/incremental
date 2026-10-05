using TMPro;
using UnityEngine;

public class UpgradeMenuUI : MenuPanel
{
    public static UpgradeMenuUI Instance { get; private set; }

    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text fireRateText;
    [SerializeField] private TMP_Text reloadText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField, Tooltip("Columna de la mejora de sangrado; se oculta en armas que no sangran")] private TMP_Text bleedText;
    [SerializeField] private TMP_Text panelMoneyText;
    [SerializeField, Tooltip("Separación horizontal entre columnas de mejora")] private float columnSpacing = 330f;

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
        panelMoneyText.text = "$ " + HudFormat.Money(MoneyManager.Instance.Money);

        fireRateText.text = UpgradeLabel(weapon, UpgradeType.FireRate);
        reloadText.text = weapon.Sword != null ? StunLabel(weapon) : UpgradeLabel(weapon, UpgradeType.Reload);
        damageText.text = UpgradeLabel(weapon, UpgradeType.Damage);

        bool bleeds = bleedText != null && weapon.Definition.AppliesBleed;
        if (bleedText != null)
        {
            bleedText.gameObject.SetActive(bleeds);
            if (bleeds) bleedText.text = BleedLabel(weapon);
        }

        CenterColumns(bleeds);
    }

    // Las columnas activas se reparten centradas: tres para el bastón, cuatro para las armas que sangran.
    private void CenterColumns(bool includeBleed)
    {
        TMP_Text[] columns = includeBleed
            ? new[] { fireRateText, reloadText, damageText, bleedText }
            : new[] { fireRateText, reloadText, damageText };

        float start = -(columns.Length - 1) * columnSpacing * 0.5f;
        for (int i = 0; i < columns.Length; i++)
        {
            RectTransform rect = columns[i].rectTransform;
            rect.anchoredPosition = new Vector2(start + i * columnSpacing, rect.anchoredPosition.y);
        }
    }

    // Además del nivel y el precio, muestra cuántas pilas de sangrado pone cada impacto.
    private static string BleedLabel(WeaponState weapon)
    {
        string cost = weapon.IsMaxLevel(UpgradeType.Bleed) ? "MAX" : "$" + HudFormat.Money(weapon.GetUpgradeCost(UpgradeType.Bleed));
        return weapon.GetUpgradeLabel(UpgradeType.Bleed)
            + "\nNv " + weapon.GetLevel(UpgradeType.Bleed) + "/" + weapon.GetMaxLevel(UpgradeType.Bleed)
            + "\nPilas: " + weapon.BleedPerHit
            + "\n" + cost;
    }

    // La espada usa este espacio para Aturdir: además del nivel y el precio, muestra la probabilidad actual.
    private static string StunLabel(WeaponState weapon)
    {
        string cost = weapon.IsMaxLevel(UpgradeType.Reload) ? "MAX" : "$" + HudFormat.Money(weapon.GetUpgradeCost(UpgradeType.Reload));
        return weapon.GetUpgradeLabel(UpgradeType.Reload)
            + "\nNv " + weapon.GetLevel(UpgradeType.Reload) + "/" + weapon.GetMaxLevel(UpgradeType.Reload)
            + "\nProb.: " + Mathf.RoundToInt(weapon.StunChance * 100f) + "%"
            + "\n" + cost;
    }

    // El nombre de cada mejora lo decide el arma (el bastón las llama Cadencia, Maná y Poder).
    private static string UpgradeLabel(WeaponState weapon, UpgradeType type)
    {
        string cost = weapon.IsMaxLevel(type) ? "MAX" : "$" + HudFormat.Money(weapon.GetUpgradeCost(type));
        return weapon.GetUpgradeLabel(type) + "\nNv " + weapon.GetLevel(type) + "/" + weapon.GetMaxLevel(type) + "\n" + cost;
    }

    // Los botones del panel llaman a estos métodos desde el Inspector.
    public void UpgradeFireRate() => Upgrade(UpgradeType.FireRate);
    public void UpgradeReload() => Upgrade(UpgradeType.Reload);
    public void UpgradeDamage() => Upgrade(UpgradeType.Damage);
    public void UpgradeBleed() => Upgrade(UpgradeType.Bleed);

    private void Upgrade(UpgradeType type)
    {
        Shooting.Instance.BuyUpgrade(selectedWeaponIndex, type);
        Refresh();
    }
}
