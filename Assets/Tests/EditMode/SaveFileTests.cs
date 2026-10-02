using System.IO;
using NUnit.Framework;

[TestFixture]
public class SaveFileTests
{
    private string dir;
    private string path;

    [SetUp]
    public void SetUp()
    {
        dir = Path.Combine(Path.GetTempPath(), "savetests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        path = Path.Combine(dir, "save.json");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    [Test]
    public void Read_NoFile_IsMissing()
    {
        Assert.AreEqual(SaveFile.ReadStatus.Missing, SaveFile.Read(path).Status);
    }

    [Test]
    public void WriteThenRead_RoundTrips()
    {
        SaveFile.Write(path, new SaveData { money = 99 });

        SaveFile.ReadResult result = SaveFile.Read(path);

        Assert.AreEqual(SaveFile.ReadStatus.Loaded, result.Status);
        Assert.AreEqual(99, result.Data.money);
        Assert.IsFalse(File.Exists(path + ".tmp"));
    }

    [Test]
    public void Write_Twice_KeepsPreviousSaveAsBackup()
    {
        SaveFile.Write(path, new SaveData { money = 1 });
        SaveFile.Write(path, new SaveData { money = 2 });

        Assert.IsTrue(File.Exists(path + ".bak"));
        Assert.AreEqual(1, SaveMigrations.Parse(File.ReadAllText(path + ".bak"), out _).money);
    }

    [Test]
    public void Read_CorruptMain_RecoversFromBackup()
    {
        SaveFile.Write(path, new SaveData { money = 1 });
        SaveFile.Write(path, new SaveData { money = 2 });
        File.WriteAllText(path, "{ truncado");

        SaveFile.ReadResult result = SaveFile.Read(path);

        Assert.AreEqual(SaveFile.ReadStatus.Recovered, result.Status);
        Assert.AreEqual(1, result.Data.money);
    }

    [Test]
    public void Read_CorruptMainAndNoBackup_IsUnreadable()
    {
        File.WriteAllText(path, "{ truncado");

        Assert.AreEqual(SaveFile.ReadStatus.Unreadable, SaveFile.Read(path).Status);
    }

    [Test]
    public void Read_V1File_MigratesAndKeepsOnePreMigrationCopy()
    {
        string v1 = "{ \"money\": 10730, \"weapons\": [] }";
        File.WriteAllText(path, v1);

        SaveFile.ReadResult result = SaveFile.Read(path);

        Assert.IsTrue(result.Migrated);
        Assert.AreEqual(10730, result.Data.money);
        Assert.AreEqual(v1, File.ReadAllText(Path.Combine(dir, "save.v1.bak")));
    }

    [Test]
    public void Read_V1FileTwice_DoesNotOverwritePreMigrationCopy()
    {
        string copy = Path.Combine(dir, "save.v1.bak");
        File.WriteAllText(copy, "original");
        File.WriteAllText(path, "{ \"money\": 5, \"weapons\": [] }");

        SaveFile.Read(path);

        Assert.AreEqual("original", File.ReadAllText(copy));
    }

    [Test]
    public void Read_NewerVersion_ReportsNewerAndLeavesFileUntouched()
    {
        string newer = "{ \"version\": " + (SaveData.CurrentVersion + 5) + ", \"money\": 1 }";
        File.WriteAllText(path, newer);

        SaveFile.ReadResult result = SaveFile.Read(path);

        Assert.AreEqual(SaveFile.ReadStatus.Newer, result.Status);
        Assert.AreEqual(newer, File.ReadAllText(path));
    }

    [Test]
    public void Delete_RemovesSaveAndAutomaticBackups_ButNotPreMigrationCopy()
    {
        SaveFile.Write(path, new SaveData());
        SaveFile.Write(path, new SaveData());
        string preMigration = Path.Combine(dir, "save.v1.bak");
        File.WriteAllText(preMigration, "x");

        SaveFile.Delete(path);

        Assert.IsFalse(File.Exists(path));
        Assert.IsFalse(File.Exists(path + ".bak"));
        Assert.IsTrue(File.Exists(preMigration));
    }
}
