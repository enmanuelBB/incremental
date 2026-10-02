using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menú de selección de personaje: lista a la izquierda, estadísticas a la derecha y un botón de acción
/// (Jugar, Desbloquear con costo, o deshabilitado con el motivo). Se arma solo a partir del elenco de
/// CharacterManager, así agregar personajes no requiere tocar la escena.
/// </summary>
public class CharacterSelectUI : MenuPanel
{
    private static readonly Color Normal = Color.white;
    private static readonly Color Browsed = new Color(1f, 0.85f, 0.3f);
    private static readonly Color Dimmed = new Color(1f, 1f, 1f, 0.5f);

    [Header("Lista")]
    [SerializeField] private Transform listContainer;
    [SerializeField, Tooltip("Botón modelo (desactivado en la escena); se clona uno por personaje")]
    private Button itemTemplate;

    [Header("Detalle")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;

    private readonly List<GameObject> items = new List<GameObject>();
    private readonly Dictionary<CharacterDefinition, TMP_Text> labels = new Dictionary<CharacterDefinition, TMP_Text>();
    private CharacterDefinition browsed;

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.MoneyChanged += OnMoneyChanged;
        if (actionButton != null) actionButton.onClick.AddListener(OnActionClicked);
    }

    protected override void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        if (actionButton != null) actionButton.onClick.RemoveListener(OnActionClicked);
        base.OnDisable();
    }

    protected override void OnOpened()
    {
        Rebuild();
        Browse(CharacterManager.Instance.Current);
        FocusBrowsedItem();
    }

    private void OnMoneyChanged(int total)
    {
        if (IsOpen && browsed != null) Browse(browsed);
    }

    private void Rebuild()
    {
        foreach (GameObject item in items) Destroy(item);
        items.Clear();
        labels.Clear();

        foreach (CharacterDefinition def in CharacterManager.Instance.Roster)
        {
            CharacterDefinition character = def;

            Button button = Instantiate(itemTemplate, listContainer);
            button.gameObject.SetActive(true);
            button.onClick.AddListener(() => Browse(character));

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.text = ListLabel(character);

            items.Add(button.gameObject);
            labels[character] = label;
        }
    }

    private static string ListLabel(CharacterDefinition def)
    {
        SaveData data = SaveSystem.Data;

        switch (CharacterRules.StatusOf(def, data))
        {
            case CharacterStatus.ComingSoon: return def.displayName;
            case CharacterStatus.Locked:
                return def.unlockKind == CharacterUnlockKind.Money
                    ? def.displayName + "  ($" + def.unlockPrice + ")"
                    : def.displayName + "  (bloqueado)";
            default:
                return def == CharacterManager.Instance.Current ? def.displayName + "  (actual)" : def.displayName;
        }
    }

    private void Browse(CharacterDefinition def)
    {
        browsed = def;
        SaveData data = SaveSystem.Data;
        CharacterStatus status = CharacterRules.StatusOf(def, data);

        moneyText.text = "Dinero: $" + data.money;
        nameText.text = def.displayName;
        descriptionText.text = def.description;

        foreach (KeyValuePair<CharacterDefinition, TMP_Text> pair in labels)
        {
            pair.Value.text = ListLabel(pair.Key);
            pair.Value.color = pair.Key == def
                ? Browsed
                : CharacterRules.StatusOf(pair.Key, data) == CharacterStatus.Unlocked ? Normal : Dimmed;
        }

        if (status == CharacterStatus.ComingSoon)
        {
            statsText.text = "";
            statusText.text = "Próximamente";
            SetAction("Próximamente", false);
            return;
        }

        CharacterSave progress = data.characters.Find(c => c.id == def.Id);
        var stats = new StringBuilder();
        foreach (StatLine line in CharacterInfo.Describe(def, progress))
            stats.Append("<b>").Append(line.Label).Append(":</b> ").AppendLine(line.Value);
        statsText.text = stats.ToString();

        switch (status)
        {
            case CharacterStatus.Unlocked:
                statusText.text = def == CharacterManager.Instance.Current ? "Personaje actual" : "Disponible";
                SetAction("Jugar con " + def.displayName, true);
                break;

            case CharacterStatus.Locked when def.unlockKind == CharacterUnlockKind.Money:
                bool enough = data.money >= def.unlockPrice;
                statusText.text = "Bloqueado: cuesta $" + def.unlockPrice;
                SetAction(enough ? "Desbloquear ($" + def.unlockPrice + ")" : "Desbloquear ($" + def.unlockPrice + "): falta dinero", enough);
                break;

            default:
                statusText.text = string.IsNullOrEmpty(def.unlockHint) ? "Bloqueado" : def.unlockHint;
                SetAction("Bloqueado", false);
                break;
        }
    }

    private void SetAction(string text, bool interactable)
    {
        actionLabel.text = text;
        actionButton.interactable = interactable;
    }

    private void OnActionClicked()
    {
        if (browsed == null) return;

        CharacterManager manager = CharacterManager.Instance;

        switch (CharacterRules.StatusOf(browsed, SaveSystem.Data))
        {
            case CharacterStatus.Unlocked:
                if (manager.TrySelect(browsed)) Close();
                break;

            case CharacterStatus.Locked:
                if (manager.TryUnlock(browsed)) Browse(browsed);
                break;
        }
    }

    // Con teclado o gamepad hace falta algo seleccionado al abrir, si no el menú no responde.
    private void FocusBrowsedItem()
    {
        if (EventSystem.current == null || items.Count == 0) return;

        int index = Mathf.Max(0, CharacterManager.Instance.Roster.ToList().IndexOf(browsed));
        EventSystem.current.SetSelectedGameObject(items[Mathf.Min(index, items.Count - 1)]);
    }
}
