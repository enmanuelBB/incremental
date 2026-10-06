using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Grupos de "elige 1" y nodos divididos del árbol (pensados para Frieren; Alucard y Guts no tienen grupos).</summary>
[TestFixture]
public class SkillTreeChoiceTests
{
    private SkillTreeDefinition tree;

    // core(raíz) - a - { x1 | x2 } (grupo "g")      x1 - t (t depende de x1)
    //              a - { h1L | h1R } (grupo "h1") - { h2L | h2R } (grupo "h2"): cada mitad conecta con las dos de abajo
    [SetUp]
    public void SetUp()
    {
        tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
        tree.nodes = new[]
        {
            Node("core", 1, "", SkillNodeHalf.None, 0f, "a"),
            Node("a", 1, "", SkillNodeHalf.None, 0f, "core"),
            Node("x1", 2, "g", SkillNodeHalf.None, 5f, "a"),
            Node("x2", 3, "g", SkillNodeHalf.None, 7f, "a"),
            Node("t", 1, "", SkillNodeHalf.None, 0f, "x1"),
            Node("h1L", 1, "h1", SkillNodeHalf.Left, 0f, "a"),
            Node("h1R", 1, "h1", SkillNodeHalf.Right, 0f, "a"),
            Node("h2L", 1, "h2", SkillNodeHalf.Left, 0f, "h1L", "h1R"),
            Node("h2R", 1, "h2", SkillNodeHalf.Right, 0f, "h1L", "h1R")
        };
        tree.nodes[0].isRoot = true;
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(tree);

    private static SkillNode Node(string id, int cost, string group, SkillNodeHalf half, float health, params string[] connections) =>
        new SkillNode
        {
            id = id, displayName = id, cost = cost, choiceGroup = group, half = half, connections = connections,
            effects = health > 0f ? new[] { new SkillEffect { type = SkillEffectType.MaxHealth, value = health } } : new SkillEffect[0]
        };

    private static CharacterSave Save(int points, params string[] owned)
    {
        var save = new CharacterSave { id = "test", skillPoints = points };
        save.skillNodes.AddRange(owned);
        return save;
    }

    [Test]
    public void CanBuy_WithTheRivalOwned_IsChoiceTaken()
    {
        Assert.AreEqual(SkillBuyBlock.ChoiceTaken, SkillTreeRules.CanBuy(tree, Save(10, "core", "a", "x1"), "x2"));
    }

    [Test]
    public void CanBuy_WithoutRival_StillWorks()
    {
        Assert.AreEqual(SkillBuyBlock.None, SkillTreeRules.CanBuy(tree, Save(10, "core", "a"), "x2"));
    }

    [Test]
    public void OwnedRival_FindsTheOtherOptionOfTheGroup()
    {
        Assert.AreEqual("x1", SkillTreeRules.OwnedRival(tree, new List<string> { "core", "a", "x1" }, "x2"));
        Assert.IsNull(SkillTreeRules.OwnedRival(tree, new List<string> { "core", "a" }, "x2"));
        Assert.IsNull(SkillTreeRules.OwnedRival(tree, new List<string> { "core", "a" }, "a"));
    }

    [Test]
    public void TrySwap_RefundsTheRivalAndChargesTheNewOne_KeepingItsPlaceInTheList()
    {
        CharacterSave save = Save(1, "core", "a", "x1");

        Assert.IsTrue(SkillTreeRules.TrySwap(tree, save, "x2"));
        Assert.AreEqual(0, save.skillPoints);   // 1 + 2 - 3
        CollectionAssert.AreEqual(new[] { "core", "a", "x2" }, save.skillNodes);
    }

    [Test]
    public void TrySwap_NotEnoughEvenWithTheRefund_IsNoPoints()
    {
        CharacterSave save = Save(0, "core", "a", "x1");

        Assert.AreEqual(SkillSwapBlock.NoPoints, SkillTreeRules.CanSwap(tree, save, "x2"));
        Assert.IsFalse(SkillTreeRules.TrySwap(tree, save, "x2"));
        CollectionAssert.AreEqual(new[] { "core", "a", "x1" }, save.skillNodes);
        Assert.AreEqual(0, save.skillPoints);
    }

    [Test]
    public void TrySwap_ThatWouldOrphanABoughtNode_IsWouldDisconnect()
    {
        Assert.AreEqual(SkillSwapBlock.WouldDisconnect, SkillTreeRules.CanSwap(tree, Save(10, "core", "a", "x1", "t"), "x2"));
    }

    [Test]
    public void TrySwap_SplitHalfInTheMiddleOfTheChain_KeepsTheHalvesAbove()
    {
        CharacterSave save = Save(0, "core", "a", "h1L", "h2L");

        Assert.IsTrue(SkillTreeRules.TrySwap(tree, save, "h1R"));
        CollectionAssert.AreEqual(new[] { "core", "a", "h1R", "h2L" }, save.skillNodes);
    }

    [Test]
    public void CanSwap_WithoutAnOwnedRival_IsNoRival()
    {
        Assert.AreEqual(SkillSwapBlock.NoRival, SkillTreeRules.CanSwap(tree, Save(10, "core", "a"), "x2"));
        Assert.AreEqual(SkillSwapBlock.NoRival, SkillTreeRules.CanSwap(tree, Save(10, "core", "a", "x2"), "x2"));
        Assert.AreEqual(SkillSwapBlock.UnknownNode, SkillTreeRules.CanSwap(tree, Save(10, "core"), "nada"));
    }

    [Test]
    public void CanSwap_ToAnOptionWhoseNeighbourIsNotBought_IsLocked()
    {
        // h2R cuelga de h1L/h1R: con h2L comprado "a mano" sin h1, cambiar a h2R no está desbloqueado.
        Assert.AreEqual(SkillSwapBlock.Locked, SkillTreeRules.CanSwap(tree, Save(10, "core", "a", "h2L"), "h2R"));
    }

    [Test]
    public void Compute_TwoOptionsOfTheSameGroup_CountsOnlyTheFirst()
    {
        Assert.AreEqual(7, SkillTreeRules.Compute(tree, new[] { "core", "a", "x2", "x1" }).MaxHealthBonus);
        Assert.AreEqual(5, SkillTreeRules.Compute(tree, new[] { "core", "a", "x1", "x2" }).MaxHealthBonus);
    }

    [Test]
    public void Reset_ReturnsEverything_IncludingBothOptionsOfABrokenSave()
    {
        CharacterSave save = Save(0, "core", "a", "x1", "x2");

        Assert.AreEqual(1 + 1 + 2 + 3, SkillTreeRules.Reset(tree, save));
        Assert.AreEqual(7, save.skillPoints);
        Assert.IsEmpty(save.skillNodes);
    }

    // Las dos mitades de un nodo dividido son un solo cuadrado en pantalla: comparten la línea que llega y la que sale.
    [Test]
    public void SquareOf_TheTwoHalvesShareTheirSquare_OtherNodesAreTheirOwn()
    {
        Assert.AreEqual(SkillTreeRules.SquareOf(tree.Find("h1L")), SkillTreeRules.SquareOf(tree.Find("h1R")));
        Assert.AreNotEqual(SkillTreeRules.SquareOf(tree.Find("h1L")), SkillTreeRules.SquareOf(tree.Find("h2L")));
        Assert.AreEqual("x1", SkillTreeRules.SquareOf(tree.Find("x1")));
        Assert.AreNotEqual(SkillTreeRules.SquareOf(tree.Find("x1")), SkillTreeRules.SquareOf(tree.Find("x2")));
    }

    // La línea entre dos cuadrados se ilumina si alguna pareja conectada de esos cuadrados está comprada (curar o frenar, da igual).
    [Test]
    public void LinkLit_LightsTheLineWhicheverHalfWasBought()
    {
        var owned = new List<string> { "core", "a", "h1L", "h2R" };
        string a = SkillTreeRules.SquareOf(tree.Find("a"));
        string h1 = SkillTreeRules.SquareOf(tree.Find("h1L"));
        string h2 = SkillTreeRules.SquareOf(tree.Find("h2L"));

        Assert.IsTrue(SkillTreeRules.LinkLit(tree, owned, a, h1));
        Assert.IsTrue(SkillTreeRules.LinkLit(tree, owned, h1, h2));
        Assert.IsTrue(SkillTreeRules.LinkLit(tree, new List<string> { "core", "a", "h1R", "h2R" }, h1, h2));
        Assert.IsFalse(SkillTreeRules.LinkLit(tree, new List<string> { "core", "a", "h1L" }, h1, h2));
        Assert.IsFalse(SkillTreeRules.LinkLit(tree, new List<string> { "core", "a" }, a, h1));
    }

    [Test]
    public void AllConnected_DetectsAnIsland()
    {
        Assert.IsTrue(SkillTreeRules.AllConnected(tree, new List<string> { "core", "a", "x1", "t" }));
        Assert.IsFalse(SkillTreeRules.AllConnected(tree, new List<string> { "core", "a", "t" }));
        Assert.IsTrue(SkillTreeRules.AllConnected(tree, new List<string> { "core", "fantasma" }));   // ids desconocidos se ignoran
    }
}
