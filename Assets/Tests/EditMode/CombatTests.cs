using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class ManaPoolTests
{
    [Test]
    public void NewPool_StartsFull()
    {
        var mana = new ManaPool(100f, 4f);
        Assert.AreEqual(100f, mana.Current, 0.001f);
    }

    [Test]
    public void TrySpend_WithEnough_SubtractsAndReturnsTrue()
    {
        var mana = new ManaPool(100f, 4f);

        Assert.IsTrue(mana.TrySpend(25f));
        Assert.AreEqual(75f, mana.Current, 0.001f);
    }

    [Test]
    public void TrySpend_WithoutEnough_ChangesNothing()
    {
        var mana = new ManaPool(100f, 4f);
        mana.TrySpend(90f);

        Assert.IsFalse(mana.TrySpend(25f));
        Assert.AreEqual(10f, mana.Current, 0.001f);
    }

    [Test]
    public void TrySpend_ExactAmount_Works()
    {
        var mana = new ManaPool(25f, 4f);
        Assert.IsTrue(mana.TrySpend(25f));
        Assert.AreEqual(0f, mana.Current, 0.001f);
    }

    [Test]
    public void Tick_RegeneratesButNeverAboveMax()
    {
        var mana = new ManaPool(100f, 4f);
        mana.TrySpend(30f);

        mana.Tick(2f);
        Assert.AreEqual(78f, mana.Current, 0.001f);

        mana.Tick(100f);
        Assert.AreEqual(100f, mana.Current, 0.001f);
    }

    [Test]
    public void Configure_LowerMax_ClampsCurrent()
    {
        var mana = new ManaPool(100f, 4f);
        mana.Configure(60f, 8f);

        Assert.AreEqual(60f, mana.Current, 0.001f);
        Assert.AreEqual(8f, mana.RegenPerSecond, 0.001f);
    }

    [Test]
    public void BurstThenSustained_FollowsTheDesignedRhythm()
    {
        // Diseño: unos 5 rayos seguidos (uno cada 3 s) y después uno cada ~6 s.
        var mana = new ManaPool(100f, 4f);
        var cooldown = new AbilityCooldown();
        float now = 0f;
        int casts = 0;

        for (int step = 0; step < 3000; step++) // 30 s a 0,01 s
        {
            now += 0.01f;
            mana.Tick(0.01f);
            if (cooldown.IsReady(now) && mana.TrySpend(25f))
            {
                cooldown.Start(now, 3f);
                casts++;
            }
        }

        // 100 maná iniciales + 30 s × 4/s = 220 maná → como máximo 8 rayos en 30 s.
        Assert.That(casts, Is.InRange(7, 8));
    }
}

[TestFixture]
public class AbilityCooldownTests
{
    [Test]
    public void New_IsReady()
    {
        Assert.IsTrue(new AbilityCooldown().IsReady(0f));
    }

    [Test]
    public void AfterStart_IsNotReadyUntilDurationPasses()
    {
        var cooldown = new AbilityCooldown();
        cooldown.Start(10f, 3f);

        Assert.IsFalse(cooldown.IsReady(12.9f));
        Assert.AreEqual(2f, cooldown.Remaining(11f), 0.001f);
        Assert.IsTrue(cooldown.IsReady(13f));
        Assert.AreEqual(0f, cooldown.Remaining(20f), 0.001f);
    }

    [Test]
    public void Reset_MakesItReadyAgain()
    {
        var cooldown = new AbilityCooldown();
        cooldown.Start(10f, 3f);
        cooldown.Reset();

        Assert.IsTrue(cooldown.IsReady(10f));
    }
}

[TestFixture]
public class StaffDefinitionTests
{
    private StaffDefinition staff;

    [SetUp]
    public void SetUp()
    {
        staff = ScriptableObject.CreateInstance<StaffDefinition>();
        staff.damage = 8;
        staff.fireRate = 0.35f;
        staff.fireRateUpgrade = new UpgradeStat { step = 0.03f, limit = 0.2f, maxLevel = 5 };
        staff.reloadUpgrade = new UpgradeStat { step = 0.8f, maxLevel = 5 };
        staff.damageUpgrade = new UpgradeStat { step = 0.1f, maxLevel = 10 };
        staff.manaRegen = 4f;
        staff.abilityDamage = 40;
        staff.abilityCooldown = 3f;
        staff.cooldownStep = 0.3f;
        staff.cooldownLimit = 1.5f;
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(staff);

    [Test]
    public void DoesNotUseAmmo_AndNamesItsUpgrades()
    {
        Assert.IsFalse(staff.UsesAmmo);
        Assert.AreEqual("Cadencia", staff.UpgradeLabel(UpgradeType.FireRate));
        Assert.AreEqual("Maná", staff.UpgradeLabel(UpgradeType.Reload));
        Assert.AreEqual("Poder", staff.UpgradeLabel(UpgradeType.Damage));
    }

    [Test]
    public void PowerLevel_MultipliesBothBasicAndAbilityDamage()
    {
        Assert.AreEqual(8, staff.DamageAt(0));
        Assert.AreEqual(16, staff.DamageAt(10));
        Assert.AreEqual(40, staff.AbilityDamageAt(0));
        Assert.AreEqual(80, staff.AbilityDamageAt(10));
    }

    [Test]
    public void ManaLevel_RaisesRegenAndLowersCooldownDownToTheLimit()
    {
        Assert.AreEqual(4f, staff.ManaRegenAt(0), 0.001f);
        Assert.AreEqual(8f, staff.ManaRegenAt(5), 0.001f);
        Assert.AreEqual(3f, staff.AbilityCooldownAt(0), 0.001f);
        Assert.AreEqual(1.5f, staff.AbilityCooldownAt(5), 0.001f);
        Assert.AreEqual(1.5f, staff.AbilityCooldownAt(50), 0.001f);
    }

    [Test]
    public void FireRateLevel_SpeedsUpTheBasicShotDownToTheLimit()
    {
        Assert.AreEqual(0.35f, staff.FireRateAt(0), 0.001f);
        Assert.AreEqual(0.2f, staff.FireRateAt(5), 0.001f);
        Assert.AreEqual(0.2f, staff.FireRateAt(50), 0.001f);
    }

    [Test]
    public void Gun_KeepsItsOriginalFormulas()
    {
        var gun = ScriptableObject.CreateInstance<WeaponDefinition>();
        gun.damage = 10;
        gun.fireRate = 0.2f;
        gun.reloadTime = 1.5f;
        gun.fireRateUpgrade = new UpgradeStat { step = 0.02f, limit = 0.05f };
        gun.reloadUpgrade = new UpgradeStat { step = 0.15f, limit = 0.5f };
        gun.damageUpgrade = new UpgradeStat { step = 1f };

        Assert.IsTrue(gun.UsesAmmo);
        Assert.AreEqual(20, gun.DamageAt(10));
        Assert.AreEqual(0.1f, gun.FireRateAt(5), 0.001f);
        Assert.AreEqual(0.75f, gun.ReloadTimeAt(5), 0.001f);
        Assert.AreEqual(0.05f, gun.FireRateAt(50), 0.001f);

        Object.DestroyImmediate(gun);
    }
}
