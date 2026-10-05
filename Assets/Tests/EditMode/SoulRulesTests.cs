using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SoulRulesTests
{
    [TestCase(150, 0.02f, false, 3f, 3)]
    [TestCase(150, 0.02f, true, 3f, 9)]
    [TestCase(40, 0.02f, false, 3f, 1)]    // mínimo 1
    [TestCase(100, 0.05f, false, 3f, 5)]
    public void HealAmount_IsAFractionOfMaxHealth_WithBossMultiplier(int max, float fraction, bool boss, float mult, int expected)
    {
        Assert.AreEqual(expected, SoulRules.HealAmount(max, fraction, boss, mult));
    }

    [TestCase(0f)]
    [TestCase(-0.5f)]
    public void HealAmount_ZeroOrNegativeFraction_GivesNothing(float fraction)
    {
        Assert.AreEqual(0, SoulRules.HealAmount(150, fraction, false, 3f));
        Assert.AreEqual(0, SoulRules.HealAmount(150, fraction, true, 3f));
    }

    [Test]
    public void IsWithinReach_InsideAndOutside()
    {
        Assert.IsTrue(SoulRules.IsWithinReach(Vector3.zero, new Vector3(1f, 0f, 0f), 1.5f));
        Assert.IsFalse(SoulRules.IsWithinReach(Vector3.zero, new Vector3(2f, 0f, 0f), 1.5f));
    }

    [Test]
    public void IsWithinReach_TheExactEdgeCounts()
    {
        Assert.IsTrue(SoulRules.IsWithinReach(Vector3.zero, new Vector3(1.5f, 0f, 0f), 1.5f));
    }

    [Test]
    public void IsWithinReach_IgnoresHeight()
    {
        Assert.IsTrue(SoulRules.IsWithinReach(new Vector3(0f, 1f, 0f), new Vector3(1f, 0.2f, 0f), 1.5f));
        Assert.IsTrue(SoulRules.IsWithinReach(Vector3.zero, new Vector3(0.5f, 40f, 0f), 1.5f));
    }

    [Test]
    public void GutsAsset_HasTheDesignValues()
    {
        var guts = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");

        Assert.AreEqual(0.02f, guts.soulHealFraction, 1e-4f);
        Assert.AreEqual(3f, guts.soulBossMultiplier, 1e-4f);
        Assert.AreEqual(10f, guts.soulLifetime, 1e-4f);
        Assert.AreEqual(1.5f, guts.soulPickupRadius, 1e-4f);
    }

    [TestCase("Assets/Data/Characters/Alucard.asset")]
    [TestCase("Assets/Data/Characters/Maga.asset")]
    public void OtherCharacters_DoNotDropSouls(string path)
    {
        var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);

        Assert.AreEqual(0f, character.soulHealFraction, 1e-4f);
    }
}
