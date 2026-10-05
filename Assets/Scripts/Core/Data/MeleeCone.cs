using UnityEngine;

/// <summary>Cono horizontal frente al jugador. Solo geometría, para poder probarla sin escena.</summary>
public static class MeleeCone
{
    /// <summary>
    /// ¿Queda el punto dentro del cono? Se ignora la altura. El radio del objetivo alarga el alcance y, si el objetivo
    /// ya toca al origen, cuenta como golpeado aunque esté fuera del arco (un enemigo pegado a Guts no se escapa).
    /// </summary>
    public static bool Contains(Vector3 origin, Vector3 forward, Vector3 point, float range, float arcDegrees, float pointRadius = 0f)
    {
        var flatForward = new Vector3(forward.x, 0f, forward.z);
        if (flatForward.sqrMagnitude < 1e-6f) return false;

        var toPoint = new Vector3(point.x - origin.x, 0f, point.z - origin.z);
        float distance = toPoint.magnitude;

        if (distance > range + pointRadius) return false;
        if (distance <= pointRadius) return true;

        return Vector3.Angle(flatForward, toPoint) <= arcDegrees * 0.5f;
    }
}
