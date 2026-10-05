using NUnit.Framework;
using UnityEngine;

/// <summary>Lógica pura del kit de Frieren: carga del Zoltraak, fórmulas del bastón, campo de flores, levitar y cargas.</summary>
[TestFixture]
public class FrierenKitTests
{
    // --- Carga del Zoltraak ---

    [Test]
    public void Charge_StartsEmpty_AndFillsOverTheChargeTime()
    {
        var charge = new ZoltraakCharge();
        charge.Begin(1.2f);
        Assert.IsTrue(charge.IsCharging);
        Assert.AreEqual(0f, charge.Fraction, 1e-4f);

        charge.Tick(0.6f);
        Assert.AreEqual(0.5f, charge.Fraction, 1e-4f);

        charge.Tick(5f);
        Assert.AreEqual(1f, charge.Fraction, 1e-4f, "no pasa de 1");
    }

    [Test]
    public void Charge_Release_ReturnsTheFraction_AndResets()
    {
        var charge = new ZoltraakCharge();
        charge.Begin(1f);
        charge.Tick(0.25f);

        Assert.AreEqual(0.25f, charge.Release(), 1e-4f);
        Assert.IsFalse(charge.IsCharging);
        Assert.AreEqual(0f, charge.Release(), 1e-4f, "soltar sin cargar no da nada");
    }

    [Test]
    public void Charge_Cancel_StopsWithoutFiring()
    {
        var charge = new ZoltraakCharge();
        charge.Begin(1f);
        charge.Tick(0.5f);
        charge.Cancel();
        Assert.IsFalse(charge.IsCharging);
        Assert.AreEqual(0f, charge.Fraction, 1e-4f);
    }

    [Test]
    public void Charge_TickWhenNotCharging_DoesNothing()
    {
        var charge = new ZoltraakCharge();
        charge.Tick(3f);
        Assert.AreEqual(0f, charge.Fraction, 1e-4f);
    }

    // --- Fórmulas del bastón ---

    private static StaffDefinition NewStaff()
    {
        var staff = ScriptableObject.CreateInstance<StaffDefinition>();
        staff.zoltraakDamage = 45;
        staff.zoltraakMinDamageFraction = 0.4f;
        staff.zoltraakMinRadius = 2.5f;
        staff.zoltraakMaxRadius = 4f;
        staff.damageUpgrade.step = 0.3f;
        return staff;
    }

    [Test]
    public void Staff_ZoltraakDamage_GoesFromFortyPercentToFull()
    {
        StaffDefinition staff = NewStaff();
        Assert.AreEqual(18, staff.ZoltraakDamageAt(0, 0f));
        Assert.AreEqual(45, staff.ZoltraakDamageAt(0, 1f));
        Assert.AreEqual(Mathf.RoundToInt(45 * 0.7f), staff.ZoltraakDamageAt(0, 0.5f));
    }

    [Test]
    public void Staff_ZoltraakDamage_ScalesWithPower()
    {
        StaffDefinition staff = NewStaff();
        Assert.AreEqual(Mathf.RoundToInt(45 * 1.6f), staff.ZoltraakDamageAt(2, 1f), "Poder 2: +30% por nivel");
    }

    [Test]
    public void Staff_ZoltraakRadius_GrowsWithCharge()
    {
        StaffDefinition staff = NewStaff();
        Assert.AreEqual(2.5f, staff.ZoltraakRadiusAt(0f), 1e-4f);
        Assert.AreEqual(4f, staff.ZoltraakRadiusAt(1f), 1e-4f);
        Assert.AreEqual(3.25f, staff.ZoltraakRadiusAt(0.5f), 1e-4f);
        Assert.AreEqual(4f, staff.ZoltraakRadiusAt(9f), 1e-4f, "la carga se limita a 1");
    }

    // --- Campo de flores ---

    [Test]
    public void Field_ClampPoint_KeepsAPointInsideRange()
    {
        Vector3 origin = new Vector3(1f, 0f, 1f);
        Vector3 point = new Vector3(6f, 3f, 1f);
        Assert.AreEqual(point, FlowerFieldRules.ClampPoint(origin, point, 25f));
    }

