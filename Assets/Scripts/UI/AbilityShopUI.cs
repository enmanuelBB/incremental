using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Estación de mejora de habilidades: se gastan los puntos de personaje (1 por nivel) en subir el rango de cada
/// habilidad y el nivel de sangrado del personaje activo. Se arma sola por código. Solo se abre antes de la
/// primera oleada (lo garantiza InteractableStation).
/// </summary>
public class AbilityShopUI : MenuPanel
{
    public static AbilityShopUI Instance { get; private set; }

    private struct Row
    {
        public GameObject Root;
        public Image Icon;
        public TMP_Text Info;
        public Button Button;
        public TMP_Text ButtonLabel;
    }

    private TMP_Text titleText;
    private TMP_Text xpText;
    private TMP_Text pointsText;
    private TMP_Text emptyText;
    private readonly Row[] abilityRows = new Row[Progression.AbilitySlots];
    private Row bleedRow;
    private bool pendingFirstChoice;
    private bool firstChoice;

    protected override void Awake()
    {
        Instance = this;
        BuildUi();
        base.Awake();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    protected override void OnOpened()
    {
        firstChoice = pendingFirstChoice;
        pendingFirstChoice = false;
        Refresh();
    }

    /// <summary>
    /// Al elegir un personaje que está en nivel 1 y todavía no aprendió ninguna habilidad, abre la estación para que
    /// escoja la principal con su primer punto. Si ya eligió antes (o no tiene habilidades) no hace nada.
    /// </summary>
    public void OpenIfFirstChoice()
    {
        if (IsOpen || Shooting.Instance == null) return;

        CharacterDefinition character = Shooting.Instance.Character;
        if (character == null || character.abilities == null || character.abilities.Length == 0) return;

        CharacterSave save = SaveSystem.Data.GetCharacter(character.Id);
        if (save.level != 1 || Progression.PointsAvailable(save) <= 0) return;
        foreach (int rank in save.abilityRanks) if (rank > 0) return;

        pendingFirstChoice = true;
        Open();
    }

    private void BuildUi()
    {
        Transform canvas = UiKit.FindCanvas();
        Image background = UiKit.Box("AbilityShopPanel", canvas, UiKit.PanelColor);
        UiKit.Stretch(background.rectTransform);
        panel = background.gameObject;
        Transform root = background.transform;

        titleText = UiKit.Label("Title", root, "", 48f, TextAlignmentOptions.Center, Color.white);
        UiKit.Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400f, 64f));
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);

        xpText = UiKit.Label("Xp", root, "", 28f, TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Place(xpText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1400f, 40f));
        xpText.rectTransform.pivot = new Vector2(0.5f, 1f);

        pointsText = UiKit.Label("Points", root, "", 40f, TextAlignmentOptions.Center, UiKit.Gold);
        UiKit.Place(pointsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -155f), new Vector2(1400f, 56f));
        pointsText.rectTransform.pivot = new Vector2(0.5f, 1f);

        RectTransform list = UiKit.Rect("Rows", root);
        UiKit.Place(list, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1500f, 700f));
        list.pivot = new Vector2(0.5f, 0.5f);
        var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < abilityRows.Length; i++)
        {
            int slot = i;
            abilityRows[i] = BuildRow(list, "AbilityRow" + (i + 1), () => UpgradeAbility(slot));
        }
        bleedRow = BuildRow(list, "BleedRow", UpgradeBleed);

        emptyText = UiKit.Label("Empty", root, "Las habilidades de este personaje todavía no se pueden mejorar.", 32f,
            TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Place(emptyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(1400f, 60f));
        emptyText.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        TMP_Text footer = UiKit.Label("Footer", root,
            "1 punto por nivel. Se gastan solo antes de empezar la primera oleada.", 24f, TextAlignmentOptions.Center, UiKit.Muted);
        UiKit.Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1400f, 36f));
        footer.rectTransform.pivot = new Vector2(0.5f, 0f);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        BuildDebugButtons(root);
