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
    private static readonly int AttackParam = Animator.StringToHash("Attack");
    private static readonly int AttackSpeedParam = Animator.StringToHash("AttackSpeed");
    private static readonly int CastParam = Animator.StringToHash("Cast");
    private static readonly int DashParam = Animator.StringToHash("Dash");
    private static readonly int PowerUpParam = Animator.StringToHash("PowerUp");
    private static readonly int MoveXParam = Animator.StringToHash("MoveX");
    private static readonly int MoveYParam = Animator.StringToHash("MoveY");
    private static readonly int RunParam = Animator.StringToHash("Run");

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

    [Header("Movimiento en 4 direcciones (Frieren)")]
    [SerializeField, Tooltip("Velocidad (m/s) al caminar: con ella MoveX/MoveY llegan a 1 en el árbol de movimiento. Solo se usa si el controlador tiene MoveX y MoveY")]
    private float locomotionWalkSpeed = 2.5f;
    [SerializeField, Tooltip("Velocidad (m/s) corriendo: con ella el parámetro Run llega a 1 y se mezclan las animaciones rápidas")]
    private float locomotionRunSpeed = 5f;

    [Header("Primera persona")]
    [SerializeField, Tooltip("En primera persona oculta todo el cuerpo (la malla con huesos), no solo la cabeza. Para personajes sin brazos propios a la vista, como Frieren; las armas (MeshRenderer) siguen visibles")]
    private bool hideBodyInFirstPerson;
    [SerializeField, Tooltip("Huesos que se encogen en primera persona (cabeza, capa...). Para modelos de una sola malla, donde no se puede apagar solo la cabeza.")]
    private Transform[] firstPersonHiddenBones;
    [SerializeField, Tooltip("En primera persona, mover el cuerpo para que la cámara quede en 'cameraFromHead' respecto del hueso de la cabeza")]
    private bool alignHeadToCamera;
    [SerializeField, Tooltip("Dónde queda la cámara respecto del hueso de la cabeza, en el espacio del cuerpo (y = arriba, z = adelante)")]
    private Vector3 cameraFromHead = new Vector3(0f, 0.1f, 0.12f);
    [SerializeField, Tooltip("Inclinación extra del cuerpo en primera persona (grados; negativo = hacia atrás, sube los brazos en pantalla)")]
    private float firstPersonTilt = 0f;
    [SerializeField, Tooltip("Como 'cameraFromHead', pero mientras ataca (se mezcla con la salida de la capa de primera persona). En el golpe las manos pasan por delante de la cara: con la cámara más arriba no la tapan.")]
    private Vector3 attackCameraFromHead = new Vector3(0f, 0.1f, 0.12f);
    [SerializeField, Tooltip("Como 'firstPersonTilt', pero mientras ataca")]
    private float attackFirstPersonTilt = 0f;
    [SerializeField, Tooltip("Capa del Animator con la pose de primera persona (vacío = ninguna). Se apaga mientras ataca, para que se vea el golpe.")]
    private string firstPersonLayer = "";
    [SerializeField, Tooltip("Velocidad con la que entra y sale la capa de primera persona (peso por segundo)")]
    private float firstPersonLayerSpeed = 8f;

    [Header("Cuerpo a cuerpo")]
    [SerializeField, Tooltip("Al atacar, el cuerpo gira hacia el punto de la mira en vez de quedar paralelo a la cámara")]
    private bool faceAimWhenAttacking;
    [SerializeField, Tooltip("Hasta qué distancia (m) se busca lo que hay en la mira para girar hacia ello")]
    private float meleeAimRange = 15f;
    [SerializeField, Tooltip("Si la mira no toca nada: distancia (m) delante del jugador hacia la que gira")]
    private float meleeAimFallback = 3f;
    [SerializeField, Tooltip("Velocidad del giro hacia la mira (grados por segundo)")]
    private float aimTurnSpeed = 720f;
    [SerializeField, Tooltip("Giro máximo respecto a la cámara (grados)")]
    private float maxAimYaw = 60f;
    [SerializeField, Tooltip("Giro extra (grados, + = derecha) al atacar: el tajo de la animación no cae justo al frente del cuerpo")]
    private float attackYawOffset = 0f;

    private Rigidbody playerBody;
    private Camera cam;
    private CameraFollow cameraFollow;
    private PlayerHover hover;
    private Transform spine;
    private Transform chest;
    private Renderer[] skin;
    private bool inAir;
    private bool dead;
    private bool hasAttack;
    private bool hasCast;
    private bool hasDash;
    private bool hasPowerUp;
    private bool hasMove2D;
    private bool parametersCached;
    private float spinTimer = -1f;
    private float spinTotal;
    private float aimYaw;
    private Shooting shooting;
    private Transform head;
    private bool bonesHidden;
    private int firstPersonLayerIndex = -1;

    public bool IsDead => dead;

    private bool IsAttacking() =>
        hasAttack && (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") || animator.GetNextAnimatorStateInfo(0).IsName("Attack"));

    // Grados que hay que girar desde la dirección de la cámara para mirar al punto de la mira.
    private float AimYawOffset(Vector3 cameraFlat)
    {
        Vector3 point;
        if (shooting != null && shooting.TryGetHit(meleeAimRange, out RaycastHit hit))
        {
            point = hit.point;
        }
        else
        {
            float cameraToPlayer = Vector3.Dot(transform.position - cam.transform.position, cameraFlat.normalized);
            point = cam.transform.position + cam.transform.forward * (cameraToPlayer + meleeAimFallback);
        }

        Vector3 toPoint = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
        if (toPoint.sqrMagnitude < 0.04f) return 0f;

        return Mathf.Clamp(Vector3.SignedAngle(cameraFlat, toPoint, Vector3.up), -maxAimYaw, maxAimYaw);
    }

    private Transform FindBone(string boneName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == boneName) return t;
        return null;
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        playerBody = GetComponentInParent<Rigidbody>();
        shooting = GetComponentInParent<Shooting>();

        // GetBoneTransform lanza una excepción si el avatar no es humanoide (y eso dejaba el componente deshabilitado).
        if (animator.isHuman)
        {
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        // Un rig Generic (Frieren) no tiene huesos humanoides: se buscan por el nombre que traen en el modelo.
        if (spine == null) spine = FindBone("spine_02");
        if (chest == null) chest = FindBone("spine_03");
        if (head == null) head = FindBone("head");
        if (!string.IsNullOrEmpty(firstPersonLayer)) firstPersonLayerIndex = animator.GetLayerIndex(firstPersonLayer);
        CacheAnimatorParameters();

        // Solo el cuerpo: las pistolas (MeshRenderer) quedan siempre a la vista.
        skin = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    // Con un rig Generic (Frieren) el Animator todavía no expone sus parámetros en Awake ni en Start (la lista llega vacía): se leen
    // en cuanto aparecen, la primera vez que hace falta.
    private void EnsureParameters()
    {
        if (parametersCached || animator.parameterCount == 0) return;

        CacheAnimatorParameters();
    }

    private void CacheAnimatorParameters()
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        parametersCached = parameters.Length > 0;   // una lista vacía es "todavía no" (rig Generic), no "no tiene"
        hasAttack = System.Array.Exists(parameters, p => p.nameHash == AttackParam);
        hasCast = System.Array.Exists(parameters, p => p.nameHash == CastParam);
        hasDash = System.Array.Exists(parameters, p => p.nameHash == DashParam);
        hasPowerUp = System.Array.Exists(parameters, p => p.nameHash == PowerUpParam);
        hasMove2D = System.Array.Exists(parameters, p => p.nameHash == MoveXParam)
            && System.Array.Exists(parameters, p => p.nameHash == MoveYParam);
    }

    private void Update()
    {
        EnsureParameters();
        if (dead) return;

        bool first = cameraFollow != null && cameraFollow.IsFirstPerson;

        Vector3 velocity = playerBody != null ? playerBody.linearVelocity : Vector3.zero;
        float flatSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        // En primera persona no se ven las piernas y la caminata solo sacudiría las pistolas: el cuerpo se queda quieto.
        animator.SetFloat(SpeedParam, first ? flatSpeed * firstPersonWalkAmount : flatSpeed, 0.08f, Time.deltaTime);

        // Movimiento en 4 direcciones (Frieren): la velocidad en el espacio del cuerpo, que mira hacia donde mira la cámara.
        // MoveX/MoveY = dirección (módulo 0 a 1, 1 = ritmo de caminar) y Run = 0 caminando a 1 corriendo.
        if (hasMove2D)
        {
            Vector3 local = transform.InverseTransformDirection(new Vector3(velocity.x, 0f, velocity.z));
            Vector2 direction = flatSpeed > 0.01f ? new Vector2(local.x, local.z) / flatSpeed : Vector2.zero;
            float amount = Mathf.Clamp01(flatSpeed / Mathf.Max(0.1f, locomotionWalkSpeed));
            animator.SetFloat(MoveXParam, direction.x * amount, 0.1f, Time.deltaTime);
            animator.SetFloat(MoveYParam, direction.y * amount, 0.1f, Time.deltaTime);
            animator.SetFloat(RunParam, Mathf.InverseLerp(locomotionWalkSpeed, locomotionRunSpeed, flatSpeed), 0.15f, Time.deltaTime);
        }

        bool rising = velocity.y > jumpVelocity;
        if (rising && !inAir && !first) animator.SetTrigger(JumpParam);
        inAir = rising || (inAir && Mathf.Abs(velocity.y) > 0.2f);

        if (firstPersonLayerIndex >= 0)
        {
            float target = first && !IsAttacking() ? 1f : 0f;
            float weight = Mathf.MoveTowards(animator.GetLayerWeight(firstPersonLayerIndex), target, firstPersonLayerSpeed * Time.deltaTime);
            animator.SetLayerWeight(firstPersonLayerIndex, weight);
        }
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
        SetBonesHidden(first && !dead);

        if (dead) return;

        // De frente a donde mira la cámara, sin inclinarse. Los pies del modelo quedan sobre el suelo; los personajes que levitan
        // (Frieren) suben con su balanceo.
        if (hover == null) hover = GetComponentInParent<PlayerHover>();
        transform.localPosition = new Vector3(0f, FeetLocalY + (hover != null ? hover.Offset : 0f), 0f);
        Vector3 flat = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (flat.sqrMagnitude > 0.001f)
        {
            // Cuerpo a cuerpo: al atacar gira hacia lo que hay en la mira (la cámara va a un costado y, si no, el golpe cae al lado).
            float targetYaw = faceAimWhenAttacking && !first && IsAttacking()
                ? Mathf.Clamp(AimYawOffset(flat) + attackYawOffset, -maxAimYaw, maxAimYaw)
                : 0f;
            aimYaw = Mathf.MoveTowardsAngle(aimYaw, targetYaw, aimTurnSpeed * Time.deltaTime);

            // Giro del dash: una vuelta completa en tercera persona (en primera no se gira para no marear).
            float spinAngle = 0f;
            if (spinTimer >= 0f)
            {
                spinTimer += Time.deltaTime;
                if (spinTimer >= spinTotal) spinTimer = -1f;
                else if (!first) spinAngle = 360f * spinTimer / spinTotal;
            }

            transform.rotation = Quaternion.AngleAxis(aimYaw + spinAngle, Vector3.up) * Quaternion.LookRotation(flat, Vector3.up);
        }

        float pitch = Mathf.DeltaAngle(0f, cam.transform.eulerAngles.x) * pitchWeight;
        Vector3 axis = transform.right;

        if (first)
        {
            // Modelos más altos o bajos que la cámara: se mueve el cuerpo para que el ojo quede donde está la cámara.
            // Con capa de primera persona: en guardia (peso 1) una colocación y al atacar (peso 0) otra, mezcladas.
            float guard = firstPersonLayerIndex >= 0 ? animator.GetLayerWeight(firstPersonLayerIndex) : 1f;
            Vector3 fromHead = Vector3.Lerp(attackCameraFromHead, cameraFromHead, guard);
            float tilt = Mathf.Lerp(attackFirstPersonTilt, firstPersonTilt, guard);

            if (alignHeadToCamera && head != null)
            {
                Vector3 desired = cam.transform.position - transform.rotation * fromHead;
                transform.position += desired - head.position;
            }

            // Primera persona: el cuerpo entero gira alrededor del ojo, así las pistolas quedan siempre en el mismo sitio
            // de la pantalla (si solo se inclinara el torso, al mirar arriba se acercarían al ojo y se cortarían).
            transform.RotateAround(cam.transform.position, axis, pitch + tilt);
            return;
        }

        // Tercera persona: el torso se inclina con la vista (arriba/abajo), repartido entre la columna y el pecho.
        if (spine != null) spine.rotation = Quaternion.AngleAxis(pitch * 0.4f, axis) * spine.rotation;
        if (chest != null) chest.rotation = Quaternion.AngleAxis(pitch * 0.6f, axis) * chest.rotation;
    }

    // Con una sola malla no se puede apagar solo la cabeza: se encogen sus huesos (el Animator Humanoid no toca la escala).
    private void SetBonesHidden(bool hidden)
    {
        if (firstPersonHiddenBones == null || hidden == bonesHidden) return;

        bonesHidden = hidden;
        foreach (Transform bone in firstPersonHiddenBones)
            if (bone != null) bone.localScale = hidden ? Vector3.one * 0.001f : Vector3.one;
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
            r.enabled = visible || (!isHead && !hideBodyInFirstPerson);
            if (r.shadowCastingMode != shadows) r.shadowCastingMode = shadows;
        }
    }

    public void PlayShoot(int barrel)
    {
        if (!dead) animator.SetTrigger(barrel == 0 ? ShootLeftParam : ShootRightParam);
    }

    /// <summary>Ataque cuerpo a cuerpo (el corte de Guts). Solo animación; no hace nada si el controlador no tiene el parámetro "Attack". "speed" acelera el corte (1 = velocidad del clip).</summary>
    public void PlayAttack(float speed = 1f)
    {
        EnsureParameters();
        if (dead || !hasAttack) return;

        animator.SetFloat(AttackSpeedParam, Mathf.Max(0.1f, speed));
        animator.SetTrigger(AttackParam);
    }

    /// <summary>Pose agachada del dash de Guts (dura lo que el dash y levantarse). No hace nada si el controlador no tiene "Dash".</summary>
    public void PlayDash()
    {
        EnsureParameters();
        if (dead || !hasDash) return;
        animator.SetTrigger(DashParam);
    }

    /// <summary>Gesto de la transformación (la armadura Berserker). No hace nada si el controlador no tiene "PowerUp".</summary>
    public void PlayPowerUp()
    {
        EnsureParameters();
        if (dead || !hasPowerUp) return;
        animator.SetTrigger(PowerUpParam);
    }

    /// <summary>Da una vuelta completa sobre el eje vertical durante unos segundos (el giro del dash). En primera persona no gira.</summary>
    public void BeginSpin(float seconds)
    {
        spinTotal = Mathf.Max(0.05f, seconds);
        spinTimer = 0f;
    }

    public void EndSpin() => spinTimer = -1f;

    /// <summary>Lanzar una habilidad con la mano (la llamarada de Guts). Solo animación; no hace nada si el controlador no tiene el parámetro "Cast".</summary>
    public void PlayCast()
    {
        EnsureParameters();
        if (dead || !hasCast) return;
        animator.SetTrigger(CastParam);
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
        if (firstPersonLayerIndex >= 0) animator.SetLayerWeight(firstPersonLayerIndex, 0f); // si no, la pose tapa la caída
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.applyRootMotion = true; // la caída baja y mueve la cadera: eso viene de la raíz del clip
        animator.SetBool(DeadParam, true);
    }
}
