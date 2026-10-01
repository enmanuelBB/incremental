using UnityEngine;
using UnityEngine.Pool;

/// <summary>Muestra "+X" cerca del contador de dinero cada vez que se gana dinero.</summary>
public class FloatingTextManager : MonoBehaviour
{
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField, Tooltip("Dónde aparece, cerca del texto de dinero")] private Transform spawnPoint;

    private ObjectPool<FloatingText> pool;

    private void Awake()
    {
        pool = new ObjectPool<FloatingText>(
            createFunc: () => Instantiate(floatingTextPrefab, spawnPoint.parent).GetComponent<FloatingText>(),
            actionOnGet: ft => ft.gameObject.SetActive(true),
            actionOnRelease: ft => ft.gameObject.SetActive(false),
            actionOnDestroy: ft => Destroy(ft.gameObject));
    }

    private void OnEnable() => GameEvents.MoneyGained += ShowMoneyGain;
    private void OnDisable() => GameEvents.MoneyGained -= ShowMoneyGain;

    private void ShowMoneyGain(int amount)
    {
        FloatingText text = pool.Get();
        text.Show("+" + amount, spawnPoint.position, pool.Release);
    }
}
