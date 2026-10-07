using UnityEngine;

/// <summary>Hacia dónde sale un rayo que nace en el arma (no en la cámara) para pasar por el punto de la mira. Lógica pura.</summary>
public static class BeamAim
{
    /// <summary>
    /// Del origen (el bastón) al punto que marca la mira, con su altura: si se apunta arriba sube, si se apunta al
    /// suelo baja. Si ese punto está encima del arma o detrás de ella (en tercera persona la mira puede tocar algo
    /// entre la cámara y el jugador), sale en la dirección de la mira.
    /// </summary>
    public static Vector3 Direction(Vector3 origin, Vector3 target, Vector3 aimDirection)
    {
        Vector3 direction = target - origin;
        if (direction.sqrMagnitude < 0.01f || Vector3.Dot(direction, aimDirection) <= 0f) return aimDirection.normalized;
        return direction.normalized;
    }
}
