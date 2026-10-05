using NUnit.Framework;
using UnityEngine;

public class MeleeConeTests
{
    // Guts en el origen mirando a +Z; alcance 3 m y arco de 120°.
    private static bool Hit(Vector3 point, float radius = 0f) =>
        MeleeCone.Contains(Vector3.zero, Vector3.forward, point, 3f, 120f, radius);

    [Test] public void InFront_WithinRange_IsHit() => Assert.IsTrue(Hit(new Vector3(0f, 0f, 2f)));
    [Test] public void InFront_BeyondRange_IsMissed() => Assert.IsFalse(Hit(new Vector3(0f, 0f, 3.5f)));
    [Test] public void BodyRadius_ExtendsTheRange() => Assert.IsTrue(Hit(new Vector3(0f, 0f, 3.5f), 0.6f));
    [Test] public void InsideTheArc_IsHit() => Assert.IsTrue(Hit(new Vector3(1.7f, 0f, 1f)));   // ~59,5°
    [Test] public void OutsideTheArc_IsMissed() => Assert.IsFalse(Hit(new Vector3(1.8f, 0f, 1f))); // ~61°
    [Test] public void Behind_IsMissed() => Assert.IsFalse(Hit(new Vector3(0f, 0f, -1f)));
    [Test] public void TouchingBehind_WithBodyRadius_IsHit() => Assert.IsTrue(Hit(new Vector3(0f, 0f, -0.3f), 0.5f));
    [Test] public void Height_IsIgnored() => Assert.IsTrue(Hit(new Vector3(0f, 5f, 2f)));

    [Test]
    public void TiltedForward_IsFlattened()
    {
        // Mirar hacia abajo no cambia el cono horizontal.
        Assert.IsTrue(MeleeCone.Contains(Vector3.zero, new Vector3(0f, -0.7f, 0.7f), new Vector3(0f, 0f, 2f), 3f, 120f));
    }

    [Test]
    public void ZeroForward_HitsNothing()
    {
        Assert.IsFalse(MeleeCone.Contains(Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 1f), 3f, 120f));
    }
}
