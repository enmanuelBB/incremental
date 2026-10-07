using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zona rectangular (en el suelo) donde aparecen los enemigos de forma dispersa: cada uno sale en un punto al azar
/// dentro del rectángulo, ajustado al NavMesh. Se mueve y se redimensiona desde la escena; el rectángulo se ve en
/// el editor como un contorno rojo.
/// </summary>
public class SpawnZone : MonoBehaviour
{
    [SerializeField, Tooltip("Ancho (x) y largo (z) de la zona, en metros")]
    private Vector2 size = new Vector2(52f, 12f);

    [SerializeField, Tooltip("Hasta dónde se busca el NavMesh más cercano si el punto cae fuera de él")]
    private float navMeshSearchRadius = 6f;

    public Vector2 Size => size;

    /// <summary>Un punto al azar dentro de la zona, sobre el NavMesh si es posible.</summary>
    public Vector3 RandomPoint()
    {
        Vector2 flat = SpawnArea.PointIn(new Vector2(transform.position.x, transform.position.z), size, Random.value, Random.value);
        Vector3 point = new Vector3(flat.x, transform.position.y, flat.y);

        return NavMesh.SamplePosition(point, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas) ? hit.position : point;
    }

    /// <summary>El punto ajustado al NavMesh si es posible (cada puesto de una manada).</summary>
    public Vector3 OnNavMesh(Vector3 point)
    {
        return NavMesh.SamplePosition(point, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas) ? hit.position : point;
    }

    /// <summary>El centro de la zona, sobre el NavMesh si es posible (por donde sale el jefe).</summary>
    public Vector3 CenterPoint()
    {
        Vector3 center = transform.position;
        return NavMesh.SamplePosition(center, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas) ? hit.position : center;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Abs(size.x), 0.1f, Mathf.Abs(size.y)));
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.12f);
        Gizmos.DrawCube(transform.position, new Vector3(Mathf.Abs(size.x), 0.05f, Mathf.Abs(size.y)));
    }
}
