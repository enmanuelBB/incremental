using NUnit.Framework;

[TestFixture]
public class HudFormatTests
{
    [TestCase(0, "0")]
    [TestCase(7, "7")]
    [TestCase(999, "999")]
    [TestCase(1000, "1.000")]
    [TestCase(9230, "9.230")]
    [TestCase(10730, "10.730")]
    [TestCase(123456, "123.456")]
    [TestCase(1234567, "1.234.567")]
    [TestCase(-1500, "-1.500")]
    public void Money_UsesDotsAsThousandsSeparator(int amount, string expected)
    {
        Assert.AreEqual(expected, HudFormat.Money(amount));
    }

    [Test]
    public void Money_HandlesTheSmallestInt()
    {
        Assert.AreEqual("-2.147.483.648", HudFormat.Money(int.MinValue));
    }

    [TestCase("pistola", "Pistola")]
    [TestCase("M16", "M16")]
    [TestCase("Bastón", "Bastón")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void DisplayName_CapitalizesTheFirstLetter(string name, string expected)
    {
        Assert.AreEqual(expected, HudFormat.DisplayName(name));
    }

    [Test]
    public void WaveLabel_SaysOleada()
    {
        Assert.AreEqual("Oleada 1", HudFormat.WaveLabel(1));
        Assert.AreEqual("Oleada 12", HudFormat.WaveLabel(12));
    }
}
