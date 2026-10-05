using UnityEngine;

/// <summary>
/// Quemadura de un enemigo: daño cada cierto tiempo durante unos segundos. Solo lógica (el tiempo se pasa desde
/// fuera), para poder probarla sin escena. Aplicarla de nuevo renueva la duración y conserva el mayor daño por tick.
/// </summary>
public class BurnState
{
    private float remaining;
    private float untilNextTick;
    private float tickSeconds = 0.5f;

    public bool IsBurning => remaining > 0f;

    /// <summary>Daño de cada tick. Sigue valiendo justo después de que la quemadura termina (hasta el próximo Apply o Clear).</summary>
    public int DamagePerTick { get; private set; }

    public void Apply(float seconds, int damagePerTick, float tick)
    {
        if (seconds <= 0f || damagePerTick <= 0 || tick <= 0f) return;

        if (!IsBurning)
        {
            remaining = seconds;
            DamagePerTick = damagePerTick;
            tickSeconds = tick;
            untilNextTick = tick;
            return;
        }

        remaining = Mathf.Max(remaining, seconds);
        DamagePerTick = Mathf.Max(DamagePerTick, damagePerTick);
    }

    /// <summary>Gasta tiempo y devuelve cuántos ticks tocan. Nunca da más ticks de los que caben en la duración.</summary>
    public int Advance(float deltaTime)
    {
        if (!IsBurning || deltaTime <= 0f) return 0;

        float step = Mathf.Min(deltaTime, remaining);
        remaining -= step;
        untilNextTick -= step;

        int ticks = 0;
        while (untilNextTick <= 1e-4f)
        {
            ticks++;
            untilNextTick += tickSeconds;
        }

        if (remaining <= 1e-5f) remaining = 0f;
        return ticks;
    }

    public void Clear()
    {
        remaining = 0f;
        DamagePerTick = 0;
    }
}
