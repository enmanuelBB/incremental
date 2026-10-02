using UnityEngine;

/// <summary>Efecto con duración (niebla, definitiva). El tiempo actual se pasa desde fuera para poder probarlo.</summary>
public class TimedEffect
{
    private float endsAt;
    private bool running;

    /// <summary>Verdadero mientras no haya llegado el momento de terminar.</summary>
    public bool IsActive(float now) => running && now < endsAt;

    public float Remaining(float now) => IsActive(now) ? endsAt - now : 0f;

    public void Start(float now, float duration)
    {
        endsAt = now + Mathf.Max(0f, duration);
        running = true;
    }

    /// <summary>
    /// Devuelve true UNA sola vez cuando el efecto acaba por tiempo (para limpiar lo que activó).
    /// Si no estaba corriendo o aún dura, devuelve false.
    /// </summary>
    public bool TryFinish(float now)
    {
        if (!running || now < endsAt) return false;

        running = false;
        return true;
    }

    /// <summary>Corta el efecto sin esperar a que acabe.</summary>
    public void Cancel() => running = false;
}
