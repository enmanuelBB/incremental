using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zoltraak de Frieren (clic izquierdo con el bastón): se mantiene el clic para cargar y al soltar cae una explosión en área
/// donde apunta la mira. Escala con la carga (daño y radio). Hoy no gasta maná (zoltraakManaCost = 0, por pedido del usuario), pero
/// si el bastón le pone costo se cobra al soltar. Con el pulso de maná activo no hay carga ni gasto: sale uno por clic. Shooting la crea y la llama cada fotograma con el bastón en la mano.
/// </summary>
public class ZoltraakCaster : MonoBehaviour
{
    private static readonly Color BoltColor = new Color(1f, 0.85f, 0.4f, 0.95f);
    private static readonly Color BlastColor = new Color(0.95f, 0.75f, 0.2f, 0.5f);

    private readonly ZoltraakCharge charge = new ZoltraakCharge();
    private readonly Collider[] buffer = new Collider[96];
    private readonly List<EnemyAI> targets = new List<EnemyAI>();
    private Shooting shooting;
    private PlayerAbilities abilities;
    private float nextAllowed;

    public bool IsCharging => charge.IsCharging;

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        abilities = GetComponent<PlayerAbilities>();
    }

    private void OnDisable() => Cancel();

    /// <summary>Corta la carga sin disparar (cambio de personaje, fin de partida, menú, colocar el campo).</summary>
    public void Cancel()
    {
        if (!charge.IsCharging) return;

        charge.Cancel();
        StaffHud.SetCharge(-1f);
    }

    public void Tick(WeaponState weapon, GameInput input)
    {
        StaffDefinition staff = weapon.Staff;
        if (staff == null) return;

        if (GameState.InputBlocked || abilities.BlocksFire)
        {
            Cancel();
            return;
        }

        // Pulso de maná: sin carga y sin maná, un disparo por clic.
        if (abilities.PulseActive)
        {
            Cancel();
            if (input.FirePressed && Time.time >= nextAllowed) Fire(weapon, 1f, false);
            return;
        }

        if (!charge.IsCharging)
        {
            bool hasMana = shooting.Mana != null && shooting.Mana.Current >= staff.zoltraakManaCost;
            if (input.FirePressed && Time.time >= nextAllowed && hasMana) charge.Begin(staff.zoltraakChargeSeconds);
            return;
        }

        charge.Tick(Time.deltaTime);
        StaffHud.SetCharge(charge.Fraction);

        if (input.FireHeld) return;

        float fraction = charge.Release();
        StaffHud.SetCharge(-1f);
        Fire(weapon, fraction, true);
    }

    /// <summary>Dispara el Zoltraak con esa carga (0 a 1). Con 'spendMana' cobra el maná y no sale si ya no alcanza.</summary>
    public void Fire(WeaponState weapon, float fraction, bool spendMana)
    {
        StaffDefinition staff = weapon.Staff;

        // El maná se cobra al soltar: si en ese momento ya no alcanza, no sale.
        if (spendMana && (shooting.Mana == null || !shooting.Mana.TrySpend(staff.zoltraakManaCost))) return;

        shooting.RefreshMana();
        nextAllowed = Time.time + (abilities.PulseActive ? staff.zoltraakInstantPause : staff.zoltraakPause);

        Vector3 point = shooting.AimGroundPoint(staff.zoltraakRange);
        float radius = staff.ZoltraakRadiusAt(fraction);
        int damage = weapon.ZoltraakDamage(fraction);

        if (shooting.Body != null) shooting.Body.PlayCast();
        shooting.ShowBolt(point, BoltColor, 0.12f, 0.12f);
        AbilityVfx.ExplosionFlash(point, radius, BlastColor);

        // Primero se recogen los enemigos y después se les daña: matar a uno lo devuelve al pool y desactiva su objeto.
        targets.Clear();
        int count = Physics.OverlapSphereNonAlloc(point, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead || targets.Contains(enemy)) continue;

            targets.Add(enemy);
        }

        foreach (EnemyAI enemy in targets) enemy.TakeDamage(damage);
    }

    /// <summary>Cuántos enemigos alcanzó el último disparo (para pruebas).</summary>
    public int LastTargetCount => targets.Count;
}
