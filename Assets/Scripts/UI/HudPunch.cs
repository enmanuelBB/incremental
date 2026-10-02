using UnityEngine;

/// <summary>Pulso breve de escala (crece y vuelve) para llamar la atención sobre un elemento del HUD que cambió.</summary>
public class HudPunch : MonoBehaviour
{
    [SerializeField] private float peakScale = 1.25f;
    [SerializeField] private float duration = 0.3f;

    private Vector3 baseScale;
    private float elapsed = -1f;

    private void Awake() => baseScale = transform.localScale;

    private void OnDisable()
    {
        elapsed = -1f;
        transform.localScale = baseScale;
    }

    public void Play() => elapsed = 0f;

    // Tiempo sin escalar: el HUD sigue animándose aunque el juego esté en cámara lenta.
    private void Update()
    {
        if (elapsed < 0f) return;

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        if (t >= 1f)
        {
            transform.localScale = baseScale;
            elapsed = -1f;
            return;
        }

        float ease = 1f - (1f - t) * (1f - t);
        transform.localScale = baseScale * Mathf.Lerp(peakScale, 1f, ease);
    }
}
