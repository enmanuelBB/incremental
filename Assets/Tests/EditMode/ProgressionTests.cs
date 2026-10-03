using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ProgressionTests
{
    private static CharacterSave NewCharacter() => new CharacterSave { id = "test" };

    // --- Experiencia y nivel ---

    [TestCase(1, 100)]
    [TestCase(2, 283)]
    [TestCase(3, 520)]
    [TestCase(4, 800)]
    [TestCase(5, 1118)]
    public void XpForNextLevel_Is100TimesLevelToThePowerOnePointFive(int level, int expected)
    {
        Assert.AreEqual(expected, Progression.XpForNextLevel(level));
    }

    [Test]
    public void AddXp_BelowThreshold_KeepsLevel()
    {
        CharacterSave c = NewCharacter();

        int gained = Progression.AddXp(c, 60);

        Assert.AreEqual(0, gained);
        Assert.AreEqual(1, c.level);
        Assert.AreEqual(60, c.xp);
    }

    [Test]
    public void AddXp_CrossingThreshold_LevelsUpAndKeepsRemainder()
    {
        CharacterSave c = NewCharacter();

        int gained = Progression.AddXp(c, 130);

        Assert.AreEqual(1, gained);
        Assert.AreEqual(2, c.level);
        Assert.AreEqual(30, c.xp);
    }

    [Test]
    public void AddXp_Large_LevelsUpSeveralTimes()
    {
        CharacterSave c = NewCharacter();

        // 100 + 283 + 520 = 903 para llegar al nivel 4.
        int gained = Progression.AddXp(c, 903 + 10);

        Assert.AreEqual(3, gained);
        Assert.AreEqual(4, c.level);
        Assert.AreEqual(10, c.xp);
    }

    [Test]
    public void AddXp_AtMaxLevel_DoesNothing()
    {
        CharacterSave c = NewCharacter();
        c.level = Progression.MaxLevel;

        int gained = Progression.AddXp(c, 99999);

        Assert.AreEqual(0, gained);
        Assert.AreEqual(Progression.MaxLevel, c.level);
        Assert.AreEqual(0, c.xp);
    }

    [Test]
    public void AddXp_ReachingMaxLevel_StopsAndClearsXp()
    {
        CharacterSave c = NewCharacter();
        c.level = Progression.MaxLevel - 1;

        Progression.AddXp(c, 999999);

        Assert.AreEqual(Progression.MaxLevel, c.level);
        Assert.AreEqual(0, c.xp);
    }

    [Test]
    public void AddXp_ZeroOrNegative_IsIgnored()
    {
        CharacterSave c = NewCharacter();

        Assert.AreEqual(0, Progression.AddXp(c, 0));
        Assert.AreEqual(0, Progression.AddXp(c, -50));
        Assert.AreEqual(0, c.xp);
    }

    [Test]
    public void WaveBonusXp_GrowsWithTheWave()
    {
        Assert.Greater(Progression.WaveBonusXp(5), Progression.WaveBonusXp(1));
        Assert.Greater(Progression.WaveBonusXp(1), 0);
    }

    // --- Puntos ---

    [Test]
    public void PointsAvailable_IsOnePerLevel()
    {
        CharacterSave c = NewCharacter();
        Assert.AreEqual(1, Progression.PointsAvailable(c));

        c.level = 7;
        Assert.AreEqual(7, Progression.PointsAvailable(c));
    }

    [Test]
    public void PointsAvailable_SubtractsAbilityRanksAndBleedLevels()
    {
        CharacterSave c = NewCharacter();
        c.level = 10;
        c.abilityRanks = new[] { 2, 1, 0 };
        c.bleedLevel = 3; // 2 puntos sobre el nivel 1, que es gratis

        Assert.AreEqual(5, Progression.SpentPoints(c));
        Assert.AreEqual(5, Progression.PointsAvailable(c));
    }

    // --- Rangos de habilidad ---

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 2)]
    [TestCase(9, 5)]
    [TestCase(30, 5)]
    public void RankCap_NormalAbilities_IsHalfTheLevelRoundedUp(int level, int expected)
    {
        Assert.AreEqual(expected, Progression.RankCapForLevel(AbilityKind.HeavyShot, level));
        Assert.AreEqual(expected, Progression.RankCapForLevel(AbilityKind.Mist, level));
    }

    [TestCase(1, 0)]
    [TestCase(5, 0)]
    [TestCase(6, 1)]
    [TestCase(11, 1)]
    [TestCase(12, 2)]
    [TestCase(17, 2)]
    [TestCase(18, 3)]
    [TestCase(30, 3)]
    public void RankCap_Ultimate_UnlocksAtLevels6_12_18(int level, int expected)
    {
        Assert.AreEqual(expected, Progression.RankCapForLevel(AbilityKind.Ultimate, level));
    }

    [Test]
    public void UpgradeAbility_WithPointAndLevel_RaisesRank()
    {
        CharacterSave c = NewCharacter();

        Assert.IsTrue(Progression.TryUpgradeAbility(c, 0, AbilityKind.HeavyShot));

        Assert.AreEqual(1, c.abilityRanks[0]);
        Assert.AreEqual(0, Progression.PointsAvailable(c));
    }

    [Test]
    public void UpgradeAbility_WithoutPoints_IsBlocked()
    {
        CharacterSave c = NewCharacter();
        Progression.TryUpgradeAbility(c, 0, AbilityKind.HeavyShot);

        Assert.AreEqual(UpgradeBlock.NoPoints, Progression.CanUpgradeAbility(c, 1, AbilityKind.Mist));
        Assert.IsFalse(Progression.TryUpgradeAbility(c, 1, AbilityKind.Mist));
        Assert.AreEqual(0, c.abilityRanks[1]);
    }

    [Test]
    public void UpgradeAbility_RankAboveLevelCap_IsBlocked()
    {
        CharacterSave c = NewCharacter();
        c.level = 2; // 2 puntos, pero el tope de rango es 1
        Progression.TryUpgradeAbility(c, 0, AbilityKind.HeavyShot);

        Assert.AreEqual(UpgradeBlock.LevelTooLow, Progression.CanUpgradeAbility(c, 0, AbilityKind.HeavyShot));
        Assert.IsFalse(Progression.TryUpgradeAbility(c, 0, AbilityKind.HeavyShot));
    }

    [Test]
    public void UpgradeAbility_Ultimate_NeedsLevel6()
    {
        CharacterSave c = NewCharacter();
        c.level = 5;
        Assert.AreEqual(UpgradeBlock.LevelTooLow, Progression.CanUpgradeAbility(c, 2, AbilityKind.Ultimate));

        c.level = 6;
        Assert.AreEqual(UpgradeBlock.None, Progression.CanUpgradeAbility(c, 2, AbilityKind.Ultimate));
    }

    [Test]
    public void UpgradeAbility_AtMaxRank_IsBlocked()
    {
        CharacterSave c = NewCharacter();
        c.level = Progression.MaxLevel;
        c.abilityRanks = new[] { 5, 0, 3 };

        Assert.AreEqual(UpgradeBlock.MaxRank, Progression.CanUpgradeAbility(c, 0, AbilityKind.HeavyShot));
        Assert.AreEqual(UpgradeBlock.MaxRank, Progression.CanUpgradeAbility(c, 2, AbilityKind.Ultimate));
    }

    [Test]
    public void LevelRequiredForRank_MatchesTheCaps()
    {
        Assert.AreEqual(1, Progression.LevelRequiredForRank(AbilityKind.Mist, 1));
        Assert.AreEqual(3, Progression.LevelRequiredForRank(AbilityKind.Mist, 2));
        Assert.AreEqual(9, Progression.LevelRequiredForRank(AbilityKind.Mist, 5));
        Assert.AreEqual(6, Progression.LevelRequiredForRank(AbilityKind.Ultimate, 1));
        Assert.AreEqual(18, Progression.LevelRequiredForRank(AbilityKind.Ultimate, 3));
    }

    // --- Sangrado ---

    [Test]
    public void UpgradeBleed_CostsOnePointAndRaisesCap()
    {
        CharacterSave c = NewCharacter();
        c.level = 3;

        Assert.IsTrue(Progression.TryUpgradeBleed(c));

        Assert.AreEqual(2, c.bleedLevel);
        Assert.AreEqual(2, Progression.PointsAvailable(c));
        Assert.AreEqual(6, BleedStacks.CapForLevel(c.bleedLevel));
    }

    [Test]
    public void UpgradeBleed_StopsAtLevel15()
    {
        CharacterSave c = NewCharacter();
        c.level = Progression.MaxLevel;
        c.bleedLevel = Progression.MaxBleedLevel;

        Assert.AreEqual(UpgradeBlock.MaxRank, Progression.CanUpgradeBleed(c));
        Assert.IsFalse(Progression.TryUpgradeBleed(c));
    }

    [Test]
    public void UpgradeBleed_WithoutPoints_IsBlocked()
    {
        CharacterSave c = NewCharacter(); // nivel 1: 1 punto
        Progression.TryUpgradeBleed(c);

        Assert.AreEqual(UpgradeBlock.NoPoints, Progression.CanUpgradeBleed(c));
    }

    // --- Guardado v3 ---

    [Test]
    public void Parse_V2_KeepsLevelAndAddsEmptyProgress()
    {
        const string v2 = @"{
            ""version"": 2, ""money"": 4100, ""prestigeCoins"": 0, ""selectedCharacterId"": ""alucard"",
            ""characters"": [
                { ""id"": ""alucard"", ""unlocked"": true, ""level"": 1, ""weapons"": [] },
                { ""id"": ""maga"", ""unlocked"": true, ""level"": 1, ""weapons"": [] }
            ]
        }";

        SaveData data = SaveMigrations.Parse(v2, out bool migrated);

        Assert.IsTrue(migrated);
        Assert.AreEqual(SaveData.CurrentVersion, data.version);
        Assert.AreEqual(4100, data.money);

        CharacterSave alucard = data.GetCharacter("alucard");
        Assert.AreEqual(1, alucard.level);
        Assert.AreEqual(0, alucard.xp);
        Assert.AreEqual(1, alucard.bleedLevel);
        CollectionAssert.AreEqual(new[] { 0, 0, 0 }, alucard.abilityRanks);
    }

    [Test]
    public void SaveProgress_RoundTripsThroughJson()
    {
        var original = new SaveData();
        CharacterSave c = original.GetCharacter("alucard");
        c.level = 8;
        c.xp = 150;
        c.abilityRanks = new[] { 3, 2, 1 };
        c.bleedLevel = 4;

        SaveData loaded = SaveMigrations.Parse(JsonUtility.ToJson(original), out bool migrated);

        Assert.IsFalse(migrated);
        CharacterSave back = loaded.GetCharacter("alucard");
        Assert.AreEqual(8, back.level);
        Assert.AreEqual(150, back.xp);
        CollectionAssert.AreEqual(new[] { 3, 2, 1 }, back.abilityRanks);
        Assert.AreEqual(4, back.bleedLevel);
    }

    [Test]
    public void Validate_FixesImpossibleProgress()
    {
        var data = new SaveData();
        CharacterSave c = data.GetCharacter("alucard");
        c.level = 99;
        c.xp = -5;
        c.bleedLevel = 0;
        c.abilityRanks = new[] { 9, -2 }; // tamaño y valores inválidos

        SaveMigrations.Validate(data);

        Assert.AreEqual(Progression.MaxLevel, c.level);
        Assert.AreEqual(0, c.xp);
        Assert.AreEqual(1, c.bleedLevel);
        Assert.AreEqual(Progression.AbilitySlots, c.abilityRanks.Length);
        Assert.AreEqual(Progression.MaxNormalRank, c.abilityRanks[0]);
        Assert.AreEqual(0, c.abilityRanks[1]);
    }

    // --- Rangos en las habilidades ---

    private static AbilityDefinition NewAbility(AbilityKind kind)
    {
        var a = ScriptableObject.CreateInstance<AbilityDefinition>();
        a.kind = kind;
        return a;
    }

    [Test]
    public void Ability_Rank1_UsesBaseValues()
    {
        AbilityDefinition a = NewAbility(AbilityKind.HeavyShot);
        a.cooldown = 8f; a.cooldownPerRank = 1f;
        a.damageMultiplier = 6f; a.damagePerRank = 1f;

        Assert.AreEqual(8f, a.CooldownAt(1), 0.001f);
        Assert.AreEqual(6f, a.DamageMultiplierAt(1), 0.001f);
        Object.DestroyImmediate(a);
    }

    [Test]
    public void Ability_HigherRanks_ScaleLinearly()
    {
        AbilityDefinition a = NewAbility(AbilityKind.Mist);
        a.cooldown = 14f; a.cooldownPerRank = 1f;
        a.duration = 2f; a.durationPerRank = 0.5f;
        a.speedMultiplier = 1.6f; a.speedPerRank = 0.1f;

        Assert.AreEqual(10f, a.CooldownAt(5), 0.001f);
        Assert.AreEqual(4f, a.DurationAt(5), 0.001f);
        Assert.AreEqual(2.0f, a.SpeedMultiplierAt(5), 0.001f);
        Object.DestroyImmediate(a);
    }

    [Test]
    public void Ability_Cooldown_NeverDropsBelowMinimum()
    {
        AbilityDefinition a = NewAbility(AbilityKind.HeavyShot);
        a.cooldown = 2f; a.cooldownPerRank = 5f;

        Assert.GreaterOrEqual(a.CooldownAt(5), 0.1f);
        Object.DestroyImmediate(a);
    }

    [Test]
    public void Ability_LifeSteal_GrowsWithRankAndCapsAtOne()
    {
        AbilityDefinition a = NewAbility(AbilityKind.Ultimate);
        a.lifeSteal = 0.5f; a.lifeStealPerRank = 0.4f;

        Assert.AreEqual(50, a.LifeStealFor(100, 1));
        Assert.AreEqual(90, a.LifeStealFor(100, 2));
        Assert.AreEqual(100, a.LifeStealFor(100, 3)); // tope: no cura más que el daño
        Object.DestroyImmediate(a);
    }

    [Test]
    public void Ability_DamageFor_UsesTheRankMultiplier()
    {
        AbilityDefinition a = NewAbility(AbilityKind.HeavyShot);
        a.damageMultiplier = 6f; a.damagePerRank = 1f;

        Assert.AreEqual(60, a.DamageFor(10, 1));
        Assert.AreEqual(90, a.DamageFor(10, 4));
        Object.DestroyImmediate(a);
    }

    [Test]
    public void Ability_DescribeRank_ZeroIsNotLearned()
    {
        AbilityDefinition a = NewAbility(AbilityKind.Mist);

        Assert.AreEqual("Sin aprender", a.DescribeRank(0));
        StringAssert.Contains("velocidad", a.DescribeRank(1));
        Object.DestroyImmediate(a);
    }
}
