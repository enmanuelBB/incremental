using System;
using UnityEngine;

/// <summary>Qué cambia un nodo del árbol. Los valores en fracción (0,03 = +3%) o en unidades según el tipo.</summary>
public enum SkillEffectType
{
    [Tooltip("Suma vida máxima")] MaxHealth,
    [Tooltip("Fracción de velocidad de movimiento que suma (0,03 = +3%)")] MoveSpeedPercent,
    [Tooltip("Fracción de daño de las balas que suma (0,03 = +3%)")] DamagePercent,

    [Tooltip("Q: balas extra por disparo pesado")] HeavyShotExtraBullets,
    [Tooltip("Q: cargas extra (lanzamientos seguidos)")] HeavyShotExtraCharges,
    [Tooltip("Q: segundos que baja el enfriamiento")] HeavyShotCooldown,

    [Tooltip("E: pilas de sangrado a cada enemigo que atraviesa")] MistBleedOnPass,
    [Tooltip("E: fracción de velocidad que quita a cada enemigo que atraviesa (0,3 = -30%)")] MistSlow,
    [Tooltip("E: segundos de duración que suma")] MistDuration,
    [Tooltip("E: segundos que baja el enfriamiento")] MistCooldown,

    [Tooltip("F: segundos de duración que suma")] UltDuration,
    [Tooltip("F: segundos que baja el enfriamiento")] UltCooldown,
    [Tooltip("F: fracción de robo de vida que suma (0,15 = +15 puntos)")] UltLifeSteal,

    [Tooltip("Guts: suma a la probabilidad de aturdir (0,03 = +3 puntos)")] StunChance,
    [Tooltip("Guts: fracción extra de daño a enemigos aturdidos (0,15 = +15%)")] StunnedDamagePercent,
    [Tooltip("Guts: fracción extra de Furia por golpe (0,25 = +25%)")] FuryGainPercent,
    [Tooltip("Guts: suma a la fracción de vida que curan las almas (0,01 = +1 punto)")] SoulHealBonus,
    [Tooltip("Guts Q: segundos extra de quemadura")] FlameBurnSeconds,
    [Tooltip("Guts Q: grados extra del cono")] FlameCone,
    [Tooltip("Guts Q: fracción extra de daño de la quemadura (0,5 = +50%)")] FlameBurnDamagePercent,
    [Tooltip("Guts Q: metros extra de alcance")] FlameRange,
    [Tooltip("Guts E: cargas extra")] DashExtraCharges,
    [Tooltip("Guts E: disparos extra tras el dash")] DashExtraShots,
    [Tooltip("Guts E: metros extra de dash")] DashDistance,
    [Tooltip("Guts E: segundos que baja el enfriamiento")] DashCooldown,
    [Tooltip("Guts F: suma al multiplicador de daño de la armadura (0,1 = +10 puntos)")] BerserkDamage,
    [Tooltip("Guts F: resta al multiplicador de daño recibido de la armadura (0,1 = -10 puntos)")] BerserkDamageTaken,
    [Tooltip("Guts F: segundos extra de aturdimiento del rugido")] RoarStun,
    [Tooltip("Guts F: metros extra de radio del rugido")] RoarRadius
}

[Serializable]
public struct SkillEffect
{
    public SkillEffectType type;
    public float value;
}

/// <summary>Un nodo del árbol. Se compra una vez; es comprable si es raíz o algún vecino conectado ya está comprado.</summary>
[Serializable]
public class SkillNode
{
    [Tooltip("Identificador único dentro del árbol; es lo que se guarda en el archivo de guardado, no lo cambies una vez publicado")]
    public string id;
    public string displayName;
    [TextArea(1, 3)] public string description;
    [Min(0)] public int cost = 1;
    [Tooltip("Posición en la pantalla, en unidades de cuadrícula (x a la derecha, y hacia arriba)")]
    public Vector2 position;
    [Tooltip("Ids de los nodos vecinos. La conexión se usa en los dos sentidos al dibujar y al desbloquear")]
    public string[] connections = new string[0];
    [Tooltip("Los nodos raíz se pueden comprar sin tener ningún vecino")]
    public bool isRoot;
    public SkillEffect[] effects = new SkillEffect[0];
}

/// <summary>
/// Árbol de mejoras de un personaje. Se crea desde Assets > Create > Game > Skill Tree y se enlaza desde
/// CharacterDefinition. Para otro personaje basta crear otro asset.
/// </summary>
[CreateAssetMenu(fileName = "NewSkillTree", menuName = "Game/Skill Tree")]
public class SkillTreeDefinition : GameDefinition
{
    public SkillNode[] nodes = new SkillNode[0];

    /// <summary>El nodo con ese id, o null si no existe.</summary>
    public SkillNode Find(string nodeId)
    {
        if (nodes == null || string.IsNullOrEmpty(nodeId)) return null;

        foreach (SkillNode node in nodes)
            if (node != null && node.id == nodeId) return node;
        return null;
    }
}
