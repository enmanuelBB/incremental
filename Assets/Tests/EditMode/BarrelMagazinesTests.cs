using NUnit.Framework;

[TestFixture]
public class BarrelMagazinesTests
{
    [Test]
    public void New_StartsFullInEveryBarrel()
    {
        var m = new BarrelMagazines(2, 12);

        Assert.AreEqual(2, m.Barrels);
        Assert.AreEqual(12, m.AmmoOf(0));
        Assert.AreEqual(12, m.AmmoOf(1));
        Assert.AreEqual(24, m.Total);
        Assert.IsTrue(m.IsFull);
        Assert.IsFalse(m.IsEmpty);
    }

    [Test]
    public void Shots_AlternateBetweenBarrels_OneBulletEach()
    {
        var m = new BarrelMagazines(2, 12);

        Assert.IsTrue(m.TryFire(out int first));
        Assert.AreEqual(0, first);
        Assert.AreEqual(11, m.AmmoOf(0));
        Assert.AreEqual(12, m.AmmoOf(1), "el segundo cargador no se toca en el primer disparo");

        Assert.IsTrue(m.TryFire(out int second));
        Assert.AreEqual(1, second);
        Assert.AreEqual(11, m.AmmoOf(0));
        Assert.AreEqual(11, m.AmmoOf(1));

        Assert.IsTrue(m.TryFire(out int third));
        Assert.AreEqual(0, third, "vuelve a empezar por el primero");
        Assert.AreEqual(10, m.AmmoOf(0));
    }

    [Test]
    public void EachShotSpendsExactlyOneBullet_SoTwentyFourShotsEmptyBothMagazines()
    {
        var m = new BarrelMagazines(2, 12);

        for (int shot = 1; shot <= 24; shot++)
        {
            Assert.IsTrue(m.TryFire(out _), "disparo " + shot);
            Assert.AreEqual(24 - shot, m.Total);
        }

        Assert.IsTrue(m.IsEmpty);
        Assert.IsFalse(m.TryFire(out int barrel));
        Assert.AreEqual(-1, barrel);
    }

    [Test]
    public void AfterAnEvenNumberOfShots_BothBarrelsHaveTheSameAmmo()
    {
        var m = new BarrelMagazines(2, 12);

        for (int i = 0; i < 10; i++) m.TryFire(out _);

        Assert.AreEqual(7, m.AmmoOf(0));
        Assert.AreEqual(7, m.AmmoOf(1));
    }

    [Test]
    public void WhenOneBarrelIsEmpty_TheOtherKeepsFiringAlone()
    {
        var m = new BarrelMagazines(2, 2);

        // Tres disparos (cañón 0, cañón 1, cañón 0) dejan el primer cargador vacío y una bala en el segundo.
        m.TryFire(out _); // cañón 0: le queda 1
        m.TryFire(out _); // cañón 1: le queda 1
        m.TryFire(out _); // cañón 0: le queda 0
        Assert.AreEqual(0, m.AmmoOf(0));
        Assert.AreEqual(1, m.AmmoOf(1));

        Assert.IsTrue(m.TryFire(out int barrel));
        Assert.AreEqual(1, barrel);
        Assert.IsTrue(m.IsEmpty);
    }

    [Test]
    public void ThreeBarrels_RotateInOrder()
    {
        var m = new BarrelMagazines(3, 2);
        var order = new System.Collections.Generic.List<int>();

        for (int i = 0; i < 6; i++)
        {
            Assert.IsTrue(m.TryFire(out int barrel));
            order.Add(barrel);
        }

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 1, 2 }, order);
        Assert.IsTrue(m.IsEmpty);
    }

    [Test]
    public void Refill_FillsEverythingAndRestartsTheTurn()
    {
        var m = new BarrelMagazines(2, 12);
        for (int i = 0; i < 7; i++) m.TryFire(out _);
        Assert.IsFalse(m.IsFull);
        Assert.AreEqual(1, m.NextBarrel, "7 disparos: el siguiente es el segundo cañón");

        m.Refill();

        Assert.IsTrue(m.IsFull);
        Assert.AreEqual(24, m.Total);
        Assert.AreEqual(0, m.NextBarrel);
    }

    [Test]
    public void IsFull_IsFalseIfAnyBarrelIsMissingABullet()
    {
        var m = new BarrelMagazines(2, 12);

        m.TryFire(out _);

        Assert.IsFalse(m.IsFull, "solo se gastó una bala, pero ya se puede recargar");
    }

    [Test]
    public void SingleBarrel_BehavesLikeAnOrdinaryMagazine()
    {
        var m = new BarrelMagazines(1, 12);

        for (int i = 0; i < 12; i++) Assert.IsTrue(m.TryFire(out int b) && b == 0);

        Assert.IsTrue(m.IsEmpty);
        Assert.IsFalse(m.TryFire(out _));
    }

    [Test]
    public void BarrelsBelowOne_AreTreatedAsOne()
    {
        Assert.AreEqual(1, new BarrelMagazines(0, 12).Barrels);
        Assert.AreEqual(1, new BarrelMagazines(-3, 12).Barrels);
    }

    [Test]
    public void Snapshot_IsACopy()
    {
        var m = new BarrelMagazines(2, 12);
        int[] copy = m.Snapshot();

        copy[0] = 0;

        Assert.AreEqual(12, m.AmmoOf(0));
    }
}
