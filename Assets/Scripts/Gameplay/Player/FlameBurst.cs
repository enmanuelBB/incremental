using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Llamarada de Guts (tecla Q): un cono de fuego frente a él que daña y quema a todos los enemigos dentro.
/// Con la Furia llena sale potenciada (x2 en el daño inicial y en cada tick de la quemadura) y gasta la barra.
/// Clase estática para que PlayerAbilities, que es de Alucard, no crezca con la lógica de Guts.
/// </summary>
public static class FlameBurst
{
    private const float SearchMargin = 3f;     // holgura para enemigos grandes (el radio real se cuenta en MeleeCone)
    private const float VfxHeight = 0.1f;      // el origen del jugador está a 1 m del piso; el fuego sale a ~1,1 m

    private static readonly Collider[] Buffer = new Collider[64];
    private static readonly List<EnemyAI> Targets = new List<EnemyAI>();

    /// <summary>Lanza la llamarada. Devuelve false si no se puede (sin espada); true aunque no toque a nadie (entra en enfriamiento).</summary>
    public static bool Cast(AbilityDefinition ability, int rank, Shooting shooting)
    {
        if (shooting.WeaponCount == 0) return false;

        WeaponState weapon = shooting.CurrentWeapon;
        SwordDefinition sword = weapon.Sword;
        if (sword == null) return false;   // el daño de la llamarada sale del de la espada

        Transform player = shooting.transform;
        Vector3 forward = Vector3.ProjectOnPlane(shooting.AimRay().direction, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = player.forward;
        forward.Normalize();

        // Mejoras del árbol: más alcance y un cono más ancho.
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        float range = ability.range + tree.FlameRangeBonus;
        float cone = ability.coneDegrees + tree.FlameConeBonus;

        Vector3 origin = player.position;
        AbilityVfx.FlameCone(origin + Vector3.up * VfxHeight, forward, range, cone);
        if (shooting.Body != null) shooting.Body.PlayCast();

        Targets.Clear();
        int count = Physics.OverlapSphereNonAlloc(origin, range + SearchMargin, Buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = Buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead || Targets.Contains(enemy)) continue;
            if (!MeleeCone.Contains(origin, forward, enemy.transform.position, range, cone, enemy.BodyRadius)) continue;

            Targets.Add(enemy);
        }

        // Sin enemigos en el cono: ni daño ni Furia gastada (igual entra en enfriamiento).
        if (Targets.Count == 0) return true;

        // Furia llena: la llamarada sale potenciada y gasta toda la barra.
        bool empowered = shooting.Fury.TryConsume();
        float multiplier = empowered ? sword.furyDamageMultiplier : 1f;

        int swordDamage = weapon.Damage;
        int damage = ability.FlameDamageFor(swordDamage, rank, multiplier);
        int tick = Mathf.Max(1, Mathf.RoundToInt(ability.BurnTickDamageFor(swordDamage, multiplier) * (1f + tree.FlameBurnDamagePercent)));
        float seconds = ability.BurnSecondsAt(rank) + tree.FlameBurnSecondsBonus;

        foreach (EnemyAI enemy in Targets)
        {
            enemy.TakeDamage(tree.ScaleVsStunned(damage, enemy.IsStunned));   // primero el daño: si muere, ApplyBurn lo ignora
            enemy.ApplyBurn(seconds, tick, ability.burnTickSeconds);
        }

        if (empowered) shooting.PublishFury();
        return true;
    }
}
