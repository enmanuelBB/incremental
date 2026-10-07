using UnityEngine;

/// <summary>
/// Apilado de enemigos estilo Megabonk: cada fotograma decide quién está subido encima de quién (EnemyStackRules) y
/// sube o baja a cada uno suavemente. Hay uno por escena; EnemyPool lo agrega solo.
/// </summary>
public class EnemyCrowd : MonoBehaviour
{
    [SerializeField, Tooltip("Se superponen (y uno trepa) si la distancia en planta es menor que esto x la suma de los radios")]
    private float overlapFactor = 0.8f;
    [SerializeField, Tooltip("Altura máxima de un montón, en metros (unos 3 pisos de enemigos normales)")]
    private float maxLift = 5f;
    [SerializeField, Tooltip("Segundos que el que llega a lo más alto vuelve a esquivar a los demás (para que el montón se ensanche)")]
    private float blockedAtTopSeconds = 1.5f;
    [SerializeField, Tooltip("Velocidad a la que se corre de lado el que no cabe arriba de un montón, en m/s")]
    private float pushSpeed = 3f;
    [SerializeField, Tooltip("Velocidad al trepar, en m/s")]
    private float climbSpeed = 6f;
    [SerializeField, Tooltip("Velocidad al caer, en m/s")]
    private float fallSpeed = 10f;

    private StackBody[] bodies = new StackBody[64];
    private float[] lifts = new float[64];
    private int[] blockedBy = new int[64];

    private void LateUpdate()
    {
        var enemies = EnemyAI.Active;
        int count = enemies.Count;
        if (count == 0) return;

        if (bodies.Length < count)
        {
            bodies = new StackBody[Mathf.NextPowerOfTwo(count)];
            lifts = new float[bodies.Length];
            blockedBy = new int[bodies.Length];
        }

        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = enemies[i];
            Vector3 position = enemy.transform.position;
            bodies[i] = new StackBody
            {
                Position = new Vector2(position.x, position.z),
                Radius = enemy.BodyRadius,
                Height = enemy.BodyHeight,
                Lift = enemy.StackLift,
                Priority = enemy.DistanceToGoal,
                CanClimb = enemy.CanClimb,
                CanSupport = enemy.CanSupport
            };
        }

        EnemyStackRules.TargetLifts(bodies, count, overlapFactor, maxLift, lifts, blockedBy);

        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = enemies[i];
            enemy.StackLift = EnemyStackRules.Step(enemy.StackLift, lifts[i], Time.deltaTime, climbSpeed, fallSpeed);

            // No cabe más arriba: se corre de lado, lejos de la columna llena, y unos segundos vuelve a esquivar.
            int blocker = blockedBy[i];
            if (blocker < 0) continue;

            Vector2 away = bodies[i].Position - bodies[blocker].Position;
            if (away.sqrMagnitude < 0.0001f) away = Random.insideUnitCircle;
            away = away.normalized * (pushSpeed * Time.deltaTime);
            enemy.MoveForced(new Vector3(away.x, 0f, away.y));
            enemy.BlockClimb(blockedAtTopSeconds);
        }
    }
}
