using UnityEngine;

/// <summary>Levitar de Frieren: altura sobre el suelo con un balanceo suave. Solo visual.</summary>
public static class HoverMath
{
    /// <summary>Altura (m) en el instante 'time'. Sin altura (0 o menos) no levita: devuelve 0 aunque haya balanceo.</summary>
    public static float Offset(float time, float height, float bob, float period)
    {
        if (height <= 0f) return 0f;

        return height + bob * Mathf.Sin(2f * Mathf.PI * time / Mathf.Max(0.1f, period));
    }
}
