# Guts: árbol de habilidades — Plan de implementación

> **Para quien ejecute:** SUB-SKILL REQUERIDA: superpowers:executing-plans (el usuario eligió ejecución inline/Native). Los pasos usan casillas (`- [ ]`).
> **Regla del usuario: NO hacer commits.** Él hace los suyos.

**Goal:** El árbol de 39 nodos de Guts con sus efectos en el juego, el stun repartido 15% tienda + 15% árbol (tope 30%), la E con 2 cargas y salvas de 2 o 3 disparos, y la armadura sin reducción de drenaje.

**Architecture:** Efectos nuevos al final de `SkillEffectType` → campos de `TreeBonuses` → `SkillTreeRules.Apply`. Lógica pura con tests (`TargetPicker.PlanShots`, `TreeBonuses.ScaleVsStunned`, `StunChanceAt` con tope). El asset `Guts_Tree` se genera por script de editor y se valida con tests. Cada efecto se consulta donde actúa (`FlameBurst`, `GutsDash`, `BerserkArmor`, `Shooting`, `SoulSpawner`, `PlayerAbilities`).

**Spec:** `docs/superpowers/specs/2026-10-04-guts-skill-tree-design.md`

## Global Constraints
- Stun total máximo **30%**: tienda `3%` por nivel (5 niveles = 15%) + árbol `+15%` (5 nodos de 3%); tope `stunChanceCap = 0,30` en `SwordDefinition`.
- **Ningún efecto reduce el drenaje de la armadura.** `Armadura.asset` `drainPerRank = 0` (drenaje 2% en los 3 rangos).
- `SkillEffectType`: **solo agregar al final** (los valores guardados de Alucard no se mueven). Los ids de nodo del árbol de Guts no se cambian una vez publicados. No cambiar el guardado.
- E: `1 + DashExtraCharges` cargas; salva de `1 + DashExtraShots` disparos (máx. 3), 0,12 s entre disparos, **cada disparo a un enemigo distinto** (más cercano primero; los sobrantes repiten el más cercano), daño completo cada uno; Furia llena potencia **toda** la salva (x2) y gasta la barra una vez; sin objetivo no dispara ni gasta Furia.
- Daño a aturdidos: `x(1 + StunnedDamagePercent)` por enemigo con `EnemyAI.IsStunned`, en espada, Q y E.
- Los tests de EditMode solo ven `Game.Core`. Escribir archivos con la herramienta Write. Pasar `port: 7890` a las herramientas de Unity; tras editar scripts forzar `AssetDatabase.Refresh(ForceUpdate)` y esperar compilación (`unity_get_compilation_errors` con `isCompiling: false`; si dice "Queue session changed", reintentar).
- **Pruebas en Play:** respaldar `save.json` **de ese momento** antes y restaurarlo después (Unity fuera de Play y quieto). **Bucles con `EditorApplication.Step` con tope de pasos y cortando si `Time.time` no avanza.** Probar con **el rango máximo y el guardado real**. No dejar enemigos de vida enorme vivos. `Physics.SyncTransforms()` tras activar enemigos en la misma llamada. El asset del árbol se crea con `AssetDatabase.CreateAsset` (no editando YAML: un `m_Script` roto oculta la pestaña).
- Línea base: **476 tests pasan, 0 fallan**. Para correrlos: el ejecutor por reflexión de `docs/superpowers/plans/2026-10-04-guts-fury.md` (sección "Cómo correr los tests de EditMode"), con `filter` por clase.

## Review Focus
1. **Tope de stun:** con tienda al máximo + todo el árbol = exactamente 30%, y nunca más.
2. **La armadura nunca baja su drenaje** (ni por rango ni por árbol): 2% en los rangos 1, 2 y 3.
3. **Salvas con pocos enemigos:** 1 enemigo y 3 disparos → 3 sobre el mismo; el enemigo muere a mitad → los disparos que quedan no dan error.
4. **Cargas de la E:** dos dashes seguidos, el 3.º bloqueado hasta recuperar una carga; el HUD no se rompe con `Max > 1` en la casilla 2.
5. **Guardado de Guts con nodos:** `skillNodes` se guarda por personaje y no afecta a Alucard.

