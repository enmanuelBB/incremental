using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class SpawnAreaTests
{
    [Test]
    public void PointIn_MiddleOfTheUnitSquare_IsTheCenter()
    {
        Vector2 p = SpawnArea.PointIn(new Vector2(10f, 90f), new Vector2(52f, 12f), 0.5f, 0.5f);

        Assert.AreEqual(10f, p.x, 0.0001f);
        Assert.AreEqual(90f, p.y, 0.0001f);
    }

    [Test]
    public void PointIn_Corners_AreTheEdgesOfTheRectangle()
    {
        Vector2 center = new Vector2(0f, 94f);
        Vector2 size = new Vector2(52f, 12f);

        Vector2 low = SpawnArea.PointIn(center, size, 0f, 0f);
        Vector2 high = SpawnArea.PointIn(center, size, 1f, 1f);

        Assert.AreEqual(-26f, low.x, 0.0001f);
        Assert.AreEqual(88f, low.y, 0.0001f);
        Assert.AreEqual(26f, high.x, 0.0001f);
        Assert.AreEqual(100f, high.y, 0.0001f);
    }

    [Test]
    public void PointIn_RandomValuesOutsideZeroToOne_StayInsideTheRectangle()
    {
        Vector2 p = SpawnArea.PointIn(Vector2.zero, new Vector2(10f, 4f), 7f, -3f);

        Assert.AreEqual(5f, p.x, 0.0001f);
        Assert.AreEqual(-2f, p.y, 0.0001f);
    }

    [Test]
    public void PointIn_NegativeSize_IsTreatedAsItsAbsoluteValue()
    {
        Vector2 p = SpawnArea.PointIn(Vector2.zero, new Vector2(-10f, -4f), 1f, 1f);

        Assert.AreEqual(5f, p.x, 0.0001f);
        Assert.AreEqual(2f, p.y, 0.0001f);
    }

    [Test]
    public void PointIn_ZeroSize_AlwaysReturnsTheCenter()
    {
        Vector2 p = SpawnArea.PointIn(new Vector2(3f, 4f), Vector2.zero, 0.2f, 0.9f);

        Assert.AreEqual(new Vector2(3f, 4f), p);
    }

    [Test]
    public void PointIn_EveryPointOfAGrid_FallsInsideTheRectangle()
    {
        Vector2 center = new Vector2(-4f, 60f);
        Vector2 size = new Vector2(52f, 12f);

        for (int i = 0; i <= 20; i++)
        {
            for (int j = 0; j <= 20; j++)
            {
                Vector2 p = SpawnArea.PointIn(center, size, i / 20f, j / 20f);

                Assert.GreaterOrEqual(p.x, center.x - 26f - 0.0001f);
                Assert.LessOrEqual(p.x, center.x + 26f + 0.0001f);
                Assert.GreaterOrEqual(p.y, center.y - 6f - 0.0001f);
                Assert.LessOrEqual(p.y, center.y + 6f + 0.0001f);
            }
        }
    }

    [Test]
    public void PointIn_SpreadsAcrossTheWholeWidth()
    {
        // 10 puntos con u distinto reparten de un lado al otro, no se amontonan en el centro.
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < 10; i++)
        {
            float x = SpawnArea.PointIn(Vector2.zero, new Vector2(52f, 12f), i / 9f, 0.5f).x;
            min = Mathf.Min(min, x);
            max = Mathf.Max(max, x);
        }

        Assert.AreEqual(52f, max - min, 0.001f);
    }
}
