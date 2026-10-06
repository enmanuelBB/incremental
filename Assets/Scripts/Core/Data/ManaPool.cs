using UnityEngine;

/// <summary>Reserva de maná que se regenera con el tiempo. Lógica pura: el reloj lo mueve quien la usa.</summary>
public class ManaPool
{
    public float Max { get; private set; }
    public float Current { get; private set; }
    public float RegenPerSecond { get; private set; }

    public ManaPool(float max, float regenPerSecond)
    {
        Max = max;
        RegenPerSecond = regenPerSecond;
        Current = max;
    }

    /// <summary>Cambia el máximo y la regeneración (por ejemplo al subir un nivel de mejora).</summary>
    public void Configure(float max, float regenPerSecond)
    {
        Max = max;
        RegenPerSecond = regenPerSecond;
        Current = Mathf.Min(Current, Max);
    }

    public void Refill() => Current = Max;

    /// <summary>Suma maná sin pasar del máximo (Absorción). Los valores negativos se ignoran.</summary>
    public void Gain(float amount) => Current = Mathf.Min(Max, Current + Mathf.Max(0f, amount));

    public void Tick(float deltaTime)
    {
        Current = Mathf.Min(Max, Current + RegenPerSecond * deltaTime);
    }

    /// <summary>Gasta el maná si alcanza. Devuelve false (sin gastar nada) si no.</summary>
    public bool TrySpend(float cost)
    {
        if (Current + 0.0001f < cost) return false;
        Current = Mathf.Max(0f, Current - cost);
        return true;
    }
}
