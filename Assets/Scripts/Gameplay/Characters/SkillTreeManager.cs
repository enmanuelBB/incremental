using UnityEngine;

/// <summary>
/// Árbol de mejoras del personaje activo. Gana puntos al completar cada oleada, compra y reinicia nodos con las
/// reglas de SkillTreeRules, y entrega los bonos ya sumados (Bonuses) a quien los necesite: vida, velocidad,
/// daño y habilidades. Los puntos y los nodos son de cada personaje y se guardan con él.
/// </summary>
public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }

    private CharacterDefinition Current => Shooting.Instance != null ? Shooting.Instance.Character : null;

    /// <summary>Árbol del personaje activo (null si no tiene).</summary>
    public SkillTreeDefinition Tree => Current != null ? Current.skillTree : null;

    /// <summary>Guardado del personaje activo (null si todavía no hay ninguno).</summary>
    public CharacterSave ActiveSave => Current != null ? SaveSystem.Data.GetCharacter(Current.Id) : null;

    /// <summary>Suma de los efectos de los nodos comprados; neutro si no hay árbol o no se compró nada.</summary>
    public TreeBonuses Bonuses { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.WaveCompleted += OnWaveCompleted;
        GameEvents.CharacterChanged += OnCharacterChanged;
        GameEvents.BossDefeated += OnBossDefeated;
    }

    private void OnDisable()
    {
        GameEvents.WaveCompleted -= OnWaveCompleted;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        GameEvents.BossDefeated -= OnBossDefeated;
        SaveSystem.Save();
    }

    private void Start()
    {
        Recompute();
        PublishPoints();
    }

    private void OnCharacterChanged(CharacterDefinition character) => PublishPoints();

    // Completar una oleada da puntos al personaje que se está jugando; si muere a mitad de oleada, esa no da nada.
    private void OnWaveCompleted(int wave)
    {
        CharacterSave save = ActiveSave;
        if (save == null || Tree == null) return;

        int points = SkillTreeRules.PointsForWave(wave);
        save.skillPoints += points;
        SaveSystem.Save();

        GameEvents.RaiseSkillPointsGained(Current.displayName, points);
        PublishPoints();
    }

    // Los jefes y minijefes dan puntos extra del árbol al morir.
    private void OnBossDefeated(int points)
    {
        CharacterSave save = ActiveSave;
        if (save == null || Tree == null || points <= 0) return;

        save.skillPoints += points;
        SaveSystem.Save();

        GameEvents.RaiseSkillPointsGained(Current.displayName, points);
        PublishPoints();
    }

    /// <summary>Vuelve a calcular los bonos con los nodos comprados del personaje activo y avisa a las habilidades.</summary>
    public void Recompute()
    {
        CharacterSave save = ActiveSave;
        Bonuses = save != null ? SkillTreeRules.Compute(Tree, save.skillNodes) : TreeBonuses.None;

        if (Shooting.Instance != null) Shooting.Instance.RefreshBuild();
    }

    public bool Buy(string nodeId)
    {
        CharacterSave save = ActiveSave;
        if (save == null || !SkillTreeRules.TryBuy(Tree, save, nodeId)) return false;

        AfterChange();
        return true;
    }

    /// <summary>Devuelve todos los puntos gastados y vacía el árbol del personaje activo. Gratis.</summary>
    public int ResetTree()
    {
        CharacterSave save = ActiveSave;
        if (save == null) return 0;

        int refunded = SkillTreeRules.Reset(Tree, save);
        AfterChange();
        return refunded;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Solo desarrollo: suma puntos al saldo del personaje activo.</summary>
    public void DebugAddPoints(int amount)
    {
        CharacterSave save = ActiveSave;
        if (save == null || Tree == null || amount <= 0) return;

        save.skillPoints += amount;
        AfterChange();
    }

    /// <summary>Solo desarrollo: deja el árbol del personaje activo en cero (sin puntos y sin nodos comprados).</summary>
    public void DebugClearTree()
    {
        CharacterSave save = ActiveSave;
        if (save == null) return;

        save.skillPoints = 0;
        save.skillNodes.Clear();
        AfterChange();
    }
#endif

    private void AfterChange()
    {
        SaveSystem.Save();
        Recompute();
        if (CharacterManager.Instance != null) CharacterManager.Instance.ReapplyStats();
        PublishPoints();
    }

    private void PublishPoints()
    {
        CharacterSave save = ActiveSave;
        GameEvents.RaiseSkillPointsChanged(save != null ? save.skillPoints : 0);
    }
}
