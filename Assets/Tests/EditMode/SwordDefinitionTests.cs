using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SwordDefinitionTests
{
    private const string SwordPath = "Assets/Data/Weapons/Espada.asset";
    private const string GutsPath = "Assets/Data/Characters/Guts.asset";

    private static SwordDefinition Sword => AssetDatabase.LoadAssetAtPath<SwordDefinition>(SwordPath);

    // --- Fórmulas (sobre una instancia con los valores de diseño) ---

    private SwordDefinition made;

    [SetUp]
    public void SetUp()
    {
        made = ScriptableObject.CreateInstance<SwordDefinition>();
        made.fireRate = 1.2f;
        made.fireRateUpgrade = new UpgradeStat { step = 0.12f, limit = 0.6f, maxLevel = 5 };
        made.reloadUpgrade = new UpgradeStat { step = 0.03f, maxLevel = 5 };
        made.stunChanceCap = 0.30f;
        made.damage = 30;
        made.damageUpgrade = new UpgradeStat { step = 5f, maxLevel = 10 };
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(made);

    [TestCase(0, 0f)]
    [TestCase(1, 0.03f)]
    [TestCase(5, 0.15f)]
    public void StunChance_GrowsThreePointsPerLevel_FifteenAtMax(int level, float expected)
    {
        Assert.AreEqual(expected, made.StunChanceAt(level), 1e-4f);
    }

    [Test]
    public void StunChance_AddsTheTreeBonus_ThirtyAtMax()
    {
        Assert.AreEqual(0.30f, made.StunChanceAt(5, 0.15f), 1e-4f);
    }

    [Test]
    public void StunChance_NeverExceedsTheThirtyPercentCap()
    {
        Assert.AreEqual(0.30f, made.StunChanceAt(5, 5f), 1e-4f);
        Assert.AreEqual(0.30f, made.StunChanceAt(5, 0.20f), 1e-4f);
    }

    [TestCase(0, 1.2f)]
    [TestCase(1, 1.08f)]
    [TestCase(5, 0.6f)]
    public void TimeBetweenHits_ShortensWithCadence(int level, float expected)
    {
        Assert.AreEqual(expected, made.FireRateAt(level), 1e-4f);
    }

    [Test]
    public void TimeBetweenHits_NeverBelowTheLimit()
    {
        Assert.AreEqual(0.6f, made.FireRateAt(50), 1e-4f);
    }

    [TestCase(0, 30)]
    [TestCase(10, 80)]
    public void Damage_AddsFivePerLevel(int level, int expected)
    {
        Assert.AreEqual(expected, made.DamageAt(level));
    }

    [Test]
    public void Sword_HasNoAmmoAndNoBleed()
    {
        Assert.IsFalse(made.UsesAmmo);
        Assert.IsFalse(made.AppliesBleed);
    }

    [Test]
    public void Labels_NameTheThreeUpgrades()
    {
        Assert.AreEqual("Cadencia", made.UpgradeLabel(UpgradeType.FireRate));
        Assert.AreEqual("Aturdir", made.UpgradeLabel(UpgradeType.Reload));
        Assert.AreEqual("Daño", made.UpgradeLabel(UpgradeType.Damage));
    }

    // --- El asset real y Guts ---

    [Test]
    public void SwordAsset_Exists_WithTheDesignValues()
    {
        SwordDefinition sword = Sword;
        Assert.IsNotNull(sword, "Falta " + SwordPath);

        Assert.AreEqual("Espada", sword.Id);
        Assert.AreEqual(3f, sword.range, 1e-4f);
        Assert.AreEqual(120f, sword.arcDegrees, 1e-4f);
        Assert.AreEqual(1.5f, sword.stunSeconds, 1e-4f);
        Assert.AreEqual(2.6f, sword.attackAnimSpeed, 1e-4f);
        Assert.IsTrue(sword.isAutomatic);
        Assert.AreEqual(0.15f, sword.StunChanceAt(sword.reloadUpgrade.maxLevel), 1e-4f);
        Assert.AreEqual(0.30f, sword.stunChanceCap, 1e-4f);
        Assert.AreEqual(1.2f, sword.FireRateAt(0), 1e-4f);
        Assert.AreEqual(0.6f, sword.FireRateAt(sword.fireRateUpgrade.maxLevel), 1e-4f);
        Assert.AreEqual(30, sword.DamageAt(0));
        Assert.Greater(sword.hitDelay, 0f);
        Assert.Less(sword.hitDelay, sword.FireRateAt(sword.fireRateUpgrade.maxLevel), "El golpe debe caer antes del siguiente");
        Assert.AreEqual(100f, sword.furyMax, 1e-4f);
        Assert.AreEqual(5f, sword.furyPerEnemyHit, 1e-4f);
        Assert.AreEqual(20f, sword.furyMaxPerSwing, 1e-4f);
        Assert.AreEqual(2f, sword.furyDamageMultiplier, 1e-4f);
    }

    [Test]
    public void Guts_CarriesTheSword_WithoutAHeldModel()
    {
        var guts = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(GutsPath);

        Assert.AreEqual(1, guts.startingWeapons.Length);
        Assert.AreSame(Sword, guts.startingWeapons[0]);
        Assert.IsNull(guts.heldItemPrefab);
    }
}
