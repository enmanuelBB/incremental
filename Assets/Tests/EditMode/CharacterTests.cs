using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class CharacterRulesTests
{
    private CharacterDefinition free;
    private CharacterDefinition paid;
    private CharacterDefinition byMission;
    private CharacterDefinition reserved;
    private SaveData data;

    private static CharacterDefinition Make(string name, int order, CharacterUnlockKind kind, int price = 0, CharacterAvailability availability = CharacterAvailability.Playable)
    {
        var def = ScriptableObject.CreateInstance<CharacterDefinition>();
        def.name = name; // GameDefinition.Id usa el nombre del asset si no hay id
        def.displayName = name;
        def.menuOrder = order;
        def.unlockKind = kind;
        def.unlockPrice = price;
        def.availability = availability;
        return def;
    }

    [SetUp]
    public void SetUp()
    {
        free = Make("alucard", 0, CharacterUnlockKind.Free);
        paid = Make("maga", 1, CharacterUnlockKind.Money, 1500);
        byMission = Make("asesino", 2, CharacterUnlockKind.Mission);
        reserved = Make("slot3", 3, CharacterUnlockKind.Event, 0, CharacterAvailability.ComingSoon);
        data = new SaveData();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var def in new[] { free, paid, byMission, reserved }) Object.DestroyImmediate(def);
    }

    [Test]
    public void Status_FreeIsUnlocked_PaidAndMissionAreLocked_ReservedIsComingSoon()
    {
        Assert.AreEqual(CharacterStatus.Unlocked, CharacterRules.StatusOf(free, data));
        Assert.AreEqual(CharacterStatus.Locked, CharacterRules.StatusOf(paid, data));
        Assert.AreEqual(CharacterStatus.Locked, CharacterRules.StatusOf(byMission, data));
        Assert.AreEqual(CharacterStatus.ComingSoon, CharacterRules.StatusOf(reserved, data));
    }

    [Test]
    public void TryUnlockWithMoney_WithEnough_ChargesAndUnlocks()
    {
        data.money = 2000;

        Assert.IsTrue(CharacterRules.TryUnlockWithMoney(paid, data));
        Assert.AreEqual(500, data.money);
        Assert.AreEqual(CharacterStatus.Unlocked, CharacterRules.StatusOf(paid, data));
    }

    [Test]
    public void TryUnlockWithMoney_WithoutEnough_ChargesNothing()
    {
        data.money = 1499;

        Assert.IsFalse(CharacterRules.TryUnlockWithMoney(paid, data));
        Assert.AreEqual(1499, data.money);
        Assert.AreEqual(CharacterStatus.Locked, CharacterRules.StatusOf(paid, data));
    }

    [Test]
    public void TryUnlockWithMoney_TwiceDoesNotChargeTwice()
    {
        data.money = 5000;

        CharacterRules.TryUnlockWithMoney(paid, data);
        Assert.IsFalse(CharacterRules.TryUnlockWithMoney(paid, data));
        Assert.AreEqual(3500, data.money);
    }

    [Test]
    public void MissionCharacter_CannotBeBoughtButCanBeUnlockedByCode()
    {
        data.money = 999999;

        Assert.IsFalse(CharacterRules.TryUnlockWithMoney(byMission, data));
        Assert.AreEqual(CharacterStatus.Locked, CharacterRules.StatusOf(byMission, data));

        CharacterRules.Unlock(byMission, data);
        Assert.AreEqual(CharacterStatus.Unlocked, CharacterRules.StatusOf(byMission, data));
        Assert.AreEqual(999999, data.money);
    }

    [Test]
    public void ReservedSlot_CanNeverBeUnlockedOrSelected()
    {
        data.money = 999999;

        CharacterRules.Unlock(reserved, data);

        Assert.AreEqual(CharacterStatus.ComingSoon, CharacterRules.StatusOf(reserved, data));
        Assert.IsFalse(CharacterRules.Select(reserved, data));
        Assert.IsFalse(CharacterRules.TryUnlockWithMoney(reserved, data));
    }

    [Test]
    public void Select_OnlyWorksForUnlockedCharacters_AndIsRemembered()
    {
        Assert.IsFalse(CharacterRules.Select(paid, data));
        Assert.AreEqual(SaveData.DefaultCharacterId, data.selectedCharacterId);

        CharacterRules.Unlock(paid, data);
        Assert.IsTrue(CharacterRules.Select(paid, data));
        Assert.AreEqual("maga", data.selectedCharacterId);
    }

    [Test]
    public void ResolveSelected_ReturnsTheSavedOne_OrFallsBackToTheFirstAvailable()
    {
        var roster = new List<CharacterDefinition> { reserved, byMission, paid, free };

        data.selectedCharacterId = "maga";
        Assert.AreSame(free, CharacterRules.ResolveSelected(roster, data), "la maga está bloqueada: cae al primero disponible");

        CharacterRules.Unlock(paid, data);
        Assert.AreSame(paid, CharacterRules.ResolveSelected(roster, data));

        data.selectedCharacterId = "no_existe";
        Assert.AreSame(free, CharacterRules.ResolveSelected(roster, data), "personaje desconocido: primero por orden del menú");
    }
}

