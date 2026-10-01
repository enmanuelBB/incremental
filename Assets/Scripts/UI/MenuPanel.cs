using UnityEngine;

/// <summary>
/// Base de los menús a pantalla completa: abre/cierra el panel y avisa a GameState
/// para que se libere el cursor y se bloquee el control del jugador.
/// </summary>
public abstract class MenuPanel : MonoBehaviour
{
    [SerializeField] protected GameObject panel;

    public bool IsOpen => panel != null && panel.activeSelf;

    protected virtual void Awake()
    {
        panel.SetActive(false);
    }

    public void Open()
    {
        if (IsOpen) return;

        panel.SetActive(true);
        GameState.MenuOpened();
        OnOpened();
    }

    public void Close()
    {
        if (!IsOpen) return;

        panel.SetActive(false);
        GameState.MenuClosed();
    }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable()
    {
        Close();
    }

    protected abstract void OnOpened();
}
