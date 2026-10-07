using NUnit.Framework;
using UnityEngine;

public class BeamAimTests
{
    private static void AreClose(Vector3 expected, Vector3 actual) =>
        Assert.IsTrue(Vector3.Distance(expected, actual) < 1e-4f, "esperado " + expected + " y salió " + actual);

    [Test]
    public void GoesStraightToTheCrosshairPoint_IncludingHeight()
    {
        // Bastón a 1,5 m; la mira marca un volador a 4 m de alto y 10 m adelante: el rayo sube hacia él.
        Vector3 origin = new Vector3(0f, 1.5f, 0f);
        Vector3 target = new Vector3(0f, 4f, 10f);

        AreClose((target - origin).normalized, BeamAim.Direction(origin, target, Vector3.forward));
    }

    [Test]
    public void AimingAtTheGround_GoesDown()
    {
        Vector3 direction = BeamAim.Direction(new Vector3(0f, 1.5f, 0f), new Vector3(0f, 0f, 5f), Vector3.forward);
        Assert.Less(direction.y, 0f);
    }

    [Test]
    public void TargetOnTopOfTheStaff_UsesTheCrosshairDirection()
    {
        Vector3 aim = new Vector3(0f, 0.3f, 1f);
        AreClose(aim.normalized, BeamAim.Direction(Vector3.one, Vector3.one + new Vector3(0f, 0f, 0.05f), aim));
    }

    [Test]
    public void TargetBehindTheStaff_UsesTheCrosshairDirection()
    {
        // En tercera persona la mira puede tocar algo entre la cámara y el bastón: no se dispara hacia atrás.
        AreClose(Vector3.forward, BeamAim.Direction(new Vector3(0f, 1.5f, 0f), new Vector3(0f, 1.5f, -2f), Vector3.forward));
    }
}
