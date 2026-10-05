using NUnit.Framework;

public class BurnStateTests
{
    private static int Run(BurnState burn, float total, float step)
    {
        int ticks = 0;
        for (float t = 0f; t < total - 1e-4f; t += step) ticks += burn.Advance(step);
        return ticks;
    }

    [Test]
    public void NotBurningByDefault()
    {
        var burn = new BurnState();

        Assert.IsFalse(burn.IsBurning);
        Assert.AreEqual(0, burn.Advance(1f));
    }

    [Test]
    public void FourSeconds_EveryHalfSecond_GivesEightTicks_ThenStops()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.IsTrue(burn.IsBurning);
        Assert.AreEqual(8, Run(burn, 6f, 0.1f));
        Assert.IsFalse(burn.IsBurning);
    }

    [Test]
    public void OneBigStep_GivesTheSameTicks()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.AreEqual(8, burn.Advance(10f));
        Assert.IsFalse(burn.IsBurning);
    }

    [Test]
    public void NoTickBeforeTheFirstInterval()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.AreEqual(0, burn.Advance(0.4f));
        Assert.AreEqual(1, burn.Advance(0.1f));
    }

    [Test]
    public void Reapply_RenewsTheDuration()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);
        burn.Advance(3f);              // quedan 1 s
        burn.Apply(4f, 9, 0.5f);       // vuelve a 4 s

        Assert.AreEqual(8, Run(burn, 6f, 0.1f));
    }

    [Test]
    public void Reapply_KeepsTheHigherDamagePerTick()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);
        burn.Apply(4f, 18, 0.5f);
        Assert.AreEqual(18, burn.DamagePerTick);

        burn.Apply(4f, 5, 0.5f);
        Assert.AreEqual(18, burn.DamagePerTick, "el menor no reemplaza al mayor");
    }

    [Test]
    public void Reapply_DoesNotShortenALongerBurn()
    {
        var burn = new BurnState();
        burn.Apply(6f, 9, 0.5f);
        burn.Apply(2f, 9, 0.5f);

        Assert.AreEqual(12, Run(burn, 8f, 0.1f));
    }

    [Test]
    public void DamagePerTick_StillReadableRightAfterItEnds()
    {
        var burn = new BurnState();
        burn.Apply(1f, 7, 0.5f);

        int ticks = burn.Advance(5f);

        Assert.AreEqual(2, ticks);
        Assert.IsFalse(burn.IsBurning);
        Assert.AreEqual(7, burn.DamagePerTick);
    }

    [Test]
    public void ANewBurnAfterItEnded_UsesItsOwnDamage()
    {
        var burn = new BurnState();
        burn.Apply(1f, 18, 0.5f);
        burn.Advance(5f);
        burn.Apply(1f, 5, 0.5f);

        Assert.AreEqual(5, burn.DamagePerTick);
    }

    [TestCase(0f, 9, 0.5f)]
    [TestCase(-1f, 9, 0.5f)]
    [TestCase(4f, 0, 0.5f)]
    [TestCase(4f, 9, 0f)]
    public void InvalidApply_DoesNothing(float seconds, int damage, float tick)
    {
        var burn = new BurnState();
        burn.Apply(seconds, damage, tick);

        Assert.IsFalse(burn.IsBurning);
    }

    [Test]
    public void Clear_StopsTheBurn()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);
        burn.Clear();

        Assert.IsFalse(burn.IsBurning);
        Assert.AreEqual(0, burn.Advance(1f));
    }

    [Test]
    public void NonPositiveDeltaTime_DoesNothing()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.AreEqual(0, burn.Advance(0f));
        Assert.AreEqual(0, burn.Advance(-1f));
        Assert.IsTrue(burn.IsBurning);
    }
}