[TestFixture]
public class CharacterInfoTests
{
    [Test]
    public void Describe_Gun_ShowsHealthSpeedAndWeaponStatsWithSavedLevels()
    {
        var gun = ScriptableObject.CreateInstance<WeaponDefinition>();
        gun.name = "Pistola";
        gun.weaponName = "pistola";
        gun.magazineSize = 12; gun.fireRate = 0.2f; gun.reloadTime = 1.5f; gun.damage = 10;
        gun.fireRateUpgrade = new UpgradeStat { step = 0.02f, limit = 0.05f, maxLevel = 5 };
        gun.reloadUpgrade = new UpgradeStat { step = 0.15f, limit = 0.5f, maxLevel = 5 };
        gun.damageUpgrade = new UpgradeStat { step = 1f, maxLevel = 10 };

        var character = ScriptableObject.CreateInstance<CharacterDefinition>();
        character.maxHealth = 120; character.moveSpeed = 4.5f; character.startingWeapons = new[] { gun };

        var save = new CharacterSave { id = "x" };
        save.GetWeapon("Pistola").upgradeLevels[(int)UpgradeType.Damage] = 10;

        List<StatLine> lines = CharacterInfo.Describe(character, save);

        Assert.AreEqual("120", lines.First(l => l.Label == "Vida").Value);
        Assert.AreEqual("4.5", lines.First(l => l.Label == "Velocidad").Value);
        StringAssert.Contains("daño 20", lines.First(l => l.Label == "pistola").Value, "usa el nivel de daño guardado");

        Object.DestroyImmediate(gun);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void Describe_Staff_ShowsManaAbilityAndDps()
    {
        var staff = ScriptableObject.CreateInstance<StaffDefinition>();
        staff.weaponName = "Bastón"; staff.damage = 8; staff.fireRate = 0.35f;
        staff.fireRateUpgrade = new UpgradeStat { step = 0.03f, limit = 0.2f, maxLevel = 5 };
        staff.reloadUpgrade = new UpgradeStat { step = 0.8f, maxLevel = 5 };
        staff.damageUpgrade = new UpgradeStat { step = 0.1f, maxLevel = 10 };
        staff.manaMax = 100f; staff.manaRegen = 4f; staff.abilityName = "Rayo"; staff.abilityDamage = 40; staff.abilityManaCost = 25f; staff.abilityCooldown = 3f;
        staff.cooldownStep = 0.3f; staff.cooldownLimit = 1.5f;

        var character = ScriptableObject.CreateInstance<CharacterDefinition>();
        character.startingWeapons = new WeaponDefinition[] { staff };

        List<StatLine> lines = CharacterInfo.Describe(character, null);

        StringAssert.Contains("sin maná", lines.First(l => l.Label == "Bastón").Value);
        StringAssert.Contains("100", lines.First(l => l.Label == "Maná").Value);
        StringAssert.Contains("daño 40", lines.First(l => l.Label == "Rayo").Value);
        StringAssert.Contains("en fila", lines.First(l => l.Label == "DPS sostenido").Value);

        Object.DestroyImmediate(staff);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void Describe_NoWeapons_StillShowsHealthAndSpeed()
    {
        var character = ScriptableObject.CreateInstance<CharacterDefinition>();

        List<StatLine> lines = CharacterInfo.Describe(character, null);

        Assert.AreEqual(2, lines.Count);
        Object.DestroyImmediate(character);
    }
}
