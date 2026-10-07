using NUnit.Framework;
using UnityEngine;

public class DamageNumberStyleTests
{
    private static void AreClose(Color expected, Color actual) =>
        Assert.IsTrue(Vector4.Distance(expected, actual) < 0.01f, "esperado " + expected + " y salió " + actual);

    [Test]
    public void HitColor_StartsRed_AndEndsWhite()
    {
        AreClose(DamageNumberStyle.HitRed, DamageNumberStyle.HitColor(0f));
        AreClose(DamageNumberStyle.HitRed, DamageNumberStyle.HitColor(DamageNumberStyle.RedHold));   // se queda rojo un instante
        AreClose(Color.white, DamageNumberStyle.HitColor(DamageNumberStyle.WhiteAt));
        AreClose(Color.white, DamageNumberStyle.HitColor(DamageNumberStyle.Duration));
    }

    [Test]
    public void HitColor_IsInBetweenMidway()
    {
        Color mid = DamageNumberStyle.HitColor((DamageNumberStyle.RedHold + DamageNumberStyle.WhiteAt) * 0.5f);
        Assert.Greater(mid.g, DamageNumberStyle.HitRed.g + 0.2f);
        Assert.Less(mid.g, 0.9f);
    }

    [Test]
    public void Alpha_FullThenFadesToZero()
    {
        Assert.AreEqual(1f, DamageNumberStyle.Alpha(0f));
        Assert.AreEqual(1f, DamageNumberStyle.Alpha(DamageNumberStyle.FadeStart));
        Assert.AreEqual(0f, DamageNumberStyle.Alpha(DamageNumberStyle.Duration), 1e-4f);
        Assert.Less(DamageNumberStyle.Alpha((DamageNumberStyle.FadeStart + DamageNumberStyle.Duration) * 0.5f), 1f);
    }

    [Test]
    public void Scale_PopsBigThenSettles()
    {
        Assert.Less(DamageNumberStyle.Scale(0f), 1f);
        Assert.AreEqual(DamageNumberStyle.PopScale, DamageNumberStyle.Scale(DamageNumberStyle.PopPeak), 1e-4f);
        Assert.Greater(DamageNumberStyle.PopScale, 1.3f);
        Assert.AreEqual(1f, DamageNumberStyle.Scale(DamageNumberStyle.SettleAt), 1e-4f);
        Assert.AreEqual(1f, DamageNumberStyle.Scale(DamageNumberStyle.Duration), 1e-4f);
    }

    [Test]
    public void Rise_GoesUpFastThenSlows()
    {
        float early = DamageNumberStyle.Rise(0.1f) - DamageNumberStyle.Rise(0f);
        float late = DamageNumberStyle.Rise(0.8f) - DamageNumberStyle.Rise(0.7f);
        Assert.AreEqual(0f, DamageNumberStyle.Rise(0f));
        Assert.Greater(early, late);
        Assert.AreEqual(DamageNumberStyle.RiseHeight, DamageNumberStyle.Rise(DamageNumberStyle.Duration), 1e-4f);
    }

    [Test]
    public void SizeFor_BiggerHitsLookBigger_WithACap()
    {
        Assert.AreEqual(1f, DamageNumberStyle.SizeFor(1), 1e-4f);
        Assert.Greater(DamageNumberStyle.SizeFor(100), DamageNumberStyle.SizeFor(10));
        Assert.Greater(DamageNumberStyle.SizeFor(1000), DamageNumberStyle.SizeFor(100));
        Assert.AreEqual(DamageNumberStyle.MaxSize, DamageNumberStyle.SizeFor(1000000), 1e-4f);
        Assert.AreEqual(1f, DamageNumberStyle.SizeFor(0), 1e-4f);
    }
}
