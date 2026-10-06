using UnityEngine;

/// <summary>Ralentizaciones que se pisan: mientras una dura, solo la reemplaza (y la renueva) otra igual o más fuerte.</summary>
public static class SlowRules
{
    /// <param name="activeMultiplier">Multiplicador de velocidad actual (0,4 = va al 40%).</param>
    /// <param name="active">Si esa ralentización sigue vigente.</param>
    /// <param name="newFraction">Fracción que quita la nueva (0,6 = -60%).</param>
    public static bool ShouldReplace(float activeMultiplier, bool active, float newFraction) =>
        !active || Mathf.Clamp(1f - newFraction, 0.1f, 1f) <= activeMultiplier + 1e-4f;

    /// <summary>
    /// Cuándo termina la ralentización tras aplicar la nueva (solo se llama si ShouldReplace). Una igual a la vigente la renueva sin
    /// acortarla (el 40% del campo no corta los 3 s de la Escarcha); una más fuerte manda con su propia duración.
    /// </summary>
    public static float EndTime(float activeMultiplier, bool active, float activeUntil, float newFraction, float now, float seconds)
    {
        bool same = active && Mathf.Abs(Mathf.Clamp(1f - newFraction, 0.1f, 1f) - activeMultiplier) <= 1e-4f;
        return same ? Mathf.Max(activeUntil, now + seconds) : now + seconds;
    }
}
