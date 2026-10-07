using UnityEngine;
using UnityEngine.Pool;

/// <summary>Muestra "+X" cerca del contador de dinero cada vez que se gana dinero. Los números de daño están en DamageNumbersUI.</summary>
public class FloatingTextManager : MonoBehaviour
{
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField, Tooltip("Dónde aparece, cerca del texto de dinero")] private Transform spawnPoint;
    [SerializeField, Tooltip("Si se asigna (el panel de dinero), el texto aparece a su derecha aunque el panel crezca con cifras largas")]
    private RectTransform followRect;
    [SerializeField] private float followOffset = 50f;

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
        Vector3 position = spawnPoint.position;
        if (followRect != null)
        {
            // El panel crece con el número nuevo, pero su layout se recalcula un frame después: se fuerza para medir bien su borde.
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(followRect);
            Rect rect = followRect.rect;
            position = followRect.TransformPoint(new Vector3(rect.xMax + followOffset, rect.center.y, 0f));
        }

        FloatingText text = pool.Get();
        text.Show("+" + HudFormat.Money(amount), position, pool.Release);
    }
}
