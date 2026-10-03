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

    [Tooltip("Solo una etiqueta por ahora; la lógica de jefes se agrega cuando se implementen")]
    public bool isBoss;

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

    [Header("Persecución")]
    public float detectionRange = 8f;
    public float chaseGiveUpTime = 5f;
    public float giveUpCooldown = 3f;
    [Tooltip("Si el jugador queda 'detrás' respecto a la base, el enemigo lo ignora")]
    public float directionTolerance = 0f;
    public float repathInterval = 0.25f;
}
