using NUnit.Framework;

public class TimedEffectTests
{
    [Test]
    public void NotStarted_IsNotActive()
    {
        var effect = new TimedEffect();
        Assert.IsFalse(effect.IsActive(0f));
        Assert.IsFalse(effect.TryFinish(100f));
    }

    [Test]
    public void IsActiveUntilTheDurationPasses()
    {
        var effect = new TimedEffect();
        effect.Start(10f, 2f);

        Assert.IsTrue(effect.IsActive(10f));
        Assert.IsTrue(effect.IsActive(11.9f));
        Assert.IsFalse(effect.IsActive(12f));
    }

    [Test]
    public void Remaining_CountsDownAndStopsAtZero()
    {
        var effect = new TimedEffect();
        effect.Start(0f, 8f);

        Assert.AreEqual(8f, effect.Remaining(0f), 0.001f);
        Assert.AreEqual(3f, effect.Remaining(5f), 0.001f);
        Assert.AreEqual(0f, effect.Remaining(9f), 0.001f);
    }

    [Test]
    public void TryFinish_ReturnsTrueOnlyOnce()
    {
        var effect = new TimedEffect();
        effect.Start(0f, 2f);

        Assert.IsFalse(effect.TryFinish(1f), "aún dura");
        Assert.IsTrue(effect.TryFinish(2.1f));
        Assert.IsFalse(effect.TryFinish(3f), "ya se avisó");
    }

    [Test]
    public void Cancel_StopsItWithoutAFinishNotification()
    {
        var effect = new TimedEffect();
        effect.Start(0f, 5f);
        effect.Cancel();

        Assert.IsFalse(effect.IsActive(1f));
        Assert.IsFalse(effect.TryFinish(10f));
    }

    [Test]
    public void Restart_StartsANewRun()
    {
        var effect = new TimedEffect();
        effect.Start(0f, 1f);
        effect.TryFinish(2f);

        effect.Start(5f, 2f);
        Assert.IsTrue(effect.IsActive(6f));
    }
}
