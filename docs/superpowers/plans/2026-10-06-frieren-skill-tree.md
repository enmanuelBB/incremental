# Frieren: árbol de habilidades — Plan de implementación

> **Para quien ejecute:** SUB-SKILL REQUERIDA: superpowers:executing-plans (Native) o superpowers:subagent-driven-development. Los pasos usan casillas (`- [ ]`).
> **Regla del usuario: NO hacer commits.** Él hace los suyos. Donde la plantilla diría "Commit", aquí se compila y se corren los tests.

**Goal:** El árbol de 56 nodos de Frieren (43 comprables a la vez, 151 puntos) con grupos de "elige 1", nodos divididos (curar | ralentizar), cambio de opción con ventana de confirmación, y todos sus efectos en el Zoltraak, el maná, la Q, la E y la F.

**Architecture:** Dos campos nuevos en `SkillNode` (`choiceGroup`, `half`) y reglas puras nuevas en `SkillTreeRules` (`ChoiceTaken`, `OwnedRival`, `CanSwap`, `TrySwap`, `AllConnected`, `Compute` que cuenta una opción por grupo). Efectos nuevos al final de `SkillEffectType` → campos de `TreeBonuses` → `SkillTreeRules.Apply`; fórmulas en `FrierenTreeMath` y `SlowRules` (Core, con tests). El asset `Frieren_Tree` se crea con un script de editor y se valida con tests. Cada efecto se lee donde actúa (`ZoltraakCaster`, `WeaponState`, `Shooting`, `ManaBeam`, `PiercingBeam`, `PlayerAbilities`, `FlowerField`, `ManaPulseEffect`, `EnemyAI`). La vista (`SkillTreeView`) dibuja mitades, marcos "Elige 1" y la ventana de cambio.

