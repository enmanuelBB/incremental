using UnityEngine;

/// <summary>Reglas de las almas de Guts: cuánto curan y cuándo se recogen. Solo lógica, para probarla sin escena.</summary>
public static class SoulRules
{
    /// <summary>Vida que cura un alma: fracción de la vida máxima (x multiplicador si viene de un jefe). Mínimo 1; 0 si la fracción es ≤ 0.</summary>
    public static int HealAmount(int maxHealth, float fraction, bool isBoss, float bossMultiplier)
    {
        if (fraction <= 0f) return 0;

        float amount = maxHealth * fraction * (isBoss ? bossMultiplier : 1f);
        return Mathf.Max(1, Mathf.RoundToInt(amount));
    }

    /// <summary>¿Está el alma al alcance del jugador? Distancia horizontal ≤ radio; la altura no cuenta.</summary>
    public static bool IsWithinReach(Vector3 playerPosition, Vector3 soulPosition, float radius)
    {
        float dx = soulPosition.x - playerPosition.x;
        float dz = soulPosition.z - playerPosition.z;
        return dx * dx + dz * dz <= radius * radius + 1e-6f;
    }
}
