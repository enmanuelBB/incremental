public class BaseHealth : Health
{
    protected override void OnHealthChanged()
    {
        GameEvents.RaiseBaseHealthChanged(CurrentHealth, maxHealth);
    }

    protected override void OnDeath()
    {
        GameEvents.RaiseGameOver("¡Han destruido la base!", GameOverCause.BaseDestroyed);
    }
}
