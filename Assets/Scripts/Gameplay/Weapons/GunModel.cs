using UnityEngine;

/// <summary>
/// Una pistola con el modelo de Blender (<c>Pistola_L</c>/<c>Pistola_R</c>). Al disparar, el nodo "Recoil" retrocede
/// y se levanta, y la corredera ("Slide_*") retrocede un poco más; después todo vuelve suave a su sitio.
/// Este componente va en un objeto SIN rotación ni escala que contiene el FBX (el FBX trae el root girado 270° y
/// con escala 100), y las distancias se calculan en el espacio de este objeto: el cañón apunta a su +Z.
/// </summary>
public class GunModel : MonoBehaviour
{
    [SerializeField] private Transform recoilNode;
    [SerializeField] private Transform slide;
    [SerializeField] private Transform muzzle;
    [SerializeField, Tooltip("-1 = pistola izquierda, 1 = derecha (de qué lado de la mira se coloca)")]
    private float side = 1f;

    private Vector3 recoilRestPosition;
    private Quaternion recoilRestRotation;
    private Vector3 slideRestPosition;

    private RecoilSettings current;
    private float kick; // 1 justo al disparar, baja a 0

    public Transform Muzzle => muzzle;
    public float Side => side;

    private bool restCaptured;

    private void Awake() => CaptureRest();

    private void CaptureRest()
    {
        if (restCaptured) return;
        restCaptured = true;

        if (recoilNode != null)
        {
            recoilRestPosition = recoilNode.localPosition;
            recoilRestRotation = recoilNode.localRotation;
        }
        if (slide != null) slideRestPosition = slide.localPosition;
    }

    public void Kick(RecoilSettings settings)
    {
        CaptureRest();
        current = settings;
        kick = 1f;
        Apply();
    }

    private void Update()
    {
        if (kick <= 0f) return;

        kick = Mathf.Max(0f, kick - Time.deltaTime / Mathf.Max(0.01f, current.recoverTime));
        Apply();
    }

    private void Apply()
    {
        // Sale de golpe y vuelve suave: la curva al cuadrado hace que el retroceso se note al principio.
        float amount = kick * kick;

        if (recoilNode != null)
        {
            Transform parent = recoilNode.parent;
            Vector3 back = parent.InverseTransformVector(-transform.forward * (current.kickBack * amount));
            Quaternion lift = Quaternion.AngleAxis(-current.kickUp * amount, transform.right);

            recoilNode.localPosition = recoilRestPosition + back;
            recoilNode.localRotation = Quaternion.Inverse(parent.rotation) * lift * parent.rotation * recoilRestRotation;
        }
        if (slide != null)
            slide.localPosition = slideRestPosition + slide.parent.InverseTransformVector(-transform.forward * (current.slideBack * amount));
    }
}