## Mapa de archivos
| Archivo | Acción |
|---|---|
| `Core/Data/SkillTreeDefinition.cs` | modificar (16 efectos al final de `SkillEffectType`) |
| `Core/Data/TreeBonuses.cs` | modificar (campos, `ScaleVsStunned`) |
| `Core/Data/SkillTreeRules.cs` | modificar (`Apply`) |
| `Core/Data/TargetPicker.cs` | modificar (`PlanShots`) |
| `Core/Data/SwordDefinition.cs` | modificar (`stunChanceCap`) |
| `Gameplay/Characters/SkillTreeManager.cs` | modificar (`Current`) |
| `Gameplay/Player/Shooting.cs`, `FlameBurst.cs`, `GutsDash.cs`, `BerserkArmor.cs`, `PlayerAbilities.cs`, `Characters/SoulSpawner.cs` | modificar (efectos) |
| `Assets/Data/Skills/Guts_Tree.asset`; `Espada.asset` (paso 0,03); `Armadura.asset` (`drainPerRank` 0); `Guts.asset` (`skillTree`) | crear / modificar (script de editor) |
| `Tests/EditMode/TargetPickerTests.cs`, `SkillTreeTests.cs`, `SwordDefinitionTests.cs`, `CharacterTests.cs`, `AbilityDefinitionTests.cs`, `GutsTreeAssetTests.cs` | modificar / crear |

---

### Task 1: Lógica pura con tests (efectos, bonos, salvas, tope de stun)

**Files:** Modify `SkillTreeDefinition.cs`, `TreeBonuses.cs`, `SkillTreeRules.cs`, `TargetPicker.cs`, `SwordDefinition.cs`; Tests.

**Interfaces — Produces:** 16 valores nuevos de `SkillEffectType`; campos de `TreeBonuses` (`StunnedDamagePercent`, `FuryGainPercent`, `SoulHealBonus`, `FlameBurnSecondsBonus`, `FlameConeBonus`, `FlameBurnDamagePercent`, `FlameRangeBonus`, `DashExtraCharges`, `DashExtraShots`, `DashDistanceBonus`, `DashCooldownReduction`, `BerserkDamageBonus`, `BerserkDamageTakenReduction`, `RoarStunBonus`, `RoarRadiusBonus`; ya existe `StunChanceBonus`); `TreeBonuses.ScaleVsStunned(int damage, bool targetStunned) : int`; `TargetPicker.PlanShots(Vector3 origin, IList<Vector3> candidates, float maxRange, int shots) : List<int>`; `SwordDefinition.stunChanceCap`.

- [ ] **Step 1: Tests que fallan.**
  - `TargetPickerTests.cs`: agregar
```csharp
    [Test]
    public void PlanShots_OneEnemy_AllShotsGoToIt()
    {
        var list = new List<Vector3> { new Vector3(4f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 0, 0, 0 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 3));
    }

    [Test]
    public void PlanShots_ThreeEnemies_AreHitNearestFirst()
    {
        var list = new List<Vector3> { new Vector3(9f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1, 2, 0 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 3));
    }

    [Test]
    public void PlanShots_FewerEnemiesThanShots_RepeatsTheNearest()
    {
        var list = new List<Vector3> { new Vector3(6f, 0f, 0f), new Vector3(3f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1, 0, 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 3));
    }

    [Test]
    public void PlanShots_MoreEnemiesThanShots_OnlyTheNearest()
    {
        var list = new List<Vector3> { new Vector3(9f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 1));
        CollectionAssert.AreEqual(new[] { 1, 2 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 2));
    }

    [Test]
    public void PlanShots_IgnoresTheOutOfRange()
    {
        var list = new List<Vector3> { new Vector3(40f, 0f, 0f), new Vector3(5f, 0f, 0f) };
        CollectionAssert.AreEqual(new[] { 1, 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 2));
    }

    [Test]
    public void PlanShots_NothingToHit_IsEmpty()
    {
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, new List<Vector3>(), 25f, 3).Count);
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, null, 25f, 3).Count);
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, new List<Vector3> { new Vector3(40f, 0f, 0f) }, 25f, 3).Count);
    }

    [TestCase(0)]
    [TestCase(-2)]
    public void PlanShots_NoShots_IsEmpty(int shots)
    {
        var list = new List<Vector3> { new Vector3(3f, 0f, 0f) };
        Assert.AreEqual(0, TargetPicker.PlanShots(Vector3.zero, list, 25f, shots).Count);
    }

    [Test]
    public void PlanShots_ATieKeepsTheListOrder()
    {
        var list = new List<Vector3> { new Vector3(4f, 0f, 0f), new Vector3(0f, 0f, 4f) };
        CollectionAssert.AreEqual(new[] { 0, 1 }, TargetPicker.PlanShots(Vector3.zero, list, 25f, 2));
    }
```
  - `SwordDefinitionTests.cs`: en `SetUp` cambiar `made.reloadUpgrade = new UpgradeStat { step = 0.06f, maxLevel = 5 };` por `step = 0.03f` y agregar `made.stunChanceCap = 0.30f;`. Reemplazar los tests de stun por:
