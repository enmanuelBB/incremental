using UnityEngine;

/// <summary>
/// Estado global mínimo: si hay un menú abierto, si terminó la partida y el cursor.
/// Los scripts de jugador consultan InputBlocked en lugar de conocer cada menú.
/// </summary>
public static class GameState
{
    private static int openMenus;

    public static bool IsGameOver { get; private set; }
    public static bool IsMenuOpen => openMenus > 0;
    public static bool InputBlocked => IsGameOver || IsMenuOpen;

    // Con "Enter Play Mode Options" sin recarga de dominio los estáticos sobreviven entre sesiones.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        openMenus = 0;
        IsGameOver = false;
    }

    public static void MenuOpened()
    {
        openMenus++;
        RefreshCursor();
    }

    public static void MenuClosed()
    {
        openMenus = Mathf.Max(0, openMenus - 1);
        RefreshCursor();
    }

    public static void SetGameOver()
    {
        IsGameOver = true;
        RefreshCursor();
    }

    public static void RefreshCursor()
    {
        bool free = InputBlocked;
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }
}
