using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD. Se actualiza solo escuchando GameEvents; no lo llama nadie directamente.</summary>
public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider playerHealthBar;
    [SerializeField] private Slider baseHealthBar;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text baseHealthText;
    [SerializeField] private TMP_Text shopPromptText;

    [Header("Dinero")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField, Tooltip("Pulso al ganar o gastar dinero (opcional)")]
    private HudPunch moneyPunch;

    [Header("Oleada")]
    [SerializeField, Tooltip("Rótulo de la oleada; está oculto hasta que empieza la primera")]
    private GameObject waveBanner;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private HudPunch wavePunch;

    [Header("Munición (personajes con armas de fuego)")]
    [SerializeField] private AmmoPanelUI ammoPanel;

    [Header("Maná (solo personajes con bastón)")]
    [SerializeField] private Slider manaBar;
    [SerializeField] private TMP_Text manaText;
    [SerializeField, Tooltip("Casilla de la habilidad: encima de las barras, con la tecla debajo")]
    private AbilitySlotUI abilitySlot;

    private float abilityReadyAt;
    private float abilityCooldownTotal = 1f;
    private int lastMoney = -1;

    private void Start()
    {
        // El rótulo de la oleada aparece cuando empieza la primera, no antes.
        if (waveBanner != null) waveBanner.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.PlayerHealthChanged += SetPlayerHealth;
        GameEvents.BaseHealthChanged += SetBaseHealth;
        GameEvents.MoneyChanged += SetMoney;
        GameEvents.WaveStarted += SetWave;
        GameEvents.WeaponSlotChanged += SetWeaponSlot;
        GameEvents.PromptChanged += SetPrompt;
        GameEvents.ResourceModeChanged += SetResourceMode;
        GameEvents.ManaChanged += SetMana;
        GameEvents.AbilityUsed += OnAbilityUsed;
    }

    private void OnDisable()
    {
        GameEvents.PlayerHealthChanged -= SetPlayerHealth;
        GameEvents.BaseHealthChanged -= SetBaseHealth;
        GameEvents.MoneyChanged -= SetMoney;
        GameEvents.WaveStarted -= SetWave;
        GameEvents.WeaponSlotChanged -= SetWeaponSlot;
        GameEvents.PromptChanged -= SetPrompt;
        GameEvents.ResourceModeChanged -= SetResourceMode;
        GameEvents.ManaChanged -= SetMana;
        GameEvents.AbilityUsed -= OnAbilityUsed;
    }

    // La cuenta atrás de la habilidad la dibuja la propia UI a partir del momento en que queda lista;
    // gameplay solo publica ese momento una vez, no cada frame.
    private void Update()
    {
        if (abilitySlot == null || !abilitySlot.isActiveAndEnabled) return;

        abilitySlot.SetCooldown(abilityReadyAt - Time.time, abilityCooldownTotal);
    }

    // Munición para armas de fuego; barra de maná y casilla de habilidad para el bastón.
    private void SetResourceMode(bool usesMana, string ability, Sprite icon)
    {
        if (ammoPanel != null)
        {
            ammoPanel.gameObject.SetActive(!usesMana);
            ammoPanel.ResetSlots();
        }

        if (manaBar != null) manaBar.gameObject.SetActive(usesMana);

        abilityReadyAt = 0f;

        if (abilitySlot == null) return;

        if (usesMana && !string.IsNullOrEmpty(ability)) abilitySlot.Show(icon, GameInput.Instance.Ability1Label);
        else abilitySlot.Hide();
    }

    private void SetWeaponSlot(WeaponSlotInfo info)
    {
        if (ammoPanel != null) ammoPanel.SetSlot(info);
    }

    private void SetMana(float current, float max)
    {
        if (manaBar == null) return;

        manaBar.maxValue = max;
        manaBar.value = current;
        if (manaText != null) manaText.text = Mathf.FloorToInt(current) + " / " + Mathf.FloorToInt(max);
    }

    // El evento trae el momento en que queda lista; la duración total es lo que falta ahora mismo.
    private void OnAbilityUsed(string ability, float readyAt)
    {
        abilityReadyAt = readyAt;
        abilityCooldownTotal = Mathf.Max(0.01f, readyAt - Time.time);
    }

    private void SetPlayerHealth(int current, int max)
    {
        playerHealthBar.maxValue = max;
        playerHealthBar.value = current;
        playerHealthText.text = current + " / " + max;
    }

    private void SetBaseHealth(int current, int max)
    {
        baseHealthBar.maxValue = max;
        baseHealthBar.value = current;
        baseHealthText.text = "Base  " + current + " / " + max;
    }

    private void SetWave(int wave)
    {
        if (waveBanner != null) waveBanner.SetActive(true);
        waveText.text = HudFormat.WaveLabel(wave);
        if (wavePunch != null) wavePunch.Play();
    }

    private void SetMoney(int amount)
    {
        moneyText.text = HudFormat.Money(amount);

        // El pulso solo cuando cambia el dinero, no al mostrar el valor inicial.
        if (moneyPunch != null && lastMoney >= 0 && amount != lastMoney) moneyPunch.Play();
        lastMoney = amount;
    }

    private void SetPrompt(string message)
    {
        bool visible = !string.IsNullOrEmpty(message);
        shopPromptText.gameObject.SetActive(visible);
        if (visible) shopPromptText.text = message;
    }
}
