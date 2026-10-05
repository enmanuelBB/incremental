using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Valida los assets reales de Frieren (el asset del personaje, sus habilidades y el bastón).</summary>
[TestFixture]
public class FrierenAssetTests
{
    private static CharacterDefinition Maga => AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Maga.asset");
    private static StaffDefinition Staff => AssetDatabase.LoadAssetAtPath<StaffDefinition>("Assets/Data/Weapons/Baston.asset");

    [Test]
    public void Frieren_HasThreeAbilities_InQEFOrder()
    {
        AbilityDefinition[] abilities = Maga.abilities;
        Assert.AreEqual(3, abilities.Length);
        Assert.AreEqual(AbilityKind.ManaBeam, abilities[0].kind);
        Assert.AreEqual(AbilityKind.FlowerField, abilities[1].kind);
        Assert.AreEqual(AbilityKind.ManaPulse, abilities[2].kind);
        foreach (AbilityDefinition ability in abilities) Assert.IsFalse(string.IsNullOrEmpty(ability.abilityName));
    }

    [Test]
    public void FlowerField_HasTheSpecValues()
    {
        AbilityDefinition field = Maga.abilities[1];
        Assert.AreEqual(5f, field.radius, 1e-4f);
        Assert.AreEqual(6f, field.duration, 1e-4f);
        Assert.AreEqual(18f, field.cooldown, 1e-4f);
        Assert.AreEqual(20f, field.manaCost, 1e-4f);
        Assert.AreEqual(0.03f, field.fieldHealFractionPerSecond, 1e-4f);
        Assert.AreEqual(0.4f, field.fieldSlowFraction, 1e-4f);
        Assert.AreEqual(25f, field.fieldPlaceRange, 1e-4f);
        Assert.Greater(field.cooldown, field.duration, "el campo no puede pisarse a sí mismo");
    }

    [Test]
    public void ManaPulse_HasTheSpecValues()
    {
        AbilityDefinition pulse = Maga.abilities[2];
        Assert.AreEqual(15f, pulse.radius, 1e-4f);
        Assert.AreEqual(9f, pulse.duration, 1e-4f);
        Assert.AreEqual(60f, pulse.cooldown, 1e-4f);
        Assert.AreEqual(50f, pulse.manaCost, 1e-4f);
        Assert.AreEqual(1.5f, pulse.pulseStunSeconds, 1e-4f);
        Assert.AreEqual(0.4f, pulse.pulseSlowFraction, 1e-4f);
        Assert.AreEqual(2f, pulse.pulseCooldownBoost, 1e-4f);
    }

    [Test]
    public void Staff_Zoltraak_HasTheSpecValues()
    {
        StaffDefinition staff = Staff;
        Assert.AreEqual(45, staff.zoltraakDamage);
        Assert.AreEqual(1.2f, staff.zoltraakChargeSeconds, 1e-4f);
        Assert.AreEqual(0.4f, staff.zoltraakMinDamageFraction, 1e-4f);
        Assert.AreEqual(2.5f, staff.zoltraakMinRadius, 1e-4f);
        Assert.AreEqual(4f, staff.zoltraakMaxRadius, 1e-4f);
        Assert.AreEqual(0f, staff.zoltraakManaCost, 1e-4f, "el Zoltraak no gasta maná");
        Assert.AreEqual(0.4f, staff.zoltraakPause, 1e-4f);
        Assert.AreEqual(0.35f, staff.zoltraakInstantPause, 1e-4f);
    }

    [Test]
    public void Staff_BeamNumbersStayInTheStaff()
    {
        StaffDefinition staff = Staff;
        Assert.AreEqual(40, staff.abilityDamage);
        Assert.AreEqual(25f, staff.abilityManaCost, 1e-4f);
        Assert.AreEqual(3f, staff.abilityCooldown, 1e-4f);
    }

