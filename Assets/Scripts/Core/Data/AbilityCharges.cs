using UnityEngine;

/// <summary>
/// Cargas de una habilidad: se pueden lanzar varias veces seguidas y se recuperan de una en una, cada
/// <c>rechargeSeconds</c>. Con una sola carga se comporta como el enfriamiento de siempre (AbilityCooldown).
/// El tiempo se pasa desde fuera para poder probarlo.
/// </summary>
public class AbilityCharges
{
    private int max = 1;
    private float recharge = 1f;
    private int charges = 1;
    private float rechargeAt;

    public int Max => max;

    /// <summary>Cambia el máximo y el tiempo de recarga y deja todas las cargas llenas.</summary>
    public void Configure(int maxCharges, float rechargeSeconds, float now)
    {
        max = Mathf.Max(1, maxCharges);
        recharge = Mathf.Max(0f, rechargeSeconds);
        Reset(now);
    }

    public void Reset(float now)
    {
        charges = max;
        rechargeAt = now;
    }

    /// <summary>Gasta una carga. False si no queda ninguna.</summary>
    public bool TryUse(float now)
    {
        Refresh(now);
        if (charges <= 0) return false;

        // El temporizador de recarga arranca al salir del estado "lleno"; usar otra carga no lo reinicia.
        if (charges == max) rechargeAt = now + recharge;
        charges--;
        return true;
    }

    public int Available(float now)
    {
        Refresh(now);
        return charges;
    }

    /// <summary>Segundos que faltan para recuperar la próxima carga (0 si están todas).</summary>
    public float RechargeRemaining(float now)
    {
        Refresh(now);
        return charges >= max ? 0f : Mathf.Max(0f, rechargeAt - now);
    }

    private void Refresh(float now)
    {
        if (charges >= max) return;

        if (recharge <= 0f)
        {
            charges = max; // sin tiempo de recarga, vuelven todas al instante (evita un bucle sin fin)
            return;
        }

        while (charges < max && now >= rechargeAt)
        {
            charges++;
            rechargeAt += recharge;
        }
    }
}
