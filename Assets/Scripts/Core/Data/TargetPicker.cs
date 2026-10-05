using System.Collections.Generic;
using UnityEngine;

/// <summary>Elige el objetivo más cercano. Solo geometría, para probarla sin escena.</summary>
public static class TargetPicker
{
    /// <summary>
    /// Plan de una salva de disparos: los índices de los objetivos en orden, uno distinto por disparo del más cercano al más lejano
    /// (los que están al alcance); si hay menos objetivos que disparos, los que sobran repiten el más cercano. Vacío si no hay
    /// ninguno al alcance o no hay disparos. En un empate gana el orden de la lista.
    /// </summary>
    public static List<int> PlanShots(Vector3 origin, IList<Vector3> candidates, float maxRange, int shots)
    {
        var plan = new List<int>();
        if (shots <= 0 || candidates == null) return plan;

        float limit = maxRange * maxRange + 1e-6f;
        var inRange = new List<int>();
        var sqr = new float[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            float dx = candidates[i].x - origin.x;
            float dz = candidates[i].z - origin.z;
            sqr[i] = dx * dx + dz * dz;
            if (sqr[i] <= limit) inRange.Add(i);
        }

        if (inRange.Count == 0) return plan;

        inRange.Sort((a, b) => sqr[a] != sqr[b] ? sqr[a].CompareTo(sqr[b]) : a.CompareTo(b));
        for (int shot = 0; shot < shots; shot++) plan.Add(shot < inRange.Count ? inRange[shot] : inRange[0]);
        return plan;
    }

    /// <summary>Índice del candidato más cercano (distancia horizontal) dentro del alcance, o -1. En un empate gana el primero.</summary>
    public static int NearestIndex(Vector3 origin, IList<Vector3> candidates, float maxRange)
    {
        if (candidates == null) return -1;

        float limit = maxRange * maxRange;
        float bestSq = float.MaxValue;
        int best = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            float dx = candidates[i].x - origin.x;
            float dz = candidates[i].z - origin.z;
            float sq = dx * dx + dz * dz;
            if (sq <= limit + 1e-6f && sq < bestSq)
            {
                best = i;
                bestSq = sq;
            }
        }
        return best;
    }
}
