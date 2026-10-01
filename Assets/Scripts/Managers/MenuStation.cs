/// <summary>Estación que abre/cierra un menú al interactuar.</summary>
public abstract class MenuStation : InteractableStation
{
    protected abstract MenuPanel Menu { get; }

    protected override void Interact()
    {
        if (Menu.IsOpen)
        {
            Menu.Close();
            ShowPrompt();
        }
        else
        {
            HidePrompt();
            Menu.Open();
        }
    }

    protected override void OnPlayerLeft()
    {
        Menu.Close();
    }
}
