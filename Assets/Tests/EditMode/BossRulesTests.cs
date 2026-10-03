using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class BossRulesTests
{
    private static BossAbility Window(BossAbilityKind kind, float min, float max) =>
        new BossAbility { kind = kind, minHealthFraction = min, maxHealthFraction = max };

    // --- Ventanas de fase ---

    [TestCase(1f)]
    [TestCase(0.75f)]
    [TestCase(0.51f)]
    public void IsActive_FirstPhase_AboveHalf(float fraction)
    {
        Assert.IsTrue(BossRules.IsActive(Window(BossAbilityKind.Summon, 0.5f, 1f), fraction));
        Assert.IsFalse(BossRules.IsActive(Window(BossAbilityKind.Summon, 0f, 0.5f), fraction));
    }

    [TestCase(0.5f)]
    [TestCase(0.25f)]
    [TestCase(0.01f)]
    public void IsActive_SecondPhase_FromHalfDown(float fraction)
    {
        Assert.IsTrue(BossRules.IsActive(Window(BossAbilityKind.Summon, 0f, 0.5f), fraction));
        Assert.IsFalse(BossRules.IsActive(Window(BossAbilityKind.Summon, 0.5f, 1f), fraction));
    }

    [Test]
    public void IsActive_ExactlyOneOfTwoConsecutivePhasesIsActiveAtEveryHealth()
    {
        BossAbility first = Window(BossAbilityKind.Shoot, 0.5f, 1f);
        BossAbility second = Window(BossAbilityKind.Shoot, 0f, 0.5f);

        for (int i = 1; i <= 100; i++)
        {
            float fraction = i / 100f;
            bool a = BossRules.IsActive(first, fraction);
            bool b = BossRules.IsActive(second, fraction);
            Assert.IsTrue(a ^ b, "con vida " + fraction + " debe haber una sola fase activa");
        }
    }

    [Test]
    public void IsActive_NothingIsActiveWhenDead()
    {
        Assert.IsFalse(BossRules.IsActive(Window(BossAbilityKind.Charge, 0f, 1f), 0f));
    }

    [Test]
    public void IsActive_DefaultWindowIsAlwaysOn()
    {
        var ability = new BossAbility();

        Assert.IsTrue(BossRules.IsActive(ability, 1f));
        Assert.IsTrue(BossRules.IsActive(ability, 0.2f));
    }

    // --- Disparos únicos (enfurecer) ---

    [Test]
    public void Triggered_WhenHealthReachesTheThreshold()
    {
        BossAbility enrage = Window(BossAbilityKind.Enrage, 0f, 0.5f);

        Assert.IsFalse(BossRules.Triggered(enrage, 0.6f));
        Assert.IsTrue(BossRules.Triggered(enrage, 0.5f));
        Assert.IsTrue(BossRules.Triggered(enrage, 0.2f));
    }

    [Test]
    public void PhaseState_FiresEachAbilityOnlyOnce()
    {
        var state = new BossPhaseState();

        Assert.IsTrue(state.TryFireOnce(2));
        Assert.IsFalse(state.TryFireOnce(2));
        Assert.IsTrue(state.TryFireOnce(3));
    }

    [Test]
    public void PhaseState_Reset_AllowsFiringAgain()
    {
        var state = new BossPhaseState();
        state.TryFireOnce(0);
        state.ReviveUsed = true;

        state.Reset();

        Assert.IsTrue(state.TryFireOnce(0));
        Assert.IsFalse(state.ReviveUsed);
    }

    // --- Resurrección ---

    [Test]
    public void ReviveHealth_IsTheConfiguredFractionOfMaxHealth()
    {
        var revive = new BossAbility { kind = BossAbilityKind.Revive, reviveHealth = 0.5f };

        Assert.AreEqual(2250, BossRules.ReviveHealth(4500, revive));
    }

    [Test]
    public void ReviveHealth_RoundsUpAndIsNeverZero()
    {
        var revive = new BossAbility { kind = BossAbilityKind.Revive, reviveHealth = 0.5f };

        Assert.AreEqual(2, BossRules.ReviveHealth(3, revive));
        Assert.AreEqual(1, BossRules.ReviveHealth(1, new BossAbility { reviveHealth = 0f }));
    }

    // --- Embestida ---

    [Test]
    public void ChargeTargetsThePlayerOnlyWithinRange()
    {
        Assert.IsTrue(BossRules.ChargeTargetsPlayer(12f, 30f));
        Assert.IsTrue(BossRules.ChargeTargetsPlayer(30f, 30f));
        Assert.IsFalse(BossRules.ChargeTargetsPlayer(30.1f, 30f));
    }

    // --- Temporizadores ---

    [Test]
    public void Timers_AreDueOnlyAfterTheirDelay()
    {
        var timers = new AbilityTimers(3);
        timers.Schedule(1, now: 10f, delay: 8f);

        Assert.IsFalse(timers.Due(1, 17.9f));
        Assert.IsTrue(timers.Due(1, 18f));
        Assert.IsTrue(timers.Due(1, 99f));
    }

    [Test]
    public void Timers_AreIndependentPerAbility()
    {
        var timers = new AbilityTimers(2);
        timers.Schedule(0, 0f, 5f);
        timers.Schedule(1, 0f, 20f);

        Assert.IsTrue(timers.Due(0, 6f));
        Assert.IsFalse(timers.Due(1, 6f));
    }

    [Test]
    public void Timers_AbilityIndexOutOfRange_IsNeverDue()
    {
        var timers = new AbilityTimers(1);

        Assert.IsFalse(timers.Due(5, 100f));
        Assert.DoesNotThrow(() => timers.Schedule(5, 0f, 1f));
    }

    // --- Datos del enemigo ---

    [Test]
    public void EnemyDefinition_TierDecidesIfItIsABoss()
    {
        var def = ScriptableObject.CreateInstance<EnemyDefinition>();

        Assert.IsFalse(def.IsBoss);
        def.tier = EnemyTier.MiniBoss;
        Assert.IsTrue(def.IsBoss);
        def.tier = EnemyTier.Boss;
        Assert.IsTrue(def.IsBoss);

        Object.DestroyImmediate(def);
    }

    // --- Oleadas de jefe ---

    private static WaveSet SetWithBosses(out EnemyDefinition charger, out EnemyDefinition summoner)
    {
        charger = ScriptableObject.CreateInstance<EnemyDefinition>();
        summoner = ScriptableObject.CreateInstance<EnemyDefinition>();

        var set = ScriptableObject.CreateInstance<WaveSet>();
        set.waves = new[] { new WaveSet.Wave { enemyGroups = new WaveSet.EnemyGroup[0], spawnInterval = 1f } };
        set.bossCycleLength = 20;
        set.bosses = new[]
        {
            new WaveSet.BossWave { wave = 5, boss = charger },
            new WaveSet.BossWave { wave = 10, boss = summoner }
        };
        return set;
    }

    [Test]
    public void BossFor_ReturnsTheBossOfThatWaveOnly()
    {
        WaveSet set = SetWithBosses(out EnemyDefinition charger, out EnemyDefinition summoner);

        Assert.AreSame(charger, WaveBuilder.BossFor(set, 5));
        Assert.AreSame(summoner, WaveBuilder.BossFor(set, 10));
        Assert.IsNull(WaveBuilder.BossFor(set, 6));
        Assert.IsNull(WaveBuilder.BossFor(set, 1));
    }

    [Test]
    public void BossFor_TheCycleRepeatsAfterItsLength()
    {
        WaveSet set = SetWithBosses(out EnemyDefinition charger, out EnemyDefinition summoner);

        Assert.AreSame(charger, WaveBuilder.BossFor(set, 25));
        Assert.AreSame(summoner, WaveBuilder.BossFor(set, 30));
        Assert.AreSame(charger, WaveBuilder.BossFor(set, 45));
        Assert.IsNull(WaveBuilder.BossFor(set, 26));
    }

    [Test]
    public void BossFor_WithoutACycle_DoesNotRepeat()
    {
        WaveSet set = SetWithBosses(out EnemyDefinition charger, out _);
        set.bossCycleLength = 0;

        Assert.AreSame(charger, WaveBuilder.BossFor(set, 5));
        Assert.IsNull(WaveBuilder.BossFor(set, 25));
    }

    [Test]
    public void BossFor_NoBossesOrNullSet_IsNull()
    {
        Assert.IsNull(WaveBuilder.BossFor(null, 5));

        var empty = ScriptableObject.CreateInstance<WaveSet>();
        Assert.IsNull(WaveBuilder.BossFor(empty, 5));
    }

    [Test]
    public void BossFor_SkipsEntriesWithoutABoss()
    {
        WaveSet set = SetWithBosses(out _, out _);
        set.bosses = new[] { new WaveSet.BossWave { wave = 5, boss = null } };

        Assert.IsNull(WaveBuilder.BossFor(set, 5));
    }

    [Test]
    public void Build_PutsTheBossInThePlanOfItsWave()
    {
        WaveSet set = SetWithBosses(out EnemyDefinition charger, out _);

        Assert.AreSame(charger, WaveBuilder.Build(set, 4).Boss);   // índice 4 = oleada 5
        Assert.IsNull(WaveBuilder.Build(set, 3).Boss);
    }
}
