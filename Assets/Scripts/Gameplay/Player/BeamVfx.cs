using UnityEngine;

/// <summary>
/// Trazo de un disparo mágico (línea que aparece y se desvanece). Provisional: sin partículas ni shaders propios.
/// </summary>
public class BeamVfx : MonoBehaviour
{
    private LineRenderer line;
    private float baseWidth;
    private float startedAt;
    private float duration;

    public static BeamVfx Create(string name)
    {
        return new GameObject(name).AddComponent<BeamVfx>();
    }

    private void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 4;

        // Sprites/Default pinta con el color del vértice y no necesita luces.
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.enabled = false;
    }

    public void Show(Vector3 from, Vector3 to, Color color, float width, float seconds)
    {
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, color.a * 0.4f);

        baseWidth = width;
        duration = seconds;
        startedAt = Time.unscaledTime;
        ApplyWidth(1f);
        line.enabled = true;
    }

    private void Update()
    {
        if (!line.enabled) return;

        float t = (Time.unscaledTime - startedAt) / duration;
        if (t >= 1f)
        {
            line.enabled = false;
            return;
        }

        ApplyWidth(1f - t);
    }

    private void ApplyWidth(float scale)
    {
        line.startWidth = baseWidth * scale;
        line.endWidth = baseWidth * scale * 0.6f;
    }
}
