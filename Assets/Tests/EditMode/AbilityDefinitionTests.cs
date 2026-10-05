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

    [TestCase(20, 10)]
    [TestCase(120, 60)]
    [TestCase(0, 0)]
    public void LifeSteal_IsHalfOfTheDirectDamage(int damage, int expected)
    {
        Assert.AreEqual(expected, ability.LifeStealFor(damage));
    }

    [TestCase(20, 12)]
    [TestCase(1, 1)]
    public void Explosion_DealsSixtyPercentToNeighboursWithAMinimumOfOne(int bullet, int expected)
    {
        Assert.AreEqual(expected, ability.ExplosionDamageFor(bullet));
    }

    [TestCase(1, 2)]
    [TestCase(3, 6)]
    [TestCase(0, 0)]
    public void UltimateBleed_DoublesTheStacksPerShot(int stacks, int expected)
    {
        Assert.AreEqual(expected, ability.BoostedBleed(stacks));
    }

    [Test]
    public void UltimateDefaults_MatchTheDesign()
    {
        Assert.AreEqual(2.5f, ability.explosionRadius, 0.001f);
        Assert.AreEqual(2, ability.explosionBleedStacks);
        Assert.AreEqual(2f, ability.riverTickSeconds, 0.001f);
    }

    // --- Llamarada de Guts ---

    private static AbilityDefinition Flame()
    {
        var flame = ScriptableObject.CreateInstance<AbilityDefinition>();
        flame.kind = AbilityKind.FlameBurst;
        flame.cooldown = 8f; flame.range = 5f; flame.coneDegrees = 90f;
        flame.damageMultiplier = 2f; flame.damagePerRank = 0.25f;
        flame.duration = 4f; flame.durationPerRank = 0.5f; flame.cooldownPerRank = 0.5f;
        flame.burnDamageFractionPerSecond = 0.6f; flame.burnTickSeconds = 0.5f;
        return flame;
    }

    [Test]
    public void EnumValues_OfAlucardsKinds_DoNotMove()
    {
        Assert.AreEqual(0, (int)AbilityKind.HeavyShot);
        Assert.AreEqual(1, (int)AbilityKind.Mist);
        Assert.AreEqual(2, (int)AbilityKind.Ultimate);
        Assert.AreEqual(3, (int)AbilityKind.FlameBurst);
        Assert.AreEqual(4, (int)AbilityKind.Dash);
        Assert.AreEqual(5, (int)AbilityKind.Berserk);
    }

    [TestCase(30, 1, 60)]
    [TestCase(30, 5, 90)]    // x3 en el rango 5
    [TestCase(80, 1, 160)]
    [TestCase(0, 1, 1)]
    public void FlameDamage_IsTheSwordDamageTimesTheRankMultiplier(int sword, int rank, int expected)
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(expected, flame.FlameDamageFor(sword, rank));
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void FlameDamage_WithFury_IsDoubled()
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(120, flame.FlameDamageFor(30, 1, 2f));
        Object.DestroyImmediate(flame);
    }

    [TestCase(30, 9)]    // 30 x 0,6 x 0,5
    [TestCase(80, 24)]
    [TestCase(1, 1)]     // mínimo 1
    public void BurnTick_IsSixtyPercentOfTheSwordPerSecond(int sword, int expected)
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(expected, flame.BurnTickDamageFor(sword));
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void BurnTick_WithFury_IsDoubled()
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(18, flame.BurnTickDamageFor(30, 2f));
        Object.DestroyImmediate(flame);
    }

    [TestCase(1, 4f, 8f)]
    [TestCase(3, 5f, 7f)]
    [TestCase(5, 6f, 6f)]
    public void Ranks_LengthenTheBurn_AndShortenTheCooldown(int rank, float burn, float cooldown)
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(burn, flame.BurnSecondsAt(rank), 1e-4f);
        Assert.AreEqual(cooldown, flame.CooldownAt(rank), 1e-4f);
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void DescribeRank_Flame_MentionsDamageBurnAndCooldown()
    {
        AbilityDefinition flame = Flame();
        string text = flame.DescribeRank(1);

        StringAssert.Contains("x2", text);
        StringAssert.Contains("quema 4", text);
        StringAssert.Contains("8", text);
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void LlamaradaAsset_Exists_WithTheDesignValues_AndGutsCarriesIt()
    {
        var flame = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Llamarada.asset");
        Assert.IsNotNull(flame, "Falta Assets/Data/Abilities/Llamarada.asset");

        Assert.AreEqual("Llamarada", flame.Id);
        Assert.AreEqual(AbilityKind.FlameBurst, flame.kind);
        Assert.AreEqual(5f, flame.range, 1e-4f);
        Assert.AreEqual(90f, flame.coneDegrees, 1e-4f);
        Assert.AreEqual(2f, flame.damageMultiplier, 1e-4f);
        Assert.AreEqual(4f, flame.duration, 1e-4f);
        Assert.AreEqual(8f, flame.cooldown, 1e-4f);
        Assert.AreEqual(0.25f, flame.damagePerRank, 1e-4f);
        Assert.AreEqual(0.5f, flame.durationPerRank, 1e-4f);
        Assert.AreEqual(0.5f, flame.cooldownPerRank, 1e-4f);
        Assert.IsNotNull(flame.icon, "Falta el icono");

        var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
        Assert.GreaterOrEqual(guts.abilities.Length, 1);
        Assert.AreSame(flame, guts.abilities[0]);
    }

    // --- Embestida y Armadura de Guts ---

    private static AbilityDefinition Dash()
    {
        var d = ScriptableObject.CreateInstance<AbilityDefinition>();
        d.kind = AbilityKind.Dash;
        d.cooldown = 7f; d.cooldownPerRank = 0.4f;
        d.damageMultiplier = 3f; d.damagePerRank = 0.25f;
        d.dashDistance = 6f; d.dashSeconds = 0.35f; d.riseSeconds = 0.25f; d.shotRange = 25f;
        return d;
    }

    private static AbilityDefinition Berserk()
    {
        var b = ScriptableObject.CreateInstance<AbilityDefinition>();
        b.kind = AbilityKind.Berserk;
        b.cooldown = 25f; b.cooldownPerRank = 3f;
        b.damageMultiplier = 1.4f; b.damagePerRank = 0.1f;
        b.drainFractionPerSecond = 0.02f; b.drainPerRank = 0f;
        return b;
    }

    [TestCase(30, 1, 90)]
    [TestCase(80, 1, 240)]
    [TestCase(80, 5, 320)]    // x4 en el rango 5
    [TestCase(0, 1, 1)]
    public void DashShot_IsTheSwordDamageTimesTheRankMultiplier(int sword, int rank, int expected)
    {
        AbilityDefinition d = Dash();
        Assert.AreEqual(expected, d.SwordScaledDamageFor(sword, rank));
        Object.DestroyImmediate(d);
    }

    [Test]
    public void DashShot_WithFury_IsDoubled()
    {
        AbilityDefinition d = Dash();
        Assert.AreEqual(180, d.SwordScaledDamageFor(30, 1, 2f));
        Object.DestroyImmediate(d);
    }

    [TestCase(1, 7f)]
    [TestCase(5, 5.4f)]
    public void DashCooldown_ShortensWithRank(int rank, float expected)
    {
        AbilityDefinition d = Dash();
        Assert.AreEqual(expected, d.CooldownAt(rank), 1e-4f);
        Object.DestroyImmediate(d);
    }

    [Test]
    public void DashDescribeRank_MentionsTheDamageAndTheCooldown()
    {
        AbilityDefinition d = Dash();
        string text = d.DescribeRank(1);

        StringAssert.Contains("x3", text);
        StringAssert.Contains("7", text);
        Object.DestroyImmediate(d);
    }

    [TestCase(1, 1.4f)]
    [TestCase(2, 1.5f)]
    [TestCase(3, 1.6f)]
    public void BerserkDamage_GrowsWithRank(int rank, float expected)
    {
        AbilityDefinition b = Berserk();
        Assert.AreEqual(expected, b.BerserkDamageAt(rank), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [TestCase(1, 0.02f)]
    [TestCase(2, 0.02f)]
    [TestCase(3, 0.02f)]
    public void BerserkDrain_DoesNotShrinkWithRank(int rank, float expected)
    {
        AbilityDefinition b = Berserk();
        Assert.AreEqual(expected, b.BerserkDrainAt(rank), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void BerserkDrain_NeverNegative()
    {
        AbilityDefinition b = Berserk();
        b.drainPerRank = 0.05f;
        Assert.AreEqual(0f, b.BerserkDrainAt(3), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [TestCase(1, 25f)]
    [TestCase(2, 22f)]
    [TestCase(3, 19f)]
    public void BerserkCooldown_ShortensWithRank(int rank, float expected)
    {
        AbilityDefinition b = Berserk();
        Assert.AreEqual(expected, b.CooldownAt(rank), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void BerserkDescribeRank_MentionsDamageDrainAndCooldown()
    {
        AbilityDefinition b = Berserk();
        string text = b.DescribeRank(1);

        StringAssert.Contains(1.4f.ToString("0.0"), text);   // el texto usa el separador decimal del idioma
        StringAssert.Contains("2%", text);
        StringAssert.Contains("25", text);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void DashAndBerserkDefaults_MatchTheDesign()
    {
        Assert.AreEqual(6f, ability.dashDistance, 1e-4f);
        Assert.AreEqual(0.35f, ability.dashSeconds, 1e-4f);
        Assert.AreEqual(0.25f, ability.riseSeconds, 1e-4f);
        Assert.AreEqual(25f, ability.shotRange, 1e-4f);
        Assert.AreEqual(4f, ability.roarRadius, 1e-4f);
        Assert.AreEqual(1.5f, ability.roarStunSeconds, 1e-4f);
        Assert.AreEqual(0.5f, ability.damageTakenMultiplier, 1e-4f);
        Assert.AreEqual(0.5f, ability.cadenceMultiplier, 1e-4f);
        Assert.AreEqual(2f, ability.furyGainMultiplier, 1e-4f);
        Assert.AreEqual(360f, ability.swingArcDegrees, 1e-4f);
    }

    [Test]
    public void EmbestidaAndArmaduraAssets_ExistWithTheDesignValues_AndGutsCarriesThem()
    {
        var dash = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Embestida.asset");
        var armor = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Armadura.asset");
        Assert.IsNotNull(dash, "Falta Embestida.asset");
        Assert.IsNotNull(armor, "Falta Armadura.asset");

        Assert.AreEqual("Embestida", dash.Id);
        Assert.AreEqual(AbilityKind.Dash, dash.kind);
        Assert.AreEqual(7f, dash.cooldown, 1e-4f);
        Assert.AreEqual(3f, dash.damageMultiplier, 1e-4f);
        Assert.AreEqual(6f, dash.dashDistance, 1e-4f);
        Assert.IsNotNull(dash.icon);

        Assert.AreEqual("Armadura", armor.Id);
        Assert.AreEqual(AbilityKind.Berserk, armor.kind);
        Assert.AreEqual(25f, armor.cooldown, 1e-4f);
        Assert.AreEqual(1.4f, armor.damageMultiplier, 1e-4f);
        Assert.AreEqual(0.02f, armor.drainFractionPerSecond, 1e-4f);
        Assert.AreEqual(0f, armor.drainPerRank, 1e-6f);
        Assert.AreEqual(360f, armor.swingArcDegrees, 1e-4f);
        Assert.IsNotNull(armor.icon);

        var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
        Assert.AreEqual(3, guts.abilities.Length);
        Assert.AreEqual("Llamarada", guts.abilities[0].Id);
        Assert.AreSame(dash, guts.abilities[1]);
        Assert.AreSame(armor, guts.abilities[2]);
    }

    [Test]
    public void FlameDefaults_MatchTheDesign()
    {
        Assert.AreEqual(90f, ability.coneDegrees, 1e-4f);
        Assert.AreEqual(0.6f, ability.burnDamageFractionPerSecond, 1e-4f);
        Assert.AreEqual(0.5f, ability.burnTickSeconds, 1e-4f);
    }
}
