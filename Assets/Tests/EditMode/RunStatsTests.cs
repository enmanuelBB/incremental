using NUnit.Framework;

[TestFixture]
public class RunStatsTests
{
    private static RunStats Started(int level = 3, float now = 10f)
    {
        var stats = new RunStats();
        stats.Begin(level, now);
        return stats;
    }

    [Test]
    public void Begin_SetsTheStartLevelAndClearsEverything()
    {
        RunStats stats = Started(3);
        stats.EnemyKilled();
        stats.MoneyEarned(50);
        stats.WaveStarted(4);

        stats.Begin(5, 100f);

        Assert.AreEqual(5, stats.StartLevel);
        Assert.AreEqual(0, stats.EnemiesKilled);
        Assert.AreEqual(0, stats.MoneyGained);
        Assert.AreEqual(0, stats.WaveReached);
    }

    [Test]
    public void WaveStarted_KeepsTheHighestWave()
    {
        RunStats stats = Started();

        stats.WaveStarted(1);
        stats.WaveStarted(5);
        stats.WaveStarted(3);

        Assert.AreEqual(5, stats.WaveReached);
    }

    [Test]
    public void Counters_Accumulate()
    {
        RunStats stats = Started();

        stats.EnemyKilled(); stats.EnemyKilled(); stats.EnemyKilled();
        stats.MoneyEarned(10); stats.MoneyEarned(25);
        stats.XpEarned(40); stats.XpEarned(5);
        stats.SkillPointsEarned(2); stats.SkillPointsEarned(3);

        Assert.AreEqual(3, stats.EnemiesKilled);
        Assert.AreEqual(35, stats.MoneyGained);
        Assert.AreEqual(45, stats.XpGained);
        Assert.AreEqual(5, stats.SkillPointsGained);
    }

    [Test]
    public void Counters_IgnoreZeroAndNegativeAmounts()
    {
        RunStats stats = Started();

        stats.MoneyEarned(-20); stats.MoneyEarned(0);
        stats.XpEarned(-1);
        stats.SkillPointsEarned(-3);

        Assert.AreEqual(0, stats.MoneyGained);
        Assert.AreEqual(0, stats.XpGained);
        Assert.AreEqual(0, stats.SkillPointsGained);
    }

    [Test]
    public void Finish_RecordsEndLevelPointsAndSurvivedTime()
    {
        RunStats stats = Started(level: 3, now: 10f);

        stats.Finish(level: 6, pointsAvailable: 4, now: 95.5f);

        Assert.AreEqual(6, stats.EndLevel);
        Assert.AreEqual(4, stats.PointsAvailable);
        Assert.AreEqual(85.5f, stats.SecondsSurvived, 0.0001f);
        Assert.AreEqual(3, stats.LevelsGained);
    }

    [Test]
    public void LevelsGained_NeverNegative_EvenIfTheLevelWasResetDuringTheRun()
    {
        RunStats stats = Started(level: 10);

        stats.Finish(level: 1, pointsAvailable: 1, now: 20f);

        Assert.AreEqual(0, stats.LevelsGained);
    }

    [Test]
    public void Finish_WithoutBegin_DoesNotProduceNegativeTime()
    {
        var stats = new RunStats();

        stats.Finish(level: 2, pointsAvailable: 0, now: 50f);

        Assert.GreaterOrEqual(stats.SecondsSurvived, 0f);
    }

    [Test]
    public void Finish_WithANowEarlierThanBegin_ClampsTimeToZero()
    {
        RunStats stats = Started(now: 100f);

        stats.Finish(level: 1, pointsAvailable: 0, now: 40f);

        Assert.AreEqual(0f, stats.SecondsSurvived);
    }

    // --- Formato ---

    [TestCase(0f, "0:00")]
    [TestCase(5f, "0:05")]
    [TestCase(59.9f, "0:59")]
    [TestCase(75f, "1:15")]
    [TestCase(600f, "10:00")]
    [TestCase(3725f, "62:05")]
    [TestCase(-8f, "0:00")]
    public void Duration_IsMinutesAndSeconds(float seconds, string expected)
    {
        Assert.AreEqual(expected, RunSummaryFormat.Duration(seconds));
    }

    [Test]
    public void LevelText_ShowsTheJumpWhenLevelsWereGained()
    {
        Assert.AreEqual("Alucard: sube del nivel 4 al 6", RunSummaryFormat.Level("Alucard", 4, 6));
    }

    [Test]
    public void LevelText_ShowsJustTheLevelWhenNothingWasGained()
    {
        Assert.AreEqual("Alucard: nivel 4", RunSummaryFormat.Level("Alucard", 4, 4));
        Assert.AreEqual("Alucard: nivel 4", RunSummaryFormat.Level("Alucard", 4, 2));
    }

    [TestCase(1, "1 punto")]
    [TestCase(0, "0 puntos")]
    [TestCase(7, "7 puntos")]
    public void Points_UsesSingularOnlyForOne(int amount, string expected)
    {
        Assert.AreEqual(expected, RunSummaryFormat.Points(amount));
    }
}
