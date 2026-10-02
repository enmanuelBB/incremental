using UnityEngine;

/// <summary>
/// Dinero del jugador. Vive en SaveSystem.Data; el disco se escribe en puntos de control
/// (fin de oleada, comprar, salir) y no en cada kill.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    public int Money => SaveSystem.Data.money;

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
        GameEvents.EnemyKilled += AddMoney;
        GameEvents.WaveCompleted += OnWaveCompleted;
    }

    private void OnDisable()
    {
        GameEvents.EnemyKilled -= AddMoney;
        GameEvents.WaveCompleted -= OnWaveCompleted;
        SaveSystem.Save();
    }

    private void Start()
    {
        GameEvents.RaiseMoneyChanged(Money);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveSystem.Save();
    }

    public void AddMoney(int amount)
    {
        SaveSystem.Data.money += amount;
        GameEvents.RaiseMoneyChanged(Money);
        GameEvents.RaiseMoneyGained(amount);
    }

    public bool SpendMoney(int amount)
    {
        if (Money < amount) return false;

        SaveSystem.Data.money -= amount;
        GameEvents.RaiseMoneyChanged(Money);
        return true;
    }

    private void OnWaveCompleted(int wave) => SaveSystem.Save();
}
