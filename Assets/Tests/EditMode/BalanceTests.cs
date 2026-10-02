using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// Protegen el balance entre Alucard y el mago con los números REALES de los assets: si alguien cambia un valor
/// y un personaje se descompensa, fallan. La referencia de Alucard es el arma que de verdad lleva
/// (sus dos pistolas). Se comparan 6 puntos de progreso (0%, 20%... 100% de las mejoras).
/// </summary>
[TestFixture]
public class BalanceTests
{
    private CharacterDefinition alucard;
    private WeaponDefinition dualPistols;
    private StaffDefinition staff;

    [SetUp]
    public void SetUp()
    {
        alucard = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Alucard.asset");
        staff = AssetDatabase.LoadAssetAtPath<StaffDefinition>("Assets/Data/Weapons/Baston.asset");

        Assert.IsNotNull(alucard, "falta Assets/Data/Characters/Alucard.asset");
        Assert.IsNotNull(staff, "falta Assets/Data/Weapons/Baston.asset");
        Assert.AreEqual(1, alucard.startingWeapons.Length, "Alucard lleva una sola arma (sus dos pistolas)");

        dualPistols = alucard.startingWeapons[0];
    }

    private static readonly float[] Progress = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };

    private static void Levels(WeaponDefinition w, float progress, out int fireRate, out int reload, out int damage)
    {
        int[] max = CombatMath.MaxLevels(w);
        fireRate = UnityEngine.Mathf.RoundToInt(max[0] * progress);
        reload = UnityEngine.Mathf.RoundToInt(max[1] * progress);
        damage = UnityEngine.Mathf.RoundToInt(max[2] * progress);
    }

    private float Alucard(float p) { Levels(dualPistols, p, out int f, out int r, out int d); return CombatMath.GunSustainedDps(dualPistols, f, r, d); }
    private float Mage(float p, int targets) { Levels(staff, p, out int f, out int r, out int d); return CombatMath.StaffSustainedDps(staff, f, r, d, targets); }

    [Test]
    public void Alucard_CarriesTwoPistolsWith24BulletsAndNoM16()
    {
        Assert.AreEqual("Pistola", dualPistols.Id);
        Assert.AreEqual(2, dualPistols.barrels, "dos pistolas que se turnan");
        Assert.AreEqual(12, dualPistols.magazineSize, "12 balas en cada una");
        Assert.AreEqual(24, dualPistols.barrels * dualPistols.magazineSize, "24 balas en total");
    }

    [Test]
    public void M16_StaysInTheProjectButNoCharacterUsesIt()
    {
        var m16 = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/Data/Weapons/M16.asset");
        Assert.IsNotNull(m16, "el M16 se conserva para un personaje futuro");

        var users = AssetDatabase.FindAssets("t:CharacterDefinition")
            .Select(g => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c.startingWeapons != null && c.startingWeapons.Contains(m16))
            .Select(c => c.displayName)
            .ToArray();

        Assert.IsEmpty(users, "ningún personaje debe usar el M16 por ahora: " + string.Join(", ", users));
    }

    [Test]
    public void AgainstOneEnemy_AlucardIsStronger_SoHeKeepsTheSingleTargetRole()
    {
        foreach (float p in Progress)
        {
            float ratio = Mage(p, 1) / Alucard(p);
            Assert.That(ratio, Is.InRange(0.60f, 0.90f), "progreso " + p + ": maga/Alucard contra 1 enemigo = " + ratio);
        }
    }

    [Test]
    public void InALineOfFour_TheMageIsStronger_SoSheKeepsTheHordeRole()
    {
        foreach (float p in Progress)
        {
            float ratio = Mage(p, CharacterInfo.LineTargets) / Alucard(p);
            Assert.That(ratio, Is.InRange(1.0f, 1.5f), "progreso " + p + ": maga(fila de 4)/Alucard = " + ratio);
        }
    }

    [Test]
    public void Mage_GainsClearlyFromLines()
    {
        foreach (float p in Progress)
            Assert.That(Mage(p, CharacterInfo.LineTargets) / Mage(p, 1), Is.GreaterThan(1.4f), "progreso " + p);
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
            UpgradeStat gun = dualPistols.GetUpgrade(type);
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