```csharp
    [TestCase(0, 0f)]
    [TestCase(1, 0.03f)]
    [TestCase(5, 0.15f)]
    public void StunChance_GrowsThreePointsPerLevel_FifteenAtMax(int level, float expected)
    {
        Assert.AreEqual(expected, made.StunChanceAt(level), 1e-4f);
    }

    [Test]
    public void StunChance_AddsTheTreeBonus_ThirtyAtMax()
    {
        Assert.AreEqual(0.30f, made.StunChanceAt(5, 0.15f), 1e-4f);
    }

    [Test]
    public void StunChance_NeverExceedsTheThirtyPercentCap()
    {
        Assert.AreEqual(0.30f, made.StunChanceAt(5, 5f), 1e-4f);
        Assert.AreEqual(0.30f, made.StunChanceAt(5, 0.20f), 1e-4f);
    }
```
    y en `SwordAsset_Exists_WithTheDesignValues`: `Assert.AreEqual(0.30f, sword.StunChanceAt(sword.reloadUpgrade.maxLevel), 1e-4f);` → `0.15f`, y agregar `Assert.AreEqual(0.30f, sword.stunChanceCap, 1e-4f);`.
  - `CharacterTests.cs` (`Describe_Sword_...`): `sword.reloadUpgrade = new UpgradeStat { step = 0.06f, maxLevel = 5 };` → `step = 0.03f` y `"aturdir 30%"` → `"aturdir 15%"`.
  - `SkillTreeTests.cs`: agregar un test que compruebe cada efecto nuevo (leer antes cómo arma el árbol de prueba con `Node(...)`); por cada tipo, un árbol de un nodo raíz con `new SkillEffect { type = X, value = v }` y `SkillTreeRules.Compute(tree, new[] { "n" })` debe dejar el campo correcto con el valor `v` y los demás neutros. Además dos tests de `TreeBonuses.ScaleVsStunned`: `(100, true)` con `StunnedDamagePercent = 0.15f` → 115; `(100, false)` → 100; con 0 bono → igual.
  - `AbilityDefinitionTests.cs`: cambiar el helper `Berserk()` para que `drainPerRank = 0f` y el test `BerserkDrain_ShrinksWithRank` por `BerserkDrain_DoesNotShrinkWithRank` (casos (1,0.02),(2,0.02),(3,0.02)); `BerserkDrain_NeverNegative` se queda (con `drainPerRank = 0.05` explícito). En `EmbestidaAndArmaduraAssets_...` agregar `Assert.AreEqual(0f, armor.drainPerRank, 1e-4f);`.
- [ ] **Step 2: Verificar que fallan** (errores de compilación / asserts).
- [ ] **Step 3: Implementar.**
  - `SkillTreeDefinition.cs` — al final de `SkillEffectType` (agregar coma al último actual `UltLifeSteal`):
```csharp
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
```
  - `TreeBonuses.cs` — campos (después de `StunChanceBonus`):
```csharp
    public float StunnedDamagePercent;
    public float FuryGainPercent;
    public float SoulHealBonus;
    public float FlameBurnSecondsBonus;
    public float FlameConeBonus;
    public float FlameBurnDamagePercent;
    public float FlameRangeBonus;
    public int DashExtraCharges;
    public int DashExtraShots;
    public float DashDistanceBonus;
    public float DashCooldownReduction;
    public float BerserkDamageBonus;
    public float BerserkDamageTakenReduction;
    public float RoarStunBonus;
    public float RoarRadiusBonus;

    /// <summary>Daño a un enemigo con el bono de Guts a los aturdidos: x(1 + bono) si el objetivo está aturdido.</summary>
    public int ScaleVsStunned(int damage, bool targetStunned) =>
        targetStunned && StunnedDamagePercent > 0f ? Mathf.RoundToInt(damage * (1f + StunnedDamagePercent)) : damage;
```
    (agregar `using UnityEngine;` al inicio si falta.)
  - `SkillTreeRules.Apply` — casos nuevos:
