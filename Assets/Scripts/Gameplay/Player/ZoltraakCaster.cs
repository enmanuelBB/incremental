using System.Collections;
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
    private bool overcharged;   // Sobrecarga del árbol: el siguiente sale cargado con un clic

    public bool IsCharging => charge.IsCharging;

    /// <summary>Hay una Sobrecarga armada (para pruebas).</summary>
    public bool Overcharged => overcharged;

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
        HoldCastPose(false);
    }

    // Mientras carga, el gesto del Cast se queda en su punto máximo; al soltar o cancelar vuelve.
    private void HoldCastPose(bool held)
    {
        if (shooting != null && shooting.Body != null) shooting.Body.HoldCast(held);
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
            if (input.FirePressed && Time.time >= nextAllowed)
            {
                Fire(weapon, 1f, false);
                if (shooting.Body != null) shooting.Body.PlayCast();
            }
            return;
        }

        if (!charge.IsCharging)
        {
            // Sobrecarga: tras uno al 100%, el siguiente sale cargado con un clic (y ese no vuelve a armarla).
            if (overcharged && input.FirePressed && Time.time >= nextAllowed)
            {
                overcharged = false;
                StaffHud.SetCharge(-1f);
                Fire(weapon, 1f, true);
                if (shooting.Body != null) shooting.Body.PlayCast();
                return;
            }

            bool hasMana = shooting.Mana != null && shooting.Mana.Current >= staff.zoltraakManaCost;
            if (input.FirePressed && Time.time >= nextAllowed && hasMana)
            {
                charge.Begin(FrierenTreeMath.ChargeSeconds(staff.zoltraakChargeSeconds, SkillTreeManager.CurrentBonuses.ZoltraakChargeReduction));
                HoldCastPose(true);
            }
            return;
        }

        charge.Tick(Time.deltaTime);
        StaffHud.SetCharge(charge.Fraction);

        if (input.FireHeld) return;

        float fraction = charge.Release();
        StaffHud.SetCharge(-1f);
        if (shooting.Body != null) shooting.Body.ReleaseCast(); // sale con el gesto al máximo y vuelve
        Fire(weapon, fraction, true);

        // Sobrecarga armada: el anillo de carga se queda lleno hasta usarla.
        overcharged = fraction >= 1f && SkillTreeManager.CurrentBonuses.ZoltraakOvercharge;
        if (overcharged) StaffHud.SetCharge(1f);
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
        shooting.ShowBolt(point, BoltColor, 0.12f, 0.12f);
        Explode(point, RadiusFor(staff, fraction), weapon.ZoltraakDamage(fraction), fraction >= 1f, true);
    }

    /// <summary>Radio del Zoltraak con esa carga, con el bono del árbol.</summary>
    public static float RadiusFor(StaffDefinition staff, float fraction) =>
        staff.ZoltraakRadiusAt(fraction) + SkillTreeManager.CurrentBonuses.ZoltraakRadiusBonus;

    /// <summary>Lluvia de Zoltraak (pulso): un rayo cae desde el cielo y, al llegar, explota como un Zoltraak a carga completa.</summary>
    public void DropFromSky(WeaponState weapon, Vector3 target) => StartCoroutine(Drop(weapon, target));

    private IEnumerator Drop(WeaponState weapon, Vector3 target)
    {
        shooting.ShowBeam(target + Vector3.up * FrierenTreeMath.RainHeight, target, BoltColor, 0.35f, FrierenTreeMath.RainDropSeconds);
        yield return new WaitForSeconds(FrierenTreeMath.RainDropSeconds);
        if (GameState.IsGameOver || weapon.Staff == null) yield break;

        Explode(target, RadiusFor(weapon.Staff, 1f), weapon.ZoltraakDamage(1f), true, true);
    }

    // Daña a todos los enemigos del radio. A carga completa aplica Escarcha y, si se permite, el Eco (que no repite Eco ni Escarcha).
    private void Explode(Vector3 point, float radius, int damage, bool fullCharge, bool allowEcho)
    {
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
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

        foreach (EnemyAI enemy in targets)
        {
            if (fullCharge && tree.ZoltraakFrostSlow > 0f) enemy.ApplySlow(tree.ZoltraakFrostSlow, FrierenTreeMath.FrostSeconds);
            enemy.TakeDamage(damage);
        }

        if (fullCharge && allowEcho && tree.ZoltraakEchoFraction > 0f)
            StartCoroutine(Echo(point, radius * FrierenTreeMath.EchoRadiusFraction, Mathf.Max(1, Mathf.RoundToInt(damage * tree.ZoltraakEchoFraction))));
    }

    private IEnumerator Echo(Vector3 point, float radius, int damage)
    {
        yield return new WaitForSeconds(FrierenTreeMath.EchoDelay);
        if (GameState.IsGameOver) yield break;

        Explode(point, radius, damage, false, false);
    }

    /// <summary>Cuántos enemigos alcanzó el último disparo (para pruebas).</summary>
    public int LastTargetCount => targets.Count;
}
