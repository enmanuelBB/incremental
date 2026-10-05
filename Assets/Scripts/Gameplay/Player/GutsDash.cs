using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Embestida de Guts (tecla E): un dash con giro (invulnerable, atraviesa enemigos) y, al levantarse, un disparo del arma de su
/// brazo al enemigo más cercano. Con la Furia llena el disparo sale potenciado (x2) y gasta la barra.
/// </summary>
public class GutsDash : MonoBehaviour
{
    private static readonly Color ShotColor = new Color(1f, 0.15f, 0.1f, 1f);
    private const float ShotHeight = 0.1f;     // el origen del jugador está a 1 m del piso; el rayo sale a ~1,1 m
    private const float IgnoreMargin = 3f;
    private const float ShotSpacing = 0.12f;   // segundos entre un disparo de la salva y el siguiente

    private Shooting shooting;
    private PlayerMovement movement;
    private PlayerHealth health;
    private Collider bodyCollider;
    private BeamVfx beam;
    private Coroutine routine;
    private readonly List<Collider> ignored = new List<Collider>();
    private readonly List<EnemyAI> candidates = new List<EnemyAI>();
    private readonly List<Vector3> positions = new List<Vector3>();

    public bool IsDashing => routine != null;

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
        bodyCollider = GetComponent<Collider>();
        beam = BeamVfx.Create("DashShotVfx");
    }

    private void OnEnable()
    {
        GameEvents.GameOver += OnGameOver;
        GameEvents.CharacterChanged += OnCharacterChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= OnGameOver;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        Cancel();
    }

    private void OnDestroy()
    {
        if (beam != null) Destroy(beam.gameObject);
    }

    private void OnGameOver(string message, GameOverCause cause) => Cancel();
    private void OnCharacterChanged(CharacterDefinition character) => Cancel();

    /// <summary>Empieza el dash. False si ya hay uno en curso o el personaje no lleva espada (el daño sale de ella).</summary>
    public bool TryStart(AbilityDefinition ability, int rank)
    {
        if (routine != null || shooting.WeaponCount == 0 || shooting.CurrentWeapon.Sword == null) return false;

        routine = StartCoroutine(Run(ability, rank));
        return true;
    }

    /// <summary>Corta el dash y deja todo como estaba (invulnerabilidad, movimiento, giro y colisiones).</summary>
    public void Cancel()
    {
        if (routine == null) return;

        StopAllCoroutines();   // también la salva de disparos, que es una corrutina anidada
        routine = null;
        Finish();
    }

    private IEnumerator Run(AbilityDefinition ability, int rank)
    {
        Vector3 aim = Vector3.ProjectOnPlane(shooting.AimRay().direction, Vector3.up);
        Vector3 direction = DashRules.Direction(movement.MoveInput, aim);
        if (direction == Vector3.zero) direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        // Mejoras del árbol: más distancia de dash.
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        float distance = ability.dashDistance + tree.DashDistanceBonus;

        // Invulnerable, sin chocar con los enemigos, empujado hacia delante y girando.
        health.Invulnerable = true;
        IgnoreEnemiesNear(distance + IgnoreMargin);
        movement.BeginForcedMove(direction * (distance / ability.dashSeconds), ability.dashSeconds);
        if (shooting.Body != null)
        {
            shooting.Body.PlayDash();
            shooting.Body.BeginSpin(ability.dashSeconds);
        }

        yield return new WaitForSeconds(ability.dashSeconds);
        movement.EndForcedMove();

        // Levantándose: sigue invulnerable y quieto.
        yield return new WaitForSeconds(ability.riseSeconds);

        // Sigue invulnerable durante toda la salva de disparos.
        yield return StartCoroutine(Volley(ability, rank, tree.DashExtraShots));
        routine = null;
        Finish();
    }

    private void Finish()
    {
        if (movement != null) movement.EndForcedMove();
        if (shooting != null && shooting.Body != null) shooting.Body.EndSpin();
        if (health != null) health.Invulnerable = false;
        RestoreEnemyCollisions();
    }

    private void IgnoreEnemiesNear(float radius)
    {
        foreach (EnemyAI enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead || (enemy.transform.position - transform.position).sqrMagnitude > radius * radius) continue;

            Collider body = enemy.GetComponent<Collider>();
            if (body == null || bodyCollider == null) continue;

            Physics.IgnoreCollision(bodyCollider, body, true);
            ignored.Add(body);
        }
    }

    private void RestoreEnemyCollisions()
    {
        foreach (Collider body in ignored)
        {
            if (body != null && bodyCollider != null && body.gameObject.activeInHierarchy)
                Physics.IgnoreCollision(bodyCollider, body, false);
        }
        ignored.Clear();
    }

    // Al levantarse: una salva de 1 + disparos extra del árbol (hasta 3), cada uno a un enemigo vivo distinto, del más cercano al
    // más lejano (sin importar hacia dónde mire); si hay menos enemigos que disparos, los que sobran repiten el más cercano.
    private IEnumerator Volley(AbilityDefinition ability, int rank, int extraShots)
    {
        WeaponState weapon = shooting.CurrentWeapon;
        SwordDefinition sword = weapon.Sword;
        if (sword == null) yield break;

        candidates.Clear();
        positions.Clear();
        foreach (EnemyAI enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            candidates.Add(enemy);
            positions.Add(enemy.transform.position);
        }

        Vector3 origin = transform.position;
        List<int> plan = TargetPicker.PlanShots(origin, positions, ability.shotRange, 1 + extraShots);
        if (plan.Count == 0) yield break;   // sin enemigos al alcance: no dispara y no gasta Furia

        // Furia llena: toda la salva sale potenciada y la barra se gasta una sola vez.
        bool empowered = shooting.Fury.TryConsume();
        float multiplier = empowered ? sword.furyDamageMultiplier : 1f;
        int damage = ability.SwordScaledDamageFor(weapon.Damage, rank, multiplier);
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;

        for (int shot = 0; shot < plan.Count; shot++)
        {
            EnemyAI target = candidates[plan[shot]];
            if (!target.IsDead)   // si el primer disparo lo mató, ese disparo se pierde (no hay error)
            {
                Vector3 from = transform.position + Vector3.up * ShotHeight;
                Vector3 to = target.transform.position + Vector3.up * 1f;
                beam.Show(from, to, ShotColor, 0.12f, 0.18f);
                AbilityVfx.ExplosionFlash(to, 0.8f);
                target.TakeDamage(tree.ScaleVsStunned(damage, target.IsStunned));
            }

            if (shot < plan.Count - 1) yield return new WaitForSeconds(ShotSpacing);
        }

        if (empowered) shooting.PublishFury();
    }
}
