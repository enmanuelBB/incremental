using NUnit.Framework;
using UnityEngine;

public class BleedUpgradeTests
{
    WeaponDefinition gun;

    [SetUp]
    public void SetUp() => gun = ScriptableObject.CreateInstance<WeaponDefinition>();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(gun);

    [Test]
    public void BleedPerHit_StartsAtOneAndGrowsOnePerLevel()
    {
        Assert.AreEqual(1, gun.BleedPerHitAt(0));
        Assert.AreEqual(2, gun.BleedPerHitAt(1));
        Assert.AreEqual(11, gun.BleedPerHitAt(10));
    }

    [TestCase(0, 600)]
    [TestCase(1, 960)]
    [TestCase(2, 1540)]
    [TestCase(9, 41230)]
    public void BleedPrice_GrowsExponentially(int level, int expected)
    {
        Assert.AreEqual(expected, gun.GetUpgrade(UpgradeType.Bleed).PriceAt(level));
    }

    [Test]
    public void BleedUpgrade_TotalCostIsAboutOneHundredNineThousand()
    {
        UpgradeStat bleed = gun.GetUpgrade(UpgradeType.Bleed);
        int total = 0;
        for (int level = 0; level < bleed.maxLevel; level++) total += bleed.PriceAt(level);
        Assert.That(total, Is.InRange(105000, 113000));
    }

    [Test]
    public void BleedUpgrade_IsFarMoreExpensiveThanDamageUpgrade()
    {
        UpgradeStat damage = gun.GetUpgrade(UpgradeType.Damage);
        int damageTotal = 0;
        for (int level = 0; level < damage.maxLevel; level++) damageTotal += damage.PriceAt(level);

        UpgradeStat bleed = gun.GetUpgrade(UpgradeType.Bleed);
        int bleedTotal = 0;
        for (int level = 0; level < bleed.maxLevel; level++) bleedTotal += bleed.PriceAt(level);

        Assert.Greater(bleedTotal, damageTotal * 10);
    }

    [Test]
    public void LinearUpgrades_KeepTheirOldPrices()
    {
        UpgradeStat damage = gun.GetUpgrade(UpgradeType.Damage);
        Assert.AreEqual(150, damage.PriceAt(0));
        Assert.AreEqual(150 + 3 * 75, damage.PriceAt(3));
    }

    [Test]
    public void GetUpgrade_ReturnsADifferentStatForEveryType()
    {
        Assert.AreNotSame(gun.GetUpgrade(UpgradeType.Damage), gun.GetUpgrade(UpgradeType.Bleed));
        Assert.AreNotSame(gun.GetUpgrade(UpgradeType.FireRate), gun.GetUpgrade(UpgradeType.Bleed));
        Assert.AreNotSame(gun.GetUpgrade(UpgradeType.Reload), gun.GetUpgrade(UpgradeType.Bleed));
    }

    [Test]
    public void Staff_DoesNotApplyBleed()
    {
        var staff = ScriptableObject.CreateInstance<StaffDefinition>();
        Assert.IsFalse(staff.AppliesBleed);
        Assert.AreEqual(0, staff.BleedPerHitAt(5));
        Object.DestroyImmediate(staff);
    }

    [Test]
    public void WeaponWithZeroBaseBleed_DoesNotBleed()
    {
        gun.baseBleedPerHit = 0;
        Assert.IsFalse(gun.AppliesBleed);
        Assert.AreEqual(0, gun.BleedPerHitAt(3));
    }

    [Test]
    public void OldSave_WithThreeUpgradeLevels_GainsAFourthAtZero()
    {
        var weapon = new WeaponSave { id = "Pistola", owned = true, upgradeLevels = new[] { 2, 1, 5 } };
        weapon.Normalize();

        Assert.AreEqual(UpgradeTypeInfo.Count, weapon.upgradeLevels.Length);
        Assert.AreEqual(5, weapon.upgradeLevels[(int)UpgradeType.Damage]);
        Assert.AreEqual(0, weapon.upgradeLevels[(int)UpgradeType.Bleed]);
    }
}
