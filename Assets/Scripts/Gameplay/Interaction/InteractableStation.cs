using UnityEngine;

/// <summary>
/// Zona (trigger) donde el jugador pulsa "Interactuar". Solo funciona antes de que
/// empiece la primera oleada. Las subclases definen el texto y la acción.
/// </summary>
public abstract class InteractableStation : MonoBehaviour
{
    protected bool PlayerInRange { get; private set; }

    protected abstract string PromptText { get; }
    protected abstract void Interact();
    protected virtual void OnPlayerLeft() { }

    protected void ShowPrompt() => GameEvents.RaisePromptChanged(PromptText);
    protected void ShowPrompt(string message) => GameEvents.RaisePromptChanged(message);
    protected void HidePrompt() => GameEvents.RaisePromptChanged(null);

    protected virtual void OnEnable() => GameEvents.GameStarted += LeaveStation;
    protected virtual void OnDisable() => GameEvents.GameStarted -= LeaveStation;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (WaveManager.Instance.HasStarted) return;

        PlayerInRange = true;
        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) LeaveStation();
    }

    private void Update()
    {
        if (!PlayerInRange) return;
        if (GameState.IsGameOver) return;

        if (GameInput.Instance.InteractPressed) Interact();
    }

    private void LeaveStation()
    {
        if (!PlayerInRange) return;

        PlayerInRange = false;
        HidePrompt();
        OnPlayerLeft();
    }
}
