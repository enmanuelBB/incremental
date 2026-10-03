using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dirige el fin de la partida: congela el control, hace la cámara lenta, lanza la animación de muerte de la cámara,
/// oscurece la pantalla y al final muestra el resumen de la run (GameOverScreenUI) con el botón de reiniciar.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [SerializeField, Tooltip("Panel antiguo de la escena: ya no se usa, se mantiene apagado")] private GameObject gameOverPanel;
    [SerializeField, Tooltip("Texto del panel antiguo: ya no se usa")] private TMP_Text gameOverText;

    [SerializeField, Tooltip("Segundos reales en cámara lenta")] private float slowMoDuration = 3f;
    [SerializeField, Tooltip("Velocidad final de la cámara lenta (0.06 = 6% de la normal)")] private float slowMoScale = 0.06f;
    [SerializeField, Tooltip("Segundo de la cámara lenta en que empieza a oscurecerse la pantalla")] private float fadeStart = 1.4f;

    private GameOverScreenUI screen;

    private void Awake()
    {
        GameState.Reset();
        Time.timeScale = 1f;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        screen = GetComponent<GameOverScreenUI>();
        if (screen == null) screen = gameObject.AddComponent<GameOverScreenUI>();
    }

    private void OnEnable() => GameEvents.GameOver += TriggerGameOver;
    private void OnDisable() => GameEvents.GameOver -= TriggerGameOver;

    private void TriggerGameOver(string message, GameOverCause cause)
    {
        if (GameState.IsGameOver) return;

        GameState.SetGameOver();
        SaveSystem.Save();

        // El resumen se toma ahora, antes de la cámara lenta, para que no cuente nada de lo que pase después.
        RunStats stats = RunSummaryTracker.Instance != null ? RunSummaryTracker.Instance.Finish() : new RunStats();

        StartDeathCamera(cause);
        StartCoroutine(Sequence(cause, stats));
    }

    private void StartDeathCamera(GameOverCause cause)
    {
        Shooting shooting = Shooting.Instance;

        // Las pistolas siguen a la cámara; durante la animación solo estorbarían.
        if (shooting != null && shooting.HeldGuns != null) shooting.HeldGuns.gameObject.SetActive(false);

        Transform focus = null;
        if (cause == GameOverCause.PlayerDied && shooting != null)
        {
            focus = shooting.transform;
            TipOver(shooting.GetComponent<Rigidbody>());
        }
        else
        {
            BaseHealth baseHealth = FindAnyObjectByType<BaseHealth>();
            if (baseHealth != null) focus = baseHealth.transform;
        }

        Camera cam = Camera.main;
        CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.PlayDeath(focus, cause);
    }

    // El cuerpo cae de lado: se le quita el bloqueo de rotación y se le da un empujón.
    private static void TipOver(Rigidbody body)
    {
        if (body == null) return;

        body.constraints = RigidbodyConstraints.None;
        body.AddTorque(new Vector3(4f, 0f, 3f), ForceMode.VelocityChange);
    }

    private IEnumerator Sequence(GameOverCause cause, RunStats stats)
    {
        float elapsed = 0f;
        bool fading = false;

        while (elapsed < slowMoDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(1f, slowMoScale, elapsed / slowMoDuration);

            if (!fading && elapsed >= fadeStart)
            {
                fading = true;
                screen.BeginFade();
            }
            yield return null;
        }

        Time.timeScale = 0f;

        CharacterDefinition character = Shooting.Instance != null ? Shooting.Instance.Character : null;
        string characterName = character != null ? character.displayName : "El personaje";
        screen.Reveal(cause, stats, characterName, character != null && character.skillTree != null, RestartGame);
    }

    // Lo llama el botón de reiniciar de la pantalla.
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
