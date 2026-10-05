using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TargetPickerTests
{
    [Test]
    public void PicksTheNearest()
    {
        var list = new List<Vector3> { new Vector3(10f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f) };
        Assert.AreEqual(1, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void OutOfRange_ReturnsMinusOne()
    {
        var list = new List<Vector3> { new Vector3(30f, 0f, 0f) };
        Assert.AreEqual(-1, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void TheExactRangeCounts()
    {
        var list = new List<Vector3> { new Vector3(25f, 0f, 0f) };
        Assert.AreEqual(0, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void EmptyOrNull_ReturnsMinusOne()
    {
        Assert.AreEqual(-1, TargetPicker.NearestIndex(Vector3.zero, new List<Vector3>(), 25f));
        Assert.AreEqual(-1, TargetPicker.NearestIndex(Vector3.zero, null, 25f));
    }

    [Test]
    public void ATie_PicksTheFirst()
    {
        var list = new List<Vector3> { new Vector3(4f, 0f, 0f), new Vector3(0f, 0f, 4f) };
        Assert.AreEqual(0, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void PlanShots_OneEnemy_AllShotsGoToIt()
    {
        var list = new List<Vector3> { new Vector3(4f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 0, 0, 0 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 3));
    }

    [Test]
    public void PlanShots_ThreeEnemies_AreHitNearestFirst()
    {
        var list = new List<Vector3> { new Vector3(9f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1, 2, 0 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 3));
    }

    [Test]
    public void PlanShots_FewerEnemiesThanShots_RepeatsTheNearest()
    {
        var list = new List<Vector3> { new Vector3(6f, 0f, 0f), new Vector3(3f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1, 0, 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 3));
    }

    [Test]
    public void PlanShots_MoreEnemiesThanShots_OnlyTheNearest()
    {
        var list = new List<Vector3> { new Vector3(9f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 1));
        CollectionAssert.AreEqual(new[] { 1, 2 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 2));
    }

    [Test]
    public void PlanShots_IgnoresTheOutOfRange()
    {
        var list = new List<Vector3> { new Vector3(40f, 0f, 0f), new Vector3(5f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1, 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 2));
    }

    [Test]
    public void PlanShots_NothingToHit_IsEmpty()
    {
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, new List<Vector3>(), 25f, 3).Count);
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, null, 25f, 3).Count);
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, new List<Vector3> { new Vector3(40f, 0f, 0f) }, 25f, 3).Count);
    }

    [TestCase(0)]
    [TestCase(-2)]
    public void PlanShots_NoShots_IsEmpty(int shots)
    {
        var list = new List<Vector3> { new Vector3(3f, 0f, 0f) };
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, list, 25f, shots).Count);
    }

    [Test]
    public void PlanShots_ATieKeepsTheListOrder()
    {
        var list = new List<Vector3> { new Vector3(4f, 0f, 0f), new Vector3(0f, 0f, 4f) };
        CollectionAssert.AreEqual(new[] { 0, 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 2));
    }

    [Test]
    public void HeightIsIgnored()
    {
        var list = new List<Vector3> { new Vector3(5f, 0f, 0f), new Vector3(4f, 50f, 0f) };
        Assert.AreEqual(1, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }
}
