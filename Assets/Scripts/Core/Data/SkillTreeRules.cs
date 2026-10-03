using System.Collections.Generic;
using UnityEngine;

/// <summary>Por qué no se puede comprar un nodo.</summary>
public enum SkillBuyBlock
{
    None,
    UnknownNode,
    Owned,
    Locked,
    NoPoints
}

/// <summary>
/// Reglas del árbol de habilidades: qué se puede comprar, comprar, reiniciar y sumar los bonos.
/// Lógica pura sobre CharacterSave y SkillTreeDefinition, sin escena, para poder probarla.
/// </summary>
public static class SkillTreeRules
{
    /// <summary>Puntos que da completar una oleada: 1 en las oleadas 1 a 4, 2 en las 5 a 9, 3 en las 10 a 14...</summary>
    public static int PointsForWave(int wave) => 1 + Mathf.Max(0, wave) / 5;

    /// <summary>Raíz, o con algún vecino (que exista en el árbol) ya comprado. La conexión vale en los dos sentidos.</summary>
    public static bool IsUnlocked(SkillTreeDefinition tree, SkillNode node, ICollection<string> owned)
    {
        if (node == null) return false;
        if (node.isRoot) return true;

        if (node.connections != null)
        {
            foreach (string neighbour in node.connections)
                if (tree.Find(neighbour) != null && owned.Contains(neighbour)) return true;
        }

        // La conexión también cuenta si solo la declara el otro nodo.
        if (tree.nodes != null)
        {
            foreach (SkillNode other in tree.nodes)
            {
                if (other == null || other.connections == null || !owned.Contains(other.id)) continue;
                if (System.Array.IndexOf(other.connections, node.id) >= 0) return true;
            }
        }

        return false;
    }

    public static SkillBuyBlock CanBuy(SkillTreeDefinition tree, CharacterSave save, string nodeId)
    {
        SkillNode node = tree != null ? tree.Find(nodeId) : null;
        if (node == null) return SkillBuyBlock.UnknownNode;
        if (save.skillNodes.Contains(nodeId)) return SkillBuyBlock.Owned;
        if (!IsUnlocked(tree, node, save.skillNodes)) return SkillBuyBlock.Locked;
        if (save.skillPoints < node.cost) return SkillBuyBlock.NoPoints;
        return SkillBuyBlock.None;
    }

    public static bool TryBuy(SkillTreeDefinition tree, CharacterSave save, string nodeId)
    {
        if (CanBuy(tree, save, nodeId) != SkillBuyBlock.None) return false;

        save.skillPoints -= tree.Find(nodeId).cost;
        save.skillNodes.Add(nodeId);
        return true;
    }

    /// <summary>Devuelve los puntos de todo lo comprado y vacía la lista. Un nodo que ya no existe en el árbol se descarta sin reembolso.</summary>
    public static int Reset(SkillTreeDefinition tree, CharacterSave save)
    {
        int refunded = 0;
        foreach (string id in save.skillNodes)
        {
            SkillNode node = tree != null ? tree.Find(id) : null;
            if (node != null) refunded += node.cost;
        }

        save.skillPoints += refunded;
        save.skillNodes.Clear();
        return refunded;
    }

    /// <summary>Suma los efectos de los nodos comprados. Los ids que no existen en el árbol se ignoran.</summary>
    public static TreeBonuses Compute(SkillTreeDefinition tree, IEnumerable<string> ownedIds)
    {
        var bonuses = new TreeBonuses();
        if (tree == null || ownedIds == null) return bonuses;

        foreach (string id in ownedIds)
        {
            SkillNode node = tree.Find(id);
            if (node == null || node.effects == null) continue;

            foreach (SkillEffect effect in node.effects) Apply(ref bonuses, effect);
        }

        return bonuses;
    }

    private static void Apply(ref TreeBonuses b, SkillEffect effect)
    {
        float v = effect.value;
        switch (effect.type)
        {
            case SkillEffectType.MaxHealth: b.MaxHealthBonus += Mathf.RoundToInt(v); break;
            case SkillEffectType.MoveSpeedPercent: b.SpeedPercent += v; break;
            case SkillEffectType.DamagePercent: b.DamagePercent += v; break;
            case SkillEffectType.HeavyShotExtraBullets: b.HeavyShotExtraBullets += Mathf.RoundToInt(v); break;
            case SkillEffectType.HeavyShotExtraCharges: b.HeavyShotExtraCharges += Mathf.RoundToInt(v); break;
            case SkillEffectType.HeavyShotCooldown: b.HeavyShotCooldownReduction += v; break;
            case SkillEffectType.MistBleedOnPass: b.MistBleedOnPass += Mathf.RoundToInt(v); break;
            case SkillEffectType.MistSlow: b.MistSlow += v; break;
            case SkillEffectType.MistDuration: b.MistDurationBonus += v; break;
            case SkillEffectType.MistCooldown: b.MistCooldownReduction += v; break;
            case SkillEffectType.UltDuration: b.UltDurationBonus += v; break;
            case SkillEffectType.UltCooldown: b.UltCooldownReduction += v; break;
            case SkillEffectType.UltLifeSteal: b.UltLifeStealBonus += v; break;
        }
    }
}
