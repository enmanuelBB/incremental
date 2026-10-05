using UnityEngine;

/// <summary>
/// Rayo de maná masivo de Frieren (tecla Q): línea recta que atraviesa y daña a todos los enemigos y se detiene en las paredes.
/// El daño, el maná, el alcance y el grosor salen del bastón (los mismos números de siempre, con el Poder de la tienda).
/// Clase estática, como FlameBurst: PlayerAbilities solo la llama.
/// </summary>
public static class ManaBeam
{
    private static readonly Color BeamColor = new Color(0.75f, 0.92f, 1f, 1f);

    /// <summary>Lanza el rayo. False (sin gastar nada ni empezar el enfriamiento) si no hay bastón o no alcanza el maná.</summary>
    public static bool Cast(AbilityDefinition ability, int rank, Shooting shooting)
    {
        if (shooting.WeaponCount == 0 || shooting.Mana == null) return false;

        WeaponState weapon = shooting.CurrentWeapon;
        StaffDefinition staff = weapon.Staff;
        if (staff == null) return false;
        if (!shooting.Mana.TrySpend(staff.abilityManaCost)) return false;

        Vector3 origin = shooting.MuzzlePoint;
        Vector3 end = PiercingBeam.Cast(new Ray(origin, Direction(shooting, origin, staff.abilityRange)),
            staff.abilityRange, staff.abilityBeamRadius, weapon.AbilityDamage, shooting.transform);

        shooting.ShowBeam(origin, end, BeamColor, staff.abilityBeamRadius * 1.5f, 0.25f);
        shooting.RefreshMana();
        return true;
    }

    // Sale horizontal, a la altura del bastón, hacia donde apunta la mira: recorre el campo a la altura de los enemigos
    // en vez de clavarse en el suelo, y atraviesa filas enteras.
    private static Vector3 Direction(Shooting shooting, Vector3 origin, float range)
    {
        Ray aim = shooting.AimRay();
        Vector3 target = shooting.TryGetHit(range, out RaycastHit hit) ? hit.point : aim.GetPoint(range);

        Vector3 direction = target - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = aim.direction;
            direction.y = 0f;
        }

        return direction.normalized;
    }
}