#endif

        Button close = UiKit.TextButton("CloseButton", root, "X", 40f, Close, out _);
        UiKit.Place((RectTransform)close.transform, new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(80f, 80f));
        close.transform.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Solo desarrollo: probar la progresión sin jugar horas. Arriba a la derecha de la estación.
    private void BuildDebugButtons(Transform root)
    {
        Button levelUp = UiKit.TextButton("DebugLevelUp", root, "Subir 1 nivel", 28f, () => DebugAction(true), out _);
        UiKit.Place((RectTransform)levelUp.transform, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(260f, 70f));
        ((RectTransform)levelUp.transform).pivot = new Vector2(1f, 1f);

        Button reset = UiKit.TextButton("DebugReset", root, "Volver a nivel 1", 28f, () => DebugAction(false), out _);
        UiKit.Place((RectTransform)reset.transform, new Vector2(1f, 1f), new Vector2(-30f, -115f), new Vector2(260f, 70f));
        ((RectTransform)reset.transform).pivot = new Vector2(1f, 1f);
    }

    private void DebugAction(bool levelUp)
    {
        if (ProgressionManager.Instance == null) return;

        if (levelUp) ProgressionManager.Instance.DebugLevelUp();
        else ProgressionManager.Instance.DebugResetLevel();

        Refresh();
    }
