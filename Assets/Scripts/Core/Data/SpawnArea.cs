using UnityEngine;

/// <summary>
/// Zona rectangular donde aparecen los enemigos. Lógica pura: dado el centro y el tamaño (x, z) y dos números entre
/// 0 y 1, devuelve un punto dentro del rectángulo. SpawnZone le pasa números al azar.
/// </summary>
public static class SpawnArea
{
    /// <param name="center">Centro del rectángulo en el plano (x, z).</param>
    /// <param name="size">Ancho (x) y largo (z); un tamaño negativo cuenta como su valor absoluto.</param>
    /// <param name="u">0 = borde izquierdo, 1 = borde derecho (se limita a 0 y 1).</param>
    /// <param name="v">0 = borde cercano, 1 = borde lejano (se limita a 0 y 1).</param>
    public static Vector2 PointIn(Vector2 center, Vector2 size, float u, float v)
    {
        float width = Mathf.Abs(size.x);
        float depth = Mathf.Abs(size.y);

        return new Vector2(
            center.x + (Mathf.Clamp01(u) - 0.5f) * width,
            center.y + (Mathf.Clamp01(v) - 0.5f) * depth);
    }
}
