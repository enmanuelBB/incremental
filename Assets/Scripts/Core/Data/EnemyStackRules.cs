using UnityEngine;

/// <summary>Un enemigo visto desde arriba, para decidir quién se sube encima de quién.</summary>
public struct StackBody
{
    /// <summary>Posición en planta (x, z).</summary>
    public Vector2 Position;
    public float Radius;
    /// <summary>Alto del cuerpo: lo que suma a la torre quien se sube encima.</summary>
    public float Height;
    /// <summary>Altura de apoyo actual (0 = en el suelo).</summary>
    public float Lift;
    /// <summary>A igual altura, el de menor prioridad queda abajo (se usa la distancia a su objetivo).</summary>
    public float Priority;
    public bool CanClimb;
    public bool CanSupport;
}

/// <summary>
/// Apilado de enemigos estilo Megabonk: el que se superpone en planta con otro que ya está debajo se sube encima.
/// Lógica pura; la aplica EnemyCrowd cada fotograma.
/// </summary>
public static class EnemyStackRules
{
    private static int[] order = new int[64];

    /// <summary>
    /// Altura de apoyo que le toca a cada enemigo. Se procesan de abajo hacia arriba (altura actual y, a igualdad,
    /// prioridad), y cada uno se apoya en la parte de arriba del más alto ya procesado con el que se superpone, sin
    /// apoyar los pies por encima de <paramref name="maxLift"/>.
    /// </summary>
    /// <param name="overlapFactor">Se superponen si la distancia en planta es menor que esto x la suma de los radios.</param>
    public static void TargetLifts(StackBody[] bodies, float overlapFactor, float maxLift, float[] result)
    {
        TargetLifts(bodies, bodies.Length, overlapFactor, maxLift, result, null);
    }

    /// <summary>
    /// Igual, pero solo con los primeros <paramref name="count"/> (para reutilizar arreglos sin crear basura). Si se
    /// pasa <paramref name="blockedBy"/>, guarda para cada uno el índice del enemigo que no lo deja subir más por el
    /// tope (o -1): hay que correrlo de lado, lejos de ese, para que el montón se ensanche.
    /// </summary>
    public static void TargetLifts(StackBody[] bodies, int count, float overlapFactor, float maxLift, float[] result, int[] blockedBy)
    {
        if (order.Length < count) order = new int[Mathf.NextPowerOfTwo(count)];

        // Inserción: entre fotogramas el orden casi no cambia, así que sale casi lineal.
        for (int i = 0; i < count; i++)
        {
            int j = i - 1;
            while (j >= 0 && Below(bodies[i], bodies[order[j]])) { order[j + 1] = order[j]; j--; }
            order[j + 1] = i;
        }

        for (int k = 0; k < count; k++)
        {
            int i = order[k];
            StackBody body = bodies[i];
            float lift = 0f;
            int blocker = -1;
            float blockerTop = 0f;

            if (body.CanClimb)
            {
                for (int p = 0; p < k; p++)
                {
                    int s = order[p];
                    StackBody support = bodies[s];
                    if (!support.CanSupport) continue;

                    float reach = overlapFactor * (body.Radius + support.Radius);
                    if ((body.Position - support.Position).sqrMagnitude >= reach * reach) continue;

                    float top = result[s] + support.Height;
                    if (top <= maxLift + 1e-4f) lift = Mathf.Max(lift, top);
                    else if (blocker < 0 || top > blockerTop) { blocker = s; blockerTop = top; }
                }
            }

            result[i] = lift;
            if (blockedBy != null) blockedBy[i] = blocker;
        }
    }

    // ¿Va a debajo de "other"? Primero el que está más abajo; a igual altura, el de menor prioridad.
    private static bool Below(StackBody body, StackBody other)
    {
        if (!Mathf.Approximately(body.Lift, other.Lift)) return body.Lift < other.Lift;
        return body.Priority < other.Priority;
    }

    /// <summary>Acerca la altura actual a la buscada: sube a <paramref name="upSpeed"/> y cae a <paramref name="downSpeed"/> m/s.</summary>
    public static float Step(float current, float target, float deltaTime, float upSpeed, float downSpeed)
    {
        float speed = target > current ? upSpeed : downSpeed;
        return Mathf.MoveTowards(current, target, speed * deltaTime);
    }
}