**Tech Stack:** Unity 6 (C#), Game.Core / Game.Runtime / Game.Tests.EditMode, NUnit por reflexión, MCP de Unity (`unity_execute_code`, `unity_get_compilation_errors`, `unity_play_mode`, capturas).

**Spec:** `docs/superpowers/specs/2026-10-06-frieren-skill-tree-design.md`

## Global Constraints
- `SkillEffectType` y `SkillBuyBlock`: **solo agregar al final** (los valores guardados de Alucard y Guts no se mueven). No cambiar los ids de nodo una vez publicados. **El guardado no cambia** (sin versión ni migración).
- Los árboles de Alucard y Guts no tienen grupos: su comportamiento no cambia (sus tests siguen pasando).
- Valores (del spec): Zoltraak +15% daño, −0,15 s de carga (mínimo **0,3 s**), +0,5 m de radio; Eco 50% del daño y 70% del radio a los 0,3 s; Escarcha 40% durante 3 s; maná +20 máx., +0,75/s; Eficiencia −40% (redondeado: 25→15, 20→12, 50→30); Absorción +3 por muerte; Concentración x2 tras 3 s sin daño; rayo +15% daño, +0,2 m, −0,5 s (mínimo 1 s); Rayo gélido 60% 3 s; Perforación +15% por enemigo previo, tope +90%; campo +1 m, +1,5 s, −3 s, cura +1%/s por mitad, ralentización +10 puntos por mitad (tope **0,8**); veneno 0,5 × daño básico por segundo en ticks de 0,5 s, dura 3 s desde el último toque, Veneno II +50%; pulso +1,5 s, +0,5 s de aturdir, −8 s; Dominio = todos los enemigos; Lluvia cada 1 s, cae en 0,4 s desde 12 m; Explosión final 3 × Zoltraak completo; Marca +30%.
- Textos para el jugador en español con acentos reales. Los tests de EditMode solo ven `Game.Core` (y los assets).
- Escribir archivos con Write/Edit. Herramientas de Unity con `port: 7890`; tras editar scripts forzar `AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate)` y esperar compilación (`unity_get_compilation_errors` con `isCompiling: false`; si dice "Queue session changed", reintentar).
- **Pruebas en Play:** respaldar `save.json` (`%USERPROFILE%/AppData/LocalLow/DefaultCompany/la primera nunca se olvida/save.json`) **de ese momento** antes y restaurarlo después, con Unity fuera de Play. Bucles con `EditorApplication.Step` con tope de pasos, midiendo **segundos de juego**. `Physics.SyncTransforms()` tras colocar enemigos. Para lanzar habilidades: `GameEvents.RaiseGameStarted()` y `PlayerAbilities.TryCast(casilla)` por reflexión (ver "Cosas que conviene recordar" en `notas/REGISTRO_DE_CAMBIOS.md`).
- El asset del árbol se crea con `AssetDatabase.CreateAsset` (no editando YAML).
- Línea base: **536 tests pasan, 0 fallan** (comprobarla al empezar). Para correrlos: el ejecutor por reflexión de `docs/superpowers/plans/2026-10-04-guts-fury.md` (sección "Cómo correr los tests de EditMode"), con `filter` por clase.

## Review Focus
1. **Ralentizaciones que se pisan:** Rayo gélido (60%) y después el campo (40% cada 0,25 s) → el enemigo sigue al 60% hasta que caduque, no baja a 40%. Test en Task 2 (`SlowRules`).
2. **Cambiar una mitad en medio de la cadena de la E** con las mitades de arriba compradas → se permite y no desconecta nada. Test en Task 1.
3. **Guardado con dos opciones del mismo grupo** (editado a mano o por un error) → solo cuenta la primera; reiniciar devuelve ambas. Test en Task 1.
4. **Sobrecarga tras cancelar:** colocar el campo cancela una carga pero no debe borrar la Sobrecarga ya ganada, y el disparo instantáneo no vuelve a armarla. Verificación en Play (Task 5).
5. **Dominio con muchos enemigos:** el búfer del `OverlapSphere` del pulso pasa de 192 a 512 para que "todo el mapa" no deje enemigos fuera. Verificación en Play (Task 6).

## Mapa de archivos
| Archivo (bajo `Assets/`) | Acción |
|---|---|
| `Scripts/Core/Data/SkillTreeDefinition.cs` | modificar (`SkillNodeHalf`, `choiceGroup`, `half`, 31 efectos al final) |
| `Scripts/Core/Data/SkillTreeRules.cs` | modificar (`ChoiceTaken`, `SkillSwapBlock`, `OwnedRival`, `CanSwap`, `TrySwap`, `AllConnected`, `Compute`, `Apply`) |
| `Scripts/Core/Data/TreeBonuses.cs` | modificar (campos de Frieren) |
| `Scripts/Core/Data/FrierenTreeMath.cs` | crear (fórmulas y constantes) |
| `Scripts/Core/Data/SlowRules.cs` | crear (gana la ralentización más fuerte) |
| `Scripts/Core/Data/ManaPool.cs` | modificar (`Gain`) |
| `Scripts/Core/GameEvents.cs` | modificar (`PoisonTick`) |
| `Data/Skills/Frieren_Tree.asset`, `Data/Characters/Maga.asset` | crear / modificar (script de editor) |
| `Scripts/Gameplay/Enemies/EnemyAI.cs`, `EnemyPoison.cs` (nuevo) | modificar / crear |
| `Scripts/Gameplay/Base/Health.cs` | modificar (`LastDamagedAt`) |
| `Scripts/UI/FloatingTextManager.cs` | modificar (número verde del veneno) |
| `Scripts/Gameplay/Weapons/WeaponState.cs` | modificar (daño del Zoltraak y del rayo) |
| `Scripts/Gameplay/Player/ZoltraakCaster.cs`, `Shooting.cs` | modificar (Zoltraak, maná) |
| `Scripts/Gameplay/Player/ManaBeam.cs`, `PiercingBeam.cs`, `PlayerAbilities.cs`, `FlowerField.cs`, `ManaPulseEffect.cs` | modificar (Q, E, F) |
| `Scripts/Gameplay/Characters/SkillTreeManager.cs` | modificar (`Swap`) |
| `Scripts/UI/SkillTreeView.cs` | modificar (mitades, alternativa, marcos, ventana) |
| `Tests/EditMode/SkillTreeChoiceTests.cs`, `FrierenTreeTests.cs`, `FrierenTreeAssetTests.cs` | crear |
| `Tests/EditMode/SkillTreeLayoutTests.cs` | modificar (Frieren, mitades) |

---

### Task 1: Grupos de "elige 1" y nodos divididos (reglas puras)

**Files:**
- Modify: `Assets/Scripts/Core/Data/SkillTreeDefinition.cs`, `Assets/Scripts/Core/Data/SkillTreeRules.cs`
- Test: `Assets/Tests/EditMode/SkillTreeChoiceTests.cs`

**Interfaces:**
- Produces: `enum SkillNodeHalf { None, Left, Right }`; `SkillNode.choiceGroup` (string, "" por defecto), `SkillNode.half`; `SkillBuyBlock.ChoiceTaken` (al final); `enum SkillSwapBlock { None, UnknownNode, NoRival, Locked, NoPoints, WouldDisconnect }`; `SkillTreeRules.OwnedRival(SkillTreeDefinition, ICollection<string>, string) : string`; `CanSwap(SkillTreeDefinition, CharacterSave, string) : SkillSwapBlock`; `TrySwap(SkillTreeDefinition, CharacterSave, string) : bool`; `AllConnected(SkillTreeDefinition, ICollection<string>) : bool`.

- [ ] **Step 1: Escribir los tests que fallan** — crear `Assets/Tests/EditMode/SkillTreeChoiceTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Grupos de "elige 1" y nodos divididos del árbol (pensados para Frieren; Alucard y Guts no tienen grupos).</summary>
[TestFixture]
public class SkillTreeChoiceTests
{
    private SkillTreeDefinition tree;

    // core(raíz) - a - { x1 | x2 } (grupo "g")      x1 - t (t depende de x1)
    //              a - { h1L | h1R } (grupo "h1") - { h2L | h2R } (grupo "h2"): cada mitad conecta con las dos de arriba
    [SetUp]
    public void SetUp()
    {
        tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
        tree.nodes = new[]
        {
            Node("core", 1, "", SkillNodeHalf.None, 0f, "a"),
            Node("a", 1, "", SkillNodeHalf.None, 0f, "core"),
            Node("x1", 2, "g", SkillNodeHalf.None, 5f, "a"),
            Node("x2", 3, "g", SkillNodeHalf.None, 7f, "a"),
            Node("t", 1, "", SkillNodeHalf.None, 0f, "x1"),
            Node("h1L", 1, "h1", SkillNodeHalf.Left, 0f, "a"),
            Node("h1R", 1, "h1", SkillNodeHalf.Right, 0f, "a"),
            Node("h2L", 1, "h2", SkillNodeHalf.Left, 0f, "h1L", "h1R"),
            Node("h2R", 1, "h2", SkillNodeHalf.Right, 0f, "h1L", "h1R")
        };
        tree.nodes[0].isRoot = true;
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(tree);

    private static SkillNode Node(string id, int cost, string group, SkillNodeHalf half, float health, params string[] connections) =>
        new SkillNode
        {
            id = id, displayName = id, cost = cost, choiceGroup = group, half = half, connections = connections,
            effects = health > 0f ? new[] { new SkillEffect { type = SkillEffectType.MaxHealth, value = health } } : new SkillEffect[0]
        };

    private static CharacterSave Save(int points, params string[] owned)
    {
        var save = new CharacterSave { id = "test", skillPoints = points };
        save.skillNodes.AddRange(owned);
        return save;
    }

    [Test]
    public void CanBuy_WithTheRivalOwned_IsChoiceTaken()
    {
        Assert.AreEqual(SkillBuyBlock.ChoiceTaken, SkillTreeRules.CanBuy(tree, Save(10, "core", "a", "x1"), "x2"));
    }

    [Test]
    public void CanBuy_WithoutRival_StillWorks()
    {
        Assert.AreEqual(SkillBuyBlock.None, SkillTreeRules.CanBuy(tree, Save(10, "core", "a"), "x2"));
    }

    [Test]
    public void OwnedRival_FindsTheOtherOptionOfTheGroup()
    {
        Assert.AreEqual("x1", SkillTreeRules.OwnedRival(tree, new List<string> { "core", "a", "x1" }, "x2"));
        Assert.IsNull(SkillTreeRules.OwnedRival(tree, new List<string> { "core", "a" }, "x2"));
        Assert.IsNull(SkillTreeRules.OwnedRival(tree, new List<string> { "core", "a" }, "a"));
    }

    [Test]
    public void TrySwap_RefundsTheRivalAndChargesTheNewOne_KeepingItsPlaceInTheList()
    {
        CharacterSave save = Save(1, "core", "a", "x1");

        Assert.IsTrue(SkillTreeRules.TrySwap(tree, save, "x2"));
        Assert.AreEqual(0, save.skillPoints);   // 1 + 2 - 3
        CollectionAssert.AreEqual(new[] { "core", "a", "x2" }, save.skillNodes);
    }

    [Test]
    public void TrySwap_NotEnoughEvenWithTheRefund_IsNoPoints()
    {
        CharacterSave save = Save(0, "core", "a", "x1");

        Assert.AreEqual(SkillSwapBlock.NoPoints, SkillTreeRules.CanSwap(tree, save, "x2"));
        Assert.IsFalse(SkillTreeRules.TrySwap(tree, save, "x2"));
        CollectionAssert.AreEqual(new[] { "core", "a", "x1" }, save.skillNodes);
        Assert.AreEqual(0, save.skillPoints);
    }

    [Test]
    public void TrySwap_ThatWouldOrphanABoughtNode_IsWouldDisconnect()
    {
        Assert.AreEqual(SkillSwapBlock.WouldDisconnect, SkillTreeRules.CanSwap(tree, Save(10, "core", "a", "x1", "t"), "x2"));
    }

    [Test]
    public void TrySwap_SplitHalfInTheMiddleOfTheChain_KeepsTheHalvesAbove()
    {
        CharacterSave save = Save(0, "core", "a", "h1L", "h2L");

        Assert.IsTrue(SkillTreeRules.TrySwap(tree, save, "h1R"));
        CollectionAssert.AreEqual(new[] { "core", "a", "h1R", "h2L" }, save.skillNodes);
    }

    [Test]
    public void CanSwap_WithoutAnOwnedRival_IsNoRival()
    {
        Assert.AreEqual(SkillSwapBlock.NoRival, SkillTreeRules.CanSwap(tree, Save(10, "core", "a"), "x2"));
        Assert.AreEqual(SkillSwapBlock.NoRival, SkillTreeRules.CanSwap(tree, Save(10, "core", "a", "x2"), "x2"));
        Assert.AreEqual(SkillSwapBlock.UnknownNode, SkillTreeRules.CanSwap(tree, Save(10, "core"), "nada"));
    }

    [Test]
    public void CanSwap_ToAnOptionWhoseNeighbourIsNotBought_IsLocked()
    {
        // h2R cuelga de h1L/h1R: con h2L comprado "a mano" sin h1, cambiar a h2R no está desbloqueado.
        Assert.AreEqual(SkillSwapBlock.Locked, SkillTreeRules.CanSwap(tree, Save(10, "core", "a", "h2L"), "h2R"));
    }

    [Test]
    public void Compute_TwoOptionsOfTheSameGroup_CountsOnlyTheFirst()
    {
        Assert.AreEqual(7, SkillTreeRules.Compute(tree, new[] { "core", "a", "x2", "x1" }).MaxHealthBonus);
        Assert.AreEqual(5, SkillTreeRules.Compute(tree, new[] { "core", "a", "x1", "x2" }).MaxHealthBonus);
    }

    [Test]
    public void Reset_ReturnsEverything_IncludingBothOptionsOfABrokenSave()
    {
        CharacterSave save = Save(0, "core", "a", "x1", "x2");

        Assert.AreEqual(1 + 1 + 2 + 3, SkillTreeRules.Reset(tree, save));
        Assert.AreEqual(7, save.skillPoints);
        Assert.IsEmpty(save.skillNodes);
    }

    [Test]
    public void AllConnected_DetectsAnIsland()
    {
        Assert.IsTrue(SkillTreeRules.AllConnected(tree, new List<string> { "core", "a", "x1", "t" }));
        Assert.IsFalse(SkillTreeRules.AllConnected(tree, new List<string> { "core", "a", "t" }));
        Assert.IsTrue(SkillTreeRules.AllConnected(tree, new List<string> { "core", "fantasma" }));   // ids desconocidos se ignoran
    }
}
```

- [ ] **Step 2: Compilar y ver que falla** — `AssetDatabase.Refresh(ForceUpdate)`; `unity_get_compilation_errors` debe mostrar errores por `SkillNodeHalf`, `choiceGroup`, `ChoiceTaken`, `SkillSwapBlock`, etc.

- [ ] **Step 3: Datos** — en `SkillTreeDefinition.cs`, antes de `public struct SkillEffect`:

```csharp
/// <summary>Mitad de un nodo dividido (dos opciones en el mismo cuadrado). None = nodo normal.</summary>
public enum SkillNodeHalf { None, Left, Right }
```

y en `SkillNode`, después de `isRoot`:

```csharp
    [Tooltip("Grupo de 'elige 1': los nodos con el mismo grupo se excluyen (solo uno comprado). Vacío = sin grupo")]
    public string choiceGroup = "";
    [Tooltip("Mitad de un nodo dividido: las dos mitades comparten grupo, posición y costo")]
    public SkillNodeHalf half = SkillNodeHalf.None;
```

- [ ] **Step 4: Reglas** — en `SkillTreeRules.cs`: `SkillBuyBlock` gana `ChoiceTaken` al final; agregar `using System;` y el enum nuevo debajo de `SkillBuyBlock`:

```csharp
/// <summary>Por qué no se puede cambiar una opción de un grupo de "elige 1" por otra.</summary>
public enum SkillSwapBlock
{
    None,
    UnknownNode,
    NoRival,
    Locked,
    NoPoints,
    WouldDisconnect
}
```

En `CanBuy`, entre la comprobación de `Owned` y la de `Locked`:

```csharp
        if (OwnedRival(tree, save.skillNodes, nodeId) != null) return SkillBuyBlock.ChoiceTaken;
```

Métodos nuevos (después de `TryBuy`):

```csharp
    /// <summary>El id del nodo comprado del mismo grupo de "elige 1" que 'nodeId' (null si no tiene grupo o no hay ninguno).</summary>
    public static string OwnedRival(SkillTreeDefinition tree, ICollection<string> owned, string nodeId)
    {
        SkillNode node = tree != null ? tree.Find(nodeId) : null;
        if (node == null || string.IsNullOrEmpty(node.choiceGroup)) return null;

        foreach (string id in owned)
        {
            if (id == nodeId) continue;
            SkillNode other = tree.Find(id);
            if (other != null && other.choiceGroup == node.choiceGroup) return id;
        }
        return null;
    }

    /// <summary>
    /// Si se puede cambiar la opción comprada del grupo por 'nodeId': hay un rival comprado, el nodo nuevo queda desbloqueado sin
    /// contar al rival, alcanzan los puntos con el reembolso, y todo lo comprado sigue conectado a una raíz.
    /// </summary>
    public static SkillSwapBlock CanSwap(SkillTreeDefinition tree, CharacterSave save, string nodeId)
    {
        SkillNode node = tree != null ? tree.Find(nodeId) : null;
        if (node == null) return SkillSwapBlock.UnknownNode;

        string rivalId = OwnedRival(tree, save.skillNodes, nodeId);
        if (rivalId == null || save.skillNodes.Contains(nodeId)) return SkillSwapBlock.NoRival;

        var after = new HashSet<string>(save.skillNodes);
        after.Remove(rivalId);
        if (!IsUnlocked(tree, node, after)) return SkillSwapBlock.Locked;
        if (save.skillPoints + tree.Find(rivalId).cost < node.cost) return SkillSwapBlock.NoPoints;

        after.Add(nodeId);
        return AllConnected(tree, after) ? SkillSwapBlock.None : SkillSwapBlock.WouldDisconnect;
    }

    /// <summary>Devuelve el rival y compra 'nodeId' en su mismo lugar de la lista. Todo o nada.</summary>
    public static bool TrySwap(SkillTreeDefinition tree, CharacterSave save, string nodeId)
    {
        if (CanSwap(tree, save, nodeId) != SkillSwapBlock.None) return false;

        string rivalId = OwnedRival(tree, save.skillNodes, nodeId);
        save.skillPoints += tree.Find(rivalId).cost - tree.Find(nodeId).cost;
        save.skillNodes[save.skillNodes.IndexOf(rivalId)] = nodeId;
        return true;
    }

    /// <summary>True si cada nodo comprado (que exista en el árbol) llega a una raíz pasando solo por nodos comprados.</summary>
    public static bool AllConnected(SkillTreeDefinition tree, ICollection<string> owned)
    {
        var reached = new HashSet<string>();
        var queue = new Queue<string>();
        foreach (string id in owned)
        {
            SkillNode node = tree.Find(id);
            if (node != null && node.isRoot && reached.Add(id)) queue.Enqueue(id);
        }

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            foreach (string id in owned)
            {
                if (reached.Contains(id)) continue;
                SkillNode node = tree.Find(id);
                if (node != null && Linked(tree, current, node) && reached.Add(id)) queue.Enqueue(id);
            }
        }

        foreach (string id in owned)
            if (tree.Find(id) != null && !reached.Contains(id)) return false;
        return true;
    }

    // La conexión vale en los dos sentidos: basta con que uno de los dos la declare.
    private static bool Linked(SkillTreeDefinition tree, string aId, SkillNode b)
    {
        if (b.connections != null && Array.IndexOf(b.connections, aId) >= 0) return true;
        SkillNode a = tree.Find(aId);
        return a != null && a.connections != null && Array.IndexOf(a.connections, b.id) >= 0;
    }
```

En `Compute`, contar una sola opción por grupo:

```csharp
        var seenGroups = new HashSet<string>();
        foreach (string id in ownedIds)
        {
            SkillNode node = tree.Find(id);
            if (node == null || node.effects == null) continue;
            // Un guardado con dos opciones del mismo grupo (editado a mano): solo cuenta la primera.
            if (!string.IsNullOrEmpty(node.choiceGroup) && !seenGroups.Add(node.choiceGroup)) continue;

            foreach (SkillEffect effect in node.effects) Apply(ref bonuses, effect);
        }
```

(`IsUnlocked` usa `System.Array.IndexOf`; con `using System;` queda igual.)

- [ ] **Step 5: Compilar y correr** — 0 errores; tests con `filter = "SkillTree"`: todos pasan (los nuevos de `SkillTreeChoiceTests` y los existentes de `SkillTreeTests`, `SkillTreeAssetTests`, `SkillTreeLayoutTests`). Después la batería completa: 536 + 12 = **548**, 0 fallan.

---

### Task 2: Efectos de Frieren, fórmulas y ralentización (lógica pura)

**Files:**
- Modify: `Assets/Scripts/Core/Data/SkillTreeDefinition.cs`, `TreeBonuses.cs`, `SkillTreeRules.cs`, `ManaPool.cs`
- Create: `Assets/Scripts/Core/Data/FrierenTreeMath.cs`, `Assets/Scripts/Core/Data/SlowRules.cs`
- Test: `Assets/Tests/EditMode/FrierenTreeTests.cs`

**Interfaces:**
- Consumes: `SkillTreeRules.Compute` (Task 1).
- Produces: 31 valores nuevos de `SkillEffectType` (lista abajo); campos de `TreeBonuses` (lista abajo); `FrierenTreeMath` (constantes y `ChargeSeconds`, `FieldSlow`, `ManaCost`, `PierceMultiplier`, `PoisonTickDamage`, `RegenMultiplier`, `Scale`); `SlowRules.ShouldReplace(float activeMultiplier, bool active, float newFraction) : bool`; `ManaPool.Gain(float)`.

- [ ] **Step 1: Escribir los tests que fallan** — crear `Assets/Tests/EditMode/FrierenTreeTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>Efectos del árbol de Frieren (suma en TreeBonuses) y sus fórmulas puras.</summary>
[TestFixture]
public class FrierenTreeTests
{
    private static TreeBonuses ComputeOne(SkillEffectType type, float value, int times = 1)
    {
        var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
        var nodes = new SkillNode[times];
        var ids = new string[times];
        for (int i = 0; i < times; i++)
        {
            ids[i] = "n" + i;
            nodes[i] = new SkillNode { id = ids[i], cost = 1, effects = new[] { new SkillEffect { type = type, value = value } } };
        }
        tree.nodes = nodes;
        TreeBonuses result = SkillTreeRules.Compute(tree, ids);
        Object.DestroyImmediate(tree);
        return result;
    }

    [Test]
    public void Compute_AddsEachFrierenEffectToItsField()
    {
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.ZoltraakDamagePercent, 0.15f, 2).ZoltraakDamagePercent, 1e-4f);
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.ZoltraakChargeTime, 0.15f, 2).ZoltraakChargeReduction, 1e-4f);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.ZoltraakRadius, 0.5f, 2).ZoltraakRadiusBonus, 1e-4f);
        Assert.IsTrue(ComputeOne(SkillEffectType.ZoltraakOvercharge, 1f).ZoltraakOvercharge);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.ZoltraakEcho, 0.5f).ZoltraakEchoFraction, 1e-4f);
        Assert.AreEqual(0.4f, ComputeOne(SkillEffectType.ZoltraakFrost, 0.4f).ZoltraakFrostSlow, 1e-4f);
        Assert.AreEqual(40f, ComputeOne(SkillEffectType.ManaMax, 20f, 2).ManaMaxBonus, 1e-4f);
        Assert.AreEqual(1.5f, ComputeOne(SkillEffectType.ManaRegen, 0.75f, 2).ManaRegenBonus, 1e-4f);
        Assert.AreEqual(0.4f, ComputeOne(SkillEffectType.ManaCostPercent, 0.4f).ManaCostReduction, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.ManaOnKill, 3f).ManaOnKill, 1e-4f);
        Assert.AreEqual(2f, ComputeOne(SkillEffectType.ManaFocus, 2f).ManaFocusMultiplier, 1e-4f);
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.BeamDamagePercent, 0.15f, 2).BeamDamagePercent, 1e-4f);
        Assert.AreEqual(0.2f, ComputeOne(SkillEffectType.BeamRadius, 0.2f).BeamRadiusBonus, 1e-4f);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.BeamCooldown, 0.5f, 2).BeamCooldownReduction, 1e-4f);
        Assert.AreEqual(1, ComputeOne(SkillEffectType.BeamExtraCharges, 1f).BeamExtraCharges);
        Assert.AreEqual(0.6f, ComputeOne(SkillEffectType.BeamFrost, 0.6f).BeamFrostSlow, 1e-4f);
        Assert.AreEqual(0.15f, ComputeOne(SkillEffectType.BeamPierceDamage, 0.15f).BeamPierceDamage, 1e-4f);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.FieldRadius, 1f).FieldRadiusBonus, 1e-4f);
        Assert.AreEqual(1.5f, ComputeOne(SkillEffectType.FieldDuration, 1.5f).FieldDurationBonus, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.FieldCooldown, 3f).FieldCooldownReduction, 1e-4f);
        Assert.AreEqual(0.04f, ComputeOne(SkillEffectType.FieldHeal, 0.01f, 4).FieldHealBonus, 1e-4f);
        Assert.AreEqual(0.4f, ComputeOne(SkillEffectType.FieldSlow, 0.1f, 4).FieldSlowBonus, 1e-4f);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.FieldPoison, 0.5f).FieldPoison, 1e-4f);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.FieldPoisonDamagePercent, 0.5f).FieldPoisonDamagePercent, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.PulseDuration, 1.5f, 2).PulseDurationBonus, 1e-4f);
        Assert.AreEqual(0.5f, ComputeOne(SkillEffectType.PulseStun, 0.5f).PulseStunBonus, 1e-4f);
        Assert.AreEqual(16f, ComputeOne(SkillEffectType.PulseCooldown, 8f, 2).PulseCooldownReduction, 1e-4f);
        Assert.IsTrue(ComputeOne(SkillEffectType.PulseWholeMap, 1f).PulseWholeMap);
        Assert.AreEqual(1f, ComputeOne(SkillEffectType.PulseZoltraakRain, 1f).PulseRainInterval, 1e-4f);
        Assert.AreEqual(3f, ComputeOne(SkillEffectType.PulseFinalBlast, 3f).PulseFinalBlastMultiplier, 1e-4f);
        Assert.AreEqual(0.3f, ComputeOne(SkillEffectType.PulseMark, 0.3f).PulseMarkBonus, 1e-4f);
    }

    [Test]
    public void NoNodes_LeavesEveryFrierenFieldNeutral()
    {
        TreeBonuses none = TreeBonuses.None;
        Assert.IsFalse(none.ZoltraakOvercharge);
        Assert.IsFalse(none.PulseWholeMap);
        Assert.AreEqual(0f, none.PulseRainInterval);
        Assert.AreEqual(0f, none.ManaFocusMultiplier);
    }

    [TestCase(1.2f, 0f, 1.2f)]
    [TestCase(1.2f, 0.3f, 0.9f)]
    [TestCase(1.2f, 5f, 0.3f)]
    public void ChargeSeconds_NeverBelowTheMinimum(float baseSeconds, float reduction, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.ChargeSeconds(baseSeconds, reduction), 1e-4f);
    }

    [TestCase(0.4f, 0f, 0.4f)]
    [TestCase(0.4f, 0.4f, 0.8f)]
    [TestCase(0.4f, 0.6f, 0.8f)]
    public void FieldSlow_IsCappedAt80Percent(float baseFraction, float bonus, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.FieldSlow(baseFraction, bonus), 1e-4f);
    }

    [TestCase(25f, 0.4f, 15f)]
    [TestCase(20f, 0.4f, 12f)]
    [TestCase(50f, 0.4f, 30f)]
    [TestCase(25f, 0f, 25f)]
    public void ManaCost_AppliesTheReduction(float baseCost, float reduction, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.ManaCost(baseCost, reduction), 1e-4f);
    }

    [TestCase(0, 1f)]
    [TestCase(1, 1.15f)]
    [TestCase(6, 1.9f)]
    [TestCase(10, 1.9f)]
    public void PierceMultiplier_GrowsPerEnemyUpToTheCap(int index, float expected)
    {
        Assert.AreEqual(expected, FrierenTreeMath.PierceMultiplier(index, 0.15f), 1e-4f);
    }

    [Test]
    public void PoisonTickDamage_IsHalfTheBasicShotPerSecond()
    {
        Assert.AreEqual(2, FrierenTreeMath.PoisonTickDamage(8, 0.5f, 0f));    // 4 por segundo en ticks de 0,5 s
        Assert.AreEqual(3, FrierenTreeMath.PoisonTickDamage(8, 0.5f, 0.5f));  // Veneno II
        Assert.AreEqual(1, FrierenTreeMath.PoisonTickDamage(1, 0.5f, 0f));    // nunca menos de 1
        Assert.AreEqual(0, FrierenTreeMath.PoisonTickDamage(8, 0f, 0f));      // sin el nodo no envenena
    }

    [Test]
    public void RegenMultiplier_DoublesOnlyAfterThreeSecondsWithoutDamage()
    {
        Assert.AreEqual(1f, FrierenTreeMath.RegenMultiplier(0f, 10f, 0f));
        Assert.AreEqual(1f, FrierenTreeMath.RegenMultiplier(2f, 10f, 8f));
        Assert.AreEqual(2f, FrierenTreeMath.RegenMultiplier(2f, 10f, 7f));
    }

    [Test]
    public void Scale_AddsTheBonusAndLeavesTheDamageAloneWithout()
    {
        Assert.AreEqual(13, FrierenTreeMath.Scale(10, 0.3f));
        Assert.AreEqual(10, FrierenTreeMath.Scale(10, 0f));
    }

    [Test]
    public void SlowRules_TheStrongerSlowWinsWhileItLasts()
    {
        Assert.IsTrue(SlowRules.ShouldReplace(1f, false, 0.4f));    // nada activo
        Assert.IsFalse(SlowRules.ShouldReplace(0.4f, true, 0.4f));  // activo al 60% (x0,4): el 40% del campo no lo pisa
        Assert.IsTrue(SlowRules.ShouldReplace(0.6f, true, 0.6f));   // la misma lo renueva
        Assert.IsTrue(SlowRules.ShouldReplace(0.6f, true, 0.8f));   // una más fuerte lo reemplaza
        Assert.IsTrue(SlowRules.ShouldReplace(0.4f, false, 0.1f));  // la vieja ya caducó
    }

    [Test]
    public void ManaPool_Gain_AddsUpToTheMax()
    {
        var pool = new ManaPool(100f, 0f);
        pool.TrySpend(50f);
        pool.Gain(3f);
        Assert.AreEqual(53f, pool.Current, 1e-4f);
        pool.Gain(500f);
        Assert.AreEqual(100f, pool.Current, 1e-4f);
        pool.Gain(-10f);
        Assert.AreEqual(100f, pool.Current, 1e-4f);
    }
}
```

- [ ] **Step 2: Compilar y ver que falla** (no existen los efectos ni `FrierenTreeMath`).

- [ ] **Step 3: Efectos al final de `SkillEffectType`** (después de `RoarRadius`, poniendo coma a `RoarRadius`):

```csharp
    [Tooltip("Frieren: fracción extra de daño del Zoltraak (0,15 = +15%)")] ZoltraakDamagePercent,
    [Tooltip("Frieren: segundos que baja la carga del Zoltraak (mínimo 0,3 s)")] ZoltraakChargeTime,
    [Tooltip("Frieren: metros extra de radio del Zoltraak (mínimo y máximo)")] ZoltraakRadius,
    [Tooltip("Frieren, Sobrecarga (1): tras uno al 100%, el siguiente sale cargado al instante")] ZoltraakOvercharge,
    [Tooltip("Frieren, Eco: fracción del daño de la segunda explosión (0,5)")] ZoltraakEcho,
    [Tooltip("Frieren, Escarcha arcana: ralentización a carga completa (0,4 = -40%)")] ZoltraakFrost,
    [Tooltip("Frieren: maná máximo extra")] ManaMax,
    [Tooltip("Frieren: maná por segundo extra")] ManaRegen,
    [Tooltip("Frieren, Eficiencia: fracción que baja el maná de Q, E y F (0,4 = -40%)")] ManaCostPercent,
    [Tooltip("Frieren, Absorción: maná por cada enemigo que muere")] ManaOnKill,
    [Tooltip("Frieren, Concentración: multiplicador de regeneración tras 3 s sin daño (2 = x2)")] ManaFocus,
    [Tooltip("Frieren Q: fracción extra de daño del rayo (0,15 = +15%)")] BeamDamagePercent,
    [Tooltip("Frieren Q: metros extra de grosor del rayo")] BeamRadius,
    [Tooltip("Frieren Q: segundos que baja el enfriamiento")] BeamCooldown,
    [Tooltip("Frieren Q: cargas extra")] BeamExtraCharges,
    [Tooltip("Frieren Q, Rayo gélido: ralentización a los atravesados (0,6 = -60%)")] BeamFrost,
    [Tooltip("Frieren Q, Perforación creciente: daño extra por cada enemigo ya atravesado (0,15)")] BeamPierceDamage,
    [Tooltip("Frieren E: metros extra de radio del campo")] FieldRadius,
    [Tooltip("Frieren E: segundos extra de duración")] FieldDuration,
    [Tooltip("Frieren E: segundos que baja el enfriamiento")] FieldCooldown,
    [Tooltip("Frieren E: fracción extra de cura por segundo (0,01 = +1 punto)")] FieldHeal,
    [Tooltip("Frieren E: ralentización extra (0,1 = +10 puntos; el total no pasa de 0,8)")] FieldSlow,
    [Tooltip("Frieren E: veneno por segundo como fracción del daño básico (0,5)")] FieldPoison,
    [Tooltip("Frieren E: fracción extra de daño del veneno (0,5 = +50%)")] FieldPoisonDamagePercent,
    [Tooltip("Frieren F: segundos extra de duración")] PulseDuration,
    [Tooltip("Frieren F: segundos extra de aturdimiento")] PulseStun,
    [Tooltip("Frieren F: segundos que baja el enfriamiento")] PulseCooldown,
    [Tooltip("Frieren F, Dominio (1): el pulso alcanza a todos los enemigos")] PulseWholeMap,
    [Tooltip("Frieren F, Lluvia de Zoltraak: segundos entre Zoltraaks (1)")] PulseZoltraakRain,
    [Tooltip("Frieren F, Explosión final: multiplicador del Zoltraak completo (3)")] PulseFinalBlast,
    [Tooltip("Frieren F, Marca de maná: daño extra que reciben los tocados (0,3 = +30%)")] PulseMark
```

- [ ] **Step 4: Campos de `TreeBonuses`** (después de `RoarRadiusBonus`):

```csharp
    public float ZoltraakDamagePercent;
    public float ZoltraakChargeReduction;
    public float ZoltraakRadiusBonus;
    public bool ZoltraakOvercharge;
    public float ZoltraakEchoFraction;
    public float ZoltraakFrostSlow;
    public float ManaMaxBonus;
    public float ManaRegenBonus;
    public float ManaCostReduction;
    public float ManaOnKill;
    /// <summary>Multiplicador de regeneración de Concentración (0 = sin el nodo).</summary>
    public float ManaFocusMultiplier;
    public float BeamDamagePercent;
    public float BeamRadiusBonus;
    public float BeamCooldownReduction;
    public int BeamExtraCharges;
    public float BeamFrostSlow;
    public float BeamPierceDamage;
    public float FieldRadiusBonus;
    public float FieldDurationBonus;
    public float FieldCooldownReduction;
    public float FieldHealBonus;
    public float FieldSlowBonus;
    public float FieldPoison;
    public float FieldPoisonDamagePercent;
    public float PulseDurationBonus;
    public float PulseStunBonus;
    public float PulseCooldownReduction;
    public bool PulseWholeMap;
    /// <summary>Segundos entre los Zoltraak de la Lluvia (0 = sin el nodo).</summary>
    public float PulseRainInterval;
    public float PulseFinalBlastMultiplier;
    public float PulseMarkBonus;
```

- [ ] **Step 5: `Apply`** — casos nuevos en el `switch` de `SkillTreeRules.Apply`:

```csharp
            case SkillEffectType.ZoltraakDamagePercent: b.ZoltraakDamagePercent += v; break;
            case SkillEffectType.ZoltraakChargeTime: b.ZoltraakChargeReduction += v; break;
            case SkillEffectType.ZoltraakRadius: b.ZoltraakRadiusBonus += v; break;
            case SkillEffectType.ZoltraakOvercharge: b.ZoltraakOvercharge |= v > 0f; break;
            case SkillEffectType.ZoltraakEcho: b.ZoltraakEchoFraction = Mathf.Max(b.ZoltraakEchoFraction, v); break;
            case SkillEffectType.ZoltraakFrost: b.ZoltraakFrostSlow = Mathf.Max(b.ZoltraakFrostSlow, v); break;
            case SkillEffectType.ManaMax: b.ManaMaxBonus += v; break;
            case SkillEffectType.ManaRegen: b.ManaRegenBonus += v; break;
            case SkillEffectType.ManaCostPercent: b.ManaCostReduction += v; break;
            case SkillEffectType.ManaOnKill: b.ManaOnKill += v; break;
            case SkillEffectType.ManaFocus: b.ManaFocusMultiplier = Mathf.Max(b.ManaFocusMultiplier, v); break;
            case SkillEffectType.BeamDamagePercent: b.BeamDamagePercent += v; break;
            case SkillEffectType.BeamRadius: b.BeamRadiusBonus += v; break;
            case SkillEffectType.BeamCooldown: b.BeamCooldownReduction += v; break;
            case SkillEffectType.BeamExtraCharges: b.BeamExtraCharges += Mathf.RoundToInt(v); break;
            case SkillEffectType.BeamFrost: b.BeamFrostSlow = Mathf.Max(b.BeamFrostSlow, v); break;
            case SkillEffectType.BeamPierceDamage: b.BeamPierceDamage += v; break;
            case SkillEffectType.FieldRadius: b.FieldRadiusBonus += v; break;
            case SkillEffectType.FieldDuration: b.FieldDurationBonus += v; break;
            case SkillEffectType.FieldCooldown: b.FieldCooldownReduction += v; break;
            case SkillEffectType.FieldHeal: b.FieldHealBonus += v; break;
            case SkillEffectType.FieldSlow: b.FieldSlowBonus += v; break;
            case SkillEffectType.FieldPoison: b.FieldPoison = Mathf.Max(b.FieldPoison, v); break;
            case SkillEffectType.FieldPoisonDamagePercent: b.FieldPoisonDamagePercent += v; break;
            case SkillEffectType.PulseDuration: b.PulseDurationBonus += v; break;
            case SkillEffectType.PulseStun: b.PulseStunBonus += v; break;
            case SkillEffectType.PulseCooldown: b.PulseCooldownReduction += v; break;
            case SkillEffectType.PulseWholeMap: b.PulseWholeMap |= v > 0f; break;
            case SkillEffectType.PulseZoltraakRain: b.PulseRainInterval = v > 0f ? v : b.PulseRainInterval; break;
            case SkillEffectType.PulseFinalBlast: b.PulseFinalBlastMultiplier = Mathf.Max(b.PulseFinalBlastMultiplier, v); break;
            case SkillEffectType.PulseMark: b.PulseMarkBonus = Mathf.Max(b.PulseMarkBonus, v); break;
```

- [ ] **Step 6: Crear `Assets/Scripts/Core/Data/FrierenTreeMath.cs`:**

```csharp
using UnityEngine;

/// <summary>
/// Fórmulas y números fijos del árbol de Frieren. Lógica pura: la usa el juego y la prueban los tests. Lo que se ajusta por
/// nodo vive en el asset del árbol (el valor de cada efecto); aquí quedan los tiempos y topes que no dependen del nodo.
/// </summary>
public static class FrierenTreeMath
{
    public const float MinChargeSeconds = 0.3f;
    public const float FieldSlowCap = 0.8f;
    public const float PierceCap = 0.9f;
    public const float FocusDelay = 3f;
    public const float EchoDelay = 0.3f;
    public const float EchoRadiusFraction = 0.7f;
    public const float FrostSeconds = 3f;
    public const float PoisonLinger = 3f;
    public const float PoisonTick = 0.5f;
    public const float RainDropSeconds = 0.4f;
    public const float RainHeight = 12f;
    /// <summary>Radio que usa el pulso con Dominio: abarca el mapa entero.</summary>
    public const float WholeMapRadius = 1000f;

    /// <summary>Carga del Zoltraak con la reducción del árbol; nunca baja de MinChargeSeconds.</summary>
    public static float ChargeSeconds(float baseSeconds, float reduction) => Mathf.Max(MinChargeSeconds, baseSeconds - reduction);

    /// <summary>Ralentización del campo con el bono de las mitades; tope FieldSlowCap.</summary>
    public static float FieldSlow(float baseFraction, float bonus) => Mathf.Min(FieldSlowCap, baseFraction + bonus);

    /// <summary>Costo de maná con Eficiencia (0,4 = -40%), redondeado a entero.</summary>
    public static float ManaCost(float baseCost, float reduction) => Mathf.Round(baseCost * (1f - Mathf.Clamp01(reduction)));

    /// <summary>Perforación creciente: el enemigo número 'index' (0 = el primero) recibe x(1 + min(index·perEnemy, tope)).</summary>
    public static float PierceMultiplier(int index, float perEnemy) => 1f + Mathf.Min(Mathf.Max(0, index) * perEnemy, PierceCap);

    /// <summary>Daño de cada tick de veneno (cada PoisonTick s): fracción del disparo básico por segundo, con el bono. 0 sin el nodo.</summary>
    public static int PoisonTickDamage(int basicDamage, float fractionPerSecond, float bonusPercent) =>
        fractionPerSecond <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(basicDamage * fractionPerSecond * (1f + bonusPercent) * PoisonTick));

    /// <summary>Concentración: el multiplicador si pasaron FocusDelay s desde el último daño; si no (o sin el nodo), 1.</summary>
    public static float RegenMultiplier(float focusMultiplier, float now, float lastDamagedAt) =>
        focusMultiplier > 1f && now - lastDamagedAt >= FocusDelay ? focusMultiplier : 1f;

    /// <summary>Daño con un bono en fracción (0,3 = +30%), redondeado.</summary>
    public static int Scale(int damage, float bonusPercent) => bonusPercent <= 0f ? damage : Mathf.RoundToInt(damage * (1f + bonusPercent));
}
```

- [ ] **Step 7: Crear `Assets/Scripts/Core/Data/SlowRules.cs`:**

```csharp
using UnityEngine;

/// <summary>Ralentizaciones que se pisan: mientras una dura, solo la reemplaza (y la renueva) otra igual o más fuerte.</summary>
public static class SlowRules
{
    /// <param name="activeMultiplier">Multiplicador de velocidad actual (0,4 = va al 40%).</param>
    /// <param name="active">Si esa ralentización sigue vigente.</param>
    /// <param name="newFraction">Fracción que quita la nueva (0,6 = -60%).</param>
    public static bool ShouldReplace(float activeMultiplier, bool active, float newFraction) =>
        !active || Mathf.Clamp(1f - newFraction, 0.1f, 1f) <= activeMultiplier + 1e-4f;
}
```

- [ ] **Step 8: `ManaPool.Gain`** (después de `Refill`):

```csharp
    /// <summary>Suma maná sin pasar del máximo (Absorción). Los valores negativos se ignoran.</summary>
    public void Gain(float amount) => Current = Mathf.Min(Max, Current + Mathf.Max(0f, amount));
```

- [ ] **Step 9: Compilar y correr** — `filter = "FrierenTree"` pasa; batería completa **sin fallos** (548 + 23 = 571 contando los `TestCase`; anotar el número real).

---

### Task 3: El asset `Frieren_Tree` y su disposición

**Files:**
- Create: `Assets/Data/Skills/Frieren_Tree.asset` (script de editor)
- Modify: `Assets/Data/Characters/Maga.asset` (`skillTree`), `Assets/Tests/EditMode/SkillTreeLayoutTests.cs`
- Test: `Assets/Tests/EditMode/FrierenTreeAssetTests.cs`

**Interfaces:**
- Consumes: `SkillNode.choiceGroup`, `SkillNode.half` (Task 1); efectos (Task 2).
- Produces: el asset con id `frieren_tree` y los ids de nodo del spec §3/§4 (los usan las pruebas en Play).

- [ ] **Step 1: Escribir los tests que fallan** — crear `Assets/Tests/EditMode/FrierenTreeAssetTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Valida el árbol real de Frieren (el asset), para que un error de edición no llegue a la partida.</summary>
[TestFixture]
public class FrierenTreeAssetTests
{
    private const string TreePath = "Assets/Data/Skills/Frieren_Tree.asset";
    private const string CharacterPath = "Assets/Data/Characters/Maga.asset";

    private static SkillTreeDefinition Tree
    {
        get
        {
            var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(TreePath);
            Assert.IsNotNull(tree, "Falta " + TreePath);
            return tree;
        }
    }

    [Test]
    public void Maga_PointsToTheTree() =>
        Assert.AreSame(Tree, AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterPath).skillTree);

    [Test]
    public void Tree_Has56NodesWithUniqueIdsAndText()
    {
        SkillNode[] nodes = Tree.nodes;
        Assert.AreEqual(56, nodes.Length);
        Assert.AreEqual(nodes.Length, nodes.Select(n => n.id).Distinct().Count(), "ids repetidos");
        foreach (SkillNode node in nodes)
        {
            Assert.IsFalse(string.IsNullOrEmpty(node.displayName), node.id + " sin nombre");
            Assert.IsFalse(string.IsNullOrEmpty(node.description), node.id + " sin descripción");
            Assert.GreaterOrEqual(node.cost, 1, node.id);
            Assert.IsNotEmpty(node.effects, node.id + " sin efecto");
        }
    }

    [Test]
    public void Tree_HasOneRootAndEverythingIsReachable()
    {
        SkillTreeDefinition tree = Tree;
        Assert.AreEqual(1, tree.nodes.Count(n => n.isRoot));
        Assert.AreEqual("core", tree.nodes.Single(n => n.isRoot).id);

        foreach (SkillNode node in tree.nodes)
            foreach (string other in node.connections)
                Assert.IsNotNull(tree.Find(other), node.id + " conecta con " + other + ", que no existe");

        // Todos los ids juntos forman un grafo conectado desde la raíz.
        Assert.IsTrue(SkillTreeRules.AllConnected(tree, tree.nodes.Select(n => n.id).ToList()));
    }

    [TestCase("zoltraak", 3)]
    [TestCase("mana", 3)]
    [TestCase("rayo", 3)]
    [TestCase("pulso", 4)]
    public void ChoiceGroups_HaveTheirOptionsAndNothingDependsOnThem(string group, int options)
    {
        SkillTreeDefinition tree = Tree;
        SkillNode[] members = tree.nodes.Where(n => n.choiceGroup == group).ToArray();
        Assert.AreEqual(options, members.Length, group);

        foreach (SkillNode member in members)
        {
            Assert.AreEqual(SkillNodeHalf.None, member.half);
            Assert.AreEqual(1, member.connections.Length, member.id + " cuelga de un solo nodo");
            // Ningún otro nodo declara conexión con una opción final.
            Assert.IsFalse(tree.nodes.Any(n => n != member && n.connections.Contains(member.id)), member.id + " tiene dependientes");
        }
        Assert.AreEqual(1, members.Select(m => m.connections[0]).Distinct().Count(), group + ": todas cuelgan del mismo nodo");
    }

    [TestCase("flor1", 2)]
    [TestCase("flor2", 3)]
    [TestCase("flor3", 4)]
    [TestCase("flor4", 5)]
    public void SplitNodes_HaveOneLeftAndOneRightHalfInTheSameSquare(string group, int cost)
    {
        SkillNode[] halves = Tree.nodes.Where(n => n.choiceGroup == group).ToArray();
        Assert.AreEqual(2, halves.Length);
        Assert.AreEqual(1, halves.Count(h => h.half == SkillNodeHalf.Left));
        Assert.AreEqual(1, halves.Count(h => h.half == SkillNodeHalf.Right));
        Assert.AreEqual(halves[0].position, halves[1].position);
        Assert.IsTrue(halves.All(h => h.cost == cost));
        Assert.AreEqual(SkillEffectType.FieldHeal, halves.Single(h => h.half == SkillNodeHalf.Left).effects[0].type);
        Assert.AreEqual(SkillEffectType.FieldSlow, halves.Single(h => h.half == SkillNodeHalf.Right).effects[0].type);
    }

    [Test]
    public void BuyingOneOptionPerGroup_Costs151()
    {
        int total = 0;
        var seen = new HashSet<string>();
        foreach (SkillNode node in Tree.nodes)
        {
            if (!string.IsNullOrEmpty(node.choiceGroup) && !seen.Add(node.choiceGroup)) continue;
            total += node.cost;
        }
        Assert.AreEqual(151, total);
    }

    [Test]
    public void FullBuild_HitsTheSpecCaps()
    {
        // Una opción por grupo (las de cura en la E): cura 3% + 4% = 7%; con las de freno: 40% + 40% = 80% (el tope).
        SkillTreeDefinition tree = Tree;
        var heal = new List<string>();
        var slow = new List<string>();
        var seen = new HashSet<string>();
        foreach (SkillNode node in tree.nodes)
        {
            if (node.half == SkillNodeHalf.Left) heal.Add(node.id);
            else if (node.half == SkillNodeHalf.Right) slow.Add(node.id);
            else if (string.IsNullOrEmpty(node.choiceGroup) || seen.Add(node.choiceGroup)) { heal.Add(node.id); slow.Add(node.id); }
        }

        TreeBonuses withHeal = SkillTreeRules.Compute(tree, heal);
        TreeBonuses withSlow = SkillTreeRules.Compute(tree, slow);
        Assert.AreEqual(0.04f, withHeal.FieldHealBonus, 1e-4f);
        Assert.AreEqual(0.4f, withSlow.FieldSlowBonus, 1e-4f);
        Assert.AreEqual(0.3f, withHeal.ZoltraakChargeReduction, 1e-4f);
        Assert.AreEqual(40f, withHeal.ManaMaxBonus, 1e-4f);
        Assert.AreEqual(16f, withHeal.PulseCooldownReduction, 1e-4f);
        Assert.AreEqual(0.15f, withHeal.DamagePercent, 1e-4f);   // 5 + 3 + 3 + 4
    }
}
```

- [ ] **Step 2: Extender `SkillTreeLayoutTests`** — agregar `[TestCase("Assets/Data/Skills/Frieren_Tree.asset")]` a los tres tests existentes, y en `Nodes_KeepAtLeastTheMinimumGapBetweenThem` saltar las dos mitades de un mismo nodo dividido:

```csharp
                SkillNode a = tree.nodes[i], b = tree.nodes[j];
                // Las dos mitades de un nodo dividido comparten el cuadrado a propósito.
                if (a.half != SkillNodeHalf.None && b.half != SkillNodeHalf.None && a.choiceGroup == b.choiceGroup) continue;
```

y un test nuevo para el aire extra que pidió el usuario:

```csharp
    [Test]
    public void Frieren_HasExtraAirBetweenSquares()
    {
        var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>("Assets/Data/Skills/Frieren_Tree.asset");
        for (int i = 0; i < tree.nodes.Length; i++)
        {
            for (int j = i + 1; j < tree.nodes.Length; j++)
            {
                SkillNode a = tree.nodes[i], b = tree.nodes[j];
                if (a.half != SkillNodeHalf.None && b.half != SkillNodeHalf.None && a.choiceGroup == b.choiceGroup) continue;

                float d = Vector2.Distance(a.position, b.position);
                Assert.GreaterOrEqual(d, 1.4f - 0.01f, a.id + " y " + b.id + " están a " + d.ToString("F2"));
            }
        }
    }
```

- [ ] **Step 3: Verificar que fallan** (`Falta Assets/Data/Skills/Frieren_Tree.asset`).

- [ ] **Step 4: Crear el árbol** — `unity_execute_code` (con `using UnityEditor; using UnityEngine; using System.Collections.Generic;`). Si `unity_execute_code` no admite funciones locales, usar `System.Func<...>`:

```csharp
var list = new List<SkillNode>();
System.Action<string, string, string, int, float, float, string[], string, SkillNodeHalf, SkillEffectType[], float[]> add =
    (id, name, desc, cost, x, y, links, group, half, types, values) =>
    {
        var effects = new SkillEffect[types.Length];
        for (int i = 0; i < types.Length; i++) effects[i] = new SkillEffect { type = types[i], value = values[i] };
        list.Add(new SkillNode { id = id, displayName = name, description = desc, cost = cost, position = new Vector2(x, y),
            connections = links, isRoot = id == "core", choiceGroup = group, half = half, effects = effects });
    };
System.Func<string[], string[]> L = a => a;
var N = SkillNodeHalf.None; var Lh = SkillNodeHalf.Left; var Rh = SkillNodeHalf.Right;
System.Func<SkillEffectType, SkillEffectType[]> T = t => new[] { t };
System.Func<float, float[]> V = v => new[] { v };

add("core", "Grimorio", "+5% de daño a todo.", 1, 0f, 0f, L(new string[0]), "", N, T(SkillEffectType.DamagePercent), V(0.05f));
// Raíces
add("v1", "Vitalidad I", "+10 de vida máxima.", 1, -1.5f, -1.5f, L(new[] { "core" }), "", N, T(SkillEffectType.MaxHealth), V(10f));
add("v2", "Vitalidad II", "+15 de vida máxima.", 2, -2.5f, -2.8f, L(new[] { "v1" }), "", N, T(SkillEffectType.MaxHealth), V(15f));
add("v3", "Vitalidad III", "+20 de vida máxima.", 3, -3.5f, -4.1f, L(new[] { "v2" }), "", N, T(SkillEffectType.MaxHealth), V(20f));
add("d1", "Poder arcano I", "+3% de daño a todo.", 1, 0f, -1.5f, L(new[] { "core" }), "", N, T(SkillEffectType.DamagePercent), V(0.03f));
add("d2", "Poder arcano II", "+3% de daño a todo.", 2, 0f, -3f, L(new[] { "d1" }), "", N, T(SkillEffectType.DamagePercent), V(0.03f));
add("d3", "Poder arcano III", "+4% de daño a todo.", 4, 0f, -4.5f, L(new[] { "d2" }), "", N, T(SkillEffectType.DamagePercent), V(0.04f));
add("s1", "Zancada I", "+3% de velocidad de movimiento.", 1, 1.5f, -1.5f, L(new[] { "core" }), "", N, T(SkillEffectType.MoveSpeedPercent), V(0.03f));
add("s2", "Zancada II", "+3% de velocidad de movimiento.", 2, 2.5f, -2.8f, L(new[] { "s1" }), "", N, T(SkillEffectType.MoveSpeedPercent), V(0.03f));
add("s3", "Zancada III", "+4% de velocidad de movimiento.", 3, 3.5f, -4.1f, L(new[] { "s2" }), "", N, T(SkillEffectType.MoveSpeedPercent), V(0.04f));
// Zoltraak (arriba)
add("za1", "Zoltraak afinado I", "+15% de daño del Zoltraak (también el Eco, la Lluvia y la Explosión final).", 2, 0f, 1.8f, L(new[] { "core" }), "", N, T(SkillEffectType.ZoltraakDamagePercent), V(0.15f));
add("zc1", "Canalización rápida I", "La carga del Zoltraak tarda 0,15 s menos.", 2, -1.5f, 3.3f, L(new[] { "za1" }), "", N, T(SkillEffectType.ZoltraakChargeTime), V(0.15f));
add("zc2", "Canalización rápida II", "La carga del Zoltraak tarda 0,15 s menos (1,2 → 0,9 s con las dos).", 4, -1.5f, 4.8f, L(new[] { "zc1" }), "", N, T(SkillEffectType.ZoltraakChargeTime), V(0.15f));
add("za2", "Zoltraak afinado II", "+15% de daño del Zoltraak.", 4, 0f, 4.2f, L(new[] { "za1" }), "", N, T(SkillEffectType.ZoltraakDamagePercent), V(0.15f));
add("zr1", "Onda expansiva I", "+0,5 m de radio del Zoltraak.", 3, 1.5f, 3.3f, L(new[] { "za1" }), "", N, T(SkillEffectType.ZoltraakRadius), V(0.5f));
add("zr2", "Onda expansiva II", "+0,5 m de radio del Zoltraak.", 5, 1.5f, 4.8f, L(new[] { "zr1" }), "", N, T(SkillEffectType.ZoltraakRadius), V(0.5f));
add("zo1", "Sobrecarga", "Elige 1. Tras soltar un Zoltraak al 100%, el siguiente sale cargado al instante.", 7, -1.6f, 6.5f, L(new[] { "za2" }), "zoltraak", N, T(SkillEffectType.ZoltraakOvercharge), V(1f));
add("zo2", "Eco", "Elige 1. A carga completa, 0,3 s después cae una segunda explosión con 50% del daño y 70% del radio.", 7, 0f, 6.2f, L(new[] { "za2" }), "zoltraak", N, T(SkillEffectType.ZoltraakEcho), V(0.5f));
add("zo3", "Escarcha arcana", "Elige 1. A carga completa, ralentiza 40% durante 3 s a los enemigos golpeados.", 7, 1.6f, 6.5f, L(new[] { "za2" }), "zoltraak", N, T(SkillEffectType.ZoltraakFrost), V(0.4f));
// Q (arriba a la izquierda)
add("qi1", "Rayo intenso I", "+15% de daño del rayo de maná.", 2, -3f, 1.6f, L(new[] { "core" }), "", N, T(SkillEffectType.BeamDamagePercent), V(0.15f));
add("qw", "Rayo ancho", "+0,2 m de grosor del rayo (0,4 → 0,6).", 3, -3f, 3.3f, L(new[] { "qi1" }), "", N, T(SkillEffectType.BeamRadius), V(0.2f));
add("qi2", "Rayo intenso II", "+15% de daño del rayo de maná.", 4, -4.6f, 3.4f, L(new[] { "qi1" }), "", N, T(SkillEffectType.BeamDamagePercent), V(0.15f));
add("qc1", "Recarga arcana I", "El rayo enfría 0,5 s menos.", 2, -6f, 3f, L(new[] { "qi1" }), "", N, T(SkillEffectType.BeamCooldown), V(0.5f));
add("qc2", "Recarga arcana II", "El rayo enfría 0,5 s menos.", 4, -6f, 4.6f, L(new[] { "qc1" }), "", N, T(SkillEffectType.BeamCooldown), V(0.5f));
add("qo1", "Doble carga", "Elige 1. +1 carga: dos rayos seguidos; se recargan de una en una.", 7, -6f, 6.2f, L(new[] { "qi2" }), "rayo", N, T(SkillEffectType.BeamExtraCharges), V(1f));
add("qo2", "Rayo gélido", "Elige 1. Ralentiza 60% durante 3 s a cada enemigo que atraviesa.", 7, -4.6f, 5.6f, L(new[] { "qi2" }), "rayo", N, T(SkillEffectType.BeamFrost), V(0.6f));
add("qo3", "Perforación creciente", "Elige 1. +15% de daño por cada enemigo ya atravesado (hasta +90%).", 7, -3.2f, 6.2f, L(new[] { "qi2" }), "rayo", N, T(SkillEffectType.BeamPierceDamage), V(0.15f));
// Maná (izquierda)
add("mr1", "Reserva I", "+20 de maná máximo.", 2, -3f, 0f, L(new[] { "core" }), "", N, T(SkillEffectType.ManaMax), V(20f));
add("mr2", "Reserva II", "+20 de maná máximo.", 4, -4.6f, -1f, L(new[] { "mr1" }), "", N, T(SkillEffectType.ManaMax), V(20f));
add("mf1", "Flujo I", "+0,75 de maná por segundo.", 2, -4.6f, 0.6f, L(new[] { "mr1" }), "", N, T(SkillEffectType.ManaRegen), V(0.75f));
add("mf2", "Flujo II", "+0,75 de maná por segundo.", 4, -6.2f, 1f, L(new[] { "mf1" }), "", N, T(SkillEffectType.ManaRegen), V(0.75f));
add("mo1", "Eficiencia", "Elige 1. La Q, la E y la F cuestan 40% menos maná.", 7, -7.6f, 2.4f, L(new[] { "mf2" }), "mana", N, T(SkillEffectType.ManaCostPercent), V(0.4f));
add("mo2", "Absorción", "Elige 1. +3 de maná por cada enemigo que muere.", 7, -8f, 0.9f, L(new[] { "mf2" }), "mana", N, T(SkillEffectType.ManaOnKill), V(3f));
add("mo3", "Concentración", "Elige 1. Regeneras el doble de maná si no recibiste daño en 3 s.", 7, -7.6f, -0.6f, L(new[] { "mf2" }), "mana", N, T(SkillEffectType.ManaFocus), V(2f));
// E (arriba a la derecha)
add("ep", "Pradera", "+1 m de radio del campo de flores.", 2, 3f, 1.6f, L(new[] { "core" }), "", N, T(SkillEffectType.FieldRadius), V(1f));
add("ev1", "Flores venenosas", "El campo envenena: cada segundo, la mitad del daño del disparo básico; sigue 3 s después de salir.", 5, 3f, 3.3f, L(new[] { "ep" }), "", N, T(SkillEffectType.FieldPoison), V(0.5f));
add("ev2", "Veneno II", "+50% de daño del veneno.", 6, 3f, 4.9f, L(new[] { "ev1" }), "", N, T(SkillEffectType.FieldPoisonDamagePercent), V(0.5f));
string[] romans = { "I", "II", "III", "IV" };
float[] splitY = { 3.4f, 4.9f, 6.4f, 7.9f };
for (int k = 0; k < 4; k++)
{
    string[] below = k == 0 ? new[] { "ep" } : new[] { "es" + k + "c", "es" + k + "r" };
    add("es" + (k + 1) + "c", "Cura " + romans[k], "Elige una mitad. El campo cura +1% de tu vida máxima por segundo.", k + 2, 4.5f, splitY[k], below, "flor" + (k + 1), Lh, T(SkillEffectType.FieldHeal), V(0.01f));
    add("es" + (k + 1) + "r", "Freno " + romans[k], "Elige una mitad. El campo ralentiza 10 puntos más (máximo 80%).", k + 2, 4.5f, splitY[k], below, "flor" + (k + 1), Rh, T(SkillEffectType.FieldSlow), V(0.1f));
}
add("ef", "Floración larga", "El campo dura 1,5 s más.", 3, 6f, 3.2f, L(new[] { "ep" }), "", N, T(SkillEffectType.FieldDuration), V(1.5f));
add("eb", "Brote rápido", "El campo enfría 3 s menos.", 3, 6.1f, 4.8f, L(new[] { "ef" }), "", N, T(SkillEffectType.FieldCooldown), V(3f));
// F (derecha)
add("fp1", "Pulso prolongado I", "El pulso dura 1,5 s más.", 3, 3f, 0f, L(new[] { "core" }), "", N, T(SkillEffectType.PulseDuration), V(1.5f));
add("ft", "Terror", "El pulso aturde 0,5 s más.", 4, 4.6f, 1.4f, L(new[] { "fp1" }), "", N, T(SkillEffectType.PulseStun), V(0.5f));
add("fp2", "Pulso prolongado II", "El pulso dura 1,5 s más.", 5, 4.8f, -0.2f, L(new[] { "fp1" }), "", N, T(SkillEffectType.PulseDuration), V(1.5f));
add("fr1", "Recuperación I", "El pulso enfría 8 s menos.", 4, 4.4f, -1.9f, L(new[] { "fp1" }), "", N, T(SkillEffectType.PulseCooldown), V(8f));
add("fr2", "Recuperación II", "El pulso enfría 8 s menos.", 6, 5.6f, -3.3f, L(new[] { "fr1" }), "", N, T(SkillEffectType.PulseCooldown), V(8f));
add("fo1", "Dominio", "Elige 1. El pulso aturde y ralentiza a todos los enemigos del mapa.", 8, 7.2f, 1.8f, L(new[] { "fp2" }), "pulso", N, T(SkillEffectType.PulseWholeMap), V(1f));
add("fo2", "Lluvia de Zoltraak", "Elige 1. Mientras dura, cada 1 s cae del cielo un Zoltraak completo sobre un enemigo al azar del radio.", 8, 7.6f, 0.4f, L(new[] { "fp2" }), "pulso", N, T(SkillEffectType.PulseZoltraakRain), V(1f));
add("fo3", "Explosión final", "Elige 1. Al terminar, todos los enemigos del radio reciben el daño de 3 Zoltraak completos.", 8, 7.6f, -1.1f, L(new[] { "fp2" }), "pulso", N, T(SkillEffectType.PulseFinalBlast), V(3f));
add("fo4", "Marca de maná", "Elige 1. Los enemigos tocados por el pulso reciben +30% de daño de todo mientras dure.", 8, 7.2f, -2.5f, L(new[] { "fp2" }), "pulso", N, T(SkillEffectType.PulseMark), V(0.3f));

// El centro declara sus 8 vecinos (la conexión ya vale en los dos sentidos; así se lee mejor en el inspector).
list[0].connections = new[] { "v1", "d1", "s1", "za1", "qi1", "mr1", "ep", "fp1" };

var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
tree.nodes = list.ToArray();
AssetDatabase.CreateAsset(tree, "Assets/Data/Skills/Frieren_Tree.asset");
var so = new SerializedObject(tree);
so.FindProperty("id").stringValue = "frieren_tree";
so.ApplyModifiedPropertiesWithoutUndo();

var maga = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Maga.asset");
maga.skillTree = tree;
EditorUtility.SetDirty(tree);
EditorUtility.SetDirty(maga);
AssetDatabase.SaveAssets();
return "nodos=" + tree.nodes.Length;
```

(`id` es el campo privado serializado de `GameDefinition`.) Esperado: `nodos=56`.

- [ ] **Step 5: Correr** `filter = "FrierenTreeAsset"` y `filter = "SkillTreeLayout"`: todo pasa. Batería completa sin fallos.

---

### Task 4: Enemigos — ralentización más fuerte, Marca, veneno; último daño del jugador

**Files:**
- Modify: `Assets/Scripts/Gameplay/Enemies/EnemyAI.cs`, `Assets/Scripts/Core/GameEvents.cs`, `Assets/Scripts/UI/FloatingTextManager.cs`, `Assets/Scripts/Gameplay/Base/Health.cs`
- Create: `Assets/Scripts/Gameplay/Enemies/EnemyPoison.cs`

**Interfaces:**
- Consumes: `SlowRules.ShouldReplace`, `FrierenTreeMath.Scale` (Task 2).
- Produces: `EnemyAI.ApplyDamageTakenBonus(float bonus, float seconds)`, `EnemyAI.ApplyPoison(float seconds, int damagePerTick, float tickSeconds)`, `EnemyAI.IsPoisoned`; `GameEvents.PoisonTick` / `RaisePoisonTick(Vector3, int)`; `Health.LastDamagedAt` (float).

- [ ] **Step 1: `EnemyAI.ApplySlow`** — gana la más fuerte:

```csharp
    /// <summary>Le quita una fracción de velocidad durante unos segundos (0,3 = -30%). Mientras una dura, solo la reemplaza otra igual o más fuerte.</summary>
    public void ApplySlow(float fraction, float seconds)
    {
        if (isDead || fraction <= 0f || seconds <= 0f) return;
        if (!SlowRules.ShouldReplace(slowMultiplier, Time.time < slowUntil, fraction)) return;

        slowMultiplier = Mathf.Clamp(1f - fraction, 0.1f, 1f);
        slowUntil = Time.time + seconds;
        ApplySpeed();
    }
```

- [ ] **Step 2: Marca de maná** — campos `private float damageTakenBonus; private float damageTakenUntil;`, reiniciados en `Spawn` (`damageTakenBonus = 0f; damageTakenUntil = 0f;` junto a `slowUntil`), método nuevo:

```csharp
    /// <summary>Recibe más daño de todo durante unos segundos (0,3 = +30%). Conserva el bono mayor.</summary>
    public void ApplyDamageTakenBonus(float bonus, float seconds)
    {
        if (isDead || bonus <= 0f || seconds <= 0f) return;
        if (Time.time < damageTakenUntil && damageTakenBonus > bonus) return;

        damageTakenBonus = bonus;
        damageTakenUntil = Mathf.Max(damageTakenUntil, Time.time + seconds);
    }
```

y en `TakeDamage`, justo después de la primera línea (`if (isDead || ...) return;`):

```csharp
        if (Time.time < damageTakenUntil) damage = FrierenTreeMath.Scale(damage, damageTakenBonus);
```

- [ ] **Step 3: Veneno** — `GameEvents`: `public static event Action<Vector3, int> PoisonTick;  // número verde del veneno de Frieren` y `public static void RaisePoisonTick(Vector3 worldPosition, int damage) => PoisonTick?.Invoke(worldPosition, damage);` junto a `BurnTick`. Crear `EnemyPoison.cs` copiando `EnemyBurn.cs` entero con estos cambios: clase `EnemyPoison`; `PoisonColor = new Color(0.35f, 0.9f, 0.2f)`; `IsPoisoned`; `GameEvents.RaisePoisonTick(...)`; resumen "Veneno de un enemigo (campo de flores de Frieren): ... número verde ...". Reutiliza `BurnState` (misma lógica: renovar, conservar el mayor daño). En `EnemyAI`: campo `private EnemyPoison poison;`, `if (poison != null) poison.Clear();` en `Spawn` y al morir (junto a los de `burn`), y:

```csharp
    /// <summary>Lo envenena unos segundos (daño cada tick). Renueva y conserva el mayor daño. No hace nada si ya murió.</summary>
    public void ApplyPoison(float seconds, int damagePerTick, float tickSeconds)
    {
        if (isDead || seconds <= 0f || damagePerTick <= 0) return;

        if (poison == null)
        {
            poison = GetComponent<EnemyPoison>();
            if (poison == null) poison = gameObject.AddComponent<EnemyPoison>();
        }

        poison.Apply(seconds, damagePerTick, tickSeconds);
    }

    public bool IsPoisoned => poison != null && poison.IsPoisoned;
```

`FloatingTextManager`: suscribir `GameEvents.PoisonTick += ShowPoisonTick` (y desuscribir), con `ShowPoisonTick` copiado de `ShowBurnTick` cambiando solo el color a verde `new Color(0.45f, 1f, 0.3f)`.

- [ ] **Step 4: `Health.LastDamagedAt`** — `public float LastDamagedAt { get; private set; } = -999f;` y en `TakeDamage`, después de la guarda (`if (IsDead || Invulnerable) return;`): `LastDamagedAt = Time.time;`.

- [ ] **Step 5: Compilar y correr toda la batería** — 0 errores, sin fallos.

---

### Task 5: Zoltraak y maná en el juego

**Files:**
- Modify: `Assets/Scripts/Gameplay/Weapons/WeaponState.cs`, `Assets/Scripts/Gameplay/Player/ZoltraakCaster.cs`, `Assets/Scripts/Gameplay/Player/Shooting.cs`

**Interfaces:**
- Consumes: `TreeBonuses` (Task 2), `FrierenTreeMath`, `ManaPool.Gain`, `Health.LastDamagedAt` (Task 4).
- Produces: `ZoltraakCaster.DropFromSky(WeaponState weapon, Vector3 target)` y `ZoltraakCaster.RadiusFor(StaffDefinition, float)` (los usa el pulso, Task 6); `ZoltraakCaster.Overcharged` (bool, para pruebas).

- [ ] **Step 1: `WeaponState`** — el rayo pasa a usar el daño del árbol, y el Zoltraak suma su bono:

```csharp
    /// <summary>Daño del rayo de maná: el del bastón con el Poder de la tienda, por el daño del árbol y el del rayo.</summary>
    public int AbilityDamage
    {
        get
        {
            int baseDamage = Staff.AbilityDamageAt(GetLevel(UpgradeType.Damage));
            TreeBonuses tree = SkillTreeManager.CurrentBonuses;
            return Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage * tree.DamageMultiplier * (1f + tree.BeamDamagePercent)));
        }
    }

    /// <summary>Daño de un Zoltraak con esa carga (0 a 1): el del bastón con el Poder de la tienda, por el daño del árbol y el del Zoltraak.</summary>
    public int ZoltraakDamage(float charge)
    {
        int baseDamage = Staff.ZoltraakDamageAt(GetLevel(UpgradeType.Damage), charge);
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        return Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage * tree.DamageMultiplier * (1f + tree.ZoltraakDamagePercent)));
    }
```

- [ ] **Step 2: `ZoltraakCaster`** — `using System.Collections;`; campo `private bool overcharged;` y `public bool Overcharged => overcharged;`. En `Tick`, el bloque `if (!charge.IsCharging)` queda:

```csharp
        if (!charge.IsCharging)
        {
            // Sobrecarga: tras uno al 100%, el siguiente sale cargado con un clic (y ese no vuelve a armarla).
            if (overcharged && input.FirePressed && Time.time >= nextAllowed)
            {
                overcharged = false;
                StaffHud.SetCharge(-1f);
                Fire(weapon, 1f, true);
                if (shooting.Body != null) shooting.Body.PlayCast();
                return;
            }

            bool hasMana = shooting.Mana != null && shooting.Mana.Current >= staff.zoltraakManaCost;
            if (input.FirePressed && Time.time >= nextAllowed && hasMana)
            {
                charge.Begin(FrierenTreeMath.ChargeSeconds(staff.zoltraakChargeSeconds, SkillTreeManager.CurrentBonuses.ZoltraakChargeReduction));
                HoldCastPose(true);
            }
            return;
        }
```

y al final de `Tick`, tras `Fire(weapon, fraction, true);`:

```csharp
        // Sobrecarga armada: el anillo de carga se queda lleno hasta usarla.
        overcharged = fraction >= 1f && SkillTreeManager.CurrentBonuses.ZoltraakOvercharge;
        if (overcharged) StaffHud.SetCharge(1f);
```

(`Cancel()` no toca `overcharged`: colocar el campo no la borra.) En `Fire`, reemplazar desde `Vector3 point = ...` hasta el final por:

```csharp
        Vector3 point = shooting.AimGroundPoint(staff.zoltraakRange);
        shooting.ShowBolt(point, BoltColor, 0.12f, 0.12f);
        Explode(point, RadiusFor(staff, fraction), weapon.ZoltraakDamage(fraction), fraction >= 1f, true);
    }

    /// <summary>Radio del Zoltraak con esa carga, con el bono del árbol.</summary>
    public static float RadiusFor(StaffDefinition staff, float fraction) =>
        staff.ZoltraakRadiusAt(fraction) + SkillTreeManager.CurrentBonuses.ZoltraakRadiusBonus;

    /// <summary>Lluvia de Zoltraak (pulso): un rayo cae desde el cielo y, al llegar, explota como un Zoltraak a carga completa.</summary>
    public void DropFromSky(WeaponState weapon, Vector3 target) => StartCoroutine(Drop(weapon, target));

    private IEnumerator Drop(WeaponState weapon, Vector3 target)
    {
        shooting.ShowBeam(target + Vector3.up * FrierenTreeMath.RainHeight, target, BoltColor, 0.35f, FrierenTreeMath.RainDropSeconds);
        yield return new WaitForSeconds(FrierenTreeMath.RainDropSeconds);
        if (GameState.IsGameOver || weapon.Staff == null) yield break;

        Explode(target, RadiusFor(weapon.Staff, 1f), weapon.ZoltraakDamage(1f), true, true);
    }

    // Daña a todos los enemigos del radio. A carga completa aplica Escarcha y, si se permite, el Eco (que no repite Eco ni Escarcha).
    private void Explode(Vector3 point, float radius, int damage, bool fullCharge, bool allowEcho)
    {
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        AbilityVfx.ExplosionFlash(point, radius, BlastColor);

        // Primero se recogen los enemigos y después se les daña: matar a uno lo devuelve al pool y desactiva su objeto.
        targets.Clear();
        int count = Physics.OverlapSphereNonAlloc(point, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead || targets.Contains(enemy)) continue;

            targets.Add(enemy);
        }

        foreach (EnemyAI enemy in targets)
        {
            if (fullCharge && tree.ZoltraakFrostSlow > 0f) enemy.ApplySlow(tree.ZoltraakFrostSlow, FrierenTreeMath.FrostSeconds);
            enemy.TakeDamage(damage);
        }

        if (fullCharge && allowEcho && tree.ZoltraakEchoFraction > 0f)
            StartCoroutine(Echo(point, radius * FrierenTreeMath.EchoRadiusFraction, Mathf.Max(1, Mathf.RoundToInt(damage * tree.ZoltraakEchoFraction))));
    }

    private IEnumerator Echo(Vector3 point, float radius, int damage)
    {
        yield return new WaitForSeconds(FrierenTreeMath.EchoDelay);
        if (GameState.IsGameOver) yield break;

        Explode(point, radius, damage, false, false);
    }
```

(El Zoltraak instantáneo del pulso llama a `Fire(weapon, 1f, false)`: cuenta como carga completa, así que tiene Eco y Escarcha, y no arma la Sobrecarga porque no pasa por `Release`.)

- [ ] **Step 3: `Shooting` — maná del árbol** — campo `private PlayerHealth playerHealth;` asignado en `Awake` (`playerHealth = GetComponent<PlayerHealth>();`). En `ConfigureMana`:

```csharp
        WeaponState weapon = CurrentWeapon;
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        float max = weapon.Staff.manaMax + tree.ManaMaxBonus;
        float regen = weapon.ManaRegen + tree.ManaRegenBonus;
        if (mana == null) mana = new ManaPool(max, regen);
        else mana.Configure(max, regen);
```

En `TickMana`:

```csharp
        // Concentración: regenera más rápido si hace un rato que no recibe daño.
        float lastHit = playerHealth != null ? playerHealth.LastDamagedAt : -999f;
        mana.Tick(Time.deltaTime * FrierenTreeMath.RegenMultiplier(SkillTreeManager.CurrentBonuses.ManaFocusMultiplier, Time.time, lastHit));
        PublishMana(false);
```

`RefreshBuild` (lo llama el árbol al comprar, reiniciar o cambiar de personaje; siempre antes de la primera oleada) también rehace el maná y lo llena:

```csharp
    /// <summary>Reconfigura el maná y las habilidades con el árbol actual (el maná se llena: solo pasa antes de la primera oleada).</summary>
    public void RefreshBuild()
    {
        ConfigureMana();
        if (mana != null)
        {
            mana.Refill();
            PublishMana(true);
        }
        abilities.RefreshBuild();
    }
```

Absorción — agregar (Shooting no tiene `OnEnable`/`OnDisable` hoy):

```csharp
    private void OnEnable() => GameEvents.EnemyDied += OnEnemyDied;
    private void OnDisable() => GameEvents.EnemyDied -= OnEnemyDied;

    // Absorción (árbol de Frieren): maná por cada enemigo que muere.
    private void OnEnemyDied(Vector3 position, bool isBoss)
    {
        float gain = SkillTreeManager.CurrentBonuses.ManaOnKill;
        if (mana == null || gain <= 0f) return;

        mana.Gain(gain);
        PublishMana(true);
    }
```

- [ ] **Step 4: Compilar y correr toda la batería** — 0 errores, sin fallos (`BalanceTests` sigue verde: en EditMode no hay `SkillTreeManager` y los bonos son neutros).

- [ ] **Step 5: Play, Zoltraak y maná** (respaldar `save.json`; Frieren con el árbol vacío salvo lo que se compra con `SkillTreeRules.TryBuy` + `SkillTreeManager.Instance.Recompute()`): medir la carga (1,2 s → 0,9 s con `zc1`+`zc2`); el radio con `zr1`+`zr2` (contar enemigos a 4,7 m del punto); Sobrecarga (tras `Fire` con carga 1 vía `Tick`, `Overcharged` = true; el siguiente sale sin carga y la deja en false; llamar `Cancel()` entre medias no la borra); Eco (vida de un tanque a los 0 s y a los 0,35 s: dos golpes, el segundo de 50%); Escarcha (velocidad del agente x0,6 durante 3 s); maná máximo 140 y regeneración 5,5/s; Absorción (+3 al matar uno); Concentración (regeneración x2 a los 3 s sin daño, x1 justo después de un golpe). Restaurar el guardado.

---

### Task 6: Q, E y F en el juego

**Files:**
- Modify: `Assets/Scripts/Gameplay/Player/ManaBeam.cs`, `PiercingBeam.cs`, `PlayerAbilities.cs`, `FlowerField.cs`, `ManaPulseEffect.cs`

**Interfaces:**
- Consumes: `TreeBonuses`, `FrierenTreeMath` (Task 2); `EnemyAI.ApplyPoison`, `ApplyDamageTakenBonus` (Task 4); `ZoltraakCaster.DropFromSky` (Task 5).
- Produces: `PiercingBeam.Cast(Ray, float, float, int, Transform, List<EnemyAI> hitEnemies = null, float pierceBonusPerEnemy = 0f, float frostSlow = 0f)`.

- [ ] **Step 1: `PiercingBeam.Cast`** — firma nueva con los dos parámetros opcionales al final, y el bucle de daño:

```csharp
        // El daño se aplica al final: matar a un enemigo lo devuelve al pool y desactiva su objeto.
        // 'damaged' va del más cercano al más lejano: Perforación creciente suma por cada enemigo previo.
        for (int i = 0; i < damaged.Count; i++)
        {
            EnemyAI enemy = damaged[i];
            if (frostSlow > 0f) enemy.ApplySlow(frostSlow, FrierenTreeMath.FrostSeconds);
            enemy.TakeDamage(Mathf.RoundToInt(damage * FrierenTreeMath.PierceMultiplier(i, pierceBonusPerEnemy)));
        }
```

- [ ] **Step 2: `ManaBeam`** — el costo con Eficiencia y el rayo con el árbol:

```csharp
    private static float CostOf(StaffDefinition staff) =>
        FrierenTreeMath.ManaCost(staff.abilityManaCost, SkillTreeManager.CurrentBonuses.ManaCostReduction);
```

`CanCast`: `return staff != null && shooting.Mana.Current >= CostOf(staff);`. En `Cast`: `if (!shooting.Mana.TrySpend(CostOf(staff))) return false;` y

```csharp
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        float radius = staff.abilityBeamRadius + tree.BeamRadiusBonus;
        Vector3 origin = shooting.MuzzlePoint;
        Vector3 end = PiercingBeam.Cast(new Ray(origin, Direction(shooting, origin, staff.abilityRange)),
            staff.abilityRange, radius, weapon.AbilityDamage, shooting.transform, null, tree.BeamPierceDamage, tree.BeamFrostSlow);

        shooting.ShowBeam(origin, end, BeamColor, radius * 1.5f, 0.25f);
```

- [ ] **Step 3: `PlayerAbilities`** — en `RefreshBuild`, cargas de la Q:

```csharp
            else if (ability != null && ability.kind == AbilityKind.ManaBeam) max = 1 + bonuses.BeamExtraCharges;
```

`CooldownFor` del rayo: `return Mathf.Max(1f, shooting.CurrentWeapon.AbilityCooldownTime - bonuses.BeamCooldownReduction);`. En `EffectiveCooldown`:

```csharp
            case AbilityKind.FlowerField: reduction = bonuses.FieldCooldownReduction; break;
            case AbilityKind.ManaPulse: reduction = bonuses.PulseCooldownReduction; break;
```

`CanCast` del pulso: `return shooting.Mana != null && shooting.Mana.Current >= FrierenTreeMath.ManaCost(ability.manaCost, Bonuses.ManaCostReduction);`.

- [ ] **Step 4: `FlowerField`** — 

```csharp
    private static TreeBonuses Tree => SkillTreeManager.CurrentBonuses;
    private static float CostOf(AbilityDefinition definition) => FrierenTreeMath.ManaCost(definition.manaCost, Tree.ManaCostReduction);
    private float Radius => ability != null ? ability.radius + Tree.FieldRadiusBonus : 0f;
```

`Begin`: `shooting.Mana.Current < CostOf(definition)`; `Confirm`: `TrySpend(CostOf(ability))`; `StartField`: `fieldEnd = Time.time + ability.DurationAt(rank) + Tree.FieldDurationBonus;`. En `TickField`:

```csharp
        TreeBonuses tree = Tree;

        // Cura a Frieren si está dentro del círculo.
        if (health != null && !health.IsDead && FlowerFieldRules.Contains(fieldCenter, Radius, transform.position))
        {
            int amount = heal.Add(health.MaxHealth, ability.fieldHealFractionPerSecond + tree.FieldHealBonus, step);
            if (amount > 0) health.Heal(amount);
        }

        float slow = FrierenTreeMath.FieldSlow(ability.fieldSlowFraction, tree.FieldSlowBonus);
        int poison = tree.FieldPoison > 0f && shooting.WeaponCount > 0
            ? FrierenTreeMath.PoisonTickDamage(shooting.CurrentWeapon.Damage, tree.FieldPoison, tree.FieldPoisonDamagePercent)
            : 0;
```

y dentro del bucle de enemigos: `enemy.ApplySlow(slow, step * 2f); if (poison > 0) enemy.ApplyPoison(FrierenTreeMath.PoisonLinger, poison, FrierenTreeMath.PoisonTick);`.

- [ ] **Step 5: `ManaPulseEffect`** — búfer a 512; campos `private Shooting owner; private float endsAt; private float nextRain;`. `Activate`:

```csharp
    public bool Activate(AbilityDefinition definition, int rank, Shooting shooter)
    {
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        if (shooter.Mana == null || !shooter.Mana.TrySpend(FrierenTreeMath.ManaCost(definition.manaCost, tree.ManaCostReduction))) return false;

        ability = definition;
        owner = shooter;
        shooter.RefreshMana();

        float duration = definition.DurationAt(rank) + tree.PulseDurationBonus;
        effect.Start(Time.time, duration);
        endsAt = Time.time + duration;
        nextTick = Time.time;
        nextRain = Time.time + tree.PulseRainInterval;

        // Terror: aturde (una sola vez) a todos los enemigos del radio. Los jefes son inmunes (EnemyAI.ApplyStun).
        foreach (EnemyAI enemy in EnemiesInRadius())
        {
            enemy.ApplyStun(definition.pulseStunSeconds + tree.PulseStunBonus);
            if (tree.PulseMarkBonus > 0f) enemy.ApplyDamageTakenBonus(tree.PulseMarkBonus, duration);
        }

        AbilityVfx.ExplosionFlash(transform.position, definition.radius, PulseColor);
        if (shooter.Body != null) shooter.Body.PlayPowerUp();
        return true;
    }
```

(El parámetro se llamaba `owner`; renombrarlo a `shooter` evita taparlo con el campo.) `ForceOff` también pone `owner = null`. `Update`:

```csharp
    private void Update()
    {
        if (ability == null) return;

        if (effect.TryFinish(Time.time))
        {
            FinalBlast();
            ForceOff();
            return;
        }

        TickRain();

        if (Time.time < nextTick) return;
        nextTick = Time.time + TickSeconds;

        // Mientras dura, los enemigos del radio van más lentos; con la Marca, los que toca reciben más daño hasta el final.
        TreeBonuses tree = SkillTreeManager.CurrentBonuses;
        float left = Mathf.Max(0f, endsAt - Time.time);
        foreach (EnemyAI enemy in EnemiesInRadius())
        {
            enemy.ApplySlow(ability.pulseSlowFraction, TickSeconds * 2f);
            if (tree.PulseMarkBonus > 0f) enemy.ApplyDamageTakenBonus(tree.PulseMarkBonus, left);
        }
    }

    // Lluvia de Zoltraak: cada intervalo cae uno sobre un enemigo al azar del radio (si no hay ninguno, ese no cae).
    private void TickRain()
    {
        float interval = SkillTreeManager.CurrentBonuses.PulseRainInterval;
        if (interval <= 0f || Time.time < nextRain || owner == null || owner.WeaponCount == 0 || owner.CurrentWeapon.Staff == null) return;
        nextRain = Time.time + interval;

        List<EnemyAI> enemies = EnemiesInRadius();
        if (enemies.Count == 0) return;

        ZoltraakCaster caster = owner.GetComponent<ZoltraakCaster>();
        if (caster == null) caster = owner.gameObject.AddComponent<ZoltraakCaster>();
        caster.DropFromSky(owner.CurrentWeapon, enemies[Random.Range(0, enemies.Count)].transform.position);
    }

    // Explosión final: al terminar (no al cortarse por fin de partida), 3 Zoltraak completos a todos los del radio.
    private void FinalBlast()
    {
        float multiplier = SkillTreeManager.CurrentBonuses.PulseFinalBlastMultiplier;
        if (multiplier <= 0f || owner == null || owner.WeaponCount == 0 || owner.CurrentWeapon.Staff == null) return;

        int damage = Mathf.RoundToInt(owner.CurrentWeapon.ZoltraakDamage(1f) * multiplier);
        foreach (EnemyAI enemy in EnemiesInRadius()) enemy.TakeDamage(damage);
        AbilityVfx.ExplosionFlash(transform.position, ability.radius, PulseColor);
    }
```

En `EnemiesInRadius`, el radio: `float radius = SkillTreeManager.CurrentBonuses.PulseWholeMap ? FrierenTreeMath.WholeMapRadius : ability.radius;`.

- [ ] **Step 6: Compilar y correr toda la batería** — 0 errores, sin fallos.

- [ ] **Step 7: Play, Q, E y F** (respaldar `save.json`; Frieren con rango máximo en Q, E y F; nodos comprados por código): Q: daño +30% con `qi1`+`qi2` (vida de un normal antes y después), radio 0,6 (un enemigo a 0,55 m del eje recibe daño), enfriamiento 2 s con `qc1`+`qc2` y la tienda en 0, Doble carga (dos rayos seguidos, el tercero bloqueado), Rayo gélido (60% y no lo baja el campo a 40% si se pone encima), Perforación (4 enemigos en fila con vida igual: daños 1; 1,15; 1,3; 1,45 ×). E: radio 6 m, duración 7,5 s, enfriamiento 15 s, cura 7%/s con las 4 mitades de cura, ralentización 80% con las 4 de freno, cambiar una mitad en medio y que todo siga, veneno (2 por tick con el básico en 8, sigue 3 s tras salir; número verde). F: 12 s, aturdir 2 s, enfriamiento 44 s; Dominio (enemigos a 40 m aturdidos); Lluvia (con 3 enemigos, ~12 explosiones en 12 s); Explosión final (daño 3 × Zoltraak completo al terminar; no al morir Frieren); Marca (+30% a un golpe del básico durante el pulso). Restaurar el guardado.

---

### Task 7: La vista del árbol — mitades, alternativas, "Elige 1" y la ventana de cambio

**Files:**
- Modify: `Assets/Scripts/Gameplay/Characters/SkillTreeManager.cs`, `Assets/Scripts/UI/SkillTreeView.cs`

**Interfaces:**
- Consumes: `SkillTreeRules.CanBuy` (`ChoiceTaken`), `OwnedRival`, `CanSwap`, `TrySwap` (Task 1); `SkillNode.half`, `choiceGroup`.
- Produces: `SkillTreeManager.Swap(string nodeId) : bool`.

- [ ] **Step 1: `SkillTreeManager.Swap`** (después de `Buy`):

```csharp
    /// <summary>Cambia la opción comprada de un grupo de "elige 1" por 'nodeId' (devuelve la vieja y cobra la nueva).</summary>
    public bool Swap(string nodeId)
    {
        CharacterSave save = ActiveSave;
        if (save == null || !SkillTreeRules.TrySwap(Tree, save, nodeId)) return false;

        AfterChange();
        return true;
    }
```

- [ ] **Step 2: Colores y estado "Alternativa"** — en `SkillTreeView`, constantes nuevas:

```csharp
    private static readonly Color AlternativeColor = new Color(0.36f, 0.26f, 0.55f, 1f);
    private static readonly Color HealHalfColor = new Color(0.2f, 0.62f, 0.38f, 1f);
    private static readonly Color SlowHalfColor = new Color(0.2f, 0.55f, 0.8f, 1f);
    private static readonly Color GroupFrameColor = new Color(0.6f, 0.45f, 0.95f, 0.14f);
    private static readonly Color GroupLabelColor = new Color(0.8f, 0.7f, 1f, 1f);
```

En `Refresh`, el color y el texto de cada nodo:

```csharp
            SkillBuyBlock block = SkillTreeRules.CanBuy(tree, save, node.id);
            bool owned = block == SkillBuyBlock.Owned;
            bool alternative = block == SkillBuyBlock.ChoiceTaken;
            bool unlocked = SkillTreeRules.IsUnlocked(tree, node, save.skillNodes);
            bool affordable = save.skillPoints >= node.cost;
            Color open = node.half == SkillNodeHalf.Left ? HealHalfColor : node.half == SkillNodeHalf.Right ? SlowHalfColor : BuyableColor;

            ui.Image.color = owned ? OwnedColor : alternative ? AlternativeColor : !unlocked ? LockedColor : affordable ? open : UnaffordableColor;
            ui.Label.color = owned ? new Color(0.1f, 0.08f, 0.01f, 1f) : unlocked || alternative ? Color.white : new Color(0.8f, 0.82f, 0.9f, 1f);
            ui.Label.fontStyle = FontStyles.Bold;
            ui.Label.text = owned ? node.displayName
                : alternative ? node.displayName + "\ncambiar"
                : node.displayName + "\n" + node.cost + (node.cost == 1 ? " pt" : " pts");
```

En `ShowDetail`, el caso nuevo:

```csharp
            case SkillBuyBlock.ChoiceTaken:
            {
                SkillNode rival = builtFor.Find(SkillTreeRules.OwnedRival(builtFor, save.skillNodes, node.id));
                status = "Alternativa a " + rival.displayName + ". Clic para cambiar: se devuelven " + rival.cost + " y se gastan " + node.cost;
                break;
            }
```

- [ ] **Step 3: Mitades y marcos en `Build`** — al crear cada botón, tamaño y posición según la mitad:

```csharp
            Vector2 size = new Vector2(NodeSize, NodeSize);
            Vector2 at = ToScreen(node.position);
            if (node.half != SkillNodeHalf.None)
            {
                // Un nodo dividido: dos botones de media anchura en el mismo cuadrado (izquierda Curar, derecha Freno).
                size = new Vector2(NodeSize * 0.5f - 3f, NodeSize);
                at.x += (node.half == SkillNodeHalf.Left ? -1f : 1f) * NodeSize * 0.25f;
            }
            UiKit.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), at, size);
```

Lista nueva `private readonly List<GameObject> frames = new List<GameObject>();` (se destruyen y vacían en `Build` junto a las líneas). Después de dibujar las líneas y antes de los nodos:

```csharp
        // Un marco detrás de cada grupo de "elige 1" (los nodos divididos ya se ven partidos y no lo llevan).
        var groups = new Dictionary<string, List<SkillNode>>();
        foreach (SkillNode node in tree.nodes)
        {
            if (string.IsNullOrEmpty(node.choiceGroup) || node.half != SkillNodeHalf.None) continue;
            if (!groups.TryGetValue(node.choiceGroup, out List<SkillNode> members)) groups[node.choiceGroup] = members = new List<SkillNode>();
            members.Add(node);
        }
        foreach (List<SkillNode> members in groups.Values)
        {
            Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = new Vector2(float.MinValue, float.MinValue);
            foreach (SkillNode member in members)
            {
                lo = Vector2.Min(lo, ToScreen(member.position));
                hi = Vector2.Max(hi, ToScreen(member.position));
            }
            float pad = NodeSize * 0.5f + 18f;
            lo -= Vector2.one * pad;
            hi += Vector2.one * pad;

            Image frame = UiKit.Box("ChoiceFrame", linesLayer, GroupFrameColor);
            frame.raycastTarget = false;
            UiKit.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), (lo + hi) * 0.5f, hi - lo);
            frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            TMP_Text label = UiKit.Label("ChoiceLabel", linesLayer, "Elige 1", 24f, TextAlignmentOptions.Center, GroupLabelColor);
            label.raycastTarget = false;
            label.fontStyle = FontStyles.Bold;
            UiKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((lo.x + hi.x) * 0.5f, hi.y + 18f), new Vector2(200f, 36f));
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            frames.Add(frame.gameObject);
            frames.Add(label.gameObject);
        }
```

El texto "Elige 1" sale hasta ~70 px por encima del nodo más alto de su grupo: en `Build`, justo después de `contentMax = max + Vector2.one * (NodeSize * 0.5f);`, agregar `contentMax.y += NodeSize;` (antes del segundo `ResetView()`), para que el arrastre y "Ver todo" lo incluyan.

- [ ] **Step 4: La ventana de cambio** — campos `private readonly Image swapPanel; private readonly TMP_Text swapText; private readonly Button swapAccept; private readonly Button swapCancel; private SkillNode pendingSwap;`. Al final del constructor:

```csharp
        // Ventana de confirmación para cambiar la opción de un grupo de "elige 1". Tapa el árbol (bloquea los clics de detrás).
        swapPanel = UiKit.Box("SwapConfirm", Root, new Color(0f, 0f, 0f, 0.65f));
        UiKit.Stretch(swapPanel.rectTransform);

        Image box = UiKit.Box("Box", swapPanel.transform, UiKit.RowColor);
        UiKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 320f));
        box.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        swapText = UiKit.Label("Text", box.transform, "", 32f, TextAlignmentOptions.Center, Color.white);
        UiKit.Place(swapText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(940f, 180f));
        swapText.rectTransform.pivot = new Vector2(0.5f, 1f);

        swapAccept = UiKit.TextButton("Accept", box.transform, "Aceptar", 32f, AcceptSwap, out _);
        UiKit.Place((RectTransform)swapAccept.transform, new Vector2(0.5f, 0f), new Vector2(-170f, 30f), new Vector2(300f, 84f));
        ((RectTransform)swapAccept.transform).pivot = new Vector2(0.5f, 0f);

        swapCancel = UiKit.TextButton("Cancel", box.transform, "Cancelar", 32f, CloseSwap, out _);
        UiKit.Place((RectTransform)swapCancel.transform, new Vector2(0.5f, 0f), new Vector2(170f, 30f), new Vector2(300f, 84f));
        ((RectTransform)swapCancel.transform).pivot = new Vector2(0.5f, 0f);

        // Con teclado o mando, el foco no se escapa a los nodos de detrás.
        swapAccept.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = swapCancel };
        swapCancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = swapAccept };

        swapPanel.gameObject.SetActive(false);
```

Métodos:

```csharp
    private void OpenSwap(SkillNode node)
    {
        CharacterSave save = SkillTreeManager.Instance.ActiveSave;
        SkillNode rival = builtFor.Find(SkillTreeRules.OwnedRival(builtFor, save.skillNodes, node.id));
        if (rival == null) return;

        SkillSwapBlock block = SkillTreeRules.CanSwap(builtFor, save, node.id);
        string problem;
        switch (block)
        {
            case SkillSwapBlock.NoPoints: problem = "Faltan " + (node.cost - save.skillPoints - rival.cost) + " puntos"; break;
            case SkillSwapBlock.WouldDisconnect: problem = "Desconectaría otros nodos comprados"; break;
            case SkillSwapBlock.Locked: problem = "Bloqueado: compra antes un nodo conectado"; break;
            default: problem = null; break;
        }

        pendingSwap = node;
        swapText.text = "¿Cambiar <b>" + rival.displayName + "</b> por <b>" + node.displayName + "</b>?\nSe devuelven " + rival.cost
            + " puntos y se gastan " + node.cost + "." + (problem != null ? "\n<color=#ff8a8a>" + problem + "</color>" : "");
        swapAccept.interactable = block == SkillSwapBlock.None;
        swapPanel.gameObject.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(swapCancel.gameObject);
    }

    private void AcceptSwap()
    {
        SkillNode node = pendingSwap;
        if (node == null || SkillTreeManager.Instance == null || !SkillTreeManager.Instance.Swap(node.id)) return;

        CloseSwap();
        onChanged?.Invoke();
    }

    private void CloseSwap()
    {
        SkillNode node = pendingSwap;
        pendingSwap = null;
        swapPanel.gameObject.SetActive(false);
        if (node != null && nodes.TryGetValue(node.id, out NodeUi ui)) EventSystem.current?.SetSelectedGameObject(ui.Button.gameObject);
    }
```

(`using UnityEngine.EventSystems;`.) En `NodeClicked`, antes de comprar:

```csharp
        if (SkillTreeManager.Instance != null && SkillTreeManager.Instance.ActiveSave != null
            && SkillTreeRules.CanBuy(builtFor, SkillTreeManager.Instance.ActiveSave, node.id) == SkillBuyBlock.ChoiceTaken)
        {
            OpenSwap(node);
            return;
        }
```

En `Build`, al principio: `pendingSwap = null; swapPanel.gameObject.SetActive(false);`.

- [ ] **Step 5: Compilar y correr toda la batería** — 0 errores, sin fallos.

- [ ] **Step 6: Play, la vista** (respaldar `save.json`): abrir la estación de habilidades con Frieren, pestaña "Árbol" (por reflexión, como en sesiones anteriores), dar puntos con `DebugAddPoints`. Capturas: (a) "Ver todo"; (b) acercado a la rama de la E con las mitades partidas y a un abanico "Elige 1"; (c) una opción comprada y las otras en violeta "cambiar"; (d) la ventana abierta con Aceptar habilitado, y otra con "Faltan N puntos". Llamar `NodeClicked` (reflexión) en una alternativa abre la ventana; `AcceptSwap` cambia y los puntos cuadran. Con Alucard y Guts la pestaña se ve igual que antes (captura). Restaurar el guardado.

---

### Task 8: Notas, plan de Roblox y cierre

**Files:**
- Modify: `notas/DECISIONES.md` (D45), `notas/REGISTRO_DE_CAMBIOS.md`, `docs/PLAN_MIGRACION_ROBLOX.md`

- [ ] **Step 1: Batería completa final** — anotar el número de tests (536 + los nuevos) y 0 fallos; `unity_get_compilation_errors` sin errores ni avisos nuevos.
- [ ] **Step 2: `DECISIONES.md` — D45** (arriba de D44): qué (grupos `choiceGroup`, mitades `half`, `TrySwap` con reembolso y comprobación de conexión, `Compute` cuenta una por grupo, 31 efectos, ralentización "gana la más fuerte", veneno, Marca, maná del árbol que se llena al comprar), por qué (pedidos del usuario: elige 1, nodos partidos curar/ralentizar, cambiar con ventana, envenenar, árbol que se divide con aire), qué se descartó (nodo con variantes dentro; reiniciar para cambiar; E6 daño directo, E7 maná, E8 curar la base; F5–F7), y pendiente (balance de los 56 nodos, sonidos, efecto visual propio de la Lluvia).
- [ ] **Step 3: `REGISTRO_DE_CAMBIOS.md`** — entrada nueva en el historial (2026-10-06) con lo hecho, lo verificado (tests y mediciones en Play) y lo **no verificado** (clic real, teclado y mando en la ventana y las mitades, balance); actualizar "Dónde nos quedamos" (Frieren ya tiene árbol; quitar "falta su árbol").
- [ ] **Step 4: `PLAN_MIGRACION_ROBLOX.md`** — siguiendo su sección 10: el sistema de grupos y nodos divididos del árbol, los efectos de Frieren, `EnemyPoison`, `PoisonTick` en los eventos, la regla de ralentización; y una línea en su "Registro de sincronización" con la fecha (2026-10-06) y "sin commit".
- [ ] **Step 5: Informar al usuario** — qué quedó hecho y verificado; **no verificado**: el clic y la navegación reales en la vista y la ventana, el balance de los 56 nodos; dónde se ajustan los valores (`Frieren_Tree.asset`, `FrierenTreeMath` para los tiempos fijos). Sin commit.
