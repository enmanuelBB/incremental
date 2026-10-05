using UnityEngine;

/// <summary>Reglas del aturdimiento. El azar y el tiempo se pasan desde fuera para poder probarlos.</summary>
public static class StunRules
{
    /// <summary>Verdadero si el dado (0 a 1) cae dentro de la probabilidad. Probabilidad 0 nunca aturde.</summary>
    public static bool Roll(float chance, float roll01) => roll01 < chance;
}

/// <summary>Cuánto tiempo sigue aturdido un enemigo. Un aturdimiento nuevo nunca acorta el actual.</summary>
public class StunTimer
{
    private float until;

    public bool IsStunned(float now) => now < until;

    public void Apply(float now, float seconds)
    {
        if (seconds <= 0f) return;
        until = Mathf.Max(until, now + seconds);
    }

    public void Clear() => until = 0f;
}
