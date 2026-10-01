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

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        OnHealthChanged();

        if (IsDead) OnDeath();
    }

    protected abstract void OnHealthChanged();
    protected abstract void OnDeath();
}