```csharp
            case SkillEffectType.StunChance: b.StunChanceBonus += v; break;
            case SkillEffectType.StunnedDamagePercent: b.StunnedDamagePercent += v; break;
            case SkillEffectType.FuryGainPercent: b.FuryGainPercent += v; break;
            case SkillEffectType.SoulHealBonus: b.SoulHealBonus += v; break;
            case SkillEffectType.FlameBurnSeconds: b.FlameBurnSecondsBonus += v; break;
            case SkillEffectType.FlameCone: b.FlameConeBonus += v; break;
            case SkillEffectType.FlameBurnDamagePercent: b.FlameBurnDamagePercent += v; break;
            case SkillEffectType.FlameRange: b.FlameRangeBonus += v; break;
            case SkillEffectType.DashExtraCharges: b.DashExtraCharges += Mathf.RoundToInt(v); break;
            case SkillEffectType.DashExtraShots: b.DashExtraShots += Mathf.RoundToInt(v); break;
            case SkillEffectType.DashDistance: b.DashDistanceBonus += v; break;
            case SkillEffectType.DashCooldown: b.DashCooldownReduction += v; break;
            case SkillEffectType.BerserkDamage: b.BerserkDamageBonus += v; break;
            case SkillEffectType.BerserkDamageTaken: b.BerserkDamageTakenReduction += v; break;
            case SkillEffectType.RoarStun: b.RoarStunBonus += v; break;
            case SkillEffectType.RoarRadius: b.RoarRadiusBonus += v; break;
```
  - `TargetPicker.cs`:
```csharp
    /// <summary>
    /// Plan de una salva de disparos: los índices de los objetivos en orden, uno distinto por disparo del más cercano al más lejano
    /// (los que están al alcance); si hay menos objetivos que disparos, los que sobran repiten el más cercano. Vacío si no hay
    /// ninguno al alcance o no hay disparos. En un empate gana el orden de la lista.
    /// </summary>
    public static List<int> PlanShots(Vector3 origin, IList<Vector3> candidates, float maxRange, int shots)
    {
        var plan = new List<int>();
        if (shots <= 0 || candidates == null) return plan;

        float limit = maxRange * maxRange + 1e-6f;
        var inRange = new List<int>();
        var sqr = new float[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            float dx = candidates[i].x - origin.x;
            float dz = candidates[i].z - origin.z;
            sqr[i] = dx * dx + dz * dz;
            if (sqr[i] <= limit) inRange.Add(i);
        }

        if (inRange.Count == 0) return plan;

        inRange.Sort((a, b) => sqr[a] != sqr[b] ? sqr[a].CompareTo(sqr[b]) : a.CompareTo(b));
        for (int shot = 0; shot < shots; shot++) plan.Add(shot < inRange.Count ? inRange[shot] : inRange[0]);
        return plan;
    }
```
  - `SwordDefinition.cs` — campo y fórmula:
```csharp
    [Range(0f, 1f), Tooltip("Tope de la probabilidad de aturdir (tienda + árbol): 0,3 = 30%")]
    public float stunChanceCap = 0.30f;
```
    y `StunChanceAt` → `Mathf.Clamp(level * reloadUpgrade.step + treeBonus, 0f, stunChanceCap);`.
- [ ] **Step 4: Assets de datos** — `unity_execute_code`: `Espada.asset` `reloadUpgrade.step = 0.03f` y `stunChanceCap = 0.30f`; `Armadura.asset` `drainPerRank = 0f`. Guardar (`SetDirty` + `SaveAssets`).
- [ ] **Step 5: GREEN** — `filter` = `TargetPicker`, `SwordDefinition`, `SkillTree`, `CharacterInfo`, `AbilityDefinition`: `fallan=0`; luego la suite completa sin fallos.

### Task 2: El árbol `Guts_Tree` (asset + tests)

**Files:** Create `Assets/Data/Skills/Guts_Tree.asset` (script de editor); Modify `Guts.asset`; Test `Assets/Tests/EditMode/GutsTreeAssetTests.cs`.

