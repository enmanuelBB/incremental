using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;

    [SerializeField, Tooltip("Segundos reales en cámara lenta")] private float slowMoDuration = 3f;
    [SerializeField, Tooltip("Velocidad final de la cámara lenta (0.06 = 6% de la normal)")] private float slowMoScale = 0.06f;

    private void Awake()
    {
        Instance = this;
        GameState.Reset();
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable() => GameEvents.GameOver += TriggerGameOver;
    private void OnDisable() => GameEvents.GameOver -= TriggerGameOver;

    private void TriggerGameOver(string message)
    {
        if (GameState.IsGameOver) return;

        GameState.SetGameOver();
        SaveSystem.Save();

        gameOverText.text = message;
        StartCoroutine(SlowMoThenFreeze());
    }

    private IEnumerator SlowMoThenFreeze()
    {
        float elapsed = 0f;

        while (elapsed < slowMoDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(1f, slowMoScale, elapsed / slowMoDuration);
            yield return null;
        }

        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }

    // Lo llama el botón de reiniciar del panel.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
