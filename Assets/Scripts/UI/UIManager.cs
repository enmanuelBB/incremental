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

    [Header("Habilidades")]
    [SerializeField, Tooltip("Las 3 casillas, en orden (Q, E, F): encima de las barras, con la tecla debajo")]
    private AbilitySlotUI[] abilitySlots;
    [SerializeField, Tooltip("Fila que contiene las casillas. Se oculta entera si el personaje no tiene habilidades: una fila activa pero vacía descuadra el HUD.")]
    private GameObject abilityRow;

    private float[] abilityReadyAt;
    private float[] abilityCooldownTotal;
    private int lastMoney = -1;

    private void Awake()
    {
        int count = abilitySlots != null ? abilitySlots.Length : 0;
        abilityReadyAt = new float[count];
        abilityCooldownTotal = new float[count];
        for (int i = 0; i < count; i++) abilityCooldownTotal[i] = 1f;
    }

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
        GameEvents.AbilitiesChanged += SetAbilities;
        GameEvents.AbilityUsed += OnAbilityUsed;
        GameEvents.AbilityChargesChanged += OnAbilityCharges;
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
        GameEvents.AbilitiesChanged -= SetAbilities;
        GameEvents.AbilityUsed -= OnAbilityUsed;
        GameEvents.AbilityChargesChanged -= OnAbilityCharges;
    }

    // La cuenta atrás de cada habilidad la dibuja la propia UI a partir del momento en que queda lista;
    // gameplay solo publica ese momento una vez, no cada frame.
    private void Update()
    {
        for (int i = 0; i < abilityReadyAt.Length; i++)
        {
            if (abilitySlots[i] == null || !abilitySlots[i].isActiveAndEnabled) continue;

            abilitySlots[i].SetCooldown(abilityReadyAt[i] - Time.time, abilityCooldownTotal[i]);
        }
    }

    // Munición para armas de fuego; barra de maná para el bastón.
    private void SetResourceMode(bool usesMana)
    {
        if (ammoPanel != null)
        {
            ammoPanel.gameObject.SetActive(!usesMana);
            ammoPanel.ResetSlots();
        }

        if (manaBar != null) manaBar.gameObject.SetActive(usesMana);
    }

    // Muestra las casillas que el personaje tiene y oculta el resto.
    private void SetAbilities(AbilityHudInfo[] info)
    {
        bool any = false;

        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilityReadyAt[i] = 0f;
            if (abilitySlots[i] == null) continue;

            bool has = info != null && i < info.Length && info[i].HasAbility;
            any |= has;

            if (has) abilitySlots[i].Show(info[i].Icon, GameInput.Instance.AbilityLabel(i));
            else abilitySlots[i].Hide();
        }

        if (abilityRow != null) abilityRow.SetActive(any);
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
    private void OnAbilityUsed(int slot, float readyAt)
    {
        if (slot < 0 || slot >= abilityReadyAt.Length) return;

        abilityReadyAt[slot] = readyAt;
        abilityCooldownTotal[slot] = Mathf.Max(0.01f, readyAt - Time.time);
    }

    private void OnAbilityCharges(int slot, int available, int max)
    {
        if (abilitySlots == null || slot < 0 || slot >= abilitySlots.Length || abilitySlots[slot] == null) return;

        abilitySlots[slot].SetCharges(available, max);
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
