using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Por qué no se puede comprar un nodo.</summary>
public enum SkillBuyBlock
{
    None,
    UnknownNode,
    Owned,
    Locked,
    NoPoints,
    ChoiceTaken
}

/// <summary>Por qué no se puede cambiar una opción de un grupo de "elige 1" por otra.</summary>
public enum SkillSwapBlock
{
    None,
    UnknownNode,
    NoRival,
    Locked,
    NoPoints,
    WouldDisconnect
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
        if (OwnedRival(tree, save.skillNodes, nodeId) != null) return SkillBuyBlock.ChoiceTaken;
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

    /// <summary>El id del nodo comprado del mismo grupo de "elige 1" que 'nodeId' (null si no tiene grupo o no hay ninguno).</summary>
    public static string OwnedRival(SkillTreeDefinition tree, ICollection<string> owned, string nodeId)
    {
        SkillNode node = tree != null ? tree.Find(nodeId) : null;
        if (node == null || string.IsNullOrEmpty(node.choiceGroup)) return null;

        foreach (string id in owned)
        {
            if (id == nodeId) continue;
            SkillNode other = tree.Find(id);
            if (other != null && other.choiceGroup == node.choiceGroup) return id;
        }
        return null;
    }

    /// <summary>
    /// Si se puede cambiar la opción comprada del grupo por 'nodeId': hay un rival comprado, el nodo nuevo queda desbloqueado sin
    /// contar al rival, alcanzan los puntos con el reembolso, y todo lo comprado sigue conectado a una raíz.
    /// </summary>
    public static SkillSwapBlock CanSwap(SkillTreeDefinition tree, CharacterSave save, string nodeId)
    {
        SkillNode node = tree != null ? tree.Find(nodeId) : null;
        if (node == null) return SkillSwapBlock.UnknownNode;

        string rivalId = OwnedRival(tree, save.skillNodes, nodeId);
        if (rivalId == null || save.skillNodes.Contains(nodeId)) return SkillSwapBlock.NoRival;

        var after = new HashSet<string>(save.skillNodes);
        after.Remove(rivalId);
        if (!IsUnlocked(tree, node, after)) return SkillSwapBlock.Locked;
        if (save.skillPoints + tree.Find(rivalId).cost < node.cost) return SkillSwapBlock.NoPoints;

        after.Add(nodeId);
        return AllConnected(tree, after) ? SkillSwapBlock.None : SkillSwapBlock.WouldDisconnect;
    }

    /// <summary>Devuelve el rival y compra 'nodeId' en su mismo lugar de la lista. Todo o nada.</summary>
    public static bool TrySwap(SkillTreeDefinition tree, CharacterSave save, string nodeId)
    {
        if (CanSwap(tree, save, nodeId) != SkillSwapBlock.None) return false;

        string rivalId = OwnedRival(tree, save.skillNodes, nodeId);
        save.skillPoints += tree.Find(rivalId).cost - tree.Find(nodeId).cost;
        save.skillNodes[save.skillNodes.IndexOf(rivalId)] = nodeId;
        return true;
    }

    /// <summary>True si cada nodo comprado (que exista en el árbol) llega a una raíz pasando solo por nodos comprados.</summary>
    public static bool AllConnected(SkillTreeDefinition tree, ICollection<string> owned)
    {
        var reached = new HashSet<string>();
        var queue = new Queue<string>();
        foreach (string id in owned)
        {
            SkillNode node = tree.Find(id);
            if (node != null && node.isRoot && reached.Add(id)) queue.Enqueue(id);
        }

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            foreach (string id in owned)
            {
                if (reached.Contains(id)) continue;
                SkillNode node = tree.Find(id);
                if (node != null && Linked(tree, current, node) && reached.Add(id)) queue.Enqueue(id);
            }
        }

