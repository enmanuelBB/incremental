using NUnit.Framework;
using UnityEditor;

/// <summary>
/// Protegen el balance entre Alucard y el mago con los números REALES de los assets: si alguien cambia un valor
/// y un personaje se descompensa, fallan. Los rangos salen del diseño (ver el spec de personajes).
/// Se comparan 6 puntos de progreso (0%, 20%... 100% de las mejoras).
/// </summary>
[TestFixture]
public class BalanceTests
{
    private WeaponDefinition pistola;
    private WeaponDefinition m16;
    private StaffDefinition staff;

    [SetUp]
    public void SetUp()
    {
        pistola = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Data/Weapons/Pistola.asset");
        m16 = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Data/Weapons/M16.asset");
        staff = AssetDatabase.LoadAssetAtPath<StaffDefinition>("Assets/Data/Weapons/Baston.asset");

        Assert.IsNotNull(pistola, "falta Assets/Data/Weapons/Pistola.asset");
        Assert.IsNotNull(m16, "falta Assets/Data/Weapons/M16.asset");
        Assert.IsNotNull(staff, "falta Assets/Data/Weapons/Baston.asset");
    }

    private static readonly float[] Progress = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };

    private static void Levels(WeaponDefinition w, float progress, out int fireRate, out int reload, out int damage)
    {
        int[] max = CombatMath.MaxLevels(w);
        fireRate = UnityEngine.Mathf.RoundToInt(max[0] * progress);
        reload = UnityEngine.Mathf.RoundToInt(max[1] * progress);
        damage = UnityEngine.Mathf.RoundToInt(max[2] * progress);
    }

    private float Pistol(float p) { Levels(pistola, p, out int f, out int r, out int d); return CombatMath.GunSustainedDps(pistola, f, r, d); }
    private float Rifle(float p) { Levels(m16, p, out int f, out int r, out int d); return CombatMath.GunSustainedDps(m16, f, r, d); }
    private float Mage(float p, int targets) { Levels(staff, p, out int f, out int r, out int d); return CombatMath.StaffSustainedDps(staff, f, r, d, targets); }

    [Test]
    public void Mage_AgainstOneEnemy_IsCloseToThePistolAtEveryProgress()
    {
        foreach (float p in Progress)
        {
            float ratio = Mage(p, 1) / Pistol(p);
            Assert.That(ratio, Is.InRange(0.85f, 1.05f), "progreso " + p + ": maga/pistola = " + ratio);
        }
    }

    [Test]
    public void Mage_InALineOfFour_IsCloseToTheRifleAtEveryProgress()
    {
        foreach (float p in Progress)
        {
            float ratio = Mage(p, CharacterInfo.LineTargets) / Rifle(p);
            Assert.That(ratio, Is.InRange(0.75f, 1.0f), "progreso " + p + ": maga(fila de 4)/M16 = " + ratio);
        }
    }

    [Test]
    public void Mage_InALine_BeatsThePistolClearly()
    {
        foreach (float p in Progress)
            Assert.That(Mage(p, CharacterInfo.LineTargets) / Pistol(p), Is.GreaterThan(1.3f), "progreso " + p);
    }

    [Test]
    public void Mage_AgainstOneEnemy_IsWeakerThanTheRifle_SoAlucardKeepsTheSingleTargetRole()
    {
        foreach (float p in Progress)
            Assert.That(Mage(p, 1) / Rifle(p), Is.LessThan(0.6f), "progreso " + p);
    }

    [Test]
    public void MageAbility_OneShotsANormalEnemyButNotATank_AtLevelZero()
    {
        var normal = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Enemies/Enemy_Normal.asset");
        var tank = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Data/Enemies/Enemy_Tank.asset");

        Assert.GreaterOrEqual(staff.AbilityDamageAt(0), normal.maxHealth);
        Assert.Less(staff.AbilityDamageAt(0), tank.maxHealth);
    }

    [Test]
    public void MageUpgrades_CostTheSameAsAGunsUpgrades()
    {
        foreach (UpgradeType type in new[] { UpgradeType.FireRate, UpgradeType.Reload, UpgradeType.Damage })
        {
            UpgradeStat gun = pistola.GetUpgrade(type);
            UpgradeStat mage = staff.GetUpgrade(type);

            Assert.AreEqual(gun.basePrice, mage.basePrice, type + " basePrice");
            Assert.AreEqual(gun.priceIncrease, mage.priceIncrease, type + " priceIncrease");
            Assert.AreEqual(gun.maxLevel, mage.maxLevel, type + " maxLevel");
        }
    }

    [Test]
    public void ManaMatters_AbilityCannotBeSpammedForever()
    {
        // Si la regeneración fuera tan alta que el maná nunca limita, el maná sería decoración.
        Assert.Greater(staff.abilityManaCost / staff.ManaRegenAt(0), staff.AbilityCooldownAt(0),
            "con el maná sin mejoras, esperar maná debe tardar más que el enfriamiento");
    }
}
