using UnityEngine;

/// <summary>
/// Pilas de sangrado de UN enemigo. Lógica pura (sin MonoBehaviour) para poder probarla.
/// Las pilas son permanentes: duran hasta que el enemigo muere (o se reutiliza del pool y se llama a Clear).
/// Cada 'TickSeconds' el enemigo recibe pilas × daño por pila.
/// </summary>
public class BleedStacks
{
    public const float TickSeconds = 1f;

    /// <summary>Fracción del daño de una bala que hace cada pila por tick.</summary>
    public const float DamageFraction = 0.10f;

    /// <summary>Tope de pilas con el nivel de sangrado 1.</summary>
    public const int BaseCap = 5;

    public int Stacks { get; private set; }
    public int Cap { get; private set; } = BaseCap;

    float timer;

    public bool IsBleeding => Stacks > 0;

    /// <summary>Suma pilas hasta el tope. Devuelve cuántas se sumaron de verdad.</summary>
    public int Add(int amount, int cap)
    {
        Cap = Mathf.Max(1, cap);
        if (amount <= 0) return 0;

        int before = Stacks;
        Stacks = Mathf.Min(Stacks + amount, Cap);

        // El primer sangrado arranca su reloj de cero: el primer tick llega 1 s después.
        if (before == 0) timer = 0f;
        return Stacks - before;
    }

    /// <summary>
    /// Avanza el reloj. Devuelve cuántos ticks cumplieron en este avance (normalmente 0 o 1;
    /// más de uno solo si el avance es mayor que un segundo, p. ej. tras una pausa larga).
    /// </summary>
    public int Advance(float deltaTime)
    {
        if (Stacks <= 0 || deltaTime <= 0f) return 0;

        timer += deltaTime;
        int ticks = 0;
        while (timer >= TickSeconds)
        {
            timer -= TickSeconds;
            ticks++;
        }
        return ticks;
    }

    /// <summary>Daño de un tick con las pilas actuales.</summary>
    public int TickDamage(int damagePerStack) => Stacks * Mathf.Max(0, damagePerStack);

    /// <summary>0 sin sangrado y 1 con las pilas al tope: intensidad del tinte rojo.</summary>
    public float Intensity => Cap <= 0 ? 0f : Mathf.Clamp01((float)Stacks / Cap);

    public void Clear()
    {
        Stacks = 0;
        timer = 0f;
        Cap = BaseCap;
    }

    /// <summary>Daño de cada pila por tick: el 10% del daño de una bala, mínimo 1.</summary>
    public static int DamagePerStack(int bulletDamage) =>
        Mathf.Max(1, Mathf.RoundToInt(bulletDamage * DamageFraction));

    /// <summary>Tope de pilas por enemigo: 5 en el nivel 1 y +1 por cada nivel de sangrado.</summary>
    public static int CapForLevel(int bleedLevel) => BaseCap - 1 + Mathf.Max(1, bleedLevel);
}
