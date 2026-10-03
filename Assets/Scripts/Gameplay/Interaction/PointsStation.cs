/// <summary>
/// Estación donde se gastan los puntos de personaje en habilidades y sangrado (donde estaba la tienda del M16).
/// Como todas las estaciones, solo funciona antes de la primera oleada.
/// </summary>
public class PointsStation : MenuStation
{
    protected override MenuPanel Menu => AbilityShopUI.Instance;

    protected override string PromptText
    {
        get
        {
            CharacterSave save = ProgressionManager.Instance != null ? ProgressionManager.Instance.ActiveSave : null;
            int points = save != null ? Progression.PointsAvailable(save) : 0;
            return "Presiona E para mejorar habilidades (" + points + (points == 1 ? " punto)" : " puntos)");
        }
    }
}
