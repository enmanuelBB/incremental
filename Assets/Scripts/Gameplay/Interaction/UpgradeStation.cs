public class UpgradeStation : MenuStation
{
    protected override string PromptText => "Presiona E para mejorar arma";
    protected override MenuPanel Menu => UpgradeMenuUI.Instance;
}
