using NUnit.Framework;
using UnityEngine;

public class AbilityDefinitionTests
{
    AbilityDefinition ability;

    [SetUp]
    public void SetUp() => ability = ScriptableObject.CreateInstance<AbilityDefinition>();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(ability);

    [TestCase(20, 120)]
    [TestCase(30, 180)]
    [TestCase(1, 6)]
    public void HeavyShot_DealsSixTimesTheBulletByDefault(int bullet, int expected)
    {
        Assert.AreEqual(expected, ability.DamageFor(bullet));
    }

    [Test]
    public void Damage_FollowsTheMultiplier()
    {
        ability.damageMultiplier = 2.5f;
        Assert.AreEqual(50, ability.DamageFor(20));
    }

    [Test]
    public void Damage_IsAtLeastOne()
    {
        ability.damageMultiplier = 0f;
        Assert.AreEqual(1, ability.DamageFor(20));
    }

    [Test]
    public void Defaults_MatchTheDesign()
    {
        Assert.AreEqual(3, ability.bleedStacks);
        Assert.AreEqual(1.6f, ability.speedMultiplier, 0.001f);
        Assert.AreEqual(0.5f, ability.lifeSteal, 0.001f);
    }

    [Test]
    public void Id_FallsBackToTheAssetName()
    {
        ability.name = "DisparoPesado";
        Assert.AreEqual("DisparoPesado", ability.Id);
    }
}
