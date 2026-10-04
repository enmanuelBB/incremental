public class UpgradeStation : MenuStation
{
    // Los personajes sin armas (por ahora Guts) no tienen nada que mejorar aquí.
    private static bool Available => Shooting.Instance != null && Shooting.Instance.WeaponCount > 0;

    protected override string PromptText => Available ? "Presiona E para mejorar arma" : "No disponible para este personaje";
    protected override MenuPanel Menu => UpgradeMenuUI.Instance;

    protected override void Interact()
    {
        if (Available || Menu.IsOpen) base.Interact();
    }
}
