/// <summary>
/// Suma de los efectos de todos los nodos comprados de un árbol. Lo calcula SkillTreeRules.Compute y el juego lo
/// consulta; sin nodos, todo es neutro (multiplicadores en 1, el resto en 0).
/// </summary>
public struct TreeBonuses
{
    public int MaxHealthBonus;
    public float SpeedPercent;
    public float DamagePercent;

    public int HeavyShotExtraBullets;
    public int HeavyShotExtraCharges;
    public float HeavyShotCooldownReduction;

    public int MistBleedOnPass;
    public float MistSlow;
    public float MistDurationBonus;
    public float MistCooldownReduction;

    public float UltDurationBonus;
    public float UltCooldownReduction;
    public float UltLifeStealBonus;

    /// <summary>Probabilidad extra de aturdir (0,2 = +20 puntos). Lo usará el árbol de Guts; por ahora siempre 0.</summary>
    public float StunChanceBonus;

    public float StunnedDamagePercent;
    public float FuryGainPercent;
    public float SoulHealBonus;
    public float FlameBurnSecondsBonus;
    public float FlameConeBonus;
    public float FlameBurnDamagePercent;
    public float FlameRangeBonus;
    public int DashExtraCharges;
    public int DashExtraShots;
    public float DashDistanceBonus;
    public float DashCooldownReduction;
    public float BerserkDamageBonus;
    public float BerserkDamageTakenReduction;
    public float RoarStunBonus;
    public float RoarRadiusBonus;

    /// <summary>Daño a un enemigo con el bono de Guts a los aturdidos: x(1 + bono) si el objetivo está aturdido.</summary>
    public int ScaleVsStunned(int damage, bool targetStunned) =>
        targetStunned && StunnedDamagePercent > 0f ? UnityEngine.Mathf.RoundToInt(damage * (1f + StunnedDamagePercent)) : damage;

    public float SpeedMultiplier => 1f + SpeedPercent;
    public float DamageMultiplier => 1f + DamagePercent;

    /// <summary>Sin bonos (valor para personajes sin árbol).</summary>
    public static TreeBonuses None => new TreeBonuses();
}
