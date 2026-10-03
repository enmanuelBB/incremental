using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class SkillTreeTests
{
    private SkillTreeDefinition tree;

    // Grafo de prueba:   core(raíz) - a - b        c (isla: sin conexión a nadie)
    [SetUp]
    public void SetUp()
    {
        tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
        tree.nodes = new[]
        {
            Node("core", 1, true, new SkillEffect { type = SkillEffectType.DamagePercent, value = 0.02f }, "a"),
            Node("a", 2, false, new SkillEffect { type = SkillEffectType.MaxHealth, value = 5f }, "core", "b"),
            Node("b", 3, false, new SkillEffect { type = SkillEffectType.MaxHealth, value = 5f }, "a", "fantasma"),
            Node("c", 1, false, new SkillEffect { type = SkillEffectType.MoveSpeedPercent, value = 0.03f })
        };
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(tree);

    private static SkillNode Node(string id, int cost, bool root, SkillEffect effect, params string[] connections) =>
        new SkillNode { id = id, displayName = id, cost = cost, isRoot = root, connections = connections, effects = new[] { effect } };

    private static CharacterSave Character(int points, params string[] owned)
    {
        var save = new CharacterSave { id = "test", skillPoints = points };
        save.skillNodes.AddRange(owned);
        return save;
    }

    // --- Puntos por oleada ---

    [TestCase(1, 1)]
    [TestCase(4, 1)]
    [TestCase(5, 2)]
    [TestCase(9, 2)]
    [TestCase(10, 3)]
    [TestCase(14, 3)]
    [TestCase(15, 4)]
    public void PointsForWave_GrowsEveryFiveWaves(int wave, int expected)
    {
        Assert.AreEqual(expected, SkillTreeRules.PointsForWave(wave));
    }

    [Test]
    public void PointsForWave_NeverBelowOne()
    {
        Assert.AreEqual(1, SkillTreeRules.PointsForWave(0));
        Assert.AreEqual(1, SkillTreeRules.PointsForWave(-3));
    }

    // --- Desbloqueo ---

    [Test]
    public void IsUnlocked_Root_IsAlwaysUnlocked()
    {
        Assert.IsTrue(SkillTreeRules.IsUnlocked(tree, tree.Find("core"), new List<string>()));
    }

    [Test]
    public void IsUnlocked_NeedsAnOwnedNeighbour()
    {
        Assert.IsFalse(SkillTreeRules.IsUnlocked(tree, tree.Find("a"), new List<string>()));
        Assert.IsTrue(SkillTreeRules.IsUnlocked(tree, tree.Find("a"), new List<string> { "core" }));
    }

    [Test]
    public void IsUnlocked_NeighbourThatDoesNotExist_DoesNotUnlockAndDoesNotThrow()
    {
        // "b" lista a "fantasma", que no existe; aunque el guardado lo tenga, no abre nada.
        Assert.IsFalse(SkillTreeRules.IsUnlocked(tree, tree.Find("b"), new List<string> { "fantasma" }));
    }

    [Test]
    public void IsUnlocked_NodeWithoutConnectionsAndNotRoot_StaysLocked()
    {
        Assert.IsFalse(SkillTreeRules.IsUnlocked(tree, tree.Find("c"), new List<string> { "core", "a", "b" }));
    }

    // --- Comprar ---

    [Test]
    public void CanBuy_ReportsEachReason()
    {
        Assert.AreEqual(SkillBuyBlock.UnknownNode, SkillTreeRules.CanBuy(tree, Character(9), "nope"));
        Assert.AreEqual(SkillBuyBlock.Owned, SkillTreeRules.CanBuy(tree, Character(9, "core"), "core"));
        Assert.AreEqual(SkillBuyBlock.Locked, SkillTreeRules.CanBuy(tree, Character(9), "a"));
        Assert.AreEqual(SkillBuyBlock.NoPoints, SkillTreeRules.CanBuy(tree, Character(0), "core"));
        Assert.AreEqual(SkillBuyBlock.None, SkillTreeRules.CanBuy(tree, Character(1), "core"));
    }

    [Test]
    public void TryBuy_SpendsPointsAndRecordsTheNode()
    {
        CharacterSave save = Character(3);

        Assert.IsTrue(SkillTreeRules.TryBuy(tree, save, "core"));

        Assert.AreEqual(2, save.skillPoints);
        CollectionAssert.AreEqual(new[] { "core" }, save.skillNodes);
    }

    [Test]
    public void TryBuy_WhenBlocked_ChangesNothing()
    {
        CharacterSave save = Character(1);

        Assert.IsFalse(SkillTreeRules.TryBuy(tree, save, "a")); // bloqueado
        Assert.AreEqual(1, save.skillPoints);
        Assert.IsEmpty(save.skillNodes);
    }

    [Test]
    public void TryBuy_CannotBuyTheSameNodeTwice()
    {
        CharacterSave save = Character(5);
        SkillTreeRules.TryBuy(tree, save, "core");

        Assert.IsFalse(SkillTreeRules.TryBuy(tree, save, "core"));
        Assert.AreEqual(4, save.skillPoints);
    }

    // --- Reinicio ---

    [Test]
    public void Reset_RefundsEverythingSpentAndEmptiesTheList()
    {
        CharacterSave save = Character(0, "core", "a");

        int refunded = SkillTreeRules.Reset(tree, save);

        Assert.AreEqual(3, refunded); // 1 + 2
        Assert.AreEqual(3, save.skillPoints);
        Assert.IsEmpty(save.skillNodes);
    }

    [Test]
    public void Reset_WithNothingBought_RefundsZeroWithoutFailing()
    {
        CharacterSave save = Character(0);

        Assert.AreEqual(0, SkillTreeRules.Reset(tree, save));
        Assert.AreEqual(0, save.skillPoints);
    }

    [Test]
    public void Reset_NodeRemovedFromTheAsset_IsDroppedWithoutRefund()
    {
        CharacterSave save = Character(0, "core", "borrado");

        int refunded = SkillTreeRules.Reset(tree, save);

        Assert.AreEqual(1, refunded);
        Assert.IsEmpty(save.skillNodes);
    }

    // --- Bonos ---

    [Test]
    public void Compute_SumsTheEffectsOfOwnedNodes()
    {
        TreeBonuses bonuses = SkillTreeRules.Compute(tree, new[] { "core", "a", "b" });

        Assert.AreEqual(10, bonuses.MaxHealthBonus);
        Assert.AreEqual(1.02f, bonuses.DamageMultiplier, 0.0001f);
        Assert.AreEqual(1f, bonuses.SpeedMultiplier, 0.0001f);
    }

    [Test]
    public void Compute_IgnoresIdsThatNoLongerExist()
    {
        TreeBonuses bonuses = SkillTreeRules.Compute(tree, new[] { "core", "borrado" });

        Assert.AreEqual(1.02f, bonuses.DamageMultiplier, 0.0001f);
        Assert.AreEqual(0, bonuses.MaxHealthBonus);
    }

    [Test]
    public void Compute_WithNoTreeOrNoNodes_IsNeutral()
    {
        TreeBonuses none = SkillTreeRules.Compute(null, new[] { "core" });
        TreeBonuses empty = SkillTreeRules.Compute(tree, new string[0]);

        Assert.AreEqual(1f, none.DamageMultiplier);
        Assert.AreEqual(0, none.HeavyShotExtraCharges);
        Assert.AreEqual(1f, empty.SpeedMultiplier);
    }

    [Test]
    public void Compute_MapsEveryAbilityEffectToItsField()
    {
        var all = ScriptableObject.CreateInstance<SkillTreeDefinition>();
        all.nodes = new[]
        {
            Node("n1", 1, true, new SkillEffect { type = SkillEffectType.HeavyShotExtraBullets, value = 1 }),
            Node("n2", 1, true, new SkillEffect { type = SkillEffectType.HeavyShotExtraCharges, value = 2 }),
            Node("n3", 1, true, new SkillEffect { type = SkillEffectType.HeavyShotCooldown, value = 1.5f }),
            Node("n4", 1, true, new SkillEffect { type = SkillEffectType.MistBleedOnPass, value = 2 }),
            Node("n5", 1, true, new SkillEffect { type = SkillEffectType.MistSlow, value = 0.3f }),
            Node("n6", 1, true, new SkillEffect { type = SkillEffectType.MistDuration, value = 1f }),
            Node("n7", 1, true, new SkillEffect { type = SkillEffectType.MistCooldown, value = 2f }),
            Node("n8", 1, true, new SkillEffect { type = SkillEffectType.UltDuration, value = 1.5f }),
            Node("n9", 1, true, new SkillEffect { type = SkillEffectType.UltCooldown, value = 8f }),
            Node("n10", 1, true, new SkillEffect { type = SkillEffectType.UltLifeSteal, value = 0.15f }),
        };

        TreeBonuses b = SkillTreeRules.Compute(all, new[] { "n1", "n2", "n3", "n4", "n5", "n6", "n7", "n8", "n9", "n10" });

        Assert.AreEqual(1, b.HeavyShotExtraBullets);
        Assert.AreEqual(2, b.HeavyShotExtraCharges);
        Assert.AreEqual(1.5f, b.HeavyShotCooldownReduction, 0.0001f);
        Assert.AreEqual(2, b.MistBleedOnPass);
        Assert.AreEqual(0.3f, b.MistSlow, 0.0001f);
        Assert.AreEqual(1f, b.MistDurationBonus, 0.0001f);
        Assert.AreEqual(2f, b.MistCooldownReduction, 0.0001f);
        Assert.AreEqual(1.5f, b.UltDurationBonus, 0.0001f);
        Assert.AreEqual(8f, b.UltCooldownReduction, 0.0001f);
        Assert.AreEqual(0.15f, b.UltLifeStealBonus, 0.0001f);
        Object.DestroyImmediate(all);
    }

    // --- Guardado v4 ---

    [Test]
    public void Parse_V3_LoadsWithEmptySkillData()
    {
        const string v3 = @"{
            ""version"": 3, ""money"": 500, ""selectedCharacterId"": ""alucard"",
            ""characters"": [ { ""id"": ""alucard"", ""unlocked"": true, ""level"": 4, ""xp"": 20, ""bleedLevel"": 2,
                                ""abilityRanks"": [1, 0, 0], ""weapons"": [] } ]
        }";

        SaveData data = SaveMigrations.Parse(v3, out bool migrated);

        Assert.IsTrue(migrated);
        Assert.AreEqual(SaveData.CurrentVersion, data.version);
        CharacterSave alucard = data.GetCharacter("alucard");
        Assert.AreEqual(4, alucard.level);
        Assert.AreEqual(0, alucard.skillPoints);
        Assert.IsNotNull(alucard.skillNodes);
        Assert.IsEmpty(alucard.skillNodes);
    }

    [Test]
    public void SkillData_RoundTripsThroughJson()
    {
        var original = new SaveData();
        CharacterSave c = original.GetCharacter("alucard");
        c.skillPoints = 7;
        c.skillNodes.AddRange(new[] { "core", "a" });

        SaveData loaded = SaveMigrations.Parse(JsonUtility.ToJson(original), out bool migrated);

        Assert.IsFalse(migrated);
        CharacterSave back = loaded.GetCharacter("alucard");
        Assert.AreEqual(7, back.skillPoints);
        CollectionAssert.AreEqual(new[] { "core", "a" }, back.skillNodes);
    }

    [Test]
    public void Validate_FixesNegativePointsNullListAndDuplicates()
    {
        var data = new SaveData();
        CharacterSave c = data.GetCharacter("alucard");
        c.skillPoints = -4;
        c.skillNodes = new List<string> { "core", "core", null, "", "a" };

        SaveMigrations.Validate(data);

        Assert.AreEqual(0, c.skillPoints);
        CollectionAssert.AreEqual(new[] { "core", "a" }, c.skillNodes);

        c.skillNodes = null;
        SaveMigrations.Validate(data);
        Assert.IsNotNull(c.skillNodes);
    }

    [Test]
    public void CurrentVersion_IsFour()
    {
        Assert.AreEqual(4, SaveData.CurrentVersion);
    }
}
