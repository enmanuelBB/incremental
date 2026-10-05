using NUnit.Framework;

public class StunTests
{
    [Test] public void Roll_ZeroChance_NeverStuns() => Assert.IsFalse(StunRules.Roll(0f, 0f));
    [Test] public void Roll_FullChance_AlwaysStuns() => Assert.IsTrue(StunRules.Roll(1f, 0.9999f));
    [Test] public void Roll_BelowChance_Stuns() => Assert.IsTrue(StunRules.Roll(0.3f, 0.29f));
    [Test] public void Roll_AtOrAboveChance_DoesNotStun() => Assert.IsFalse(StunRules.Roll(0.3f, 0.3f));

    [Test]
    public void Timer_StunsForTheGivenSeconds()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 1.5f);

        Assert.IsTrue(timer.IsStunned(10f));
        Assert.IsTrue(timer.IsStunned(11.4f));
        Assert.IsFalse(timer.IsStunned(11.5f));
    }

    [Test]
    public void Timer_ANewShorterStun_DoesNotCutTheCurrentOne()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 2f);
        timer.Apply(10.5f, 0.5f);   // terminaría antes: no acorta

        Assert.IsTrue(timer.IsStunned(11.9f));
    }

    [Test]
    public void Timer_ANewLongerStun_Extends()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 1f);
        timer.Apply(10.5f, 2f);

        Assert.IsTrue(timer.IsStunned(12.4f));
        Assert.IsFalse(timer.IsStunned(12.5f));
    }

    [Test]
    public void Timer_NonPositiveSeconds_DoNothing()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 0f);
        timer.Apply(10f, -1f);

        Assert.IsFalse(timer.IsStunned(10f));
    }

    [Test]
    public void Timer_Clear_EndsTheStun()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 5f);
        timer.Clear();

        Assert.IsFalse(timer.IsStunned(10.1f));
    }
}
