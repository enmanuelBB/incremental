public class WeaponInfoStation : MenuStation
{
    protected override string PromptText => "Presiona E para ver armas";
    protected override MenuPanel Menu => WeaponInfoUI.Instance;
}