- [ ] **Step 1: Test que falla** — `GutsTreeAssetTests.cs`:
```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

/// <summary>Valida el árbol real de Guts (el asset), para que un error de edición no llegue a la partida.</summary>
[TestFixture]
public class GutsTreeAssetTests
{
    private const string TreePath = "Assets/Data/Skills/Guts_Tree.asset";
    private const string CharacterPath = "Assets/Data/Characters/Guts.asset";

    private SkillTreeDefinition Tree => AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(TreePath);

    [Test]
    public void Tree_Exists_AndGutsLinksToIt()
    {
        Assert.IsNotNull(Tree, "Falta " + TreePath);
        var guts = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterPath);
        Assert.AreSame(Tree, guts.skillTree);
    }

    [Test]
    public void Tree_Has39Nodes_WithUniqueIds_AndTextForEach()
    {
        Assert.AreEqual(39, Tree.nodes.Length);
        List<string> ids = Tree.nodes.Select(n => n.id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count());
        Assert.IsTrue(Tree.nodes.All(n => !string.IsNullOrEmpty(n.id) && !string.IsNullOrEmpty(n.displayName) && !string.IsNullOrEmpty(n.description)));
        Assert.IsTrue(Tree.nodes.All(n => n.cost >= 1));
    }

    [Test]
    public void Tree_EveryConnectionPointsToAnExistingNode()
    {
        foreach (SkillNode node in Tree.nodes)
            foreach (string neighbour in node.connections)
                Assert.IsNotNull(Tree.Find(neighbour), node.id + " se conecta a '" + neighbour + "', que no existe");
    }

    [Test]
    public void Tree_HasOneRoot_AndEveryNodeIsReachableFromIt()
    {
        SkillNode[] roots = Tree.nodes.Where(n => n.isRoot).ToArray();
        Assert.AreEqual(1, roots.Length);

        var owned = new List<string>();
        bool grew = true;
        owned.Add(roots[0].id);
        while (grew)
        {
            grew = false;
            foreach (SkillNode node in Tree.nodes)
            {
                if (owned.Contains(node.id) || !SkillTreeRules.IsUnlocked(Tree, node, owned)) continue;
                owned.Add(node.id);
                grew = true;
            }
        }
        Assert.AreEqual(Tree.nodes.Length, owned.Count, "hay nodos que no se pueden alcanzar desde la raíz");
    }

    [Test]
    public void Tree_StunNodesAddUpToExactlyFifteenPercent()
    {
        float sum = Tree.nodes.SelectMany(n => n.effects).Where(e => e.type == SkillEffectType.StunChance).Sum(e => e.value);
        Assert.AreEqual(0.15f, sum, 1e-4f);

        var sword = AssetDatabase.LoadAssetAtPath<SwordDefinition>("Assets/Data/Weapons/Espada.asset");
        Assert.AreEqual(0.30f, sword.StunChanceAt(sword.reloadUpgrade.maxLevel, sum), 1e-4f, "tienda al máximo + árbol completo = 30%");
    }

    [Test]
    public void Tree_DashChainRequiresTheDoubleShotBeforeTheTripleShot()
    {
        SkillNode triple = Tree.Find("e3");
        SkillNode doubleShot = Tree.Find("e2");
        Assert.IsNotNull(triple);
        Assert.IsNotNull(doubleShot);
        Assert.IsTrue(triple.effects.Any(e => e.type == SkillEffectType.DashExtraShots));
        Assert.IsTrue(doubleShot.effects.Any(e => e.type == SkillEffectType.DashExtraShots));

        var onlyRootAndE1 = new List<string> { "core", "e1" };
        Assert.IsFalse(SkillTreeRules.IsUnlocked(Tree, triple, onlyRootAndE1), "el disparo triple no debe poder comprarse sin el doble");
        Assert.IsTrue(SkillTreeRules.IsUnlocked(Tree, triple, new List<string> { "core", "e1", "e2" }));
    }

    [Test]
    public void Tree_DashShotsAddUpToTwo_AndDashChargesToOne()
    {
        var all = Tree.nodes.SelectMany(n => n.effects).ToList();
        Assert.AreEqual(2f, all.Where(e => e.type == SkillEffectType.DashExtraShots).Sum(e => e.value), 1e-4f);
        Assert.AreEqual(1f, all.Where(e => e.type == SkillEffectType.DashExtraCharges).Sum(e => e.value), 1e-4f);
    }

    [Test]
    public void Tree_NothingReducesTheArmorDrain()
    {
        // No existe un efecto de drenaje de la armadura, y el rango tampoco lo baja (Armadura.asset: drainPerRank = 0).
        string[] names = System.Enum.GetNames(typeof(SkillEffectType));
        Assert.IsFalse(names.Any(n => n.ToLowerInvariant().Contains("drain")), "ningún efecto del árbol debe reducir el drenaje");

        var armor = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Armadura.asset");
        Assert.AreEqual(armor.BerserkDrainAt(1), armor.BerserkDrainAt(3), 1e-6f);
    }
}
```
  Antes: confirmar que `SkillTreeRules.IsUnlocked(tree, node, IEnumerable<string>)` es público; si es `internal/private`, ajustar el test para usar `CanBuy` con un `CharacterSave` (`save.skillNodes` + `skillPoints`).
