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

    private void LateUpdate()
    {
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
