using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class WavePackTests
{
    private EnemyDefinition normal;
    private EnemyDefinition flyer;
    private EnemyDefinition tank;

    [SetUp]
    public void SetUp()
    {
        normal = ScriptableObject.CreateInstance<EnemyDefinition>();
        flyer = ScriptableObject.CreateInstance<EnemyDefinition>();
        tank = ScriptableObject.CreateInstance<EnemyDefinition>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(normal);
        Object.DestroyImmediate(flyer);
        Object.DestroyImmediate(tank);
    }

    private List<WaveGroup> Groups(params (EnemyDefinition enemy, int count)[] groups)
    {
        var list = new List<WaveGroup>();
        foreach (var g in groups) list.Add(new WaveGroup { Enemy = g.enemy, Count = g.count });
        return list;
    }

    [Test]
    public void Interleave_MixesTheTypes()
    {
        List<EnemyDefinition> order = WaveBuilder.Interleave(Groups((normal, 3), (flyer, 2), (tank, 1)));

        CollectionAssert.AreEqual(new[] { normal, flyer, tank, normal, flyer, normal }, order);
    }

    [Test]
    public void Interleave_KeepsEveryEnemy_AndSkipsEmptyGroups()
    {
        List<EnemyDefinition> order = WaveBuilder.Interleave(Groups((normal, 0), (flyer, 4), (null, 2)));

        CollectionAssert.AreEqual(new[] { flyer, flyer, flyer, flyer }, order);
    }

    [Test]
    public void CountRemaining_GroupsWhatHasNotSpawnedYet()
    {
        var order = new List<EnemyDefinition> { normal, flyer, normal, flyer, normal };

        List<WaveGroup> remaining = WaveBuilder.CountRemaining(order, 2);

        Assert.AreEqual(2, remaining.Count);
        Assert.AreEqual(normal, remaining[0].Enemy);
        Assert.AreEqual(2, remaining[0].Count);
        Assert.AreEqual(flyer, remaining[1].Enemy);
        Assert.AreEqual(1, remaining[1].Count);
    }

    [Test]
    public void CountRemaining_WhenAllSpawned_IsEmpty()
    {
        var order = new List<EnemyDefinition> { normal, flyer };
        Assert.AreEqual(0, WaveBuilder.CountRemaining(order, 2).Count);
        Assert.AreEqual(0, WaveBuilder.CountRemaining(order, 7).Count);
    }

    // Devuelve siempre los mismos números "al azar", en orden.
    private static System.Func<float> Sequence(params float[] values)
    {
        int i = 0;
        return () => values[i++ % values.Length];
    }

    [Test]
    public void RowWidths_AreRandomBetweenMinAndMax()
    {
        CollectionAssert.AreEqual(new[] { 3, 5 }, PackFormation.RowWidths(2, 3, 5, 100, Sequence(0f, 0.999f)));
        CollectionAssert.AreEqual(new[] { 4, 4 }, PackFormation.RowWidths(2, 3, 5, 100, Sequence(0.5f)));
    }

    [Test]
    public void RowWidths_NeverPassWhatIsLeftInTheWave()
    {
        CollectionAssert.AreEqual(new[] { 5, 2 }, PackFormation.RowWidths(2, 3, 5, 7, Sequence(0.999f)));
        CollectionAssert.AreEqual(new[] { 2 }, PackFormation.RowWidths(2, 3, 5, 2, Sequence(0.5f)));
        CollectionAssert.IsEmpty(PackFormation.RowWidths(2, 3, 5, 0, Sequence(0.5f)));
    }

    [Test]
    public void RowWidths_InvalidLimits_StillGiveAtLeastOnePerRow()
    {
        CollectionAssert.AreEqual(new[] { 1, 1 }, PackFormation.RowWidths(2, 0, 0, 10, Sequence(0.5f)));
        CollectionAssert.AreEqual(new[] { 3 }, PackFormation.RowWidths(0, 3, 5, 10, Sequence(0f)));   // al menos una fila
    }

    // Formación: x = de lado (derecha positiva), y = hacia atrás (lejos de la base). Separación 2,5 m.
    [Test]
    public void Slot_RowOfFive_IsCenteredSideBySide()
    {
        int[] rows = { 5 };
        Assert.AreEqual(new Vector2(-5f, 0f), PackFormation.Slot(0, rows, 2.5f));
        Assert.AreEqual(new Vector2(0f, 0f), PackFormation.Slot(2, rows, 2.5f));
        Assert.AreEqual(new Vector2(5f, 0f), PackFormation.Slot(4, rows, 2.5f));
    }

    [Test]
    public void Slot_TwoRowsOfDifferentWidth_EachCentered()
    {
        int[] rows = { 5, 3 };
        Assert.AreEqual(new Vector2(-5f, -1.25f), PackFormation.Slot(0, rows, 2.5f));   // primera fila, adelante
        Assert.AreEqual(new Vector2(-2.5f, 1.25f), PackFormation.Slot(5, rows, 2.5f));  // segunda fila (3), atrás
        Assert.AreEqual(new Vector2(2.5f, 1.25f), PackFormation.Slot(7, rows, 2.5f));
    }
}
