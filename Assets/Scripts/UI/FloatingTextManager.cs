using UnityEngine;
using UnityEngine.Pool;

/// <summary>Muestra "+X" cerca del contador de dinero cada vez que se gana dinero.</summary>
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

    private static readonly Color BleedColor = new Color(0.95f, 0.15f, 0.15f);
    private static readonly Color BurnColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color PoisonColor = new Color(0.45f, 1f, 0.3f);

    private void OnEnable()
    {
        GameEvents.MoneyGained += ShowMoneyGain;
        GameEvents.BleedTick += ShowBleedTick;
        GameEvents.BurnTick += ShowBurnTick;
        GameEvents.PoisonTick += ShowPoisonTick;
    }

    private void OnDisable()
    {
        GameEvents.MoneyGained -= ShowMoneyGain;
        GameEvents.BleedTick -= ShowBleedTick;
        GameEvents.BurnTick -= ShowBurnTick;
        GameEvents.PoisonTick -= ShowPoisonTick;
    }

    // Número naranja sobre el enemigo que se quema (igual que el sangrado, pero de otro color).
    private void ShowBurnTick(Vector3 worldPosition, int damage)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f) return;

        pool.Get().Show(damage.ToString(), screen, pool.Release, BurnColor);
    }

    // Número verde sobre el enemigo envenenado por el campo de flores de Frieren.
    private void ShowPoisonTick(Vector3 worldPosition, int damage)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f) return;

        pool.Get().Show(damage.ToString(), screen, pool.Release, PoisonColor);
    }

    // Número rojo sobre el enemigo. El canvas es Screen Space Overlay: la posición del texto es la de pantalla.
    private void ShowBleedTick(Vector3 worldPosition, int damage)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f) return; // detrás de la cámara

        pool.Get().Show(damage.ToString(), screen, pool.Release, BleedColor);
    }

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
