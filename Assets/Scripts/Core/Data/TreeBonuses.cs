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

    public float SpeedMultiplier => 1f + SpeedPercent;
    public float DamageMultiplier => 1f + DamagePercent;

    /// <summary>Sin bonos (valor para personajes sin árbol).</summary>
    public static TreeBonuses None => new TreeBonuses();
}
