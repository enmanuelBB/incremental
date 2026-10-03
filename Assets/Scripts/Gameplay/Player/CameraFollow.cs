using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Zoom")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomFOV = 30f;
    [SerializeField] private float zoomSpeed = 10f;

    [Header("Posición")]
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(2f, 1f, -5f);
    [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Tooltip("Punto del jugador desde el que se mide la colisión de la cámara")]
    private Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float collisionRadius = 0.25f;

    [Header("Sensibilidad")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField, Tooltip("Grados por segundo con el stick del gamepad")] private float stickSensitivity = 150f;

    private Camera cam;
    private bool isFirstPerson;
    private float rotationX;   // vertical
    private float rotationY;   // horizontal

    private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    // --- Animación de muerte ---
    private enum DeathMode { None, Player, Base }
    private const float DeathDuration = 4f;
    private DeathMode deathMode;
    private Transform deathFocus;
    private float deathTime;
    private float deathStartFov;
    private Vector3 deathStartPosition;
    private Quaternion deathStartRotation;
    private Vector3 deathOffset;        // de dónde mira, relativo al punto de foco, al empezar
    private Vector3 deathFlatDirection; // dirección horizontal de partida (si la cámara está justo encima del foco)

    public bool IsFirstPerson => isFirstPerson;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        GameState.RefreshCursor();
    }

    private void Update()
    {
        if (GameState.InputBlocked) return;

        GameInput input = GameInput.Instance;

        if (input.ToggleViewPressed) isFirstPerson = !isFirstPerson;

        float targetFov = input.AimHeld ? zoomFOV : normalFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, zoomSpeed * Time.deltaTime);

        Vector2 look = input.LookDelta * mouseSensitivity + input.LookStick * (stickSensitivity * Time.unscaledDeltaTime);
        rotationY += look.x;
        rotationX = Mathf.Clamp(rotationX - look.y, -60f, 60f);
    }

    /// <summary>
    /// Suelta el control de la cámara y hace la animación de fin de partida. Si muere el jugador, se separa del cuerpo
    /// y lo rodea subiendo y alejándose, con una leve inclinación; si destruyen la base, gira hacia ella y tiembla.
    /// Usa tiempo real, así que no le afecta la cámara lenta.
    /// </summary>
    public void PlayDeath(Transform focus, GameOverCause cause)
    {
        if (focus == null) return;

        deathMode = cause == GameOverCause.PlayerDied ? DeathMode.Player : DeathMode.Base;
        deathFocus = focus;
        deathTime = 0f;
        deathStartFov = cam.fieldOfView;
        deathStartPosition = transform.position;
        deathStartRotation = transform.rotation;
        deathOffset = deathStartPosition - DeathFocusPoint();

        Vector3 flat = new Vector3(deathOffset.x, 0f, deathOffset.z);
        deathFlatDirection = flat.sqrMagnitude > 0.01f
            ? flat.normalized
            : -Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (deathFlatDirection.sqrMagnitude < 0.01f) deathFlatDirection = Vector3.back;
    }

    private Vector3 DeathFocusPoint() => deathFocus.position + (deathMode == DeathMode.Player ? Vector3.up * 0.6f : Vector3.zero);

    private void UpdateDeath()
    {
        deathTime += Time.unscaledDeltaTime;
        float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(deathTime / DeathDuration));

        if (deathMode == DeathMode.Player) UpdateDeathAroundBody(u);
        else UpdateDeathTowardsBase(u);
    }

    // Arco alrededor del cuerpo: se aleja, sube, gira y se inclina de lado; sigue derivando despacio al terminar.
    private void UpdateDeathAroundBody(float u)
    {
        Vector3 center = DeathFocusPoint();
        float yaw = 45f * u + deathTime * 2.5f;
        Vector3 direction = Quaternion.AngleAxis(yaw, Vector3.up) * deathFlatDirection;

        float startRadius = new Vector2(deathOffset.x, deathOffset.z).magnitude;
        float radius = Mathf.Lerp(startRadius, 6.5f, u);
        float height = Mathf.Lerp(deathOffset.y, 4.6f, u);
        Vector3 position = center + direction * radius + Vector3.up * height;

        Quaternion look = Quaternion.LookRotation(center - position, Vector3.up);
        transform.SetPositionAndRotation(position, look * Quaternion.Euler(0f, 0f, 9f * u));
        cam.fieldOfView = Mathf.Lerp(deathStartFov, 46f, u);
    }

    // Gira hacia la base, se aleja un poco y tiembla con una fuerza que se apaga.
    private void UpdateDeathTowardsBase(float u)
    {
        Vector3 toBase = deathFocus.position - deathStartPosition;
        Quaternion target = toBase.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toBase, Vector3.up) : deathStartRotation;
        Quaternion rotation = Quaternion.Slerp(deathStartRotation, target, u);

        Vector3 position = deathStartPosition - rotation * Vector3.forward * (2.5f * u) + Vector3.up * (1.2f * u);

        float shake = 0.45f * (1f - u) * (1f - u);
        float t = deathTime * 22f;
        Vector3 jitter = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * (2f * shake);

        transform.SetPositionAndRotation(position + rotation * jitter, rotation);
        cam.fieldOfView = Mathf.Lerp(deathStartFov, 52f, u);
    }

    private void LateUpdate()
    {
        if (deathMode != DeathMode.None)
        {
            UpdateDeath();
            return;
        }

        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0f);

        Vector3 position = isFirstPerson
            ? target.position + firstPersonOffset
            : ResolveThirdPersonPosition(rotation);

        transform.SetPositionAndRotation(position, rotation);
    }

    // Acerca la cámara al jugador si hay una pared entre ambos, para que no la atraviese.
    // Se ignoran los objetos con Rigidbody (enemigos, jugador) para que no empujen la cámara.
    private Vector3 ResolveThirdPersonPosition(Quaternion rotation)
    {
        Vector3 pivot = target.position + pivotOffset;
        Vector3 desired = target.position + rotation * thirdPersonOffset;

        Vector3 toCamera = desired - pivot;
        float distance = toCamera.magnitude;
        if (distance < 0.01f) return desired;

        Vector3 direction = toCamera / distance;
        int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, direction, hitBuffer, distance,
            ~0, QueryTriggerInteraction.Ignore);

        float closest = distance;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.collider.attachedRigidbody != null) continue;
            if (hit.distance <= 0f) continue;
            closest = Mathf.Min(closest, hit.distance);
        }

        return pivot + direction * closest;
    }
}
