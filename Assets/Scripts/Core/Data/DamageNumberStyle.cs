using UnityEngine;

/// <summary>
/// Animación de los números de daño sobre los enemigos: aparecen con un "pop" (crecen y se asientan), el golpe directo
/// empieza rojo y pasa a blanco, suben frenando y se desvanecen al final. Los golpes más fuertes salen más grandes.
/// Lógica pura (tiempos en segundos desde que aparece el número); la usa DamageNumbersUI.
/// </summary>
public static class DamageNumberStyle
{
    public const float Duration = 0.9f;

    // Color del golpe directo: rojo un instante, luego a blanco.
    public static readonly Color HitRed = new Color(1f, 0.12f, 0.08f);
    public const float RedHold = 0.1f;
    public const float WhiteAt = 0.35f;

    // Pop: de chico a grande muy rápido y vuelta al tamaño normal.
    public const float StartScale = 0.5f;
    public const float PopScale = 1.5f;
    public const float PopPeak = 0.07f;
    public const float SettleAt = 0.22f;

    public const float FadeStart = 0.6f;
    /// <summary>Metros que sube en total.</summary>
    public const float RiseHeight = 1.3f;

    /// <summary>Tamaño máximo por daño (sobre el normal).</summary>
    public const float MaxSize = 1.6f;

    public static Color HitColor(float t)
    {
        if (t <= RedHold) return HitRed;
        return Color.Lerp(HitRed, Color.white, Mathf.Clamp01((t - RedHold) / (WhiteAt - RedHold)));
    }

    public static float Alpha(float t)
    {
        if (t <= FadeStart) return 1f;
        return 1f - Mathf.Clamp01((t - FadeStart) / (Duration - FadeStart));
    }

    public static float Scale(float t)
    {
        if (t <= PopPeak) return Mathf.Lerp(StartScale, PopScale, EaseOut(t / PopPeak));
        if (t <= SettleAt) return Mathf.Lerp(PopScale, 1f, Mathf.SmoothStep(0f, 1f, (t - PopPeak) / (SettleAt - PopPeak)));
        return 1f;
    }

    /// <summary>Metros que lleva subidos: rápido al principio y frenando.</summary>
    public static float Rise(float t) => RiseHeight * EaseOut(Mathf.Clamp01(t / Duration));

    /// <summary>Tamaño según el daño: 1 con 1 de daño, +0,2 por cada cifra, hasta <see cref="MaxSize"/>.</summary>
    public static float SizeFor(int damage)
    {
        if (damage <= 1) return 1f;
        return Mathf.Min(MaxSize, 1f + 0.2f * Mathf.Log10(damage));
    }

    private static float EaseOut(float x) => 1f - (1f - x) * (1f - x);
}
