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
    public void Q_AndUltimate_TakeAMomentToCast_TheUltimateABitLonger()
    {
        AbilityDefinition beam = Maga.abilities[0], field = Maga.abilities[1], pulse = Maga.abilities[2];
        Assert.AreEqual(0.2f, beam.castSeconds, 1e-4f, "la Q no sale al instante: el gesto sube 0,2 s y el rayo sale en su máximo");
        Assert.AreEqual(0.35f, pulse.castSeconds, 1e-4f, "la definitiva tarda un poco más que la Q");
        Assert.Greater(pulse.castSeconds, beam.castSeconds);
        Assert.AreEqual(0f, field.castSeconds, "la E ya tiene su tiempo: el gesto se mantiene mientras se elige el círculo");
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
        Assert.AreEqual(4.5f, staff.abilityCooldown, 1e-4f);
    }

    // Pedido del usuario (2026-10-07): al máximo la Q se recargaba en 1 s, más rápido que el Zoltraak cargado.
    // Con la tienda y el árbol completos debe tardar 3 s: más del doble que un Zoltraak a carga completa.
    [Test]
    public void Q_AtMaxBuild_RechargesIn3Seconds_SlowerThanAFullZoltraak()
    {
        StaffDefinition staff = Staff;
        SkillTreeDefinition tree = Maga.skillTree;
        TreeBonuses bonuses = SkillTreeRules.Compute(tree, System.Linq.Enumerable.Select(tree.nodes, n => n.id));
        int manaLevel = CombatMath.MaxLevels(staff)[1];

        float shop = staff.AbilityCooldownAt(manaLevel);
        float beam = Mathf.Max(1f, shop - bonuses.BeamCooldownReduction);   // igual que PlayerAbilities.CooldownFor
        float zoltraak = FrierenTreeMath.ChargeSeconds(staff.zoltraakChargeSeconds, bonuses.ZoltraakChargeReduction) + staff.zoltraakPause;

        Assert.AreEqual(3.5f, shop, 1e-4f, "con la tienda al máximo");
        Assert.AreEqual(3f, beam, 1e-4f, "con la tienda y el árbol al máximo");
        Assert.Greater(beam, 2f * zoltraak, "la Q tiene que ser bastante más lenta que el Zoltraak cargado (" + zoltraak + " s)");
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

        // El hueso del bastón cuelga de Root, no de la mano: PlayerBody lo hace seguir a la mano izquierda cuando inclina el torso con la cámara.
        var bodySettings = new SerializedObject(System.Array.Find(body.GetComponents<MonoBehaviour>(), c => c != null && c.GetType().Name == "PlayerBody"));
        var heldProp = (Transform)bodySettings.FindProperty("heldProp").objectReferenceValue;
        var propHand = (Transform)bodySettings.FindProperty("propHand").objectReferenceValue;
        Assert.IsNotNull(heldProp, "falta el bastón en PlayerBody (heldProp): si no, queda fijo al mover la cámara");
        Assert.AreEqual("staff", heldProp.name);
        Assert.AreEqual("hand_l", propHand != null ? propHand.name : null, "Frieren sostiene el bastón con la mano izquierda");

        bool hasMuzzle = false;
        foreach (Transform t in body.GetComponentsInChildren<Transform>(true))
            if (t.name == "Muzzle" && t.parent != null && t.parent.name == "Staff") hasMuzzle = true;
        Assert.IsTrue(hasMuzzle, "falta el Muzzle en la punta del bastón (de ahí salen los disparos)");

        var controller = (UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
        // Hover es la base de todo: los movimientos se suman encima y el Cast encima de los dos (capas aditivas).
        Assert.AreEqual(3, controller.layers.Length, "Hover (base) + movimiento + Cast");
        var baseState = controller.layers[0].stateMachine.defaultState;
        Assert.AreEqual("Hover", baseState.motion.name, "la capa base es solo Hover");
        Assert.AreEqual(UnityEditor.Animations.AnimatorLayerBlendingMode.Additive, controller.layers[1].blendingMode, "los movimientos se suman a Hover");
        Assert.AreEqual(1f, controller.layers[1].defaultWeight);
        var moveState = controller.layers[1].stateMachine.defaultState;
        Assert.IsTrue(moveState.timeParameterActive, "el fotograma de los move lo decide el código (MovePhase), no el reloj");
        Assert.AreEqual("MovePhase", moveState.timeParameter);
        var locomotion = moveState.motion as UnityEditor.Animations.BlendTree;
        Assert.IsNotNull(locomotion, "el movimiento es un árbol de mezcla");
        foreach (var speed in locomotion.children)
            foreach (var child in ((UnityEditor.Animations.BlendTree)speed.motion).children)
                Assert.AreNotEqual("Hover", child.motion.name, "Hover no va en la capa aditiva (se sumaría dos veces): al centro va la pose neutra");
        Assert.AreEqual(UnityEditor.Animations.AnimatorLayerBlendingMode.Additive, controller.layers[2].blendingMode, "el Cast se suma a Hover y al movimiento");
        Assert.AreEqual(1f, controller.layers[2].defaultWeight);
        Assert.IsNotNull(controller.layers[2].avatarMask, "la capa del Cast lleva su máscara");
        var castState = controller.layers[2].stateMachine.defaultState;
        Assert.AreEqual("Cast", castState.motion != null ? castState.motion.name : null, "la capa del Cast siempre está en Cast (fase 0 = sin gesto)");
        Assert.IsTrue(castState.timeParameterActive, "el fotograma del Cast lo decide el código (CastPhase): al máximo al disparar o mientras carga");
        Assert.AreEqual("CastPhase", castState.timeParameter);

        // Máscara por huesos: activos el bastón, los brazos y la cabeza; inactivas las piernas
        AvatarMask mask = controller.layers[2].avatarMask;
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
        foreach (string parameter in new[] { "MoveX", "MoveY", "Run", "MovePhase", "CastPhase", "Cast", "Dead" })
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
