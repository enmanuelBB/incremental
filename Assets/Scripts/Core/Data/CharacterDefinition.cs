using UnityEngine;

/// <summary>
/// Datos de un personaje jugable. Se crea desde Assets > Create > Game > Character.
/// Los stats propios de cada personaje (vida, velocidad, nivel) se agregan aquí cuando se diseñen.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character")]
public class CharacterDefinition : GameDefinition
{
    public string displayName = "Personaje";

    [Tooltip("Costo para desbloquearlo con dinero del juego (0 = disponible desde el inicio)")]
    public int unlockPrice;

    [Tooltip("Armas del personaje, en el orden de los atajos 1, 2, 3...")]
    public WeaponDefinition[] startingWeapons;
}
