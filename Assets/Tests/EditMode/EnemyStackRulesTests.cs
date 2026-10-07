using NUnit.Framework;
using UnityEngine;

public class EnemyStackRulesTests
{
    private const float Overlap = 0.8f;
    private const float MaxLift = 8f;

    // Enemigo de 1,6 m (radio 0,8) en el suelo.
    private static StackBody Body(float x, float z, float lift = 0f, float priority = 0f, bool climbs = true, bool supports = true) =>
        new StackBody { Position = new Vector2(x, z), Radius = 0.8f, Height = 1.6f, Lift = lift, Priority = priority, CanClimb = climbs, CanSupport = supports };

    private static float[] Lifts(params StackBody[] bodies)
    {
        var result = new float[bodies.Length];
        EnemyStackRules.TargetLifts(bodies, Overlap, MaxLift, result);
        return result;
    }

    [Test]
    public void FarApart_BothStayOnTheGround()
    {
        float[] lifts = Lifts(Body(0f, 0f), Body(5f, 0f));
        Assert.AreEqual(0f, lifts[0]);
        Assert.AreEqual(0f, lifts[1]);
    }

    [Test]
    public void Overlapping_TheOneFartherFromItsTargetClimbsOnTop()
    {
        // Prioridad = distancia a su objetivo: el más cercano queda abajo.
        float[] lifts = Lifts(Body(0f, 0f, priority: 5f), Body(0.5f, 0f, priority: 2f));
        Assert.AreEqual(1.6f, lifts[0], 1e-4f);
        Assert.AreEqual(0f, lifts[1]);
    }

    [Test]
    public void TouchingOnlyAtTheEdge_DoesNotClimb()
    {
        // 0,8 x (0,8 + 0,8) = 1,28 m: a 1,4 m solo se rozan.
        float[] lifts = Lifts(Body(0f, 0f, priority: 5f), Body(1.4f, 0f, priority: 2f));
        Assert.AreEqual(0f, lifts[0]);
    }

    [Test]
    public void ThreeOnTheSameSpot_MakeATower()
    {
        float[] lifts = Lifts(Body(0f, 0f, priority: 1f), Body(0.2f, 0f, priority: 2f), Body(0.1f, 0.1f, priority: 3f));
        Assert.AreEqual(0f, lifts[0]);
        Assert.AreEqual(1.6f, lifts[1], 1e-4f);
        Assert.AreEqual(3.2f, lifts[2], 1e-4f);
    }

    [Test]
    public void OneAlreadyOnTop_StaysOnTopEvenIfCloserToItsTarget()
    {
        // La altura actual manda sobre la prioridad: así no se intercambian en cada fotograma.
        float[] lifts = Lifts(Body(0f, 0f, lift: 1.6f, priority: 1f), Body(0.3f, 0f, priority: 9f));
        Assert.AreEqual(1.6f, lifts[0], 1e-4f);
        Assert.AreEqual(0f, lifts[1]);
    }

    [Test]
    public void SupportLeaves_TheOneOnTopFalls()
    {
        float[] lifts = Lifts(Body(0f, 0f, lift: 1.6f), Body(4f, 0f));
        Assert.AreEqual(0f, lifts[0]);
    }

    [Test]
    public void NonClimber_StaysDownButCanBeClimbed()
    {
        // Un jefe no trepa, pero otro puede subirse a él.
        var boss = new StackBody { Position = Vector2.zero, Radius = 1.5f, Height = 3f, Priority = 9f, CanClimb = false, CanSupport = true };
        float[] lifts = Lifts(boss, Body(0.5f, 0f, priority: 10f));
        Assert.AreEqual(0f, lifts[0]);
        Assert.AreEqual(3f, lifts[1], 1e-4f);
    }

    [Test]
    public void NonSupporter_IsIgnored()
    {
        // Un volador ni trepa ni sirve de apoyo.
        float[] lifts = Lifts(Body(0f, 0f, priority: 1f, climbs: false, supports: false), Body(0.2f, 0f, priority: 2f));
        Assert.AreEqual(0f, lifts[1]);
    }

    [Test]
    public void Tower_IsCappedAtMaxLift()
    {
        var bodies = new StackBody[8];
        for (int i = 0; i < bodies.Length; i++) bodies[i] = Body(0f, 0f, priority: i);

        var result = new float[bodies.Length];
        EnemyStackRules.TargetLifts(bodies, Overlap, 7f, result);

        // Nadie apoya los pies por encima del tope (7 m): el más alto queda en 6,4 (sobre cuatro pisos de 1,6).
        foreach (float lift in result) Assert.LessOrEqual(lift, 7f);
        Assert.AreEqual(6.4f, Mathf.Max(result), 1e-4f);
    }

    [Test]
    public void FullColumn_TheNextOneDoesNotGoAboveTheCap_AndKnowsWhoBlocksIt()
    {
        // Tope 3 m: el segundo apoya los pies a 1,6; el tercero tendría que apoyarlos a 3,2 y no puede.
        var bodies = new[] { Body(0f, 0f, priority: 1f), Body(0.1f, 0f, priority: 2f), Body(0.2f, 0f, priority: 3f) };
        var lifts = new float[3];
        var blockedBy = new int[3];

        EnemyStackRules.TargetLifts(bodies, bodies.Length, Overlap, 3f, lifts, blockedBy);

        Assert.AreEqual(1.6f, lifts[1], 1e-4f);
        Assert.AreEqual(1.6f, lifts[2], 1e-4f);   // se queda a la altura que sí cabe
        Assert.AreEqual(-1, blockedBy[0]);
        Assert.AreEqual(-1, blockedBy[1]);
        Assert.AreEqual(1, blockedBy[2]);          // lo bloquea el de arriba de la columna: se lo empuja lejos de él
    }

    [Test]
    public void Step_ClimbsAndFallsAtTheirSpeeds()
    {
        Assert.AreEqual(0.6f, EnemyStackRules.Step(0f, 1.6f, 0.1f, 6f, 10f), 1e-4f);
        Assert.AreEqual(0.6f, EnemyStackRules.Step(1.6f, 0f, 0.1f, 6f, 10f), 1e-4f);
        Assert.AreEqual(1.6f, EnemyStackRules.Step(1.5f, 1.6f, 0.1f, 6f, 10f), 1e-4f); // no se pasa
    }
}
