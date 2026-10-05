using NUnit.Framework;

public class BerserkDrainTests
{
    private static int Sum(BerserkDrain drain, float total, float step, int max, float fraction)
    {
        int lost = 0;
        for (float t = 0f; t < total - 1e-4f; t += step) lost += drain.Advance(step, max, fraction);
        return lost;
    }

    [Test]
    public void TwoPercentOf150_IsThreePerSecond()
    {
        Assert.AreEqual(3, Sum(new BerserkDrain(), 1f, 0.1f, 150, 0.02f));
        Assert.AreEqual(6, Sum(new BerserkDrain(), 2f, 0.1f, 150, 0.02f));
    }

    [Test]
    public void SmallSteps_NeverLoseOrDuplicatePoints()
    {
        // 100 de vida y 1%: 1 punto por segundo, en pasos de 0,016 s (como a ~60 fps)
        Assert.AreEqual(10, Sum(new BerserkDrain(), 10f, 0.016f, 100, 0.01f), 1);
    }

    [Test]
    public void OneBigStep_GivesTheWholePoints()
    {
        var drain = new BerserkDrain();
        Assert.AreEqual(6, drain.Advance(1.5f, 200, 0.02f));   // 200 x 0,02 x 1,5 = 6
    }

    [Test]
    public void InvalidInputs_LoseNothing()
    {
        var drain = new BerserkDrain();
        Assert.AreEqual(0, drain.Advance(0f, 150, 0.02f));
        Assert.AreEqual(0, drain.Advance(-1f, 150, 0.02f));
        Assert.AreEqual(0, drain.Advance(1f, 0, 0.02f));
        Assert.AreEqual(0, drain.Advance(1f, 150, 0f));
    }

    [Test]
    public void Reset_ForgetsTheLeftover()
    {
        var drain = new BerserkDrain();
        drain.Advance(0.2f, 150, 0.02f);   // 0,6 de punto guardado
        drain.Reset();

        Assert.AreEqual(0, drain.Advance(0.2f, 150, 0.02f));
    }

    [TestCase(5, 9, 4)]
    [TestCase(150, 3, 3)]
    [TestCase(1, 5, 0)]
    [TestCase(0, 5, 0)]
    [TestCase(10, -2, 0)]
    public void Allowed_NeverLeavesLessThanOne(int current, int toLose, int expected)
    {
        Assert.AreEqual(expected, BerserkDrain.Allowed(current, toLose));
    }

    [TestCase(2, false)]
    [TestCase(1, true)]
    [TestCase(0, true)]
    public void ShouldStop_AtOneOrLess(int current, bool expected)
    {
        Assert.AreEqual(expected, BerserkDrain.ShouldStop(current));
    }
}
