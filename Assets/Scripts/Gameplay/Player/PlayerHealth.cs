public class PlayerHealth : Health
{
    protected override void OnHealthChanged()
    {
        GameEvents.RaisePlayerHealthChanged(CurrentHealth, maxHealth);
    }

    protected override void OnDeath()
    {
        GameEvents.RaiseGameOver("Has muerto", GameOverCause.PlayerDied);
    }
}
