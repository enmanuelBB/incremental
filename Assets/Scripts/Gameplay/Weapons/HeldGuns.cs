using UnityEngine;

/// <summary>
/// Las armas que el personaje lleva en la mano (en Alucard, dos pistolas: posición 0 = izquierda, 1 = derecha).
/// Cada una retrocede por separado con los valores de <see cref="RecoilSettings"/> del arma.
/// Las pistolas siguen la cámara: se colocan a los lados de la mira y apuntan al punto que apunta el jugador.
/// </summary>
[DefaultExecutionOrder(100)] // después de CameraFollow, que mueve la cámara en LateUpdate
public class HeldGuns : MonoBehaviour
{
    [SerializeField] private GunModel[] guns = new GunModel[0];

    [Header("Primera persona")]
    [SerializeField, Tooltip("Posición respecto a la cámara (x se refleja según el lado de cada pistola)")]
    private Vector3 firstPersonOffset = new Vector3(0.3f, -0.25f, 0.55f);
    [SerializeField] private float firstPersonScale = 1.6f;

    [Header("Tercera persona")]
    [SerializeField, Tooltip("Posición respecto al pecho del jugador, en la dirección de la cámara")]
    private Vector3 thirdPersonOffset = new Vector3(0.8f, -0.1f, 0.9f);
    [SerializeField] private float thirdPersonScale = 2.5f;
    [SerializeField] private float chestHeight = 1.4f;

    [SerializeField, Tooltip("Distancia del punto de la mira al que apuntan las pistolas")]
    private float aimDistance = 40f;

    private Transform player;
    private Camera cam;
    private CameraFollow cameraFollow;

    public int Count => guns.Length;

    private void Awake()
    {
        player = transform.parent;
        // Las pistolas se colocan en el mundo cada frame; sin padre no heredan la escala del jugador.
        transform.SetParent(null, true);
    }

    /// <summary>Boca del cañón indicado (null si no hay modelo para ese cañón).</summary>
    public Transform MuzzleOf(int barrel) =>
        barrel >= 0 && barrel < guns.Length && guns[barrel] != null ? guns[barrel].Muzzle : null;

    public void Fire(int barrel, RecoilSettings recoil)
    {
        if (barrel >= 0 && barrel < guns.Length && guns[barrel] != null) guns[barrel].Kick(recoil);
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
        Vector3 offset = first ? firstPersonOffset : thirdPersonOffset;
        float scale = first ? firstPersonScale : thirdPersonScale;

        Vector3 anchor = first || player == null ? cam.transform.position : player.position + Vector3.up * chestHeight;
        Quaternion view = cam.transform.rotation;
        Vector3 aimPoint = cam.transform.position + cam.transform.forward * aimDistance;

        foreach (GunModel gun in guns)
        {
            if (gun == null) continue;

            Transform t = gun.transform;
            t.position = anchor + view * new Vector3(offset.x * gun.Side, offset.y, offset.z);
            t.localScale = Vector3.one * scale;

            Vector3 toAim = aimPoint - t.position;
            t.rotation = toAim.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toAim, view * Vector3.up) : view;
        }
    }
}
