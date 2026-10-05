using UnityEngine;

/// <summary>
/// En qué punto (tiempo normalizado, 0 a 1) va un clip de Frieren (los move y el Cast). Cada clip va de quieta (fotograma 0) al
/// movimiento máximo (fotograma 40) y vuelve a quieta (fotograma 79): mientras se mueve (o carga) sube hasta el máximo y se queda
/// ahí; al soltar sigue hasta el final y queda quieta. Solo visual.
/// </summary>
public static class MovePhase
{
    /// <summary>Fotograma 40 de 79: el movimiento máximo.</summary>
    public const float Peak = 40f / 79f;

    /// <summary>Avanza la fase. 'delta' = segundos transcurridos / duración del clip (fracción del clip que se recorre).</summary>
    public static float Step(float phase, bool moving, float delta)
    {
        if (moving)
        {
            // Subiendo, hasta el máximo; si estaba volviendo, desanda la vuelta hasta el máximo (mismas poses, sin saltos).
            return phase <= Peak ? Mathf.Min(Peak, phase + delta) : Mathf.Max(Peak, phase - delta);
        }

        if (phase <= 0f) return 0f;

        // Soltó antes de llegar al máximo: sigue desde el punto de la vuelta con la misma intensidad (sin pasar por el máximo).
        if (phase < Peak) phase = Peak + (1f - Peak) * (1f - phase / Peak);

        phase += delta;
        return phase >= 1f ? 0f : phase; // el último fotograma es la misma pose que el primero
    }
}
