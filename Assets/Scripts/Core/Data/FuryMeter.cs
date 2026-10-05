using UnityEngine;

/// <summary>
/// Barra de Furia de Guts: se carga con sus golpes y, llena, el siguiente golpe sale potenciado. Solo lógica, para
/// poder probarla sin escena. Un medidor de máximo 0 (personajes sin Furia) nunca está lleno ni acumula nada.
/// </summary>
public class FuryMeter
{
    public float Max { get; }
    public float Current { get; private set; }
    public float Fraction => Max > 0f ? Current / Max : 0f;
    public bool IsFull => Max > 0f && Current >= Max;

    public FuryMeter(float max)
    {
        Max = Mathf.Max(0f, max);
    }

    /// <summary>Suma Furia sin pasar del máximo. Las cantidades ≤ 0 se ignoran.</summary>
    public void Add(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Min(Max, Current + amount);
    }

    /// <summary>Si está llena la vacía y devuelve true; si no, no hace nada y devuelve false.</summary>
    public bool TryConsume()
    {
        if (!IsFull) return false;

        Current = 0f;
        return true;
    }

    public void Reset() => Current = 0f;

    /// <summary>Furia que da un golpe: perEnemy por enemigo golpeado, sin pasar de maxPerSwing. Sin enemigos, 0.</summary>
    public static float GainForHits(int enemiesHit, float perEnemy, float maxPerSwing)
    {
        if (enemiesHit <= 0 || perEnemy <= 0f) return 0f;
        return Mathf.Min(enemiesHit * perEnemy, maxPerSwing);
    }
}
