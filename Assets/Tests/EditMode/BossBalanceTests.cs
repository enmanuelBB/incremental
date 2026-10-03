using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Balance de los jefes contra una build MÁXIMA de Alucard (nivel 30: mejoras de dinero al tope, árbol completo,
/// rango 5 en la Q). Con los primeros jefes un personaje en el máximo debe ganar con holgura, y más adentro de la partida los jefes tienen que volver a pesar. Los números salen de los
/// assets reales; el modelo es conservador: 6 clics por segundo (la pistola es semiautomática) más la Q, sin contar
/// sangrado, niebla ni definitiva. Si alguien toca vidas, daños o el árbol y los jefes se descompensan, fallan.
/// </summary>
[TestFixture]
public class BossBalanceTests
{
    private const float ClicksPerSecond = 6f;

    private CharacterDefinition alucard;
    private WaveSet waves;
    private float dps;

    [SetUp]
    public void SetUp()
    {
        alucard = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Alucard.asset");
        waves = AssetDatabase.LoadAssetAtPath<WaveSet>("Assets/Data/Waves/Waves_Default.asset");
        Assert.IsNotNull(alucard.skillTree, "Alucard necesita su árbol");

        WeaponDefinition gun = alucard.startingWeapons[0];
        int[] max = CombatMath.MaxLevels(gun);

        TreeBonuses tree = SkillTreeRules.Compute(alucard.skillTree, alucard.skillTree.nodes.Select(n => n.id));

        int baseDamage = gun.DamageAt(max[2]);
        int bullet = Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage * tree.DamageMultiplier)); // igual que WeaponState.Damage

        float bulletsPerSecond = Mathf.Min(ClicksPerSecond, 1f / gun.FireRateAt(max[0]));
        float bullets = gun.barrels * gun.magazineSize;
        float gunDps = bullets * bullet / (bullets / bulletsPerSecond + gun.ReloadTimeAt(max[1]));

        AbilityDefinition heavy = alucard.abilities[0];
        float perCast = (1 + tree.HeavyShotExtraBullets) * heavy.DamageFor(bullet, Progression.MaxNormalRank);
        float cooldown = Mathf.Max(1f, heavy.CooldownAt(Progression.MaxNormalRank) - tree.HeavyShotCooldownReduction);

        dps = gunDps + perCast / cooldown;
    }

    private float BossHealth(int wave)
    {
        EnemyDefinition boss = WaveBuilder.BossFor(waves, wave);
        Assert.IsNotNull(boss, "no hay jefe en la oleada " + wave);
        return boss.maxHealth * WaveBuilder.Build(waves, wave - 1).HealthScale;
    }

    // Vida de toda la oleada: escolta normal + jefe.
    private float WaveHealth(int wave)
    {
        WavePlan plan = WaveBuilder.Build(waves, wave - 1);
        float total = BossHealth(wave);
        foreach (WaveGroup group in plan.Groups) total += group.Count * group.Enemy.maxHealth * plan.HealthScale;
        return total;
    }

    [TestCase(5, 4f)]
    [TestCase(10, 10f)]
    [TestCase(15, 15f)]
    [TestCase(20, 30f)]
    public void AtMaxBuild_EachBossDiesWithinItsTimeLimit(int wave, float maxSeconds)
    {
        float seconds = BossHealth(wave) / dps;

        Assert.LessOrEqual(seconds, maxSeconds, "oleada " + wave + ": el jefe tarda " + seconds.ToString("0.0") + " s con " + dps.ToString("0") + " de daño por segundo");
    }

    // Los primeros jefes pueden caer rápido con la build completa (esa es la idea: nivel máximo = fácil), pero más adentro de
    // la partida la vida de los jefes sigue creciendo (+15% por oleada) y tienen que volver a pesar.
    [TestCase(40, 8f)]
    [TestCase(60, 12f)]
    [TestCase(80, 15f)]
    public void AtMaxBuild_DeepBossesStillTakeAWhile(int wave, float minSeconds)
    {
        float seconds = BossHealth(wave) / dps;

        Assert.GreaterOrEqual(seconds, minSeconds, "oleada " + wave + ": el jefe cae demasiado rápido (" + seconds.ToString("0.0") + " s)");
    }

    [TestCase(5)]
    [TestCase(10)]
    [TestCase(15)]
    [TestCase(20)]
    public void AtMaxBuild_AWholeBossWaveIsClearedBeforeTheyReachTheBase(int wave)
    {
        // Los enemigos tardan ~55 s en llegar desde el fondo del mapa: dejamos margen.
        float seconds = WaveHealth(wave) / dps;

        Assert.LessOrEqual(seconds, 50f, "oleada " + wave + ": limpiarla tarda " + seconds.ToString("0") + " s");
    }
}
