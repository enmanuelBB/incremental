using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

/// <summary>Valida el árbol real de Guts (el asset), para que un error de edición no llegue a la partida.</summary>
[TestFixture]
public class GutsTreeAssetTests
{
    private const string TreePath = "Assets/Data/Skills/Guts_Tree.asset";
    private const string CharacterPath = "Assets/Data/Characters/Guts.asset";

    private SkillTreeDefinition Tree => AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(TreePath);

    [Test]
    public void Tree_Exists_AndGutsLinksToIt()
    {
        Assert.IsNotNull(Tree, "Falta " + TreePath);
        var guts = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterPath);
        Assert.AreSame(Tree, guts.skillTree);
    }

    [Test]
    public void Tree_Has39Nodes_WithUniqueIds_AndTextForEach()
    {
        Assert.AreEqual(39, Tree.nodes.Length);
        List<string> ids = Tree.nodes.Select(n => n.id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
        Assert.IsTrue(Tree.nodes.All(n => !string.IsNullOrEmpty(n.id) && !string.IsNullOrEmpty(n.displayName) && !string.IsNullOrEmpty(n.description)));
        Assert.IsTrue(Tree.nodes.All(n => n.cost >= 1));
    }

    [Test]
    public void Tree_EveryConnectionPointsToAnExistingNode()
    {
        foreach (SkillNode node in Tree.nodes)
            foreach (string neighbour in node.connections)
                Assert.IsNotNull(Tree.Find(neighbour), node.id + " se conecta a '" + neighbour + "', que no existe");
    }

    [Test]
    public void Tree_HasOneRoot_AndEveryNodeIsReachableFromIt()
    {
        SkillNode[] roots = Tree.nodes.Where(n => n.isRoot).ToArray();
        Assert.AreEqual(1, roots.Length);

        var owned = new List<string> { roots[0].id };
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (SkillNode node in Tree.nodes)
            {
                if (owned.Contains(node.id) || !SkillTreeRules.IsUnlocked(Tree, node, owned)) continue;
                owned.Add(node.id);
                grew = true;
            }
        }
        Assert.AreEqual(Tree.nodes.Length, owned.Count, "hay nodos que no se pueden alcanzar desde la raíz");
    }

    [Test]
    public void Tree_StunNodesAddUpToExactlyFifteenPercent()
    {
        float sum = Tree.nodes.SelectMany(n => n.effects).Where(e => e.type == SkillEffectType.StunChance).Sum(e => e.value);
        Assert.AreEqual(0.15f, sum, 1e-4f);

        var sword = AssetDatabase.LoadAssetAtPath<SwordDefinition>("Assets/Data/Weapons/Espada.asset");
        Assert.AreEqual(0.30f, sword.StunChanceAt(sword.reloadUpgrade.maxLevel, sum), 1e-4f, "tienda al máximo + árbol completo = 30%");
    }

    [Test]
    public void Tree_DashChainRequiresTheDoubleShotBeforeTheTripleShot()
    {
        SkillNode triple = Tree.Find("e3");
        SkillNode doubleShot = Tree.Find("e2");
        Assert.IsNotNull(triple);
        Assert.IsNotNull(doubleShot);
        Assert.IsTrue(triple.effects.Any(e => e.type == SkillEffectType.DashExtraShots));
        Assert.IsTrue(doubleShot.effects.Any(e => e.type == SkillEffectType.DashExtraShots));

        Assert.IsFalse(SkillTreeRules.IsUnlocked(Tree, triple, new List<string> { "core", "e1" }), "el disparo triple no debe poder comprarse sin el doble");
        Assert.IsTrue(SkillTreeRules.IsUnlocked(Tree, triple, new List<string> { "core", "e1", "e2" }));
    }

    [Test]
    public void Tree_DashShotsAddUpToTwo_AndDashChargesToOne()
    {
        var all = Tree.nodes.SelectMany(n => n.effects).ToList();
        Assert.AreEqual(2f, all.Where(e => e.type == SkillEffectType.DashExtraShots).Sum(e => e.value), 1e-4f);
        Assert.AreEqual(1f, all.Where(e => e.type == SkillEffectType.DashExtraCharges).Sum(e => e.value), 1e-4f);
    }

    [Test]
    public void Tree_UsesEveryGutsEffectType()
    {
        var present = Tree.nodes.SelectMany(n => n.effects).Select(e => e.type).Distinct().ToList();

        // Los efectos de Guts son los que van de StunChance a RoarRadius (después vienen los de Frieren).
        foreach (SkillEffectType type in System.Enum.GetValues(typeof(SkillEffectType)))
        {
            if (type < SkillEffectType.StunChance || type > SkillEffectType.RoarRadius) continue;
            Assert.Contains(type, present, "El árbol de Guts no usa " + type);
        }
    }

    [Test]
    public void Tree_NothingReducesTheArmorDrain()
    {
        // No existe un efecto de drenaje de la armadura, y el rango tampoco lo baja (Armadura.asset: drainPerRank = 0).
        string[] names = System.Enum.GetNames(typeof(SkillEffectType));
        Assert.IsFalse(names.Any(n => n.ToLowerInvariant().Contains("drain")), "ningún efecto del árbol debe reducir el drenaje");

        var armor = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Armadura.asset");
        Assert.AreEqual(armor.BerserkDrainAt(1), armor.BerserkDrainAt(3), 1e-6f);
    }
}
