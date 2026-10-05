using NUnit.Framework;
using UnityEngine;

public class DashRulesTests
{
    [Test]
    public void WithInput_UsesTheInput()
    {
        Vector3 d = DashRules.Direction(new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f));
        Assert.AreEqual(1f, d.x, 1e-4f);
        Assert.AreEqual(0f, d.z, 1e-4f);
    }

    [Test]
    public void WithoutInput_UsesTheAim()
    {
        Vector3 d = DashRules.Direction(Vector3.zero, new Vector3(0f, 0f, 3f));
        Assert.AreEqual(0f, d.x, 1e-4f);
        Assert.AreEqual(1f, d.z, 1e-4f);
    }

    [Test]
    public void ATinyInput_CountsAsNoInput()
    {
        Vector3 d = DashRules.Direction(new Vector3(0.05f, 0f, 0f), new Vector3(0f, 0f, 1f));
        Assert.AreEqual(1f, d.z, 1e-4f);
    }

    [Test]
    public void BothNull_GivesZero()
    {
        Assert.AreEqual(Vector3.zero, DashRules.Direction(Vector3.zero, Vector3.zero));
    }

    [Test]
    public void TheResultIsFlatAndNormalized()
    {
        Vector3 d = DashRules.Direction(Vector3.zero, new Vector3(0f, -0.7f, 0.7f));
        Assert.AreEqual(0f, d.y, 1e-4f);
        Assert.AreEqual(1f, d.magnitude, 1e-4f);
    }

    [Test]
    public void TheInputIsNormalizedToo()
    {
        Vector3 d = DashRules.Direction(new Vector3(3f, 0f, 4f), Vector3.forward);
        Assert.AreEqual(1f, d.magnitude, 1e-4f);
    }
}