    [Test]
    public void Field_ClampPoint_PullsAFarPointToTheRange_KeepingHeightAndDirection()
    {
        Vector3 origin = Vector3.zero;
        Vector3 clamped = FlowerFieldRules.ClampPoint(origin, new Vector3(100f, 2f, 0f), 25f);
        Assert.AreEqual(25f, clamped.x, 1e-3f);
        Assert.AreEqual(0f, clamped.z, 1e-3f);
        Assert.AreEqual(2f, clamped.y, 1e-3f);
    }

    [Test]
    public void Field_Contains_IgnoresHeight_AndCountsTheMargin()
    {
        Vector3 center = Vector3.zero;
        Assert.IsTrue(FlowerFieldRules.Contains(center, 5f, new Vector3(3f, 40f, 4f)), "exactamente 5 m en horizontal");
        Assert.IsFalse(FlowerFieldRules.Contains(center, 5f, new Vector3(6f, 0f, 0f)));
        Assert.IsTrue(FlowerFieldRules.Contains(center, 5f, new Vector3(6f, 0f, 0f), 1.5f), "con el radio del enemigo cuenta");
    }

    [Test]
    public void Heal_AccumulatesTheRemainder_SoSmallTicksStillAddUp()
    {
        var heal = new HealOverTime();
        int total = 0;
        for (int i = 0; i < 4; i++) total += heal.Add(100, 0.03f, 0.25f);   // 1 s a 3% de 100 = 3 de vida
        Assert.AreEqual(3, total);
    }

    [Test]
    public void Heal_Reset_DropsTheRemainder()
    {
        var heal = new HealOverTime();
        Assert.AreEqual(0, heal.Add(100, 0.03f, 0.25f));
        heal.Reset();
        Assert.AreEqual(0, heal.Add(100, 0.03f, 0.25f));
    }

    // --- Levitar ---

    [Test]
    public void Hover_WithNoHeight_IsZeroEvenWithBob()
    {
        Assert.AreEqual(0f, HoverMath.Offset(1.3f, 0f, 0.05f, 2.5f), 1e-6f);
    }

    [Test]
    public void Hover_Oscillates_AroundTheHeight()
    {
        Assert.AreEqual(0.45f, HoverMath.Offset(0f, 0.45f, 0.05f, 2.5f), 1e-4f);
        Assert.AreEqual(0.5f, HoverMath.Offset(0.625f, 0.45f, 0.05f, 2.5f), 1e-4f, "a un cuarto del periodo: cresta");
        Assert.AreEqual(0.4f, HoverMath.Offset(1.875f, 0.45f, 0.05f, 2.5f), 1e-4f, "a tres cuartos: valle");
    }

    // --- Fase de los clips de movimiento (fotograma 0 quieta, 40 máximo, 79 quieta otra vez) ---

    [Test]
    public void MovePhase_WhileMoving_RisesToThePeak_AndStaysThere()
    {
        float phase = MovePhase.Step(0f, true, 0.2f);
        Assert.AreEqual(0.2f, phase, 1e-6f);

        for (int i = 0; i < 20; i++) phase = MovePhase.Step(phase, true, 0.2f);
        Assert.AreEqual(MovePhase.Peak, phase, 1e-6f, "mientras se mueve se queda en el fotograma 40");
    }

    [Test]
    public void MovePhase_WhenStopping_GoesOnPastThePeak_UntilItIsStillAgain()
    {
        float phase = MovePhase.Step(MovePhase.Peak, false, 0.2f);
        Assert.AreEqual(MovePhase.Peak + 0.2f, phase, 1e-6f, "al soltar sigue hacia el fotograma 79");

        for (int i = 0; i < 20; i++) phase = MovePhase.Step(phase, false, 0.2f);
        Assert.AreEqual(0f, phase, "termina quieta (fotograma 0 = fotograma 79)");
    }

    [Test]
    public void MovePhase_StoppingBeforeThePeak_ContinuesFromTheSameIntensityOnTheWayBack()
    {
        // A mitad de subida (mitad de intensidad) salta al punto de bajada con la misma intensidad: no pasa por el máximo.
        float halfUp = MovePhase.Peak * 0.5f;
        float halfDown = MovePhase.Peak + (1f - MovePhase.Peak) * 0.5f;
        Assert.AreEqual(halfDown, MovePhase.Step(halfUp, false, 0f), 1e-6f);
    }

