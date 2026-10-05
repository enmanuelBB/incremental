using UnityEngine;

/// <summary>Reglas del dash de Guts. Solo geometría, para probarla sin escena.</summary>
public static class DashRules
{
    private const float MinInputSqr = 0.01f;   // 0,1 al cuadrado

    /// <summary>Dirección plana y normalizada del dash: la entrada de movimiento si la hay; si no, hacia donde mira. Cero si no hay ninguna.</summary>
    public static Vector3 Direction(Vector3 moveInput, Vector3 aimForward)
    {
        var move = new Vector3(moveInput.x, 0f, moveInput.z);
        if (move.sqrMagnitude >= MinInputSqr) return move.normalized;

        var aim = new Vector3(aimForward.x, 0f, aimForward.z);
        return aim.sqrMagnitude > 1e-6f ? aim.normalized : Vector3.zero;
    }
}