- [ ] **Step 2: Verificar que falla** (`Falta Assets/Data/Skills/Guts_Tree.asset`).
- [ ] **Step 3: Crear el árbol** — `unity_execute_code` (crea el asset con `CreateAsset`, no YAML a mano):
```csharp
var cells = new System.Collections.Generic.Dictionary<string, Vector2>();
SkillNode N(string id, string name, string desc, int cost, int col, int row, bool root, string[] conns, params SkillEffect[] fx)
{
    return new SkillNode { id = id, displayName = name, description = desc, cost = cost, isRoot = root,
        position = new Vector2(col * 1.4f, row * 1.3f), connections = conns, effects = fx };
}
SkillEffect E(SkillEffectType t, float v) => new SkillEffect { type = t, value = v };
```
  (Si `unity_execute_code` no admite funciones locales, usar `System.Func<...>` como en pasos anteriores.) Nodos (id · nombre · descripción · costo · celda (col,row) · conexiones · efectos). Descripciones en español con acentos reales:
  - `core` Dragonslayer · "+5% de daño." · 1 · (0,0) · root · [v1,s1,d1,i1,q1,e1] · DamagePercent 0.05
  - `v1` Vitalidad I 1 (-1,-1) [core,v2] MaxHealth 10 · `v2` II 2 (-2,-1) [v1,v3] 15 · `v3` III 3 (-3,-1) [v2] 20
  - `s1` Zancada I 1 (1,-1) [core,s2] MoveSpeedPercent .03 · `s2` II 2 (2,-1) [s1,s3] .03 · `s3` III 3 (3,-1) [s2] .04
  - `d1` Filo I 1 (0,-1) [core,d2] DamagePercent .03 · `d2` II 2 (0,-2) [d1,d3,f1] .03 · `d3` III 2 (0,-3) [d2,d4] .03 · `d4` IV 6 (0,-4) [d3] .04
  - `i1` Impacto I 2 (0,1) [core,i2,ve1,e4?]: conexiones i1: [core,i2,ve1]; StunChance .03 · `i2` II 3 (0,2) [i1,i3,r1,a1] .03 · `i3` III 4 (0,3) [i2,i4] .03 · `i4` IV 5 (0,4) [i3,i5] .03 · `i5` V 6 (0,5) [i4] .03
  - `ve1` Verdugo 5 (-1,1) [i1,ve2] StunnedDamagePercent .15 · `ve2` Verdugo II 7 (-2,1) [ve1] .15
  - `a1` Alma voraz 4 (-1,2) [i2,a2] SoulHealBonus .01 · `a2` Alma voraz II 6 (-2,2) [a1] .01
  - `r1` Rabia 4 (1,2) [i2,r2] FuryGainPercent .25 · `r2` Rabia II 6 (2,2) [r1] .25
  - Q: `q1` Brasas 3 (-1,0) [core,q2] FlameBurstSeconds 2 · `q2` Llama ancha 3 (-2,0) [q1,q3] FlameCone 30 · `q3` Infierno 5 (-3,0) [q2,q4] FlameBurnDamagePercent .5 · `q4` Llama larga 4 (-4,0) [q3,q5] FlameRange 2 · `q5` Brasas II 6 (-5,0) [q4,q6] FlameBurnSeconds 2 · `q6` Infierno II 8 (-6,0) [q5] FlameBurnDamagePercent .5
  - E: `e1` Doble carga 4 (1,0) [core,e2,e4] DashExtraCharges 1 · `e2` Disparo doble 5 (2,0) [e1,e3] DashExtraShots 1 · `e3` Disparo triple 8 (3,0) [e2] DashExtraShots 1 · `e4` Zancada de embestida 3 (1,1) [e1,e5] DashDistance 1.5 · `e5` Recarga veloz 3 (2,1) [e4,e6] DashCooldown 1 · `e6` Recarga veloz II 6 (3,1) [e5] DashCooldown 1
  - F: `f1` Rugido atronador 4 (-1,-2) [d2,f2,f4] RoarStun 1 + RoarRadius 1.5 · `f2` Ira 5 (-2,-2) [f1,f3,f5] BerserkDamage .1 · `f3` Piel de hierro 6 (-3,-2) [f2] BerserkDamageTaken .1 · `f4` Rugido atronador II 6 (-1,-3) [f1] RoarStun 1 + RoarRadius 1.5 · `f5` Ira II 8 (-2,-3) [f2] BerserkDamage .1
  (Total 39. Las conexiones se usan en los dos sentidos; basta con declararlas en un lado, pero declarar ambos no daña.)
  Crear: `var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>(); tree.nodes = ...; AssetDatabase.CreateAsset(tree, "Assets/Data/Skills/Guts_Tree.asset");` poner el `id` privado con `SerializedObject` (`"guts_tree"`), `SetDirty`, y enlazar `Guts.asset`: `guts.skillTree = tree`. Guardar.
- [ ] **Step 4: GREEN** — `filter = "GutsTreeAsset"`: `fallan=0`; luego la suite completa sin fallos.

### Task 3: Efectos en el juego

**Files:** Modify `SkillTreeManager.cs`, `Shooting.cs`, `FlameBurst.cs`, `GutsDash.cs`, `BerserkArmor.cs`, `PlayerAbilities.cs`, `SoulSpawner.cs`. Sin tests de EditMode (runtime); se prueban en Play (Tarea 4).

