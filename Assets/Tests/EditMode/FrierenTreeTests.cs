using NUnit.Framework;
using UnityEngine;

/// <summary>Efectos del árbol de Frieren (suma en TreeBonuses) y sus fórmulas puras.</summary>
[TestFixture]
public class FrierenTreeTests
{
    private static TreeBonuses ComputeOne(SkillEffectType type, float value, int times = 1)
    {
        var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
        var nodes = new SkillNode[times];
        var ids = new string[times];
        for (int i = 0; i < times; i++)
        {
            ids[i] = "n" + i;
            nodes[i] = new SkillNode { id = ids[i], cost = 1, effects = new[] { new SkillEffect { type = type, value = value } } };
        }
        tree.nodes = nodes;
        TreeBonuses result = SkillTreeRules.Compute(tree, ids);
        Object.DestroyImmediate(tree);
        return result;
    }

    [Test]
    public void Compute_AddsEachFrierenEffectToItsField()
    {
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.ZoltraakDamagePercent, 0.15f, 2).ZoltraakDamagePercent, 1e-4f);
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.ZoltraakChargeTime, 0.15f, 2).ZoltraakChargeReduction, 1e-4f);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.ZoltraakRadius, 0.5f, 2).ZoltraakRadiusBonus, 1e-4f);
        Assert.IsTrue(ComputeOne(SkillEffectType.ZoltraakOvercharge, 1f).ZoltraakOvercharge);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.ZoltraakEcho, 0.5f).ZoltraakEchoFraction, 1e-4f);
        Assert.AreEqual(0.4f, ComputeOne(SkillEffectType.ZoltraakFrost, 0.4f).ZoltraakFrostSlow, 1e-4f);
        Assert.AreEqual(40f, ComputeOne(SkillEffectType.ManaMax, 20f, 2).ManaMaxBonus, 1e-4f);
        Assert.AreEqual(1.5f, ComputeOne(SkillEffectType.ManaRegen, 0.75f, 2).ManaRegenBonus, 1e-4f);
        Assert.AreEqual(0.4f, ComputeOne(SkillEffectType.ManaCostPercent, 0.4f).ManaCostReduction, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.ManaOnKill, 3f).ManaOnKill, 1e-4f);
        Assert.AreEqual(2f, ComputeOne(SkillEffectType.ManaFocus, 2f).ManaFocusMultiplier, 1e-4f);
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.BeamDamagePercent, 0.15f, 2).BeamDamagePercent, 1e-4f);
        Assert.AreEqual(0.2f, ComputeOne(SkillEffectType.BeamRadius, 0.2f).BeamRadiusBonus, 1e-4f);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.BeamCooldown, 0.5f, 2).BeamCooldownReduction, 1e-4f);
        Assert.AreEqual(1, ComputeOne(SkillEffectType.BeamExtraCharges, 1f).BeamExtraCharges);
        Assert.AreEqual(0.6f, ComputeOne(SkillEffectType.BeamFrost, 0.6f).BeamFrostSlow, 1e-4f);
        Assert.AreEqual(0.15f, ComputeOne(SkillEffectType.BeamPierceDamage, 0.15f).BeamPierceDamage, 1e-4f);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.FieldRadius, 1f).FieldRadiusBonus, 1e-4f);
        Assert.AreEqual(1.5f, ComputeOne(SkillEffectType.FieldDuration, 1.5f).FieldDurationBonus, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.FieldCooldown, 3f).FieldCooldownReduction, 1e-4f);
        Assert.AreEqual(0.04f, ComputeOne(SkillEffectType.FieldHeal, 0.01f, 4).FieldHealBonus, 1e-4f);
        Assert.AreEqual(0.4f, ComputeOne(SkillEffectType.FieldSlow, 0.1f, 4).FieldSlowBonus, 1e-4f);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.FieldPoison, 0.5f).FieldPoison, 1e-4f);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.FieldPoisonDamagePercent, 0.5f).FieldPoisonDamagePercent, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.PulseDuration, 1.5f, 2).PulseDurationBonus, 1e-4f);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.PulseStun, 0.5f).PulseStunBonus, 1e-4f);
        Assert.AreEqual(16f, ComputeOne(SkillEffectType.PulseCooldown, 8f, 2).PulseCooldownReduction, 1e-4f);
        Assert.IsTrue(ComputeOne(SkillEffectType.PulseWholeMap, 1f).PulseWholeMap);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.PulseZoltraakRain, 1f).PulseRainInterval, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.PulseFinalBlast, 3f).PulseFinalBlastMultiplier, 1e-4f);
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.PulseMark, 0.3f).PulseMarkBonus, 1e-4f);
    }

    [Test]
    public void NoNodes_LeavesEveryFrierenFieldNeutral()
    {
        TreeBonuses none = TreeBonuses.None;
        Assert.IsFalse(none.ZoltraakOvercharge);
        Assert.IsFalse(none.PulseWholeMap);
        Assert.AreEqual(0f, none.PulseRainInterval);
        Assert.AreEqual(0f, none.ManaFocusMultiplier);
    }

    [TestCase(1.2f, 0f, 1.2f)]
    [TestCase(1.2f, 0.3f, 0.9f)]
    [TestCase(1.2f, 5f, 0.3f)]
    public void ChargeSeconds_NeverBelowTheMinimum(float baseSeconds, float reduction, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.ChargeSeconds(baseSeconds, reduction), 1e-4f);
    }

    [TestCase(0.4f, 0f, 0.4f)]
    [TestCase(0.4f, 0.4f, 0.8f)]
    [TestCase(0.4f, 0.6f, 0.8f)]
    public void FieldSlow_IsCappedAt80Percent(float baseFraction, float bonus, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.FieldSlow(baseFraction, bonus), 1e-4f);
    }

    [TestCase(25f, 0.4f, 15f)]
    [TestCase(20f, 0.4f, 12f)]
    [TestCase(50f, 0.4f, 30f)]
    [TestCase(25f, 0f, 25f)]
    public void ManaCost_AppliesTheReduction(float baseCost, float reduction, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.ManaCost(baseCost, reduction), 1e-4f);
    }

    [TestCase(0, 1f)]
    [TestCase(1, 1.15f)]
    [TestCase(6, 1.9f)]
    [TestCase(10, 1.9f)]
    public void PierceMultiplier_GrowsPerEnemyUpToTheCap(int index, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.PierceMultiplier(index, 0.15f), 1e-4f);
    }

    [Test]
    public void PoisonTickDamage_IsHalfTheBasicShotPerSecond()
    {
        Assert.AreEqual(2, FrierenTreeMath.PoisonTickDamage(8, 0.5f, 0f));    // 4 por segundo en ticks de 0,5 s
        Assert.AreEqual(3, FrierenTreeMath.PoisonTickDamage(8, 0.5f, 0.5f));  // Veneno II
        Assert.AreEqual(1, FrierenTreeMath.PoisonTickDamage(1, 0.5f, 0f));    // nunca menos de 1
        Assert.AreEqual(0, FrierenTreeMath.PoisonTickDamage(8, 0f, 0f));      // sin el nodo no envenena
    }

    [Test]
    public void RegenMultiplier_DoublesOnlyAfterThreeSecondsWithoutDamage()
    {
        Assert.AreEqual(1f, FrierenTreeMath.RegenMultiplier(0f, 10f, 0f));
        Assert.AreEqual(1f, FrierenTreeMath.RegenMultiplier(2f, 10f, 8f));
        Assert.AreEqual(2f, FrierenTreeMath.RegenMultiplier(2f, 10f, 7f));
    }

    [Test]
    public void Scale_AddsTheBonusAndLeavesTheDamageAloneWithout()
    {
        Assert.AreEqual(13, FrierenTreeMath.Scale(10, 0.3f));
        Assert.AreEqual(10, FrierenTreeMath.Scale(10, 0f));
    }

    [Test]
    public void SlowRules_TheStrongerSlowWinsWhileItLasts()
    {
        Assert.IsTrue(SlowRules.ShouldReplace(1f, false, 0.4f));    // nada activo
        Assert.IsFalse(SlowRules.ShouldReplace(0.4f, true, 0.4f));  // activo al 60% (x0,4): el 40% del campo no lo pisa
        Assert.IsTrue(SlowRules.ShouldReplace(0.6f, true, 0.4f));   // la misma (x0,6) lo renueva
        Assert.IsTrue(SlowRules.ShouldReplace(0.6f, true, 0.6f));   // una más fuerte (x0,4) lo reemplaza
        Assert.IsTrue(SlowRules.ShouldReplace(0.6f, true, 0.8f));   // una mucho más fuerte también
        Assert.IsTrue(SlowRules.ShouldReplace(0.4f, false, 0.1f));  // la vieja ya caducó
    }

    // La Escarcha (40% por 3 s) no se acorta cuando el campo o el pulso le ponen el mismo 40% por medio segundo.
    [Test]
    public void SlowRules_TheSameSlowRenewsButNeverShortens()
    {
        Assert.AreEqual(13f, SlowRules.EndTime(0.6f, true, 13f, 0.4f, 10f, 0.5f), 1e-4f);   // igual y más corta: se queda la de 13
        Assert.AreEqual(14f, SlowRules.EndTime(0.6f, true, 13f, 0.4f, 10f, 4f), 1e-4f);     // igual y más larga: se alarga
        Assert.AreEqual(10.5f, SlowRules.EndTime(0.6f, true, 13f, 0.6f, 10f, 0.5f), 1e-4f); // más fuerte: vale la nueva
        Assert.AreEqual(10.5f, SlowRules.EndTime(0.6f, false, 9f, 0.4f, 10f, 0.5f), 1e-4f); // la vieja ya caducó
    }

    [Test]
    public void ManaPool_Gain_AddsUpToTheMax()
    {
        var pool = new ManaPool(100f, 0f);
        pool.TrySpend(50f);
        pool.Gain(3f);
        Assert.AreEqual(53f, pool.Current, 1e-4f);
        pool.Gain(500f);
        Assert.AreEqual(100f, pool.Current, 1e-4f);
        pool.Gain(-10f);
        Assert.AreEqual(100f, pool.Current, 1e-4f);
    }
}
