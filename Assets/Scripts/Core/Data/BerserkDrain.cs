using UnityEngine;

/// <summary>
/// Drenaje de vida de la armadura Berserker: convierte "X% de la vida máxima por segundo" en puntos enteros, guardando el resto
/// para no perder ni duplicar puntos con pasos de tiempo pequeños. Nunca deja menos de 1 de vida.
/// </summary>
public class BerserkDrain
{
    private float carry;

    /// <summary>Puntos enteros que toca perder tras este paso de tiempo.</summary>
    public int Advance(float deltaTime, int maxHealth, float fractionPerSecond)
    {
        if (deltaTime <= 0f || maxHealth <= 0 || fractionPerSecond <= 0f) return 0;

        carry += maxHealth * fractionPerSecond * deltaTime;
        int whole = Mathf.FloorToInt(carry + 1e-4f);
        carry -= whole;
        return whole;
    }

    public void Reset() => carry = 0f;

    /// <summary>Limita lo que se pierde para que la vida nunca baje de 1.</summary>
    public static int Allowed(int current, int toLose) => Mathf.Clamp(toLose, 0, Mathf.Max(0, current - 1));

    /// <summary>Verdadero cuando la vida llegó a 1 o menos: la armadura se apaga sola.</summary>
    public static bool ShouldStop(int current) => current <= 1;
}
