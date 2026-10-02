using System;
using NUnit.Framework;

[TestFixture]
public class SaveMigrationTests
{
    // Guardado real en el formato original (sin campo "version").
    private const string V1Json = @"{
        ""money"": 10730,
        ""weapons"": [
            { ""id"": ""Pistola"", ""owned"": false, ""upgradeLevels"": [5, 5, 10] },
            { ""id"": ""M16"", ""owned"": true, ""upgradeLevels"": [5, 5, 10] }
        ]
    }";

    [Test]
    public void Parse_V1_MovesProgressToDefaultCharacter()
    {
        SaveData data = SaveMigrations.Parse(V1Json, out bool migrated);

        Assert.IsTrue(migrated);
        Assert.AreEqual(SaveData.CurrentVersion, data.version);
        Assert.AreEqual(10730, data.money);

        CharacterSave main = data.GetCharacter(SaveData.DefaultCharacterId);
        Assert.IsTrue(main.unlocked);
        Assert.AreEqual(2, main.weapons.Count);
    }

    [Test]
    public void Parse_V1_KeepsWeaponOwnershipAndLevels()
    {
        SaveData data = SaveMigrations.Parse(V1Json, out _);
        CharacterSave main = data.GetCharacter(SaveData.DefaultCharacterId);

        WeaponSave m16 = main.GetWeapon("M16");
        Assert.IsTrue(m16.owned);
        // Los tres niveles viejos se conservan; la mejora de sangrado (nueva) empieza en 0.
        CollectionAssert.AreEqual(new[] { 5, 5, 10, 0 }, m16.upgradeLevels);

        Assert.IsFalse(main.GetWeapon("Pistola").owned);
    }

    [Test]
    public void Parse_CurrentVersion_RoundTripsWithoutMigrating()
    {
        var original = new SaveData { money = 250, prestigeCoins = 3 };
        original.GetCharacter(SaveData.DefaultCharacterId).GetWeapon("M16").owned = true;

        SaveData loaded = SaveMigrations.Parse(UnityEngine.JsonUtility.ToJson(original), out bool migrated);

        Assert.IsFalse(migrated);
        Assert.AreEqual(250, loaded.money);
        Assert.AreEqual(3, loaded.prestigeCoins);
        Assert.IsTrue(loaded.GetCharacter(SaveData.DefaultCharacterId).GetWeapon("M16").owned);
    }

    [Test]
    public void Parse_NewerVersion_Throws()
    {
        string json = "{\"version\": " + (SaveData.CurrentVersion + 1) + ", \"money\": 5}";

        var e = Assert.Throws<NewerSaveVersionException>(() => SaveMigrations.Parse(json, out _));
        Assert.AreEqual(SaveData.CurrentVersion + 1, e.FoundVersion);
    }

    [Test]
    public void Parse_Garbage_Throws()
    {
        Assert.Catch<Exception>(() => SaveMigrations.Parse("esto no es json", out _));
    }

    [Test]
    public void Parse_NegativeMoneyAndLevels_AreClamped()
    {
        string json = @"{ ""money"": -50, ""weapons"": [ { ""id"": ""M16"", ""owned"": true, ""upgradeLevels"": [-3, 2, 1] } ] }";

        SaveData data = SaveMigrations.Parse(json, out _);

        Assert.AreEqual(0, data.money);
        CollectionAssert.AreEqual(new[] { 0, 2, 1, 0 }, data.GetCharacter(SaveData.DefaultCharacterId).GetWeapon("M16").upgradeLevels);
    }

    [Test]
    public void Parse_ShortUpgradeArray_IsResizedToUpgradeCount()
    {
        string json = @"{ ""money"": 1, ""weapons"": [ { ""id"": ""M16"", ""owned"": true, ""upgradeLevels"": [4] } ] }";

        SaveData data = SaveMigrations.Parse(json, out _);

        int[] levels = data.GetCharacter(SaveData.DefaultCharacterId).GetWeapon("M16").upgradeLevels;
        Assert.AreEqual(UpgradeTypeInfo.Count, levels.Length);
        Assert.AreEqual(4, levels[0]);
    }

    [Test]
    public void GetWeapon_WithLegacyIndex_RecoversPositionBasedProgress()
    {
        var character = new CharacterSave { id = SaveData.DefaultCharacterId };
        WeaponSave legacy = character.GetWeapon("legacy_1");
        legacy.owned = true;

        WeaponSave found = character.GetWeapon("M16", 1);

        Assert.AreSame(legacy, found);
        Assert.AreEqual("M16", found.id);
        Assert.IsTrue(found.owned);
    }

    [Test]
    public void GetCharacter_UnknownId_CreatesLockedCharacter()
    {
        var data = new SaveData();

        CharacterSave other = data.GetCharacter("otro");

        Assert.IsFalse(other.unlocked);
        Assert.AreEqual(1, other.level);
    }
}
