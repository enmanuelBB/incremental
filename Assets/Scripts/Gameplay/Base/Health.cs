using UnityEngine;

/// <summary>
/// Vida común para jugador y base. Cada subclase decide qué evento publicar
/// al cambiar la vida y qué pasa al morir.
/// </summary>
public abstract class Health : MonoBehaviour, IDamageable
{
    [SerializeField] protected int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    /// <summary>Momento (Time.time) del último golpe que le llegó (la Concentración de Frieren lo mira). -999 si nunca.</summary>
    public float LastDamagedAt { get; private set; } = -999f;
    public bool IsDead => CurrentHealth <= 0;

    protected virtual void Start()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged();
    }

    /// <summary>Cambia la vida máxima y la llena. Lo usa el personaje elegido antes de empezar la partida.</summary>
    public void SetMaxHealth(int value)
    {
        maxHealth = Mathf.Max(1, value);
        CurrentHealth = maxHealth;
        OnHealthChanged();
    }

    /// <summary>Mientras sea true no recibe daño (niebla de Alucard).</summary>
    public bool Invulnerable { get; set; }

    /// <summary>Multiplicador del daño que recibe (1 = normal; 0,5 = la mitad). Lo usa la armadura Berserker de Guts.</summary>
    public float DamageTakenMultiplier { get; set; } = 1f;

    /// <summary>Recupera vida sin pasar del máximo. No revive a quien ya murió.</summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0 || CurrentHealth >= maxHealth) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged();
    }

    /// <summary>Quita vida sin matar (nunca baja de 1) y sin multiplicadores ni invulnerabilidad: el drenaje de la armadura de Guts.</summary>
    public void Drain(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Max(1, CurrentHealth - amount);
        OnHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || Invulnerable) return;

        LastDamagedAt = Time.time;
        if (!Mathf.Approximately(DamageTakenMultiplier, 1f)) amount = Mathf.Max(1, Mathf.RoundToInt(amount * DamageTakenMultiplier));
        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        OnHealthChanged();

        if (IsDead) OnDeath();
    }

    protected abstract void OnHealthChanged();
    protected abstract void OnDeath();
}
