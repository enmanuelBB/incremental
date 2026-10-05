using UnityEngine;

/// <summary>
/// Carga del Zoltraak de Frieren: se mantiene el clic y la fracción sube de 0 a 1 en 'seconds'. Al soltar se obtiene la
/// fracción y la carga se reinicia. Lógica pura: el reloj lo mueve quien la usa.
/// </summary>
public class ZoltraakCharge
{
    private float chargeSeconds = 1f;
    private float held;

    public bool IsCharging { get; private set; }

    public float Fraction => Mathf.Clamp01(held / chargeSeconds);

    public void Begin(float seconds)
    {
        chargeSeconds = Mathf.Max(0.01f, seconds);
        held = 0f;
        IsCharging = true;
    }

    public void Tick(float deltaTime)
    {
        if (IsCharging) held += deltaTime;
    }

    /// <summary>Suelta la carga: devuelve la fracción alcanzada (0 si no se estaba cargando) y se reinicia.</summary>
    public float Release()
    {
        float fraction = IsCharging ? Fraction : 0f;
        Cancel();
        return fraction;
    }

    public void Cancel()
    {
        IsCharging = false;
        held = 0f;
    }
}
