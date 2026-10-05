using NUnit.Framework;

public class FuryMeterTests
{
    [Test]
    public void StartsEmpty()
    {
        var meter = new FuryMeter(100f);

        Assert.AreEqual(0f, meter.Current, 1e-4f);
        Assert.AreEqual(0f, meter.Fraction, 1e-4f);
        Assert.IsFalse(meter.IsFull);
    }

    [Test]
    public void Add_RaisesTheBar()
    {
        var meter = new FuryMeter(100f);
        meter.Add(30f);

        Assert.AreEqual(30f, meter.Current, 1e-4f);
        Assert.AreEqual(0.3f, meter.Fraction, 1e-4f);
    }

    [Test]
    public void Add_NeverPassesTheMax()
    {
        var meter = new FuryMeter(100f);
        meter.Add(80f);
        meter.Add(80f);

        Assert.AreEqual(100f, meter.Current, 1e-4f);
        Assert.IsTrue(meter.IsFull);
    }

    [TestCase(0f)]
    [TestCase(-5f)]
    public void Add_IgnoresNonPositiveAmounts(float amount)
    {
        var meter = new FuryMeter(100f);
        meter.Add(40f);
        meter.Add(amount);

        Assert.AreEqual(40f, meter.Current, 1e-4f);
    }

    [Test]
    public void TryConsume_WhenFull_EmptiesTheBar()
    {
        var meter = new FuryMeter(100f);
        meter.Add(100f);

        Assert.IsTrue(meter.TryConsume());
        Assert.AreEqual(0f, meter.Current, 1e-4f);
        Assert.IsFalse(meter.IsFull);
    }

    [Test]
    public void TryConsume_WhenNotFull_DoesNothing()
    {
        var meter = new FuryMeter(100f);
        meter.Add(99f);

        Assert.IsFalse(meter.TryConsume());
        Assert.AreEqual(99f, meter.Current, 1e-4f);
    }

    [Test]
    public void Reset_EmptiesTheBar()
    {
        var meter = new FuryMeter(100f);
        meter.Add(60f);
        meter.Reset();

        Assert.AreEqual(0f, meter.Current, 1e-4f);
    }

    [Test]
    public void ZeroMax_IsNeverFull_AndHasNoFraction()
    {
        var meter = new FuryMeter(0f);
        meter.Add(10f);

        Assert.IsFalse(meter.IsFull);
        Assert.IsFalse(meter.TryConsume());
        Assert.AreEqual(0f, meter.Fraction, 1e-4f);
        Assert.AreEqual(0f, meter.Current, 1e-4f);
    }

    [TestCase(0, 0f)]
    [TestCase(-3, 0f)]
    [TestCase(1, 5f)]
    [TestCase(3, 15f)]
    [TestCase(4, 20f)]
    [TestCase(10, 20f)]
    public void GainForHits_IsFivePerEnemy_CappedAtTwentyPerSwing(int enemiesHit, float expected)
    {
        Assert.AreEqual(expected, FuryMeter.GainForHits(enemiesHit, 5f, 20f), 1e-4f);
    }
}
