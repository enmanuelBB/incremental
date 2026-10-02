using UnityEngine;

/// <summary>Enfriamiento de una habilidad. El tiempo actual se pasa desde fuera para poder probarlo.</summary>
public class AbilityCooldown
{
    private float readyAt;

    public bool IsReady(float now) => now >= readyAt;
    public float Remaining(float now) => Mathf.Max(0f, readyAt - now);

    public void Start(float now, float duration) => readyAt = now + duration;
    public void Reset() => readyAt = 0f;
}
