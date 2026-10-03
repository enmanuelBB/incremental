using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Cerebro de un jefe o minijefe: ejecuta las habilidades de su EnemyDefinition (embestida, invocar, enfurecer,
/// resucitar y disparar) según su vida, y publica su vida para la barra de la pantalla. Va en el prefab del jefe,
/// junto a EnemyAI; los jefes se mueven y mueren igual que cualquier enemigo. Los tiempos y las fases salen de
/// BossRules, AbilityTimers y BossPhaseState (lógica pura con tests).
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class BossController : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color TelegraphColor = new Color(0.95f, 0.1f, 0.05f);

    private EnemyAI enemy;
    private EnemyDefinition def;
    private BossAbility[] abilities = new BossAbility[0];
    private AbilityTimers timers = new AbilityTimers(0);
    private readonly BossPhaseState phases = new BossPhaseState();
    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private Vector3 baseScale;
    private bool active;
    private bool busy;           // embistiendo: no empieza otra habilidad a la vez
    private int lastPublishedHealth = -1;

    private void Awake()
    {
        enemy = GetComponent<EnemyAI>();
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        baseScale = transform.localScale;
    }

    /// <summary>Lo llama EnemyAI cada vez que el jefe aparece (viene de un pool: empieza de cero).</summary>
    public void Begin(EnemyAI owner, EnemyDefinition definition)
    {
        enemy = owner;
        def = definition;
        abilities = definition.abilities ?? new BossAbility[0];
        timers = new AbilityTimers(abilities.Length);
        phases.Reset();
        busy = false;
        active = true;
        lastPublishedHealth = -1;
        transform.localScale = baseScale;

        // Las habilidades con intervalo empiezan tras la mitad de su espera, para no atacar nada más aparecer.
        for (int i = 0; i < abilities.Length; i++) timers.Schedule(i, Time.time, abilities[i].interval * 0.5f);

        StopAllCoroutines();
        GameEvents.RaiseBossAppeared("¡Llegó el " + (def.tier == EnemyTier.MiniBoss ? "minijefe" : "jefe") + ": " + def.displayName + "!");
        PublishHealth();
    }

    private void OnDisable()
    {
        if (!active) return;

        active = false;
        StopAllCoroutines();
        GameEvents.RaiseBossHealthChanged(def != null ? def.displayName : "", 0, 1);
    }

    private void Update()
    {
        if (!active || enemy.IsDead || GameState.IsGameOver) return;

        PublishHealth();

        float fraction = enemy.HealthFraction;
        for (int i = 0; i < abilities.Length; i++)
        {
            BossAbility ability = abilities[i];

            if (ability.kind == BossAbilityKind.Enrage)
            {
                if (BossRules.Triggered(ability, fraction) && phases.TryFireOnce(i)) enemy.Enrage(ability.speedBonus, ability.damageBonus);
                continue;
            }

            if (ability.kind == BossAbilityKind.Revive || busy) continue;
            if (!BossRules.IsActive(ability, fraction) || !timers.Due(i, Time.time)) continue;

            timers.Schedule(i, Time.time, ability.interval);
            switch (ability.kind)
            {
                case BossAbilityKind.Charge: StartCoroutine(ChargeRoutine(ability)); break;
                case BossAbilityKind.Summon: Summon(ability); break;
                case BossAbilityKind.Shoot: StartCoroutine(ShootRoutine(ability)); break;
            }
        }
    }

    private void PublishHealth()
    {
        if (enemy.CurrentHealth == lastPublishedHealth) return;

        lastPublishedHealth = enemy.CurrentHealth;
        GameEvents.RaiseBossHealthChanged(def.displayName, enemy.CurrentHealth, enemy.MaxHealthScaled);
    }

    // --- Resucitar ---

    /// <summary>EnemyAI lo llama cuando la vida llega a 0: si le queda una resurrección, devuelve la vida con la que se levanta.</summary>
    public bool TryRevive(out int health)
    {
        health = 0;
        if (!active || phases.ReviveUsed) return false;

        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i].kind != BossAbilityKind.Revive) continue;

            phases.ReviveUsed = true;
            health = BossRules.ReviveHealth(enemy.MaxHealthScaled, abilities[i]);
            enemy.SetInvulnerable(abilities[i].reviveInvulnerableSeconds);
            GameEvents.RaiseBossAppeared("¡" + def.displayName + " se levanta!");
            return true;
        }
        return false;
    }

    // --- Invocar ---

    private void Summon(BossAbility ability)
    {
        if (ability.summon == null || EnemyPool.Instance == null) return;

        float radius = enemy.BodyRadius + 2f;
        for (int i = 0; i < ability.summonCount; i++)
        {
            float angle = (i / (float)ability.summonCount) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 position = transform.position + offset;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas)) position = hit.position;

            EnemyPool.Instance.Spawn(ability.summon, position, enemy.HealthScale);

            // Las invocadas cuentan para cerrar la oleada: no termina hasta matarlas también.
            if (WaveManager.Instance != null) WaveManager.Instance.RegisterSummoned();
        }
    }

    // --- Embestida ---

    private IEnumerator ChargeRoutine(BossAbility ability)
    {
        busy = true;
        enemy.SetControlled(true);

        // Aviso: se detiene, apunta al objetivo y se pone rojo.
        float t = 0f;
        Vector3 direction = ChargeDirection(ability);
        SetTint(true);
        while (t < ability.telegraphSeconds)
        {
            if (GameState.IsGameOver || enemy.IsDead) { EndCharge(); yield break; }

            t += Time.deltaTime;
            direction = ChargeDirection(ability);
            enemy.FaceDirection(direction);
            transform.localScale = baseScale * (1f + 0.08f * Mathf.Sin(t * 25f));
            yield return null;
        }
        transform.localScale = baseScale;

        // Embestida: línea recta a gran velocidad; daña una vez si atropella al jugador.
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        bool hitPlayer = false;
        float remaining = ability.chargeSeconds;
        while (remaining > 0f)
        {
            if (GameState.IsGameOver || enemy.IsDead) break;

            float dt = Time.deltaTime;
            remaining -= dt;
            enemy.MoveForced(direction * (ability.chargeSpeed * dt));

            if (!hitPlayer && player != null)
            {
                Vector3 toPlayer = player.transform.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.magnitude <= enemy.BodyRadius + 0.8f)
                {
                    hitPlayer = true;
                    player.TakeDamage(ability.chargeDamage);
                }
            }
            yield return null;
        }

        EndCharge();
    }

    private void EndCharge()
    {
        SetTint(false);
        transform.localScale = baseScale;
        enemy.SetControlled(false);
        busy = false;
    }

    // Hacia el jugador si está dentro del alcance; si no, hacia la base.
    private Vector3 ChargeDirection(BossAbility ability)
    {
        PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
        BaseHealth baseHealth = FindAnyObjectByType<BaseHealth>();

        Vector3 target = transform.position + Vector3.back;
        if (player != null && BossRules.ChargeTargetsPlayer(Vector3.Distance(transform.position, player.transform.position), ability.chargeRange))
            target = player.transform.position;
        else if (baseHealth != null)
            target = baseHealth.transform.position;

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.back;
    }

    private void SetTint(bool on)
    {
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            if (!on)
            {
                rend.SetPropertyBlock(null);
                continue;
            }

            Material material = rend.sharedMaterial;
            Color original = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(original, TelegraphColor, 0.8f));
            rend.SetPropertyBlock(block);
        }
    }

    // --- Disparar ---

    private IEnumerator ShootRoutine(BossAbility ability)
    {
        for (int i = 0; i < ability.burst; i++)
        {
            if (GameState.IsGameOver || enemy.IsDead) yield break;

            PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
            if (player != null)
            {
                Vector3 origin = transform.position + Vector3.up * 1.2f;
                BossProjectile.Launch(origin, player.transform, ability.projectileSpeed, ability.projectileDamage);
            }
            if (i < ability.burst - 1) yield return new WaitForSeconds(ability.burstSpacing);
        }
    }
}
