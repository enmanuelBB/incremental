using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rayo que atraviesa enemigos: daña a todos los que toca a lo largo de una línea recta y se detiene
/// en la primera pared (cualquier sólido que no sea un enemigo ni el propio jugador).
/// </summary>
public static class PiercingBeam
{
    private sealed class DistanceComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }

    private static readonly RaycastHit[] castBuffer = new RaycastHit[64];
    private static readonly Collider[] overlapBuffer = new Collider[32];
    private static readonly DistanceComparer comparer = new DistanceComparer();
    private static readonly List<EnemyAI> damaged = new List<EnemyAI>();

    /// <param name="ignoreRoot">Transform del jugador, para que el rayo no se golpee a sí mismo.</param>
    /// <param name="hitEnemies">Opcional: recibe los enemigos dañados (para pruebas).</param>
    /// <param name="pierceBonusPerEnemy">Perforación creciente (árbol de Frieren): daño extra por cada enemigo ya atravesado.</param>
    /// <param name="frostSlow">Rayo gélido (árbol de Frieren): ralentización a cada enemigo atravesado (0 = ninguna).</param>
    /// <returns>Punto donde termina el rayo: el alcance máximo o la pared que lo detuvo.</returns>
    public static Vector3 Cast(Ray ray, float range, float radius, int damage, Transform ignoreRoot, List<EnemyAI> hitEnemies = null,
        float pierceBonusPerEnemy = 0f, float frostSlow = 0f)
    {
        damaged.Clear();
        Vector3 end = ray.origin + ray.direction * range;

        // Un SphereCast no reporta lo que ya está tocando la esfera al salir (un enemigo pegado al bastón),
        // así que esos se buscan aparte.
        int overlaps = Physics.OverlapSphereNonAlloc(ray.origin, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps; i++)
        {
            EnemyAI enemy = overlapBuffer[i].GetComponentInParent<EnemyAI>();
            if (enemy != null && !damaged.Contains(enemy)) damaged.Add(enemy);
        }

        int count = Physics.SphereCastNonAlloc(ray, radius, castBuffer, range, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(castBuffer, 0, count, comparer);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = castBuffer[i];
            if (ignoreRoot != null && hit.transform.IsChildOf(ignoreRoot)) continue;

            EnemyAI enemy = hit.collider.GetComponentInParent<EnemyAI>();
            if (enemy != null)
            {
                if (!damaged.Contains(enemy)) damaged.Add(enemy);
                continue;
            }

            // Una pared: el rayo termina aquí y lo que quede detrás no se toca.
            end = ray.origin + ray.direction * hit.distance;
            break;
        }

        // El daño se aplica al final: matar a un enemigo lo devuelve al pool y desactiva su objeto.
        // 'damaged' va del más cercano al más lejano: Perforación creciente suma por cada enemigo previo.
        for (int i = 0; i < damaged.Count; i++)
        {
            EnemyAI enemy = damaged[i];
            if (frostSlow > 0f) enemy.ApplySlow(frostSlow, FrierenTreeMath.FrostSeconds);
            enemy.TakeDamage(Mathf.RoundToInt(damage * FrierenTreeMath.PierceMultiplier(i, pierceBonusPerEnemy)));
        }
        if (hitEnemies != null) hitEnemies.AddRange(damaged);

        return end;
    }
}