- [ ] **Step 1: `SkillTreeManager`** — `public static TreeBonuses Current => Instance != null ? Instance.Bonuses : TreeBonuses.None;` (junto a `Instance`).
- [ ] **Step 2: Espada (`Shooting.SwingSword`)** — en el `foreach`, `int hit = tree.ScaleVsStunned(damage, enemy.IsStunned);` con `TreeBonuses tree = SkillTreeManager.Current;` (antes del bucle) y `enemy.TakeDamage(hit);` (la comprobación `IsStunned` va **antes** de aplicar el stun del mismo golpe: el orden actual ya es daño primero, stun después). Furia: `* BerserkArmor.FuryGainMultiplier` → `* (1f + tree.FuryGainPercent) * BerserkArmor.FuryGainMultiplier`.
- [ ] **Step 3: `FlameBurst.Cast`** — `TreeBonuses tree = SkillTreeManager.Current; float range = ability.range + tree.FlameRangeBonus; float cone = ability.coneDegrees + tree.FlameConeBonus;` y usarlos en `AbilityVfx.FlameCone`, en `OverlapSphere` (`range + SearchMargin`) y en `MeleeCone.Contains`; `seconds = ability.BurnSecondsAt(rank) + tree.FlameBurnSecondsBonus`; `tick = Mathf.Max(1, Mathf.RoundToInt(ability.BurnTickDamageFor(swordDamage, multiplier) * (1f + tree.FlameBurnDamagePercent)))`; en el `foreach`: `enemy.TakeDamage(tree.ScaleVsStunned(damage, enemy.IsStunned));` (mismo orden: daño, luego quemadura).
- [ ] **Step 4: Cargas y enfriamiento de la E (`PlayerAbilities`)** — en `RefreshBuild`: `int max = ... HeavyShot ? 1 + bonuses.HeavyShotExtraCharges : ability != null && ability.kind == AbilityKind.Dash ? 1 + bonuses.DashExtraCharges : 1;`. En `EffectiveCooldown` (switch): `case AbilityKind.Dash: reduction = bonuses.DashCooldownReduction; break;`.
- [ ] **Step 5: Dash (`GutsDash`)** — distancia: `float distance = ability.dashDistance + tree.DashDistanceBonus` (en `IgnoreEnemiesNear` y en `BeginForcedMove`). Disparos: reemplazar `Shoot` por una salva:
  - Reunir `candidates`/`positions` como ahora; `List<int> plan = TargetPicker.PlanShots(origin, positions, ability.shotRange, 1 + tree.DashExtraShots)`; si está vacío → return.
  - `bool empowered = shooting.Fury.TryConsume()` **una sola vez**; `multiplier` como ahora; `int baseDamage = ability.SwordScaledDamageFor(weapon.Damage, rank, multiplier)`.
  - Para cada `index` del plan: `EnemyAI target = candidates[index]; if (target.IsDead) { yield return Spacing; continue; }` → rayo/flash/`target.TakeDamage(tree.ScaleVsStunned(baseDamage, target.IsStunned))`; `yield return new WaitForSeconds(0.12f)` entre disparos (no después del último).
  - Convertir `Shoot` en corrutina `IEnumerator Volley(...)` y llamarla con `yield return StartCoroutine(Volley(...))` desde `Run` **antes** de `routine = null; Finish();` (así el jugador sigue invulnerable durante la salva). `if (empowered) shooting.PublishFury();` al terminar.
- [ ] **Step 6: Armadura (`BerserkArmor`)** — `TreeBonuses tree = SkillTreeManager.Current;` en `Activate`: `damageMultiplier = armor.BerserkDamageAt(armorRank) + tree.BerserkDamageBonus;` `health.DamageTakenMultiplier = Mathf.Max(0.1f, armor.damageTakenMultiplier - tree.BerserkDamageTakenReduction);` y `Roar`: radio `armor.roarRadius + tree.RoarRadiusBonus`, aturdimiento `armor.roarStunSeconds + tree.RoarStunBonus` (pasar `tree` a `Roar` o leerlo dentro). **No tocar el drenaje.**
- [ ] **Step 7: Almas (`SoulSpawner.OnEnemyDied`)** — `float fraction = character.soulHealFraction + SkillTreeManager.Current.SoulHealBonus;` y usarla en `SoulRules.HealAmount(...)` (la comprobación `soulHealFraction <= 0` sigue sobre el valor base: solo Guts suelta almas).
- [ ] **Step 8: Compilar** — 0 errores.

### Task 4: Verificación en Play, notas y memoria

