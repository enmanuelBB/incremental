using System;
using UnityEngine;

public enum UpgradeType
{
    FireRate = 0,
    Reload = 1,
    Damage = 2,
    Bleed = 3
}

public static class UpgradeTypeInfo
{
    public const int Count = 4;

    public static string DisplayName(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate: return "Cadencia";
            case UpgradeType.Reload: return "Recarga";
            case UpgradeType.Damage: return "Daño";
            case UpgradeType.Bleed: return "Sangrado";
        }
        return type.ToString();
    }
}

/// <summary>Configuración de una mejora: cuánto cambia el stat por nivel y cuánto cuesta cada nivel.</summary>
[Serializable]
public class UpgradeStat
{
    [Tooltip("Cuánto cambia el stat por nivel (resta en cadencia y recarga, suma en daño)")]
    public float step = 0.02f;

    [Tooltip("Límite del stat (no aplica al daño)")]
    public float limit = 0.05f;

    public int basePrice = 100;
    public int priceIncrease = 50;

    [Tooltip("Si es mayor que 0, el precio crece de forma exponencial: basePrice × crecimiento^nivel (y priceIncrease se ignora). Con 0 el precio es lineal.")]
    public float priceGrowth = 0f;

    public int maxLevel = 5;

    /// <summary>Precio de subir DESDE el nivel indicado al siguiente. Redondeado a la decena en el modo exponencial.</summary>
    public int PriceAt(int level)
    {
        if (priceGrowth <= 0f) return basePrice + level * priceIncrease;

        float exact = basePrice * Mathf.Pow(priceGrowth, level);
        return Mathf.RoundToInt(exact / 10f) * 10;
    }
}
