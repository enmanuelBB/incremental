using System;
using UnityEngine;

public enum UpgradeType
{
    FireRate = 0,
    Reload = 1,
    Damage = 2
}

public static class UpgradeTypeInfo
{
    public const int Count = 3;

    public static string DisplayName(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate: return "Cadencia";
            case UpgradeType.Reload: return "Recarga";
            case UpgradeType.Damage: return "Daño";
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
    public int maxLevel = 5;

    public int PriceAt(int level) => basePrice + level * priceIncrease;
}
