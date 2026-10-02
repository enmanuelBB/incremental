using UnityEngine;

/// <summary>Objetivo que, al dispararle, inicia la primera horda.</summary>
public class StartTrigger : MonoBehaviour
{
    private bool activated;

    public void Activate()
    {
        if (activated) return;
        activated = true;

        WaveManager.Instance.BeginGame();
        gameObject.SetActive(false);
    }
}
