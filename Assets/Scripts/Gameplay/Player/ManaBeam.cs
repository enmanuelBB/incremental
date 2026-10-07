using UnityEngine;

/// <summary>
/// Rayo de maná masivo de Frieren (tecla Q): línea recta que atraviesa y daña a todos los enemigos y se detiene en las paredes.
/// El daño, el maná, el alcance y el grosor salen del bastón (los mismos números de siempre, con el Poder de la tienda).
/// Clase estática, como FlameBurst: PlayerAbilities solo la llama.
/// </summary>
public static class ManaBeam
{
    private static readonly Color BeamColor = new Color(0.75f, 0.92f, 1f, 1f);

    /// <summary>True si se puede lanzar ahora: bastón en la mano y maná suficiente (no gasta nada).</summary>
    public static bool CanCast(Shooting shooting)
    {
        if (shooting.WeaponCount == 0 || shooting.Mana == null) return false;

        StaffDefinition staff = shooting.CurrentWeapon.Staff;
        return staff != null && shooting.Mana.Current >= CostOf(staff);
    }

    // Maná del rayo con Eficiencia del árbol.
    private static float CostOf(StaffDefinition staff) =>
        FrierenTreeMath.ManaCost(staff.abilityManaCost, SkillTreeManager.CurrentBonuses.ManaCostReduction);

    /// <summary>Lanza el rayo. False (sin gastar nada ni empezar el enfriamiento) si no hay bastón o no alcanza el maná.</summary>
    public static bool Cast(AbilityDefinition ability, int rank, Shooting shooting)
    {
        if (shooting.WeaponCount == 0 || shooting.Mana == null) return false;

        WeaponState weapon = shooting.CurrentWeapon;
        StaffDefinition staff = weapon.Staff;
        if (staff == null) return false;
        if (!shooting.Mana.TrySpend(CostOf(staff))) return false;

        // El árbol ensancha el rayo y le puede dar Perforación creciente o Rayo gélido.
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        float radius = staff.abilityBeamRadius + tree.BeamRadiusBonus;
        Vector3 origin = shooting.MuzzlePoint;
        Vector3 end = PiercingBeam.Cast(new Ray(origin, Direction(shooting, origin, staff.abilityRange)),
            staff.abilityRange, radius, weapon.AbilityDamage, shooting.transform, null, tree.BeamPierceDamage, tree.BeamFrostSlow);

        shooting.ShowBeam(origin, end, BeamColor, radius * 1.5f, 0.25f);
        shooting.RefreshMana();
        return true;
    }

    // Sale del bastón hacia el punto exacto de la mira, con su altura (pedido del usuario, 2026-10-07): apuntando arriba
    // alcanza a voladores y montones; apuntando al suelo se clava ahí. Antes salía siempre horizontal.
    private static Vector3 Direction(Shooting shooting, Vector3 origin, float range)
    {
        Ray aim = shooting.AimRay();
        Vector3 target = shooting.TryGetHit(range, out RaycastHit hit) ? hit.point : aim.GetPoint(range);

        return BeamAim.Direction(origin, target, aim.direction);
    }
}