#endif

    private static Row BuildRow(Transform parent, string name, UnityEngine.Events.UnityAction onClick)
    {
        Image back = UiKit.Box(name, parent, UiKit.RowColor);
        var layoutElement = back.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 150f;

        var group = back.gameObject.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(20, 20, 10, 10);
        group.spacing = 24f;
        group.childAlignment = TextAnchor.MiddleLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        Image icon = UiKit.Box("Icon", back.transform, Color.white);
        icon.preserveAspect = true;
        var iconSize = icon.gameObject.AddComponent<LayoutElement>();
        iconSize.preferredWidth = 110f;
        iconSize.preferredHeight = 110f;

        TMP_Text info = UiKit.Label("Info", back.transform, "", 28f, TextAlignmentOptions.MidlineLeft, Color.white);
        var infoSize = info.gameObject.AddComponent<LayoutElement>();
        infoSize.flexibleWidth = 1f;
        infoSize.preferredHeight = 130f;

        Button button = UiKit.TextButton("UpgradeButton", back.transform, "", 30f, onClick, out TMP_Text buttonLabel);
        var buttonSize = button.gameObject.AddComponent<LayoutElement>();
        buttonSize.preferredWidth = 320f;
        buttonSize.preferredHeight = 90f;

        return new Row { Root = back.gameObject, Icon = icon, Info = info, Button = button, ButtonLabel = buttonLabel };
    }

    private CharacterDefinition Character => Shooting.Instance.Character;
    private CharacterSave Save => SaveSystem.Data.GetCharacter(Character.Id);

    private static bool AppliesBleed(CharacterDefinition character)
    {
        if (character.startingWeapons == null) return false;
        foreach (WeaponDefinition weapon in character.startingWeapons)
            if (weapon != null && weapon.AppliesBleed) return true;
        return false;
    }

    private void Refresh()
    {
        CharacterDefinition character = Character;
        CharacterSave save = Save;

        int availablePoints = Progression.PointsAvailable(save);
        titleText.text = (firstChoice && availablePoints > 0 ? "Elige tu habilidad principal" : "Mejora de habilidades")
            + " · " + character.displayName;
        xpText.text = save.level >= Progression.MaxLevel
            ? "Nivel " + save.level + " (máximo)"
            : "Nivel " + save.level + "   ·   XP " + save.xp + " / " + Progression.XpForNextLevel(save.level);

        int points = Progression.PointsAvailable(save);
        pointsText.text = "Puntos disponibles: " + points;

        int shown = 0;
        for (int i = 0; i < abilityRows.Length; i++)
        {
            AbilityDefinition ability = character.abilities != null && i < character.abilities.Length ? character.abilities[i] : null;
            abilityRows[i].Root.SetActive(ability != null);
            if (ability == null) continue;

            shown++;
            RefreshAbilityRow(abilityRows[i], ability, i, save);
        }

        bool bleeds = AppliesBleed(character);
        bleedRow.Root.SetActive(bleeds);
        if (bleeds) RefreshBleedRow(save);

        emptyText.gameObject.SetActive(shown == 0);

        FocusFirstButton();
    }

    private static void RefreshAbilityRow(Row row, AbilityDefinition ability, int slot, CharacterSave save)
    {
        int rank = save.abilityRanks[slot];
        int max = Progression.MaxRank(ability.kind);

        row.Icon.sprite = ability.icon;
        row.Icon.enabled = ability.icon != null;
        row.Icon.color = rank > 0 ? Color.white : new Color(1f, 1f, 1f, 0.45f);

        string next = rank >= max ? "Rango máximo" : "Siguiente: " + ability.DescribeRank(rank + 1);
        row.Info.text = "<b>" + ability.abilityName + "</b>   Rango " + rank + "/" + max
            + "\nAhora: " + ability.DescribeRank(rank)
            + "\n" + next;

        UpgradeBlock block = Progression.CanUpgradeAbility(save, slot, ability.kind);
        SetButton(row, block, block == UpgradeBlock.LevelTooLow ? Progression.LevelRequiredForRank(ability.kind, rank + 1) : 0);
    }

    private void RefreshBleedRow(CharacterSave save)
    {
        int level = save.bleedLevel;
        bleedRow.Icon.enabled = false;

        string next = level >= Progression.MaxBleedLevel ? "Nivel máximo" : "Siguiente: tope de " + (BleedStacks.CapForLevel(level + 1)) + " pilas";
        bleedRow.Info.text = "<b>Sangrado</b>   Nivel " + level + "/" + Progression.MaxBleedLevel
            + "\nAhora: tope de " + BleedStacks.CapForLevel(level) + " pilas por enemigo"
            + "\n" + next;

        SetButton(bleedRow, Progression.CanUpgradeBleed(save), 0);
    }

    private static void SetButton(Row row, UpgradeBlock block, int requiredLevel)
    {
        row.Button.interactable = block == UpgradeBlock.None;

        switch (block)
        {
            case UpgradeBlock.None: row.ButtonLabel.text = "Subir (1 punto)"; break;
            case UpgradeBlock.NoPoints: row.ButtonLabel.text = "Sin puntos"; break;
            case UpgradeBlock.LevelTooLow: row.ButtonLabel.text = "Requiere nivel " + requiredLevel; break;
            default: row.ButtonLabel.text = "MAX"; break;
        }
    }

    // Con teclado o mando hace falta tener un botón seleccionado para poder navegar.
    private void FocusFirstButton()
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        foreach (Row row in abilityRows)
        {
            if (row.Root.activeSelf && row.Button.interactable)
            {
                EventSystem.current.SetSelectedGameObject(row.Button.gameObject);
                return;
            }
        }
        if (bleedRow.Root.activeSelf && bleedRow.Button.interactable)
            EventSystem.current.SetSelectedGameObject(bleedRow.Button.gameObject);
    }

    private void UpgradeAbility(int slot)
    {
        AbilityDefinition ability = Character.abilities[slot];
        if (!Progression.TryUpgradeAbility(Save, slot, ability.kind)) return;

        AfterSpend();
    }

    private void UpgradeBleed()
    {
        if (!Progression.TryUpgradeBleed(Save)) return;

        AfterSpend();
    }

    private void AfterSpend()
    {
        SaveSystem.Save();
        Shooting.Instance.RefreshAbilityHud();
        if (ProgressionManager.Instance != null) ProgressionManager.Instance.Refresh();
        Refresh();
    }
}
