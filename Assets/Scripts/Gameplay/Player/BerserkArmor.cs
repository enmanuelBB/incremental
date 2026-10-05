using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Armadura Berserker de Guts (ulti, tecla F): un interruptor. Puesta, da bonos fuertes (daño, golpes más rápidos, menos daño
/// recibido, golpe en 360°, más Furia) y le va drenando vida sin matarlo; se apaga a mano o sola al llegar a 1 de vida.
/// El enfriamiento lo maneja PlayerAbilities y empieza al apagarla. Los bonos se consultan por los miembros estáticos
/// (neutros cuando está apagada). Aspecto provisional: tinte rojo oscuro y humo rojo.
/// </summary>
public class BerserkArmor : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color ArmorTint = new Color(0.4f, 0.03f, 0.05f);

    public static BerserkArmor Instance { get; private set; }

    private static bool Active => Instance != null && Instance.IsActive;

    /// <summary>Multiplicador del daño de Guts (espada, Q y E). 1 si la armadura está apagada.</summary>
    public static float DamageMultiplier => Active ? Instance.damageMultiplier : 1f;

    /// <summary>Multiplicador del tiempo entre golpes de espada (0,5 = el doble de rápido). 1 si está apagada.</summary>
    public static float CadenceMultiplier => Active ? Instance.cadenceMultiplier : 1f;

    /// <summary>Multiplicador de la Furia que carga cada golpe. 1 si está apagada.</summary>
    public static float FuryGainMultiplier => Active ? Instance.furyGainMultiplier : 1f;

    /// <summary>Apertura del golpe de espada: la de la espada, o la de la armadura (360°) si está puesta.</summary>
    public static float SwingArc(float normalArc) => Active ? Mathf.Max(normalArc, Instance.swingArc) : normalArc;

    private readonly BerserkDrain drain = new BerserkDrain();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private PlayerHealth health;
    private Shooting shooting;
    private AbilityDefinition ability;
    private int rank = 1;
    private Action onDeactivated;
    private ParticleSystem aura;
    private MaterialPropertyBlock block;

    private float damageMultiplier = 1f;
    private float cadenceMultiplier = 1f;
    private float furyGainMultiplier = 1f;
    private float swingArc;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        Instance = this;
        health = GetComponent<PlayerHealth>();
        shooting = GetComponent<Shooting>();
        block = new MaterialPropertyBlock();
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
        ForceOff();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnGameOver(string message, GameOverCause cause) => ForceOff();
    private void OnCharacterChanged(CharacterDefinition character) => ForceOff();

    /// <summary>Se la pone: rugido, bonos y drenaje. "whenOff" se llama al apagarla a mano o sola (no al forzarla).</summary>
    public bool Activate(AbilityDefinition armor, int armorRank, Action whenOff)
    {
        if (IsActive || health == null) return false;

        ability = armor;
        rank = armorRank;
        onDeactivated = whenOff;
        // Mejoras del árbol: más daño, menos daño recibido y un rugido más fuerte. Ninguna toca el drenaje.
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        damageMultiplier = armor.BerserkDamageAt(armorRank) + tree.BerserkDamageBonus;
        cadenceMultiplier = armor.cadenceMultiplier;
        furyGainMultiplier = armor.furyGainMultiplier;
        swingArc = armor.swingArcDegrees;
        health.DamageTakenMultiplier = Mathf.Max(0.1f, armor.damageTakenMultiplier - tree.BerserkDamageTakenReduction);
        drain.Reset();
        IsActive = true;

        Roar(armor, tree);
        if (shooting != null && shooting.Body != null) shooting.Body.PlayPowerUp();
        SetTint(true);
        StartAura();
        return true;
    }

    /// <summary>La apaga (a mano o porque la vida llegó a 1) y avisa para que empiece el enfriamiento fijo.</summary>
    public void Deactivate()
    {
        if (!IsActive) return;

        TurnOff();
        Action callback = onDeactivated;
        onDeactivated = null;
        callback?.Invoke();
    }

    /// <summary>La apaga sin enfriamiento (cambio de personaje, fin de partida o al desactivar el componente).</summary>
    public void ForceOff()
    {
        if (!IsActive) return;

        TurnOff();
        onDeactivated = null;
    }

    private void TurnOff()
    {
        IsActive = false;
        if (health != null) health.DamageTakenMultiplier = 1f;
        SetTint(false);
        if (aura != null) aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Update()
    {
        if (!IsActive) return;

        if (GameState.IsGameOver || health == null || health.IsDead)
        {
            ForceOff();
            return;
        }

        int lose = BerserkDrain.Allowed(health.CurrentHealth, drain.Advance(Time.deltaTime, health.MaxHealth, ability.BerserkDrainAt(rank)));
        if (lose > 0) health.Drain(lose);

        if (BerserkDrain.ShouldStop(health.CurrentHealth)) Deactivate();
    }

    // Rugido al ponérsela: aturde a los enemigos cercanos (los jefes son inmunes por EnemyAI.ApplyStun).
    private void Roar(AbilityDefinition armor, TreeBonuses tree)
    {
        float radius = armor.roarRadius + tree.RoarRadiusBonus;
        float stunSeconds = armor.roarStunSeconds + tree.RoarStunBonus;
        float radiusSqr = radius * radius;
        foreach (EnemyAI enemy in UnityEngine.Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radiusSqr) enemy.ApplyStun(stunSeconds);
        }
    }

    private void StartAura()
    {
        if (aura == null) aura = AbilityVfx.CreateBerserkAura(transform);
        aura.Play();
    }

    private void SetTint(bool on)
    {
        renderers.Clear();
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer || rend is LineRenderer) continue;
            renderers.Add(rend);
        }

        foreach (Renderer rend in renderers)
        {
            if (!on)
            {
                rend.SetPropertyBlock(null);
                continue;
            }

            Material material = rend.sharedMaterial;
            Color original = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(original, ArmorTint, 0.7f));
            rend.SetPropertyBlock(block);
        }
    }
}