    [Test]
    public void MovePhase_MovingAgainWhileReturning_WalksBackToThePeak()
    {
        float phase = MovePhase.Step(0.8f, true, 0.1f);
        Assert.AreEqual(0.7f, phase, 1e-6f, "desanda la vuelta en vez de saltar");

        for (int i = 0; i < 10; i++) phase = MovePhase.Step(phase, true, 0.1f);
        Assert.AreEqual(MovePhase.Peak, phase, 1e-6f);
    }

    [Test]
    public void MovePhase_Still_StaysStill()
    {
        Assert.AreEqual(0f, MovePhase.Step(0f, false, 0.3f));
    }

    // --- Cargas más rápidas (la definitiva acelera Q y E) ---

    [Test]
    public void Charges_SpeedUp_BringsTheRechargeCloser()
    {
        var charges = new AbilityCharges();
        charges.Configure(1, 10f, 0f);
        Assert.IsTrue(charges.TryUse(0f));

        charges.SpeedUp(4f);
        Assert.AreEqual(0, charges.Available(5f));
        Assert.AreEqual(1, charges.Available(6f));
    }

    [Test]
    public void Charges_SpeedUp_WhenFull_DoesNothing()
    {
        var charges = new AbilityCharges();
        charges.Configure(1, 10f, 0f);
        charges.SpeedUp(4f);
        Assert.IsTrue(charges.TryUse(1f));
        Assert.AreEqual(0, charges.Available(10f), "la recarga arranca al usarla: 10 s completos");
        Assert.AreEqual(1, charges.Available(11f));
    }

    // --- Tipos de habilidad y rangos ---

    [Test]
    public void ManaPulse_IsAnUltimate_UnlockedAtLevelSix_WithRankThree()
    {
        Assert.IsTrue(Progression.IsUltimate(AbilityKind.ManaPulse));
        Assert.AreEqual(3, Progression.MaxRank(AbilityKind.ManaPulse));
        Assert.AreEqual(0, Progression.RankCapForLevel(AbilityKind.ManaPulse, 5));
        Assert.AreEqual(1, Progression.RankCapForLevel(AbilityKind.ManaPulse, 6));
    }

    [Test]
    public void ManaBeamAndFlowerField_AreNormalAbilities_LearnableAtLevelOne()
    {
        foreach (AbilityKind kind in new[] { AbilityKind.ManaBeam, AbilityKind.FlowerField })
        {
            Assert.IsFalse(Progression.IsUltimate(kind));
            Assert.AreEqual(5, Progression.MaxRank(kind));
            Assert.AreEqual(1, Progression.RankCapForLevel(kind, 1));
        }
    }

    [Test]
    public void AbilityKinds_NewOnesAreAppendedAfterBerserk()
    {
        Assert.AreEqual(5, (int)AbilityKind.Berserk, "los valores guardados no cambian");
        Assert.AreEqual(6, (int)AbilityKind.ManaBeam);
        Assert.AreEqual(7, (int)AbilityKind.FlowerField);
        Assert.AreEqual(8, (int)AbilityKind.ManaPulse);
    }

    [Test]
    public void DescribeRank_NewKinds_HaveText()
    {
        var field = ScriptableObject.CreateInstance<AbilityDefinition>();
        field.kind = AbilityKind.FlowerField;
        field.duration = 6f;
        field.radius = 5f;
        field.cooldown = 18f;
        StringAssert.Contains("6", field.DescribeRank(1));
        StringAssert.Contains("18", field.DescribeRank(1));

        var pulse = ScriptableObject.CreateInstance<AbilityDefinition>();
        pulse.kind = AbilityKind.ManaPulse;
        pulse.duration = 9f;
        pulse.cooldown = 60f;
        StringAssert.Contains("9", pulse.DescribeRank(1));
        StringAssert.Contains("60", pulse.DescribeRank(1));

        var beam = ScriptableObject.CreateInstance<AbilityDefinition>();
        beam.kind = AbilityKind.ManaBeam;
        Assert.IsNotEmpty(beam.DescribeRank(1));
        Assert.AreEqual("Sin aprender", beam.DescribeRank(0));
    }
}