- [ ] **Step 1: Respaldar `save.json` de este momento** (`save_backup_tree.json` en el scratchpad).
- [ ] **Step 2: Play** (pausado, funciones por reflexión, topes de pasos; **con el guardado real**): `TrySelect(Guts)`; dar puntos de árbol y comprar nodos con `SkillTreeRules.TryBuy(tree, save, id)` + `SkillTreeManager.Instance.Refresh...` (leer en `SkillTreeManager` el método que recalcula `Bonuses` y llamarlo; si hace falta, por reflexión).
  1. La pestaña **Árbol** aparece con Guts (`Character.skillTree != null`) y el árbol carga con 39 nodos; con Alucard sigue igual.
  2. **Stun:** con `i1..i5` y la tienda al nivel 5, `WeaponState.StunChance == 0,30`; con un bono artificial mayor no pasa de 0,30. Con la tienda al nivel 5 sin árbol 0,15.
  3. **Verdugo:** `ve1`: un enemigo aturdido recibe +15% (p. ej. 92 en vez de 80 con espada 80) y uno no aturdido el daño normal; con `ve2` +30%. Probar con el golpe de espada, la Q y la E.
  4. **E cargas:** con `e1`, lanzar la E dos veces seguidas (la segunda tras terminar el dash); la 3.ª se bloquea; tras el enfriamiento (7 s de juego) se recupera una carga. `RechargeRemaining` coherente.
  5. **E salvas:** con `e1,e2,e3` y 3 enemigos a distintas distancias: tras el dash caen 3 disparos, uno por enemigo (el más cercano primero, 0,12 s entre ellos); con 1 solo enemigo los 3 van a él (el daño total = 3 × disparo); con 2 enemigos: [cercano, lejano, cercano]; con Furia llena todos x2 y la barra en 0; sin enemigos no dispara y no gasta Furia; un enemigo que muere con el primer disparo no produce error.
  6. **Q:** `q1`, `q2`, `q4`, `q3`: la quemadura dura 2 s más, el cono es 30° más ancho, el alcance +2 m y el tick sube 50%.
  7. **Rabia / Alma voraz:** con `r1` la Furia por golpe sube 25% (5 → 6,25 por enemigo); con `a1` un alma cura 3 en vez de... (2% + 1% = 3% de 150 = 4,5 → 5).
  8. **Armadura:** `f1` el rugido aturde 1 s más y 1,5 m más lejos; `f2` el daño x1,5 con rango 1 (x1,4 + 0,1); `f3` el daño recibido x0,4; y el **drenaje sigue siendo 2% (3 por segundo)** en los rangos 1 y 3 y con todos los nodos de la F comprados.
  9. Guardado: los nodos comprados quedan en `skillNodes` de Guts y no en los de Alucard.
  10. Consola sin errores nuevos.
- [ ] **Step 3: Salir de Play y restaurar el guardado** (Unity quieto).
- [ ] **Step 4: Notas y memoria** (**no commitear**): `REGISTRO_DE_CAMBIOS.md` (entrada + "Dónde nos quedamos"), `DECISIONES.md` D43 (árbol de Guts: stun 15+15 con tope 30%, armadura sin reducción de drenaje ni por rango ni por árbol, salvas a objetivos distintos, Furia potencia toda la salva), `NOTAS_DEL_JUEGO.md`, memoria.
- [ ] **Step 5: Informar al usuario** — qué quedó hecho y verificado; **no verificado**: la vista del árbol (posiciones, desplazamiento y clic reales), el balance de los 39 nodos y de las salvas; y los valores a ajustar en `Guts_Tree.asset`.

---

## Self-Review (hecha al escribir)
- **Cobertura del spec:** §3 → Tarea 1 (pasos 3-4); §4 los 39 nodos → Tarea 2; §5 efectos → Tareas 1 y 3; §6 cargas y salvas → Tareas 1 y 3; §7 aturdidos → Tarea 3; §8 pruebas → Tareas 1, 2 y 4.
- **Consistencia de nombres:** `TreeBonuses.{StunnedDamagePercent,FuryGainPercent,SoulHealBonus,FlameBurnSecondsBonus,FlameConeBonus,FlameBurnDamagePercent,FlameRangeBonus,DashExtraCharges,DashExtraShots,DashDistanceBonus,DashCooldownReduction,BerserkDamageBonus,BerserkDamageTakenReduction,RoarStunBonus,RoarRadiusBonus,ScaleVsStunned}`, `SkillTreeManager.Current`, `TargetPicker.PlanShots`, `SwordDefinition.stunChanceCap`: iguales en todas las tareas.
- **Sin marcadores pendientes.** En el Step 3 de la Tarea 1 el efecto de Q "FlameBurstSeconds" del listado de nodos es `FlameBurnSeconds` (nombre real del enum).
