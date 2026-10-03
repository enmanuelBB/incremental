using UnityEngine;

/// <summary>
/// El cuerpo animado del jugador (el modelo de Blender con su Animator). Mira hacia donde mira la cámara, inclina el
/// torso con la vista para que las pistolas apunten a la mira, y mueve los parámetros del Animator: velocidad, salto,
/// disparo, recarga y muerte. En primera persona oculta el cuerpo (las pistolas siguen a la vista).
/// Va en la raíz del prefab del cuerpo, que es hijo del jugador (el cilindro con la física).
/// </summary>
[DefaultExecutionOrder(110)] // después de CameraFollow (LateUpdate mueve la cámara) y de HeldGuns
public class PlayerBody : MonoBehaviour
{
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int JumpParam = Animator.StringToHash("Jump");
    private static readonly int ShootLeftParam = Animator.StringToHash("ShootL");
    private static readonly int ShootRightParam = Animator.StringToHash("ShootR");
    private static readonly int ReloadParam = Animator.StringToHash("Reload");
    private static readonly int ReloadSpeedParam = Animator.StringToHash("ReloadSpeed");
    private static readonly int DeadParam = Animator.StringToHash("Dead");

    /// <summary>Altura local del cuerpo bajo el jugador: el cilindro mide 2 m con el centro a 1 m, y los pies del modelo están en su origen.</summary>
    public const float FeetLocalY = -1f;

    [SerializeField] private Animator animator;
    [SerializeField, Tooltip("Cuánto del giro vertical de la cámara toma el torso (el resto lo pone la cabeza al mirar)")]
    private float pitchWeight = 1f;
    [SerializeField, Tooltip("Velocidad vertical (m/s) a partir de la cual cuenta como salto")]
    private float jumpVelocity = 1.5f;
    [SerializeField, Tooltip("Duración, en segundos, de la animación Reload original")]
    private float reloadClipLength = 1.6f;
    [SerializeField, Range(0f, 1f), Tooltip("Cuánto de la animación de caminar se aplica en primera persona (0 = el cuerpo queda quieto y las pistolas no se mueven al caminar)")]
    private float firstPersonWalkAmount = 0f;

    private Rigidbody playerBody;
    private Camera cam;
    private CameraFollow cameraFollow;
    private Transform spine;
    private Transform chest;
    private Renderer[] skin;
    private bool inAir;
    private bool dead;

    public bool IsDead => dead;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        playerBody = GetComponentInParent<Rigidbody>();

        spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        chest = animator.GetBoneTransform(HumanBodyBones.Chest);

        // Solo el cuerpo: las pistolas (MeshRenderer) quedan siempre a la vista.
        skin = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    private void Update()
    {
        if (dead) return;

        bool first = cameraFollow != null && cameraFollow.IsFirstPerson;

        Vector3 velocity = playerBody != null ? playerBody.linearVelocity : Vector3.zero;
        float flatSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        // En primera persona no se ven las piernas y la caminata solo sacudiría las pistolas: el cuerpo se queda quieto.
        animator.SetFloat(SpeedParam, first ? flatSpeed * firstPersonWalkAmount : flatSpeed, 0.08f, Time.deltaTime);

        bool rising = velocity.y > jumpVelocity;
        if (rising && !inAir && !first) animator.SetTrigger(JumpParam);
        inAir = rising || (inAir && Mathf.Abs(velocity.y) > 0.2f);
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
            cameraFollow = cam.GetComponent<CameraFollow>();
        }

        bool first = cameraFollow != null && cameraFollow.IsFirstPerson;
        SetSkinVisible(!first || dead); // al morir la cámara rodea el cuerpo, aunque se jugara en primera persona

        if (dead) return;

        // De frente a donde mira la cámara, sin inclinarse.
        transform.localPosition = new Vector3(0f, FeetLocalY, 0f);
        Vector3 flat = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat, Vector3.up);

        float pitch = Mathf.DeltaAngle(0f, cam.transform.eulerAngles.x) * pitchWeight;
        Vector3 axis = transform.right;

        if (first)
        {
            // Primera persona: el cuerpo entero gira alrededor del ojo, así las pistolas quedan siempre en el mismo sitio
            // de la pantalla (si solo se inclinara el torso, al mirar arriba se acercarían al ojo y se cortarían).
            transform.RotateAround(cam.transform.position, axis, pitch);
            return;
        }

        // Tercera persona: el torso se inclina con la vista (arriba/abajo), repartido entre la columna y el pecho.
        if (spine != null) spine.rotation = Quaternion.AngleAxis(pitch * 0.4f, axis) * spine.rotation;
        if (chest != null) chest.rotation = Quaternion.AngleAxis(pitch * 0.6f, axis) * chest.rotation;
    }

    // En primera persona solo se ven los brazos y las pistolas: la cabeza se apaga (si no, se ve por dentro al mirar arriba)
    // y el cuerpo no proyecta sombra, porque gira con la vista y su sombra se vería torcerse.
    private void SetSkinVisible(bool visible)
    {
        UnityEngine.Rendering.ShadowCastingMode shadows = visible
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.Off;

        foreach (Renderer r in skin)
        {
            if (r == null) continue;

            bool isHead = r.name.Contains("Head");
            r.enabled = visible || !isHead;
            if (r.shadowCastingMode != shadows) r.shadowCastingMode = shadows;
        }
    }

    public void PlayShoot(int barrel)
    {
        if (!dead) animator.SetTrigger(barrel == 0 ? ShootLeftParam : ShootRightParam);
    }

    /// <summary>La animación de recarga se estira o acorta para durar lo mismo que la recarga real.</summary>
    public void PlayReload(float reloadSeconds)
    {
        if (dead) return;

        animator.SetFloat(ReloadSpeedParam, reloadClipLength / Mathf.Max(0.1f, reloadSeconds));
        animator.SetTrigger(ReloadParam);
    }

    /// <summary>Cae al suelo con la animación Death. Usa tiempo real, para que la cámara lenta no la estire.</summary>
    public void PlayDeath()
    {
        if (dead) return;

        dead = true;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.applyRootMotion = true; // la caída baja y mueve la cadera: eso viene de la raíz del clip
        animator.SetBool(DeadParam, true);
    }
}
