using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Proyectil de un enemigo (el Lanzador o el jefe Tirador): una esfera luminosa que viaja en línea recta hacia donde
/// estará su objetivo (el jugador o la base) y le hace daño al tocarlo. Se crea por código (no necesita prefab) y se
/// destruye al impactar o a los pocos segundos.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private const float Lifetime = 6f;

    public static readonly Color BossColor = new Color(1f, 0.35f, 0.1f);

    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    private Collider target;
    private Health health;
    private Vector3 velocity;
    private int damage;
    private float hitRadius;
    private float diesAt;

    /// <summary>Lanza un proyectil al collider del objetivo, adelantándose a donde estará si se mueve.</summary>
    public static void Launch(Vector3 origin, Collider target, float speed, int damage, Color color, float size = 0.55f)
    {
        if (target == null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "EnemyProjectile";
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * size;

        Collider ownCollider = go.GetComponent<Collider>();
        if (ownCollider != null) Destroy(ownCollider); // el impacto se calcula por distancia, no por física

        Renderer rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = GetMaterial(color);
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var projectile = go.AddComponent<EnemyProjectile>();
        projectile.target = target;
        projectile.health = target.GetComponentInParent<Health>();
        projectile.damage = damage;
        projectile.hitRadius = size * 0.5f + 0.1f;   // la esfera toca la superficie del objetivo (se puede esquivar)
        projectile.diesAt = Time.time + Lifetime;

        // Apunta al punto del objetivo más cercano, con un poco de adelanto: la distancia dividida por la velocidad es
        // el tiempo que tarda en llegar.
        Vector3 aim = AimPoint(target, origin);
        Rigidbody body = target.attachedRigidbody;
        if (body != null && !body.isKinematic && speed > 0.1f)
            aim += new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z) * (Vector3.Distance(origin, aim) / speed);

        projectile.velocity = (aim - origin).normalized * speed;
    }

    // El jugador es una cápsula: se apunta al pecho. La base es grande: se apunta a su punto más cercano.
    private static Vector3 AimPoint(Collider target, Vector3 from)
    {
        Vector3 chest = target.bounds.center;
        chest.y = Mathf.Min(chest.y, target.bounds.min.y + 1f);
        return target.GetComponent<PlayerHealth>() != null ? chest : target.ClosestPoint(new Vector3(from.x, chest.y, from.z));
    }

    private static Material GetMaterial(Color color)
    {
        if (materials.TryGetValue(color, out Material material) && material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        material = new Material(shader) { name = "EnemyProjectile" };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        materials[color] = material;
        return material;
    }

    private void Update()
    {
        if (GameState.IsGameOver || Time.time >= diesAt || target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += velocity * Time.deltaTime;

        Vector3 nearest = target.ClosestPoint(transform.position);
        if ((nearest - transform.position).sqrMagnitude > hitRadius * hitRadius) return;

        if (health != null) health.TakeDamage(damage);
        Destroy(gameObject);
    }
}
