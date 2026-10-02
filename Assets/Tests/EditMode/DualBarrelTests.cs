using System.Linq;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class DualBarrelTests
{
    private WeaponDefinition Gun(int barrels)
    {
        var gun = ScriptableObject.CreateInstance<WeaponDefinition>();
        gun.name = "Pistola";
        gun.weaponName = "Pistolas";
        gun.magazineSize = 12;
        gun.barrels = barrels;
        gun.fireRate = 0.2f;
        gun.reloadTime = 1.5f;
        gun.damage = 10;
        gun.fireRateUpgrade = new UpgradeStat { step = 0.02f, limit = 0.05f, maxLevel = 5 };
        gun.reloadUpgrade = new UpgradeStat { step = 0.15f, limit = 0.5f, maxLevel = 5 };
        gun.damageUpgrade = new UpgradeStat { step = 1f, maxLevel = 10 };
        return gun;
    }

    [Test]
    public void NewWeapons_HaveOneBarrelByDefault()
    {
        var gun = ScriptableObject.CreateInstance<WeaponDefinition>();
        Assert.AreEqual(1, gun.barrels);
        Object.DestroyImmediate(gun);
    }

    [Test]
    public void TwoBarrels_AlternatingDoNotDoubleTheDps_TheyOnlyReloadLessOften()
    {
        WeaponDefinition one = Gun(1), two = Gun(2);

        // nivel 0: una pistola 12×10 ÷ (12×0,2 + 1,5) = 30,77; dos pistolas 24×10 ÷ (24×0,2 + 1,5) = 38,10
        Assert.AreEqual(30.769f, CombatMath.GunSustainedDps(one, 0, 0, 0), 0.01f);
        Assert.AreEqual(38.095f, CombatMath.GunSustainedDps(two, 0, 0, 0), 0.01f);

        foreach (int level in new[] { 0, 3, 5 })
        {
            float single = CombatMath.GunSustainedDps(one, level, level, level * 2);
            float dual = CombatMath.GunSustainedDps(two, level, level, level * 2);

            Assert.Greater(dual, single, "nivel " + level + ": recarga menos veces");
            Assert.Less(dual, single * 1.4f, "nivel " + level + ": pero NO el doble");
        }

        Object.DestroyImmediate(one);
        Object.DestroyImmediate(two);
    }

    [Test]
    public void TwoBarrels_HaveDoubleTheBulletsPerReload()
    {
        WeaponDefinition gun = Gun(2);
        var magazines = new BarrelMagazines(gun.barrels, gun.magazineSize);

        Assert.AreEqual(24, magazines.Total);

        Object.DestroyImmediate(gun);
    }

    [Test]
    public void TwoBarrels_DoNotChangeTimingOrDamagePerBullet()
    {
        WeaponDefinition one = Gun(1), two = Gun(2);

        Assert.AreEqual(one.FireRateAt(3), two.FireRateAt(3), 0.0001f);
        Assert.AreEqual(one.ReloadTimeAt(3), two.ReloadTimeAt(3), 0.0001f);
        Assert.AreEqual(one.DamageAt(7), two.DamageAt(7));

        Object.DestroyImmediate(one);
        Object.DestroyImmediate(two);
    }

    [Test]
    public void Describe_DualPistols_ShowsBothMagazinesAndTheTotal()
    {
        WeaponDefinition dual = Gun(2);
        var character = ScriptableObject.CreateInstance<CharacterDefinition>();
        character.startingWeapons = new[] { dual };

        string line = CharacterInfo.Describe(character, null).First(l => l.Label == "Pistolas").Value;

        StringAssert.Contains("daño 10 ·", line, "una bala por clic: el daño no se multiplica");
        StringAssert.DoesNotContain("×", line);
        StringAssert.Contains("cargador 12 + 12 (24 balas)", line);
        StringAssert.Contains("turnándose", line);

        Object.DestroyImmediate(dual);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void Describe_SinglePistol_KeepsTheSimpleText()
    {
        WeaponDefinition single = Gun(1);
        var character = ScriptableObject.CreateInstance<CharacterDefinition>();
        character.startingWeapons = new[] { single };

        string line = CharacterInfo.Describe(character, null).First(l => l.Label == "Pistolas").Value;

        StringAssert.Contains("cargador 12 ·", line);
        StringAssert.DoesNotContain("×", line);
        StringAssert.DoesNotContain("balas)", line);

        Object.DestroyImmediate(single);
        Object.DestroyImmediate(character);
    }
}
