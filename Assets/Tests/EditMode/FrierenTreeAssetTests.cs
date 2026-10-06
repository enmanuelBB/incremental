using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Valida el árbol real de Frieren (el asset), para que un error de edición no llegue a la partida.</summary>
[TestFixture]
public class FrierenTreeAssetTests
{
    private const string TreePath = "Assets/Data/Skills/Frieren_Tree.asset";
    private const string CharacterPath = "Assets/Data/Characters/Maga.asset";

    private static SkillTreeDefinition Tree
    {
        get
        {
            var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(TreePath);
            Assert.IsNotNull(tree, "Falta " + TreePath);
            return tree;
        }
    }

    [Test]
    public void Maga_PointsToTheTree()
    {
        Assert.AreSame(Tree, AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterPath).skillTree);
    }

    [Test]
    public void Tree_Has56NodesWithUniqueIdsAndText()
    {
        SkillNode[] nodes = Tree.nodes;
        Assert.AreEqual(56, nodes.Length);
        Assert.AreEqual(nodes.Length, nodes.Select(n => n.id).Distinct().Count(), "ids repetidos");
        foreach (SkillNode node in nodes)
        {
            Assert.IsFalse(string.IsNullOrEmpty(node.displayName), node.id + " sin nombre");
            Assert.IsFalse(string.IsNullOrEmpty(node.description), node.id + " sin descripción");
            Assert.GreaterOrEqual(node.cost, 1, node.id);
            Assert.IsNotEmpty(node.effects, node.id + " sin efecto");
        }
    }

    [Test]
    public void Tree_HasOneRootAndEverythingIsReachable()
    {
        SkillTreeDefinition tree = Tree;
        Assert.AreEqual(1, tree.nodes.Count(n => n.isRoot));
        Assert.AreEqual("core", tree.nodes.Single(n => n.isRoot).id);

        foreach (SkillNode node in tree.nodes)
            foreach (string other in node.connections)
                Assert.IsNotNull(tree.Find(other), node.id + " conecta con " + other + ", que no existe");

        // Todos los ids juntos forman un grafo conectado desde la raíz.
        Assert.IsTrue(SkillTreeRules.AllConnected(tree, tree.nodes.Select(n => n.id).ToList()));
    }

    [TestCase("zoltraak", 3)]
    [TestCase("mana", 3)]
    [TestCase("rayo", 3)]
    [TestCase("pulso", 4)]
    public void ChoiceGroups_HaveTheirOptionsAndNothingDependsOnThem(string group, int options)
    {
        SkillTreeDefinition tree = Tree;
        SkillNode[] members = tree.nodes.Where(n => n.choiceGroup == group).ToArray();
        Assert.AreEqual(options, members.Length, group);

        foreach (SkillNode member in members)
        {
            Assert.AreEqual(SkillNodeHalf.None, member.half);
            Assert.AreEqual(1, member.connections.Length, member.id + " cuelga de un solo nodo");
            // Ningún otro nodo declara conexión con una opción final.
            Assert.IsFalse(tree.nodes.Any(n => n != member && n.connections.Contains(member.id)), member.id + " tiene dependientes");
        }
        Assert.AreEqual(1, members.Select(m => m.connections[0]).Distinct().Count(), group + ": todas cuelgan del mismo nodo");
    }

    [TestCase("flor1", 2)]
    [TestCase("flor2", 3)]
    [TestCase("flor3", 4)]
    [TestCase("flor4", 5)]
    public void SplitNodes_HaveOneLeftAndOneRightHalfInTheSameSquare(string group, int cost)
    {
        SkillNode[] halves = Tree.nodes.Where(n => n.choiceGroup == group).ToArray();
        Assert.AreEqual(2, halves.Length);
        Assert.AreEqual(1, halves.Count(h => h.half == SkillNodeHalf.Left));
        Assert.AreEqual(1, halves.Count(h => h.half == SkillNodeHalf.Right));
        Assert.AreEqual(halves[0].position, halves[1].position);
        Assert.IsTrue(halves.All(h => h.cost == cost));
        Assert.AreEqual(SkillEffectType.FieldHeal, halves.Single(h => h.half == SkillNodeHalf.Left).effects[0].type);
        Assert.AreEqual(SkillEffectType.FieldSlow, halves.Single(h => h.half == SkillNodeHalf.Right).effects[0].type);
    }

    [Test]
    public void BuyingOneOptionPerGroup_Costs151()
    {
        int total = 0;
        var seen = new HashSet<string>();
        foreach (SkillNode node in Tree.nodes)
        {
            if (!string.IsNullOrEmpty(node.choiceGroup) && !seen.Add(node.choiceGroup)) continue;
            total += node.cost;
        }
        Assert.AreEqual(151, total);
    }

    [Test]
    public void FullBuild_HitsTheSpecCaps()
    {
        // Una opción por grupo (las de cura en la E): cura 3% + 4% = 7%; con las de freno: 40% + 40% = 80% (el tope).
        SkillTreeDefinition tree = Tree;
        var heal = new List<string>();
        var slow = new List<string>();
        var seen = new HashSet<string>();
        foreach (SkillNode node in tree.nodes)
        {
            if (node.half == SkillNodeHalf.Left) heal.Add(node.id);
            else if (node.half == SkillNodeHalf.Right) slow.Add(node.id);
            else if (string.IsNullOrEmpty(node.choiceGroup) || seen.Add(node.choiceGroup)) { heal.Add(node.id); slow.Add(node.id); }
        }

        TreeBonuses withHeal = SkillTreeRules.Compute(tree, heal);
        TreeBonuses withSlow = SkillTreeRules.Compute(tree, slow);
        Assert.AreEqual(0.04f, withHeal.FieldHealBonus, 1e-4f);
        Assert.AreEqual(0.4f, withSlow.FieldSlowBonus, 1e-4f);
        Assert.AreEqual(0.3f, withHeal.ZoltraakChargeReduction, 1e-4f);
        Assert.AreEqual(40f, withHeal.ManaMaxBonus, 1e-4f);
        Assert.AreEqual(16f, withHeal.PulseCooldownReduction, 1e-4f);
        Assert.AreEqual(0.15f, withHeal.DamagePercent, 1e-4f);   // 5 + 3 + 3 + 4
    }
}