        foreach (string id in owned)
            if (tree.Find(id) != null && !reached.Contains(id)) return false;
        return true;
    }

    /// <summary>El cuadrado de un nodo en pantalla: las dos mitades de un nodo dividido comparten el suyo (su grupo); los demás, su id.</summary>
    public static string SquareOf(SkillNode node) =>
        node.half != SkillNodeHalf.None && !string.IsNullOrEmpty(node.choiceGroup) ? "#" + node.choiceGroup : node.id;

    /// <summary>Si la línea entre dos cuadrados va iluminada: alguna pareja de nodos de esos cuadrados, conectados entre sí, está comprada.</summary>
    public static bool LinkLit(SkillTreeDefinition tree, ICollection<string> owned, string squareA, string squareB)
    {
        foreach (string a in owned)
        {
            SkillNode nodeA = tree.Find(a);
            if (nodeA == null || SquareOf(nodeA) != squareA) continue;

            foreach (string b in owned)
            {
                SkillNode nodeB = tree.Find(b);
                if (nodeB != null && SquareOf(nodeB) == squareB && Linked(tree, a, nodeB)) return true;
            }
        }
        return false;
    }

    // La conexión vale en los dos sentidos: basta con que uno de los dos la declare.
    private static bool Linked(SkillTreeDefinition tree, string aId, SkillNode b)
    {
        if (b.connections != null && Array.IndexOf(b.connections, aId) >= 0) return true;
        SkillNode a = tree.Find(aId);
        return a != null && a.connections != null && Array.IndexOf(a.connections, b.id) >= 0;
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

        var seenGroups = new HashSet<string>();
        foreach (string id in ownedIds)
        {
            SkillNode node = tree.Find(id);
            if (node == null || node.effects == null) continue;
            // Un guardado con dos opciones del mismo grupo (editado a mano): solo cuenta la primera.
            if (!string.IsNullOrEmpty(node.choiceGroup) && !seenGroups.Add(node.choiceGroup)) continue;

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
            case SkillEffectType.StunChance: b.StunChanceBonus += v; break;
            case SkillEffectType.StunnedDamagePercent: b.StunnedDamagePercent += v; break;
            case SkillEffectType.FuryGainPercent: b.FuryGainPercent += v; break;
            case SkillEffectType.SoulHealBonus: b.SoulHealBonus += v; break;
            case SkillEffectType.FlameBurnSeconds: b.FlameBurnSecondsBonus += v; break;
            case SkillEffectType.FlameCone: b.FlameConeBonus += v; break;
            case SkillEffectType.FlameBurnDamagePercent: b.FlameBurnDamagePercent += v; break;
            case SkillEffectType.FlameRange: b.FlameRangeBonus += v; break;
            case SkillEffectType.DashExtraCharges: b.DashExtraCharges += Mathf.RoundToInt(v); break;
            case SkillEffectType.DashExtraShots: b.DashExtraShots += Mathf.RoundToInt(v); break;
            case SkillEffectType.DashDistance: b.DashDistanceBonus += v; break;
            case SkillEffectType.DashCooldown: b.DashCooldownReduction += v; break;
            case SkillEffectType.BerserkDamage: b.BerserkDamageBonus += v; break;
            case SkillEffectType.BerserkDamageTaken: b.BerserkDamageTakenReduction += v; break;
            case SkillEffectType.RoarStun: b.RoarStunBonus += v; break;
            case SkillEffectType.RoarRadius: b.RoarRadiusBonus += v; break;
            case SkillEffectType.ZoltraakDamagePercent: b.ZoltraakDamagePercent += v; break;
            case SkillEffectType.ZoltraakChargeTime: b.ZoltraakChargeReduction += v; break;
            case SkillEffectType.ZoltraakRadius: b.ZoltraakRadiusBonus += v; break;
            case SkillEffectType.ZoltraakOvercharge: b.ZoltraakOvercharge |= v > 0f; break;
            case SkillEffectType.ZoltraakEcho: b.ZoltraakEchoFraction = Mathf.Max(b.ZoltraakEchoFraction, v); break;
            case SkillEffectType.ZoltraakFrost: b.ZoltraakFrostSlow = Mathf.Max(b.ZoltraakFrostSlow, v); break;
            case SkillEffectType.ManaMax: b.ManaMaxBonus += v; break;
            case SkillEffectType.ManaRegen: b.ManaRegenBonus += v; break;
            case SkillEffectType.ManaCostPercent: b.ManaCostReduction += v; break;
            case SkillEffectType.ManaOnKill: b.ManaOnKill += v; break;
            case SkillEffectType.ManaFocus: b.ManaFocusMultiplier = Mathf.Max(b.ManaFocusMultiplier, v); break;
            case SkillEffectType.BeamDamagePercent: b.BeamDamagePercent += v; break;
            case SkillEffectType.BeamRadius: b.BeamRadiusBonus += v; break;
            case SkillEffectType.BeamCooldown: b.BeamCooldownReduction += v; break;
            case SkillEffectType.BeamExtraCharges: b.BeamExtraCharges += Mathf.RoundToInt(v); break;
            case SkillEffectType.BeamFrost: b.BeamFrostSlow = Mathf.Max(b.BeamFrostSlow, v); break;
            case SkillEffectType.BeamPierceDamage: b.BeamPierceDamage += v; break;
            case SkillEffectType.FieldRadius: b.FieldRadiusBonus += v; break;
            case SkillEffectType.FieldDuration: b.FieldDurationBonus += v; break;
            case SkillEffectType.FieldCooldown: b.FieldCooldownReduction += v; break;
            case SkillEffectType.FieldHeal: b.FieldHealBonus += v; break;
            case SkillEffectType.FieldSlow: b.FieldSlowBonus += v; break;
            case SkillEffectType.FieldPoison: b.FieldPoison = Mathf.Max(b.FieldPoison, v); break;
            case SkillEffectType.FieldPoisonDamagePercent: b.FieldPoisonDamagePercent += v; break;
            case SkillEffectType.PulseDuration: b.PulseDurationBonus += v; break;
            case SkillEffectType.PulseStun: b.PulseStunBonus += v; break;
            case SkillEffectType.PulseCooldown: b.PulseCooldownReduction += v; break;
            case SkillEffectType.PulseWholeMap: b.PulseWholeMap |= v > 0f; break;
            case SkillEffectType.PulseZoltraakRain: if (v > 0f) b.PulseRainInterval = v; break;
            case SkillEffectType.PulseFinalBlast: b.PulseFinalBlastMultiplier = Mathf.Max(b.PulseFinalBlastMultiplier, v); break;
            case SkillEffectType.PulseMark: b.PulseMarkBonus = Mathf.Max(b.PulseMarkBonus, v); break;
        }
    }
}
