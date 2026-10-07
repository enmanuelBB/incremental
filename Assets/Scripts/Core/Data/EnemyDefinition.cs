using UnityEngine;

/// <summary>
/// Datos de un tipo de enemigo. Se crea desde Assets > Create > Game > Enemy.
/// El prefab aporta el modelo, el NavMeshAgent y el EnemyAI; todo lo demás sale de aquí,
/// así varios tipos pueden compartir prefab.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy")]
public class EnemyDefinition : GameDefinition
{
    public string displayName = "Enemigo";
    public GameObject prefab;

    [Header("Jefe")]
    public EnemyTier tier = EnemyTier.Normal;
    [Tooltip("Obsoleto: usa 'tier'. Si está marcado, cuenta como jefe igualmente")]
    public bool isBoss;
    [Tooltip("Puntos del árbol del personaje que da al morir (jefes y minijefes)")]
    public int treePointsReward;
    [Tooltip("Habilidades del jefe; se ejecutan con BossController (el prefab del jefe debe llevarlo)")]
    public BossAbility[] abilities = new BossAbility[0];

    /// <summary>Minijefe o jefe.</summary>
    public bool IsBoss => tier != EnemyTier.Normal || isBoss;

    [Header("Stats")]
    public float speed = 2f;
    public int maxHealth = 30;
    public int moneyReward = 10;
    [Tooltip("Experiencia que da al morir (la gana el personaje que se está jugando)")]
    public int xpReward = 10;

    [Header("Ataque")]
    public int damageToBase = 10;
    public int damageToPlayer = 10;
    public float damageInterval = 1f;
    [Tooltip("Distancia desde el borde del enemigo al borde del objetivo para poder atacar")]
    public float attackReach = 0.6f;

    [Header("Ataque a distancia")]
    [Tooltip("Ataca lanzando proyectiles desde 'attackReach' metros en vez de golpear de cerca")]
    public bool ranged;
    public float projectileSpeed = 11f;
    public float projectileSize = 0.4f;
    public Color projectileColor = new Color(0.4f, 1f, 0.3f);

    [Header("Vuelo y apilado")]
    [Tooltip("Altura a la que flota sobre el suelo (0 = camina). Los voladores no se apilan")]
    public float flyHeight;
    [Tooltip("Se sube encima de otros enemigos cuando se amontonan (los jefes no trepan, pero sirven de apoyo)")]
    public bool climbsOthers = true;

    /// <summary>Vuela: ni trepa ni sirve de apoyo.</summary>
    public bool Flies => flyHeight > 0f;

    [Header("Persecución")]
    public float detectionRange = 8f;
    public float chaseGiveUpTime = 5f;
    public float giveUpCooldown = 3f;
    [Tooltip("Si el jugador queda 'detrás' respecto a la base, el enemigo lo ignora")]
    public float directionTolerance = 0f;
    public float repathInterval = 0.25f;
}
