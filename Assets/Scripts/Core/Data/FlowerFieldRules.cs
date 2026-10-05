using UnityEngine;

/// <summary>Reglas del campo de flores de Frieren. Todo se mide en horizontal: la altura no cuenta.</summary>
public static class FlowerFieldRules
{
    /// <summary>Deja el punto donde está si queda a 'range' metros o menos del origen; si no, lo acerca hasta el límite (misma dirección y altura).</summary>
    public static Vector3 ClampPoint(Vector3 origin, Vector3 point, float range)
    {
        Vector3 flat = new Vector3(point.x - origin.x, 0f, point.z - origin.z);
        if (flat.magnitude <= range) return point;

        Vector3 limited = origin + flat.normalized * range;
        limited.y = point.y;
        return limited;
    }

    /// <summary>Si el punto está dentro del círculo (el margen suma al radio: sirve para el tamaño del cuerpo de un enemigo).</summary>
    public static bool Contains(Vector3 center, float radius, Vector3 point, float margin = 0f) =>
        new Vector2(point.x - center.x, point.z - center.z).magnitude <= radius + margin;
}

/// <summary>Cura por tiempo en números enteros: lo que no alcanza a ser 1 punto en un tick se acumula para el siguiente.</summary>
public class HealOverTime
{
    private float carry;

    /// <summary>Puntos de vida (enteros) que toca curar en este tick. 'fractionPerSecond' es fracción de la vida máxima.</summary>
    public int Add(int maxHealth, float fractionPerSecond, float deltaTime)
    {
        carry += maxHealth * fractionPerSecond * deltaTime;

        int whole = Mathf.FloorToInt(carry + 1e-4f);
        carry -= whole;
        return Mathf.Max(0, whole);
    }

    public void Reset() => carry = 0f;
}
