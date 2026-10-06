using UnityEngine;

/// <summary>
/// Fórmulas y números fijos del árbol de Frieren. Lógica pura: la usa el juego y la prueban los tests. Lo que se ajusta por
/// nodo vive en el asset del árbol (el valor de cada efecto); aquí quedan los tiempos y topes que no dependen del nodo.
/// </summary>
public static class FrierenTreeMath
{
    public const float MinChargeSeconds = 0.3f;
    public const float FieldSlowCap = 0.8f;
    public const float PierceCap = 0.9f;
    public const float FocusDelay = 3f;
    public const float EchoDelay = 0.3f;
    public const float EchoRadiusFraction = 0.7f;
    public const float FrostSeconds = 3f;
    public const float PoisonLinger = 3f;
    public const float PoisonTick = 0.5f;
    public const float RainDropSeconds = 0.4f;
    public const float RainHeight = 12f;
    /// <summary>Radio que usa el pulso con Dominio: abarca el mapa entero.</summary>
    public const float WholeMapRadius = 1000f;

    /// <summary>Carga del Zoltraak con la reducción del árbol; nunca baja de MinChargeSeconds.</summary>
    public static float ChargeSeconds(float baseSeconds, float reduction) => Mathf.Max(MinChargeSeconds, baseSeconds - reduction);

    /// <summary>Ralentización del campo con el bono de las mitades; tope FieldSlowCap.</summary>
    public static float FieldSlow(float baseFraction, float bonus) => Mathf.Min(FieldSlowCap, baseFraction + bonus);

    /// <summary>Costo de maná con Eficiencia (0,4 = -40%), redondeado a entero.</summary>
    public static float ManaCost(float baseCost, float reduction) => Mathf.Round(baseCost * (1f - Mathf.Clamp01(reduction)));

    /// <summary>Perforación creciente: el enemigo número 'index' (0 = el primero) recibe x(1 + min(index·perEnemy, tope)).</summary>
    public static float PierceMultiplier(int index, float perEnemy) => 1f + Mathf.Min(Mathf.Max(0, index) * perEnemy, PierceCap);

    /// <summary>Daño de cada tick de veneno (cada PoisonTick s): fracción del disparo básico por segundo, con el bono. 0 sin el nodo.</summary>
    public static int PoisonTickDamage(int basicDamage, float fractionPerSecond, float bonusPercent) =>
        fractionPerSecond <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(basicDamage * fractionPerSecond * (1f + bonusPercent) * PoisonTick));

    /// <summary>Concentración: el multiplicador si pasaron FocusDelay s desde el último daño; si no (o sin el nodo), 1.</summary>
    public static float RegenMultiplier(float focusMultiplier, float now, float lastDamagedAt) =>
        focusMultiplier > 1f && now - lastDamagedAt >= FocusDelay ? focusMultiplier : 1f;

    /// <summary>Daño con un bono en fracción (0,3 = +30%), redondeado.</summary>
    public static int Scale(int damage, float bonusPercent) => bonusPercent <= 0f ? damage : Mathf.RoundToInt(damage * (1f + bonusPercent));
}
