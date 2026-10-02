using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 3f;
    [SerializeField, Tooltip("Multiplicador de velocidad mientras está en el aire")] private float jumpDistance = 1.5f;
    [SerializeField, Tooltip("Gravedad extra al caer, para que el salto no se sienta flotante")] private float fallMultiplier = 3f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector3 movement;
    private bool jumpRequested;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
    }

    private void Update()
    {
        if (GameState.InputBlocked)
        {
            movement = Vector3.zero;
            jumpRequested = false;
            return;
        }

        GameInput input = GameInput.Instance;
        Vector2 move = input.Move;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        movement = Vector3.ClampMagnitude(forward * move.y + right * move.x, 1f);

        if (input.JumpPressed && isGrounded) jumpRequested = true;
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        float currentSpeed = isGrounded ? speed : speed * jumpDistance;
        Vector3 velocity = rb.linearVelocity;
        velocity.x = movement.x * currentSpeed;
        velocity.z = movement.z * currentSpeed;
        rb.linearVelocity = velocity;

        if (jumpRequested)
        {
            jumpRequested = false;
            if (isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;
            }
        }

        if (rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    // Detecta suelo bajo los pies en cada paso de física, así caminar fuera de un borde
    // ya no deja "grounded" y no permite saltar en el aire.
    private bool CheckGrounded()
    {
        Bounds bounds = capsule.bounds;
        float radius = bounds.extents.x * 0.9f;
        float castDistance = bounds.extents.y - radius + groundCheckDistance;

        return Physics.SphereCast(bounds.center, radius, Vector3.down, out _, castDistance,
            ~0, QueryTriggerInteraction.Ignore);
    }
}