    [Test]
    public void Frieren_Levitates_AndOthersDoNot()
    {
        // Su modelo ya flota ~0,44 m por sí solo: el código suma poco (0,1 m) más el balanceo.
        Assert.AreEqual(0.1f, Maga.hoverHeight, 1e-4f);
        Assert.AreEqual(0.05f, Maga.hoverBob, 1e-4f);
        Assert.AreEqual(2.5f, Maga.hoverPeriod, 1e-4f);
        Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Alucard.asset").hoverHeight);
        Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset").hoverHeight);
    }

    [Test]
    public void Frieren_HasHerBody_WithMuzzleOnTheStaff_AndAnimatorLayers()
    {
        GameObject body = Maga.bodyPrefab;
        Assert.IsNotNull(body, "Frieren necesita su cuerpo (Frieren_Character.prefab)");
        // PlayerBody vive en Game.Runtime, que estos tests no ven: se busca por nombre.
        Assert.IsTrue(System.Array.Exists(body.GetComponents<MonoBehaviour>(), c => c != null && c.GetType().Name == "PlayerBody"), "el cuerpo necesita PlayerBody");

        var animator = body.GetComponent<Animator>();
        Assert.IsNotNull(animator.avatar);
        Assert.IsTrue(animator.avatar.isValid);
        // Generic a propósito: en Humanoide Unity descarta el hueso del bastón y reescribe los dedos (agarre del bastón).
        Assert.IsFalse(animator.avatar.isHuman, "el rig de Frieren es Generic: así se animan el bastón y los dedos tal cual el FBX");

        bool hasMuzzle = false;
        foreach (Transform t in body.GetComponentsInChildren<Transform>(true))
            if (t.name == "Muzzle" && t.parent != null && t.parent.name == "Staff") hasMuzzle = true;
        Assert.IsTrue(hasMuzzle, "falta el Muzzle en la punta del bastón (de ahí salen los disparos)");

        var controller = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
        Assert.AreEqual(2, controller.layers.Length, "capa base + capa superior (Cast)");
        Assert.IsNotNull(controller.layers[1].avatarMask, "la capa superior lleva su máscara");

        // Máscara por huesos: activos el bastón, los brazos y la cabeza; inactivas las piernas
        AvatarMask mask = controller.layers[1].avatarMask;
        System.Func<string, bool?> active = path =>
        {
            for (int i = 0; i < mask.transformCount; i++) if (mask.GetTransformPath(i) == path) return mask.GetTransformActive(i);
            return null;
        };
        Assert.AreEqual(true, active("Frieren90/Root/staff"), "la máscara incluye el hueso del bastón");
        Assert.AreEqual(true, active("Frieren90/Root/staff/Staff"));
        Assert.AreEqual(true, active("Frieren90/Root/pelvis/spine_01/spine_02/spine_03/clavicle_r/upperarm_r"));
        Assert.AreEqual(true, active("Frieren90/Root/pelvis/spine_01/spine_02/spine_03/neck_01/head"));
        Assert.AreEqual(false, active("Frieren90/Root/pelvis/thigh_l"), "las piernas no entran en la capa superior");
        foreach (string parameter in new[] { "MoveX", "MoveY", "Run", "Cast", "Dead" })
            Assert.IsTrue(System.Array.Exists(controller.parameters, p => p.name == parameter), "falta el parámetro " + parameter);
    }

    [Test]
    public void Frieren_FirstTwoAbilitiesCanBeLearnedAtLevelOne_ButNotTheUltimate()
    {
        var save = new CharacterSave { level = 1 };
        AbilityDefinition[] abilities = Maga.abilities;

        Assert.AreEqual(UpgradeBlock.None, Progression.CanUpgradeAbility(save, 0, abilities[0].kind));
        Assert.AreEqual(UpgradeBlock.None, Progression.CanUpgradeAbility(save, 1, abilities[1].kind));
        Assert.AreEqual(UpgradeBlock.LevelTooLow, Progression.CanUpgradeAbility(save, 2, abilities[2].kind));
        Assert.AreEqual(1, Progression.PointsAvailable(save), "un solo punto: se elige entre la Q y la E");
    }
}
