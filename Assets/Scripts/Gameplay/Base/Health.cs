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

    /// <summary>Recupera vida sin pasar del máximo. No revive a quien ya murió.</summary>
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0 || CurrentHealth >= maxHealth) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || Invulnerable) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        OnHealthChanged();

        if (IsDead) OnDeath();
    }

    protected abstract void OnHealthChanged();
    protected abstract void OnDeath();
}
