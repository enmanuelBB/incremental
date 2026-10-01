using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD. Se actualiza solo escuchando GameEvents; no lo llama nadie directamente.</summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private Slider playerHealthBar;
    [SerializeField] private Slider baseHealthBar;
    [SerializeField] private TMP_Text playerHealthText;
    [SerializeField] private TMP_Text baseHealthText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text shopPromptText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.PlayerHealthChanged += SetPlayerHealth;
        GameEvents.BaseHealthChanged += SetBaseHealth;
        GameEvents.MoneyChanged += SetMoney;
        GameEvents.WaveStarted += SetWave;
        GameEvents.AmmoChanged += SetAmmo;
    }

    private void OnDisable()
    {
        GameEvents.PlayerHealthChanged -= SetPlayerHealth;
        GameEvents.BaseHealthChanged -= SetBaseHealth;
        GameEvents.MoneyChanged -= SetMoney;
        GameEvents.WaveStarted -= SetWave;
        GameEvents.AmmoChanged -= SetAmmo;
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

    public void ShowShopPrompt(string message)
    {
        shopPromptText.gameObject.SetActive(true);
        shopPromptText.text = message;
    }

    public void HideShopPrompt()
    {
        shopPromptText.gameObject.SetActive(false);
    }
}
