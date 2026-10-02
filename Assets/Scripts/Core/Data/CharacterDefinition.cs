using UnityEngine;

public enum CharacterAvailability
{
    Playable,
    [Tooltip("Aparece en el menú como un hueco reservado, sin estadísticas")]
    ComingSoon
}

public enum CharacterUnlockKind
{
    Free,
    Money,
    Mission,
    Event
}

/// <summary>
/// Datos de un personaje jugable. Se crea desde Assets > Create > Game > Character.
/// Vida, velocidad y armas son de cada personaje, así que tanques, asesinos y magos
/// se distinguen solo con datos.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character")]
public class CharacterDefinition : GameDefinition
{
    public string displayName = "Personaje";
    [TextArea(2, 4)] public string description;

    [Tooltip("Orden en el menú de selección (menor = más arriba)")]
    public int menuOrder;

    public CharacterAvailability availability = CharacterAvailability.Playable;

    [Header("Stats")]
    public int maxHealth = 100;
    public float moveSpeed = 5f;

    [Header("Desbloqueo")]
    public CharacterUnlockKind unlockKind = CharacterUnlockKind.Free;
    [Tooltip("Solo aplica a 'Money': costo para desbloquearlo con dinero del juego")]
    public int unlockPrice;
    [Tooltip("Texto que se muestra si se desbloquea por misión o evento")]
    public string unlockHint;

    [Header("Equipo")]
    [Tooltip("Armas del personaje, en el orden de los atajos 1, 2, 3...")]
    public WeaponDefinition[] startingWeapons;

    [Tooltip("Habilidades en el orden de las casillas del HUD: la 1.ª con Q, la 2.ª con E, la 3.ª con F. Máximo 3. Frieren aún usa la habilidad de su bastón.")]
    public AbilityDefinition[] abilities;

    [Tooltip("Objeto que lleva en la mano (por ejemplo el bastón). Si tiene un hijo llamado 'Muzzle', de ahí salen los disparos.")]
    public GameObject heldItemPrefab;
}
