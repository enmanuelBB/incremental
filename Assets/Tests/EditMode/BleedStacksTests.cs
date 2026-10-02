using NUnit.Framework;

public class BleedStacksTests
{
    [Test]
    public void NewStacks_DoNotBleed()
    {
        var bleed = new BleedStacks();
        Assert.IsFalse(bleed.IsBleeding);
        Assert.AreEqual(0, bleed.Advance(5f));
        Assert.AreEqual(0f, bleed.Intensity);
    }

    [Test]
    public void Add_StopsAtTheCap()
    {
        var bleed = new BleedStacks();
        Assert.AreEqual(3, bleed.Add(3, 5));
        Assert.AreEqual(2, bleed.Add(10, 5), "solo caben 2 más");
        Assert.AreEqual(5, bleed.Stacks);
        Assert.AreEqual(0, bleed.Add(1, 5), "al tope no suma");
    }

    [Test]
    public void Add_WithZeroOrNegative_DoesNothing()
    {
        var bleed = new BleedStacks();
        Assert.AreEqual(0, bleed.Add(0, 5));
        Assert.AreEqual(0, bleed.Add(-2, 5));
        Assert.IsFalse(bleed.IsBleeding);
    }

    [Test]
    public void RaisingTheCap_LetsMoreStacksIn()
    {
        var bleed = new BleedStacks();
        bleed.Add(10, 5);
        Assert.AreEqual(5, bleed.Stacks);

        bleed.Add(10, 9);
        Assert.AreEqual(9, bleed.Stacks);
    }

    [Test]
    public void FirstTick_ArrivesOneSecondAfterTheFirstStack()
    {
        var bleed = new BleedStacks();
        bleed.Add(1, 5);

        Assert.AreEqual(0, bleed.Advance(0.9f));
        Assert.AreEqual(1, bleed.Advance(0.2f));
    }

    [Test]
    public void Advance_KeepsTheLeftoverBetweenTicks()
    {
        var bleed = new BleedStacks();
        bleed.Add(1, 5);

        int ticks = 0;
        for (int i = 0; i < 30; i++) ticks += bleed.Advance(0.1f);

        Assert.AreEqual(3, ticks, "3 s de juego = 3 ticks");
    }

    [Test]
    public void AddingMoreStacks_DoesNotResetTheClock()
    {
        var bleed = new BleedStacks();
        bleed.Add(1, 5);
        bleed.Advance(0.8f);

        bleed.Add(1, 5);

        Assert.AreEqual(1, bleed.Advance(0.2f));
    }

    [Test]
    public void LongPause_GivesSeveralTicks()
    {
        var bleed = new BleedStacks();
        bleed.Add(2, 5);
        Assert.AreEqual(3, bleed.Advance(3.5f));
    }

    [Test]
    public void TickDamage_IsStacksTimesDamagePerStack()
    {
        var bleed = new BleedStacks();
        bleed.Add(4, 5);
        Assert.AreEqual(8, bleed.TickDamage(2));
        Assert.AreEqual(4, bleed.TickDamage(1));
    }

    [Test]
    public void Intensity_GrowsWithStacksUntilTheCap()
    {
        var bleed = new BleedStacks();
        bleed.Add(1, 4);
        Assert.AreEqual(0.25f, bleed.Intensity, 0.001f);
        bleed.Add(3, 4);
        Assert.AreEqual(1f, bleed.Intensity, 0.001f);
    }

    [Test]
    public void Clear_RemovesEverythingAndRestoresTheBaseCap()
    {
        var bleed = new BleedStacks();
        bleed.Add(5, 12);
        bleed.Advance(0.5f);

        bleed.Clear();

        Assert.IsFalse(bleed.IsBleeding);
        Assert.AreEqual(BleedStacks.BaseCap, bleed.Cap);
        bleed.Add(1, 5);
        Assert.AreEqual(0, bleed.Advance(0.9f), "el reloj también se reinició");
    }

    [TestCase(10, 1)]
    [TestCase(4, 1)]
    [TestCase(1, 1)]
    [TestCase(14, 1)]
    [TestCase(15, 2)]
    [TestCase(20, 2)]
    [TestCase(100, 10)]
    public void DamagePerStack_IsTenPercentOfTheBulletWithAMinimumOfOne(int bullet, int expected)
    {
        Assert.AreEqual(expected, BleedStacks.DamagePerStack(bullet));
    }

    [TestCase(1, 5)]
    [TestCase(2, 6)]
    [TestCase(15, 19)]
    [TestCase(0, 5)]
    public void CapForLevel_StartsAtFiveAndGrowsOnePerLevel(int level, int expected)
    {
        Assert.AreEqual(expected, BleedStacks.CapForLevel(level));
    }
}
