using UnityEngine;

/// <summary>
/// Espada de un personaje cuerpo a cuerpo (Guts). Solo datos: no tiene modelo, el del personaje ya lleva la espada.
/// Se crea desde Assets > Create > Game > Sword. Usa los mismos 3 espacios de mejora que las armas, con otro significado:
///   Cadencia (FireRate): segundos entre golpes (lento al inicio, baja con la mejora).
///   Aturdir (Reload): probabilidad de aturdir a cada enemigo golpeado (+step por nivel).
///   Daño (Damage): suma daño por nivel.
/// El alcance del golpe es el campo heredado "range".
/// </summary>
[CreateAssetMenu(fileName = "NewSword", menuName = "Game/Sword")]
public class SwordDefinition : WeaponDefinition
{
    [Header("Golpe")]
    [Tooltip("Apertura del cono frente al jugador, en grados")]
    public float arcDegrees = 120f;

    [Min(0f)]
    [Tooltip("Segundos desde el clic hasta que cae el daño (que coincida con el corte de la animación)")]
    public float hitDelay = 0.12f;

    [Min(0.1f)]
    [Tooltip("Multiplicador de la animación de ataque: más alto = el corte se ve más rápido")]
    public float attackAnimSpeed = 2.6f;

    [Header("Aturdimiento")]
    [Tooltip("Cuánto dura el aturdimiento")]
    public float stunSeconds = 1.5f;

    [Range(0f, 1f), Tooltip("Tope de la probabilidad de aturdir (tienda + árbol): 0,3 = 30%")]
    public float stunChanceCap = 0.30f;

    [Header("Furia")]
    [Min(0f)]
    [Tooltip("Tamaño de la barra de Furia. 0 = el arma no tiene Furia")]
    public float furyMax = 100f;

    [Min(0f)]
    [Tooltip("Furia que gana cada enemigo golpeado por el golpe básico")]
    public float furyPerEnemyHit = 5f;

    [Min(0f)]
    [Tooltip("Tope de Furia que puede dar un solo golpe, golpee a cuantos golpee")]
    public float furyMaxPerSwing = 20f;

    [Min(1f)]
    [Tooltip("Multiplicador de daño del golpe potenciado (Furia llena). Además aturde seguro a todos los golpeados")]
    public float furyDamageMultiplier = 2f;

    public override bool UsesAmmo => false;
    public override bool AppliesBleed => false;

    public override string UpgradeLabel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate: return "Cadencia";
            case UpgradeType.Reload: return "Aturdir";
            case UpgradeType.Bleed: return "Sangrado";
            default: return "Daño";
        }
    }

    /// <summary>Probabilidad de aturdir (0 a 1) con el nivel de Aturdir y el bono del árbol.</summary>
    public float StunChanceAt(int level, float treeBonus = 0f) =>
        Mathf.Clamp(level * reloadUpgrade.step + treeBonus, 0f, stunChanceCap);
}
