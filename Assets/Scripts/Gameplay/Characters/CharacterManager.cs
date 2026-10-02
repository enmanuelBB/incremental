using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Elenco de personajes y personaje activo. Al empezar la escena aplica el último elegido (o el primero disponible)
/// y abre el menú de selección. Aplicar un personaje le pone sus armas, vida, velocidad y el objeto que lleva en la mano.
/// Las reglas de desbloqueo viven en CharacterRules (Game.Core).
/// </summary>
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [SerializeField, Tooltip("Todos los personajes del juego, incluidos los bloqueados y los huecos reservados")]
    private CharacterDefinition[] roster;

    [SerializeField] private Shooting shooting;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField, Tooltip("Menú que se abre al empezar la escena")]
    private CharacterSelectUI menu;

    private List<CharacterDefinition> ordered = new List<CharacterDefinition>();
    private GameObject heldItem;

    public IReadOnlyList<CharacterDefinition> Roster => ordered;
    public CharacterDefinition Current { get; private set; }

    private void Awake()
    {
        Instance = this;
        ordered = roster.Where(c => c != null).OrderBy(c => c.menuOrder).ToList();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        CharacterDefinition selected = CharacterRules.ResolveSelected(ordered, SaveSystem.Data);
        if (selected != null) Apply(selected);

        // Solo se elige personaje antes de la primera horda.
        if (menu != null && !WaveManager.Instance.HasStarted) menu.Open();
    }

    /// <summary>Elige el personaje (si está disponible) y lo aplica. Solo antes de que empiece la partida.</summary>
    public bool TrySelect(CharacterDefinition def)
    {
        if (WaveManager.Instance.HasStarted) return false;
        if (!CharacterRules.Select(def, SaveSystem.Data)) return false;

        SaveSystem.Save();
        Apply(def);
        return true;
    }

    /// <summary>Desbloquea con dinero del juego. Cobra, guarda y avisa a la interfaz.</summary>
    public bool TryUnlock(CharacterDefinition def)
    {
        SaveData data = SaveSystem.Data;
        if (!CharacterRules.TryUnlockWithMoney(def, data)) return false;

        SaveSystem.Save();
        GameEvents.RaiseMoneyChanged(data.money);
        return true;
    }

    /// <summary>Solo para desarrollo (ver DebugCheats): desbloquea todos los personajes disponibles.</summary>
    public void DebugUnlockAll()
    {
        foreach (CharacterDefinition def in ordered) CharacterRules.Unlock(def, SaveSystem.Data);
        SaveSystem.Save();
    }

    private void Apply(CharacterDefinition def)
    {
        Current = def;

        shooting.SetCharacter(def);
        playerHealth.SetMaxHealth(def.maxHealth);
        playerMovement.SetSpeed(def.moveSpeed);

        if (heldItem != null) Destroy(heldItem);
        heldItem = null;
        shooting.Muzzle = null;

        if (def.heldItemPrefab != null)
        {
            heldItem = Instantiate(def.heldItemPrefab, shooting.transform);
            shooting.Muzzle = heldItem.transform.Find("Muzzle");
        }

        GameEvents.RaiseCharacterChanged(def);
    }
}
