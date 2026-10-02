using UnityEngine;

/// <summary>
/// Efectos visuales provisionales de las habilidades, creados por código (sin prefabs ni arte): niebla,
/// río de sangre y destello de explosión. Cuando haya arte final se reemplaza este archivo.
/// </summary>
public class AbilityVfx : MonoBehaviour
{
    private const int FlashCount = 8;

    private static AbilityVfx instance;
    private static Texture2D softCircle;

    private class Flash
    {
        public Transform transform;
        public Material material;
        public Color color;
        public float radius;
        public float timer = -1f;
    }

    private readonly Flash[] flashes = new Flash[FlashCount];
    private int nextFlash;

    [SerializeField, Tooltip("Cuánto dura el destello de una explosión")] private float flashSeconds = 0.25f;

    private static AbilityVfx Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("AbilityVfx").AddComponent<AbilityVfx>();
            return instance;
        }
    }

    private void Awake()
    {
        for (int i = 0; i < FlashCount; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "ExplosionFlash";
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(transform);
            sphere.SetActive(false);

            var flash = new Flash { transform = sphere.transform, material = NewMaterial(Color.white) };
            sphere.GetComponent<Renderer>().sharedMaterial = flash.material;
            flashes[i] = flash;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        foreach (Flash flash in flashes) if (flash != null && flash.material != null) Destroy(flash.material);
    }

    private void Update()
    {
        foreach (Flash flash in flashes)
        {
            if (flash.timer < 0f) continue;

            flash.timer += Time.deltaTime;
            float t = flash.timer / flashSeconds;
            if (t >= 1f)
            {
                flash.timer = -1f;
                flash.transform.gameObject.SetActive(false);
                continue;
            }

            float size = flash.radius * 2f * Mathf.Lerp(0.4f, 1f, t);
            flash.transform.localScale = Vector3.one * size;
            Color c = flash.color;
            c.a *= 1f - t;
            flash.material.color = c;
        }
    }

    /// <summary>Esfera roja translúcida que se expande y se desvanece en el punto de una explosión.</summary>
    public static void ExplosionFlash(Vector3 position, float radius)
    {
        AbilityVfx vfx = Instance;
        Flash flash = vfx.flashes[vfx.nextFlash];
        vfx.nextFlash = (vfx.nextFlash + 1) % FlashCount;

        flash.color = new Color(0.9f, 0.05f, 0.05f, 0.45f);
        flash.radius = radius;
        flash.timer = 0f;
        flash.transform.position = position;
        flash.transform.gameObject.SetActive(true);
    }

    /// <summary>Disco rojo plano en el suelo (río de sangre). Quien lo crea lo coloca cada frame.</summary>
    public static GameObject CreateRiver(float radius)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "BloodRiver";
        Destroy(disc.GetComponent<Collider>());
        disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        disc.GetComponent<Renderer>().sharedMaterial = NewMaterial(new Color(0.75f, 0.03f, 0.05f, 0.4f));
        disc.SetActive(false);
        return disc;
    }

    /// <summary>Humo oscuro alrededor del jugador mientras dura la niebla. Se enciende con Play y se corta con Stop.</summary>
    public static ParticleSystem CreateMist(Transform parent)
    {
        var go = new GameObject("MistVfx");
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.9f;
        main.startSpeed = 0.6f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startColor = new Color(0.12f, 0.1f, 0.14f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 45f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.7f;

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        Material material = NewMaterial(Color.white);
        material.mainTexture = SoftCircle();
        rend.sharedMaterial = material;

        return ps;
    }

    private static Material NewMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        return new Material(shader) { color = color };
    }

    private static Texture2D SoftCircle()
    {
        if (softCircle != null) return softCircle;

        const int size = 64;
        softCircle = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                softCircle.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        softCircle.Apply();
        return softCircle;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        softCircle = null;
    }
}
