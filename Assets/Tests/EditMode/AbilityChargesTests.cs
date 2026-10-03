using NUnit.Framework;

[TestFixture]
public class AbilityChargesTests
{
    private static AbilityCharges Make(int max, float recharge, float now = 0f)
    {
        var charges = new AbilityCharges();
        charges.Configure(max, recharge, now);
        return charges;
    }

    [Test]
    public void OneCharge_BehavesLikeTheOldCooldown()
    {
        AbilityCharges c = Make(1, 8f);

        Assert.IsTrue(c.TryUse(0f));
        Assert.IsFalse(c.TryUse(1f));
        Assert.IsFalse(c.TryUse(7.9f));
        Assert.IsTrue(c.TryUse(8f));
    }

    [Test]
    public void ThreeCharges_AllowThreeUsesInARowAndThenBlock()
    {
        AbilityCharges c = Make(3, 8f);

        Assert.IsTrue(c.TryUse(0f));
        Assert.IsTrue(c.TryUse(0.5f));
        Assert.IsTrue(c.TryUse(1f));
        Assert.IsFalse(c.TryUse(1.5f));
    }

    [Test]
    public void Recharge_RestoresOneChargeAtATime()
    {
        AbilityCharges c = Make(3, 8f);
        c.TryUse(0f); c.TryUse(0f); c.TryUse(0f);

        Assert.AreEqual(0, c.Available(7.9f));
        Assert.AreEqual(1, c.Available(8f));
        Assert.AreEqual(2, c.Available(16f));
        Assert.AreEqual(3, c.Available(24f));
    }

    [Test]
    public void Charges_NeverExceedTheMaximum()
    {
        AbilityCharges c = Make(2, 5f);

        Assert.AreEqual(2, c.Available(1000f));
        c.TryUse(1000f);
        Assert.AreEqual(1, c.Available(1000f));
        Assert.AreEqual(2, c.Available(1005f));
        Assert.AreEqual(2, c.Available(5000f));
    }

    [Test]
    public void RechargeRemaining_IsZeroWhenFull_AndCountsDownWhenNot()
    {
        AbilityCharges c = Make(2, 10f);
        Assert.AreEqual(0f, c.RechargeRemaining(0f), 0.0001f);

        c.TryUse(0f);
        Assert.AreEqual(10f, c.RechargeRemaining(0f), 0.0001f);
        Assert.AreEqual(4f, c.RechargeRemaining(6f), 0.0001f);
    }

    [Test]
    public void UsingWhileRecharging_DoesNotRestartTheTimer()
    {
        AbilityCharges c = Make(2, 10f);
        c.TryUse(0f);   // empieza a recargar: lista a los 10 s
        c.TryUse(4f);   // segundo uso: no reinicia el temporizador

        Assert.AreEqual(0, c.Available(9.9f));
        Assert.AreEqual(1, c.Available(10f));
    }

    [Test]
    public void Configure_WithLessThanOneCharge_CountsAsOne()
    {
        AbilityCharges c = Make(0, 5f);

        Assert.AreEqual(1, c.Max);
        Assert.IsTrue(c.TryUse(0f));
        Assert.IsFalse(c.TryUse(0f));
    }

    [Test]
    public void Configure_WithZeroRecharge_DoesNotLoopForever()
    {
        AbilityCharges c = Make(3, 0f);
        c.TryUse(0f); c.TryUse(0f); c.TryUse(0f);

        Assert.DoesNotThrow(() => c.Available(100f));
        Assert.LessOrEqual(c.Available(100f), 3);
    }

    [Test]
    public void Configure_StartsFull_AndReconfigureRefills()
    {
        AbilityCharges c = Make(2, 5f);
        c.TryUse(0f); c.TryUse(0f);

        c.Configure(3, 5f, 1f);

        Assert.AreEqual(3, c.Available(1f));
    }

    [Test]
    public void Reset_RefillsEverything()
    {
        AbilityCharges c = Make(2, 5f);
        c.TryUse(0f); c.TryUse(0f);

        c.Reset(1f);

        Assert.AreEqual(2, c.Available(1f));
    }
}
