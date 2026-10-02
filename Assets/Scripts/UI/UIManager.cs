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

    private void OnEnable()
    {
        GameEvents.PlayerHealthChanged += SetPlayerHealth;
        GameEvents.BaseHealthChanged += SetBaseHealth;
        GameEvents.MoneyChanged += SetMoney;
        GameEvents.WaveStarted += SetWave;
        GameEvents.AmmoChanged += SetAmmo;
        GameEvents.PromptChanged += SetPrompt;
    }

    private void OnDisable()
    {
        GameEvents.PlayerHealthChanged -= SetPlayerHealth;
        GameEvents.BaseHealthChanged -= SetBaseHealth;
        GameEvents.MoneyChanged -= SetMoney;
        GameEvents.WaveStarted -= SetWave;
        GameEvents.AmmoChanged -= SetAmmo;
        GameEvents.PromptChanged -= SetPrompt;
    }

    private void SetPlayerHealth(int current, int max)
    {
        playerHealthBar.maxValue = max;
        playerHealthBar.value = current;
        playerHealthText.text = current + "/" + max;
    }

    private void SetBaseHealth(int current, int max)
    {
        baseHealthBar.maxValue = max;
        baseHealthBar.value = current;
        baseHealthText.text = current + "/" + max;
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
