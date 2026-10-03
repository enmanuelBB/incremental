using UnityEngine;

/// <summary>
/// Proyectil de un jefe: una esfera luminosa que viaja en línea recta hacia donde estará el jugador y le hace daño al
/// tocarlo. Se crea por código (no necesita prefab) y se destruye al impactar o a los pocos segundos.
/// </summary>
public class BossProjectile : MonoBehaviour
{
    private const float Lifetime = 6f;
    private const float HitRadius = 0.9f;

    private static Material sharedMaterial;

    private Transform player;
    private Vector3 velocity;
    private int damage;
    private float diesAt;

    /// <summary>Lanza un proyectil hacia el jugador, adelantándose a donde estará por su velocidad actual.</summary>
    public static void Launch(Vector3 origin, Transform target, float speed, int damage)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "BossProjectile";
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 0.55f;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider); // el impacto se calcula por distancia, no por física

        Renderer rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = GetMaterial();
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var projectile = go.AddComponent<BossProjectile>();
        projectile.player = target;
        projectile.damage = damage;
        projectile.diesAt = Time.time + Lifetime;

        // Apunta con un poco de adelanto: la distancia dividida por la velocidad es el tiempo que tarda en llegar.
        Vector3 aim = target.position + Vector3.up * 0.8f;
        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body != null && speed > 0.1f)
            aim += new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z) * (Vector3.Distance(origin, aim) / speed);

        Vector3 direction = (aim - origin).normalized;
        projectile.velocity = direction * speed;
    }

    private static Material GetMaterial()
    {
        if (sharedMaterial != null) return sharedMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        sharedMaterial = new Material(shader) { name = "BossProjectile" };
        Color color = new Color(1f, 0.35f, 0.1f);
        if (sharedMaterial.HasProperty("_BaseColor")) sharedMaterial.SetColor("_BaseColor", color);
        if (sharedMaterial.HasProperty("_Color")) sharedMaterial.SetColor("_Color", color);
        return sharedMaterial;
    }

    private void Update()
    {
        if (GameState.IsGameOver || Time.time >= diesAt || player == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += velocity * Time.deltaTime;

        Vector3 toPlayer = (player.position + Vector3.up * 0.8f) - transform.position;
        if (toPlayer.magnitude > HitRadius) return;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(damage);
        Destroy(gameObject);
    }
}
