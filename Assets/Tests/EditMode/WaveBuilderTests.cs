using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class WaveBuilderTests
{
    private EnemyDefinition normal;
    private EnemyDefinition tank;
    private WaveSet set;

    [SetUp]
    public void SetUp()
    {
        normal = ScriptableObject.CreateInstance<EnemyDefinition>();
        tank = ScriptableObject.CreateInstance<EnemyDefinition>();

        // Mismas oleadas que la escena original.
        set = ScriptableObject.CreateInstance<WaveSet>();
        set.healthMultiplierPerWave = 0.15f;
        set.extraEnemiesPerWave = 2;
        set.infiniteSpawnInterval = 1f;
        set.waves = new[]
        {
            Wave("Oleada 1", 2f, (normal, 10)),
            Wave("Oleada 2", 1.5f, (normal, 20)),
            Wave("Oleada 3", 1.5f, (normal, 15), (tank, 5)),
        };
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(normal);
        Object.DestroyImmediate(tank);
        Object.DestroyImmediate(set);
    }

    private static WaveSet.Wave Wave(string name, float interval, params (EnemyDefinition enemy, int count)[] groups)
    {
        var wave = new WaveSet.Wave { waveName = name, spawnInterval = interval };
        wave.enemyGroups = new WaveSet.EnemyGroup[groups.Length];
        for (int i = 0; i < groups.Length; i++)
            wave.enemyGroups[i] = new WaveSet.EnemyGroup { enemy = groups[i].enemy, count = groups[i].count };
        return wave;
    }

    [Test]
    public void Build_DefinedWave_UsesItsGroupsAndInterval()
    {
        WavePlan plan = WaveBuilder.Build(set, 2);

        Assert.AreEqual(2, plan.Groups.Count);
        Assert.AreSame(normal, plan.Groups[0].Enemy);
        Assert.AreEqual(15, plan.Groups[0].Count);
        Assert.AreSame(tank, plan.Groups[1].Enemy);
        Assert.AreEqual(5, plan.Groups[1].Count);
        Assert.AreEqual(1.5f, plan.SpawnInterval, 0.0001f);
        Assert.AreEqual(1f, plan.HealthScale, 0.0001f);
    }

    [Test]
    public void Build_FirstExtraWave_AddsEnemiesAndHealthToLastWave()
    {
        WavePlan plan = WaveBuilder.Build(set, 3);

        Assert.AreEqual(17, plan.Groups[0].Count);
        Assert.AreEqual(7, plan.Groups[1].Count);
        Assert.AreEqual(1f, plan.SpawnInterval, 0.0001f);
        Assert.AreEqual(1.15f, plan.HealthScale, 0.0001f);
    }

    [Test]
    public void Build_LaterExtraWave_ScalesLinearly()
    {
        WavePlan plan = WaveBuilder.Build(set, 5); // 3 por encima de la última definida

        Assert.AreEqual(21, plan.Groups[0].Count);
        Assert.AreEqual(11, plan.Groups[1].Count);
        Assert.AreEqual(1.45f, plan.HealthScale, 0.0001f);
    }

    [Test]
    public void Build_DoesNotModifyTheAsset()
    {
        WaveBuilder.Build(set, 5);

        Assert.AreEqual(15, set.waves[2].enemyGroups[0].count);
    }

    [Test]
    public void Build_NullOrEmptySet_ReturnsEmptyPlan()
    {
        Assert.AreEqual(0, WaveBuilder.Build(null, 0).Groups.Count);

        set.waves = new WaveSet.Wave[0];
        Assert.AreEqual(0, WaveBuilder.Build(set, 0).Groups.Count);
    }

    [Test]
    public void Merge_SameEnemy_AddsCounts()
    {
        var baseGroups = new List<WaveGroup> { new WaveGroup { Enemy = normal, Count = 10 } };
        var extra = new List<WaveGroup> { new WaveGroup { Enemy = normal, Count = 4 } };

        List<WaveGroup> merged = WaveBuilder.Merge(baseGroups, extra);

        Assert.AreEqual(1, merged.Count);
        Assert.AreEqual(14, merged[0].Count);
    }

    [Test]
    public void Merge_NewEnemy_IsAppended()
    {
        var baseGroups = new List<WaveGroup> { new WaveGroup { Enemy = normal, Count = 10 } };
        var extra = new List<WaveGroup> { new WaveGroup { Enemy = tank, Count = 3 } };

        List<WaveGroup> merged = WaveBuilder.Merge(baseGroups, extra);

        Assert.AreEqual(2, merged.Count);
        Assert.AreSame(tank, merged[1].Enemy);
    }

    [Test]
    public void Merge_DoesNotModifyInputs()
    {
        var baseGroups = new List<WaveGroup> { new WaveGroup { Enemy = normal, Count = 10 } };
        var extra = new List<WaveGroup> { new WaveGroup { Enemy = normal, Count = 4 } };

        WaveBuilder.Merge(baseGroups, extra);

        Assert.AreEqual(10, baseGroups[0].Count);
        Assert.AreEqual(4, extra[0].Count);
    }
}
