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
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text shopPromptText;

    [Header("Maná (solo personajes con bastón)")]
    [SerializeField] private Slider manaBar;
    [SerializeField] private TMP_Text manaText;
    [SerializeField, Tooltip("Casilla de la habilidad: se muestra a la derecha, con la tecla debajo")]
    private AbilitySlotUI abilitySlot;

    private float abilityReadyAt;
    private float abilityCooldownTotal = 1f;

    private void OnEnable()
    {
        GameEvents.PlayerHealthChanged += SetPlayerHealth;
        GameEvents.BaseHealthChanged += SetBaseHealth;
        GameEvents.MoneyChanged += SetMoney;
        GameEvents.WaveStarted += SetWave;
        GameEvents.AmmoChanged += SetAmmo;
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
        GameEvents.AmmoChanged -= SetAmmo;
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
        ammoText.gameObject.SetActive(!usesMana);
        if (manaBar != null) manaBar.gameObject.SetActive(usesMana);

        abilityReadyAt = 0f;

        if (abilitySlot == null) return;

        if (usesMana && !string.IsNullOrEmpty(ability)) abilitySlot.Show(icon, GameInput.Instance.Ability1Label);
        else abilitySlot.Hide();
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

    private void SetWave(int wave) => waveText.text = "Horda " + wave;

    private void SetMoney(int amount) => moneyText.text = "$" + amount;

    private void SetAmmo(int current, int max, bool reloading)
    {
        ammoText.text = reloading ? "Recargando..." : current + " / " + max;
    }

    private void SetPrompt(string message)
    {
        bool visible = !string.IsNullOrEmpty(message);
        shopPromptText.gameObject.SetActive(visible);
        if (visible) shopPromptText.text = message;
    }
}
