# Espada de Guts y aturdimiento — Plan de implementación

> **Para quien ejecute:** SUB-SKILL REQUERIDA: superpowers:subagent-driven-development (recomendada) o superpowers:executing-plans, tarea por tarea. Los pasos usan casillas (`- [ ]`).
> **Regla del usuario: NO hacer commits.** Él hace los suyos. Donde la plantilla pondría "Commit", aquí no hay paso de commit.

**Goal:** Guts ataca con un golpe de espada en área frente a él, mejorable en la tienda (cadencia, daño, probabilidad de aturdir) con el stun al 30% al máximo.

**Architecture:** `SwordDefinition : WeaponDefinition` (solo datos, sin modelo) reutiliza los 3 espacios de mejora, como `StaffDefinition`. La lógica pura (cono, dado de stun, temporizador) vive en `Game.Core` con tests de EditMode. `Shooting` gana `UpdateSword` (animación rápida al clic, daño tras `hitDelay`, tiempo largo entre golpes) y `EnemyAI` gana `ApplyStun`.

**Tech Stack:** Unity 6000.6, C#, NUnit (EditMode), asmdefs `Game.Core` / `Game.Runtime`.

**Spec:** `docs/superpowers/specs/2026-10-04-guts-sword-and-stun-design.md`

## Global Constraints
- Stun al máximo de la tienda: **30%** (5 niveles × 6%). Tope total con árbol: **50%** (el árbol de Guts NO se hace ahora; solo el campo `TreeBonuses.StunChanceBonus`, que vale 0).
- Cadencia: **1,2 s** entre golpes, −0,12 s por nivel, mínimo **0,6 s**, 5 niveles. Daño base **30**, +5 por nivel, 10 niveles.
- Cono: alcance **3 m**, **120°**. `hitDelay` ~**0,12 s**, `attackAnimSpeed` **2,6** (el estado `Attack` está a 1,3 → ~0,87 s; queda ~0,44 s). Stun **1,5 s**.
- **Sin arma en el personaje:** no hay modelo ni prefab de espada; `Guts.heldItemPrefab` sigue vacío.
- **Jefes y minijefes inmunes** al stun (`EnemyDefinition.IsBoss`).
- No cambiar el guardado ni `UpgradeType` (los niveles de la espada usan `upgradeLevels` existentes).
- No cambiar el `id` de ningún asset existente. El `id` de la espada es `Espada`.
- Los tests de EditMode solo ven `Game.Core` (el asmdef de tests no referencia `Game.Runtime`): la lógica a probar va en `Game.Core`.
- Escribir archivos con la herramienta Write (los heredocs largos de bash fallaron en esta máquina). Tras editar scripts, esperar a que `unity_get_compilation_errors` dé `isCompiling: false`; a veces hay que reseleccionar la instancia con `unity_list_instances` / `unity_select_instance` (puerto 7890).
- **Probar en Play cambia el `save.json` real:** respaldarlo antes y restaurarlo después con Unity fuera de Play (ver Tarea 8).

## Review Focus
1. **Golpe sin enemigos en el cono:** no debe dar error ni gastar nada; solo se reinicia el tiempo entre golpes. (Tarea 6, paso de verificación en Play.)
2. **Enemigo que muere por el golpe no recibe stun** ni da error al pasar al pool. (Tarea 4 `ApplyStun` ignora muertos; Tarea 6 aplica el daño antes del stun.)
3. **Cambiar de personaje o morir durante el `hitDelay`:** el golpe pendiente se descarta. (Tarea 6.)
4. **Enemigo reaparecido desde el pool conserva un stun viejo:** `Spawn` debe limpiarlo. (Tarea 4 + test de `StunTimer.Clear`.)
5. **Jefe golpeado:** recibe daño pero no stun. (Tarea 4.)

## Mapa de archivos
| Archivo | Acción | Responsabilidad |
|---|---|---|
| `Assets/Scripts/Core/Data/MeleeCone.cs` | crear | Geometría del cono horizontal |
| `Assets/Scripts/Core/Data/StunRules.cs` | crear | Dado de stun (`Roll`) y temporizador (`StunTimer`) |
| `Assets/Scripts/Core/Data/SwordDefinition.cs` | crear | Datos y fórmulas de la espada |
| `Assets/Scripts/Core/Data/TreeBonuses.cs` | modificar | Campo `StunChanceBonus` |
| `Assets/Data/Weapons/Espada.asset` | crear | Asset de la espada |
| `Assets/Data/Characters/Guts.asset` | modificar | `startingWeapons = [Espada]`, descripción |
| `Assets/Scripts/Gameplay/Weapons/WeaponState.cs` | modificar | `Sword`, `StunChance` |
| `Assets/Scripts/Gameplay/Enemies/EnemyAI.cs` | modificar | `ApplyStun`, detenerse y no atacar aturdido |
| `Assets/Animation/Guts.controller` | modificar (por script de editor) | Parámetro `AttackSpeed` |
| `Assets/Scripts/Gameplay/Player/PlayerBody.cs` | modificar | `PlayAttack(float speed)` |
| `Assets/Scripts/Gameplay/Player/Shooting.cs` | modificar | `UpdateSword`, `SwingSword`, HUD sin maná |
| `Assets/Scripts/UI/UpgradeMenuUI.cs` | modificar | Etiqueta de Aturdir con el % |
| `Assets/Scripts/Gameplay/Interaction/UpgradeStation.cs` | modificar | Comentario |
| `Assets/Tests/EditMode/MeleeConeTests.cs`, `StunTests.cs`, `SwordDefinitionTests.cs` | crear | Tests |
| `notas/*`, memoria | modificar | Registro al terminar |

## Cómo correr los tests de EditMode
Se corren por reflexión desde el editor (decisión D9). Pegar este código en `unity_execute_code` y cambiar `filter` (texto que debe contener el nombre de la clase; `""` = todas). Devuelve `OK/FAIL` por prueba y un resumen:

```csharp
string filter = "";
var asm = System.Array.Find(System.AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "Game.Tests.EditMode");
int pass = 0, fail = 0; string log = "";
foreach (var type in asm.GetTypes())
{
    if (type.GetCustomAttributes(typeof(NUnit.Framework.TestFixtureAttribute), true).Length == 0
        && System.Array.Find(type.GetMethods(), m => m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), true).Length > 0
            || m.GetCustomAttributes(typeof(NUnit.Framework.TestCaseAttribute), true).Length > 0) == null) continue;
    if (filter != "" && !type.Name.Contains(filter)) continue;
    foreach (var m in type.GetMethods())
    {
        var cases = m.GetCustomAttributes(typeof(NUnit.Framework.TestCaseAttribute), true);
        bool plain = m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), true).Length > 0;
        if (cases.Length == 0 && !plain) continue;
        var argSets = new System.Collections.Generic.List<object[]>();
        if (plain && cases.Length == 0) argSets.Add(new object[0]);
        foreach (NUnit.Framework.TestCaseAttribute c in cases) argSets.Add(c.Arguments);
        foreach (var args in argSets)
        {
            object obj = System.Activator.CreateInstance(type);
            try
            {
                foreach (var s in type.GetMethods()) if (s.GetCustomAttributes(typeof(NUnit.Framework.SetUpAttribute), true).Length > 0) s.Invoke(obj, null);
                var ps = m.GetParameters(); var conv = new object[args.Length];
                for (int i = 0; i < args.Length; i++) conv[i] = System.Convert.ChangeType(args[i], ps[i].ParameterType);
                m.Invoke(obj, conv); pass++;
            }
            catch (System.Exception e) { fail++; log += "FAIL " + type.Name + "." + m.Name + ": " + (e.InnerException ?? e).Message + "\n"; }
            finally { foreach (var s in type.GetMethods()) if (s.GetCustomAttributes(typeof(NUnit.Framework.TearDownAttribute), true).Length > 0) try { s.Invoke(obj, null); } catch { } }
        }
    }
}
return "pasan=" + pass + " fallan=" + fail + "\n" + log;
```
Línea base antes de empezar: debe dar `fallan=0` (el registro dice 320 tests; si el número difiere, anotarlo y seguir).

---

### Task 1: Lógica pura — cono, dado de stun y temporizador

**Files:**
- Create: `Assets/Scripts/Core/Data/MeleeCone.cs`
- Create: `Assets/Scripts/Core/Data/StunRules.cs`
- Modify: `Assets/Scripts/Core/Data/TreeBonuses.cs`
- Test: `Assets/Tests/EditMode/MeleeConeTests.cs`, `Assets/Tests/EditMode/StunTests.cs`

**Interfaces:**
- Produces: `MeleeCone.Contains(Vector3 origin, Vector3 forward, Vector3 point, float range, float arcDegrees, float pointRadius = 0f) : bool`; `StunRules.Roll(float chance, float roll01) : bool`; `class StunTimer { bool IsStunned(float now); void Apply(float now, float seconds); void Clear(); }`; `TreeBonuses.StunChanceBonus : float`.

- [ ] **Step 1: Escribir los tests que fallan** — `Assets/Tests/EditMode/MeleeConeTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class MeleeConeTests
{
    // Guts en el origen mirando a +Z; alcance 3 m y arco de 120°.
    private static bool Hit(Vector3 point, float radius = 0f) =>
        MeleeCone.Contains(Vector3.zero, Vector3.forward, point, 3f, 120f, radius);

    [Test] public void InFront_WithinRange_IsHit() => Assert.IsTrue(Hit(new Vector3(0f, 0f, 2f)));
    [Test] public void InFront_BeyondRange_IsMissed() => Assert.IsFalse(Hit(new Vector3(0f, 0f, 3.5f)));
    [Test] public void BodyRadius_ExtendsTheRange() => Assert.IsTrue(Hit(new Vector3(0f, 0f, 3.5f), 0.6f));
    [Test] public void InsideTheArc_IsHit() => Assert.IsTrue(Hit(new Vector3(1.7f, 0f, 1f)));   // ~59,5°
    [Test] public void OutsideTheArc_IsMissed() => Assert.IsFalse(Hit(new Vector3(1.8f, 0f, 1f))); // ~61°
    [Test] public void Behind_IsMissed() => Assert.IsFalse(Hit(new Vector3(0f, 0f, -1f)));
    [Test] public void TouchingBehind_WithBodyRadius_IsHit() => Assert.IsTrue(Hit(new Vector3(0f, 0f, -0.3f), 0.5f));
    [Test] public void Height_IsIgnored() => Assert.IsTrue(Hit(new Vector3(0f, 5f, 2f)));

    [Test]
    public void TiltedForward_IsFlattened()
    {
        // Mirar hacia abajo no cambia el cono horizontal.
        Assert.IsTrue(MeleeCone.Contains(Vector3.zero, new Vector3(0f, -0.7f, 0.7f), new Vector3(0f, 0f, 2f), 3f, 120f));
    }

    [Test]
    public void ZeroForward_HitsNothing()
    {
        Assert.IsFalse(MeleeCone.Contains(Vector3.zero, Vector3.zero, new Vector3(0f, 0f, 1f), 3f, 120f));
    }
}
```

`Assets/Tests/EditMode/StunTests.cs`:

```csharp
using NUnit.Framework;

public class StunTests
{
    [Test] public void Roll_ZeroChance_NeverStuns() => Assert.IsFalse(StunRules.Roll(0f, 0f));
    [Test] public void Roll_FullChance_AlwaysStuns() => Assert.IsTrue(StunRules.Roll(1f, 0.9999f));
    [Test] public void Roll_BelowChance_Stuns() => Assert.IsTrue(StunRules.Roll(0.3f, 0.29f));
    [Test] public void Roll_AtOrAboveChance_DoesNotStun() => Assert.IsFalse(StunRules.Roll(0.3f, 0.3f));

    [Test]
    public void Timer_StunsForTheGivenSeconds()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 1.5f);

        Assert.IsTrue(timer.IsStunned(10f));
        Assert.IsTrue(timer.IsStunned(11.4f));
        Assert.IsFalse(timer.IsStunned(11.5f));
    }

    [Test]
    public void Timer_ANewShorterStun_DoesNotCutTheCurrentOne()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 2f);
        timer.Apply(10.5f, 0.5f);   // terminaría antes: no acorta

        Assert.IsTrue(timer.IsStunned(11.9f));
    }

    [Test]
    public void Timer_ANewLongerStun_Extends()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 1f);
        timer.Apply(10.5f, 2f);

        Assert.IsTrue(timer.IsStunned(12.4f));
        Assert.IsFalse(timer.IsStunned(12.5f));
    }

    [Test]
    public void Timer_NonPositiveSeconds_DoNothing()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 0f);
        timer.Apply(10f, -1f);

        Assert.IsFalse(timer.IsStunned(10f));
    }

    [Test]
    public void Timer_Clear_EndsTheStun()
    {
        var timer = new StunTimer();
        timer.Apply(10f, 5f);
        timer.Clear();

        Assert.IsFalse(timer.IsStunned(10.1f));
    }
}
```

- [ ] **Step 2: Verificar que fallan** — esperar a que compile (`unity_get_compilation_errors`): debe dar errores CS0103/CS0246 de `MeleeCone`, `StunRules`, `StunTimer` (aún no existen). Eso cuenta como "falla".

- [ ] **Step 3: Implementar** — `Assets/Scripts/Core/Data/MeleeCone.cs`:

```csharp
using UnityEngine;

/// <summary>Cono horizontal frente al jugador. Solo geometría, para poder probarla sin escena.</summary>
public static class MeleeCone
{
    /// <summary>
    /// ¿Queda el punto dentro del cono? Se ignora la altura. El radio del objetivo alarga el alcance y, si el objetivo
    /// ya toca al origen, cuenta como golpeado aunque esté fuera del arco (un enemigo pegado a Guts no se escapa).
    /// </summary>
    public static bool Contains(Vector3 origin, Vector3 forward, Vector3 point, float range, float arcDegrees, float pointRadius = 0f)
    {
        var flatForward = new Vector3(forward.x, 0f, forward.z);
        if (flatForward.sqrMagnitude < 1e-6f) return false;

        var toPoint = new Vector3(point.x - origin.x, 0f, point.z - origin.z);
        float distance = toPoint.magnitude;

        if (distance > range + pointRadius) return false;
        if (distance <= pointRadius) return true;

        return Vector3.Angle(flatForward, toPoint) <= arcDegrees * 0.5f;
    }
}
```

`Assets/Scripts/Core/Data/StunRules.cs`:

```csharp
using UnityEngine;

/// <summary>Reglas del aturdimiento. El azar y el tiempo se pasan desde fuera para poder probarlos.</summary>
public static class StunRules
{
    /// <summary>Verdadero si el dado (0 a 1) cae dentro de la probabilidad. Probabilidad 0 nunca aturde.</summary>
    public static bool Roll(float chance, float roll01) => roll01 < chance;
}

/// <summary>Cuánto tiempo sigue aturdido un enemigo. Un aturdimiento nuevo nunca acorta el actual.</summary>
public class StunTimer
{
    private float until;

    public bool IsStunned(float now) => now < until;

    public void Apply(float now, float seconds)
    {
        if (seconds <= 0f) return;
        until = Mathf.Max(until, now + seconds);
    }

    public void Clear() => until = 0f;
}
```

`TreeBonuses.cs` — después de `UltLifeStealBonus`:

```csharp
    /// <summary>Probabilidad extra de aturdir (0,2 = +20 puntos). Lo usará el árbol de Guts; por ahora siempre 0.</summary>
    public float StunChanceBonus;
```

- [ ] **Step 4: Verificar que pasan** — esperar a compilar sin errores y correr el ejecutor con `filter = "Melee"` y luego `filter = "Stun"`. Esperado: `fallan=0`.

---

### Task 2: `SwordDefinition`, asset de la espada y enlace con Guts

**Files:**
- Create: `Assets/Scripts/Core/Data/SwordDefinition.cs`
- Create (por script de editor): `Assets/Data/Weapons/Espada.asset`
- Modify (por script de editor): `Assets/Data/Characters/Guts.asset`
- Test: `Assets/Tests/EditMode/SwordDefinitionTests.cs`

**Interfaces:**
- Consumes: `WeaponDefinition` (`FireRateAt`, `DamageAt`, `reloadUpgrade`, `UseAmmo`, `AppliesBleed`, `UpgradeLabel`), `TreeBonuses` (Tarea 1 solo para el valor del bono, que llega como `float`).
- Produces: `SwordDefinition { float arcDegrees; float hitDelay; float attackAnimSpeed; float stunSeconds; float StunChanceAt(int level, float treeBonus = 0f) }`. El alcance es el campo heredado `range`.

- [ ] **Step 1: Escribir los tests que fallan** — `Assets/Tests/EditMode/SwordDefinitionTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SwordDefinitionTests
{
    private const string SwordPath = "Assets/Data/Weapons/Espada.asset";
    private const string GutsPath = "Assets/Data/Characters/Guts.asset";

    private static SwordDefinition Sword => AssetDatabase.LoadAssetAtPath<SwordDefinition>(SwordPath);

    // --- Fórmulas (sobre una instancia con los valores de diseño) ---

    private SwordDefinition made;

    [SetUp]
    public void SetUp()
    {
        made = ScriptableObject.CreateInstance<SwordDefinition>();
        made.fireRate = 1.2f;
        made.fireRateUpgrade = new UpgradeStat { step = 0.12f, limit = 0.6f, maxLevel = 5 };
        made.reloadUpgrade = new UpgradeStat { step = 0.06f, maxLevel = 5 };
        made.damage = 30;
        made.damageUpgrade = new UpgradeStat { step = 5f, maxLevel = 10 };
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(made);

    [TestCase(0, 0f)]
    [TestCase(1, 0.06f)]
    [TestCase(5, 0.30f)]
    public void StunChance_GrowsSixPointsPerLevel(int level, float expected)
    {
        Assert.AreEqual(expected, made.StunChanceAt(level), 1e-4f);
    }

    [Test]
    public void StunChance_AddsTheTreeBonus_UpToFiftyAtMax()
    {
        Assert.AreEqual(0.50f, made.StunChanceAt(5, 0.20f), 1e-4f);
    }

    [Test]
    public void StunChance_NeverExceedsOneHundredPercent()
    {
        Assert.AreEqual(1f, made.StunChanceAt(5, 5f), 1e-4f);
    }

    [TestCase(0, 1.2f)]
    [TestCase(1, 1.08f)]
    [TestCase(5, 0.6f)]
    public void TimeBetweenHits_ShortensWithCadence(int level, float expected)
    {
        Assert.AreEqual(expected, made.FireRateAt(level), 1e-4f);
    }

    [Test]
    public void TimeBetweenHits_NeverBelowTheLimit()
    {
        Assert.AreEqual(0.6f, made.FireRateAt(50), 1e-4f);
    }

    [TestCase(0, 30)]
    [TestCase(10, 80)]
    public void Damage_AddsFivePerLevel(int level, int expected)
    {
        Assert.AreEqual(expected, made.DamageAt(level));
    }

    [Test]
    public void Sword_HasNoAmmoAndNoBleed()
    {
        Assert.IsFalse(made.UsesAmmo);
        Assert.IsFalse(made.AppliesBleed);
    }

    [Test]
    public void Labels_NameTheThreeUpgrades()
    {
        Assert.AreEqual("Cadencia", made.UpgradeLabel(UpgradeType.FireRate));
        Assert.AreEqual("Aturdir", made.UpgradeLabel(UpgradeType.Reload));
        Assert.AreEqual("Daño", made.UpgradeLabel(UpgradeType.Damage));
    }

    // --- El asset real y Guts ---

    [Test]
    public void SwordAsset_Exists_WithTheDesignValues()
    {
        SwordDefinition sword = Sword;
        Assert.IsNotNull(sword, "Falta " + SwordPath);

        Assert.AreEqual("Espada", sword.Id);
        Assert.AreEqual(3f, sword.range, 1e-4f);
        Assert.AreEqual(120f, sword.arcDegrees, 1e-4f);
        Assert.AreEqual(1.5f, sword.stunSeconds, 1e-4f);
        Assert.AreEqual(2.6f, sword.attackAnimSpeed, 1e-4f);
        Assert.IsTrue(sword.isAutomatic);
        Assert.AreEqual(0.30f, sword.StunChanceAt(sword.reloadUpgrade.maxLevel), 1e-4f);
        Assert.AreEqual(1.2f, sword.FireRateAt(0), 1e-4f);
        Assert.AreEqual(0.6f, sword.FireRateAt(sword.fireRateUpgrade.maxLevel), 1e-4f);
        Assert.AreEqual(30, sword.DamageAt(0));
        Assert.Greater(sword.hitDelay, 0f);
        Assert.Less(sword.hitDelay, sword.FireRateAt(sword.fireRateUpgrade.maxLevel), "El golpe debe caer antes del siguiente");
    }

    [Test]
    public void Guts_CarriesTheSword_WithoutAHeldModel()
    {
        var guts = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(GutsPath);

        Assert.AreEqual(1, guts.startingWeapons.Length);
        Assert.AreSame(Sword, guts.startingWeapons[0]);
        Assert.IsNull(guts.heldItemPrefab);
    }
}
```

- [ ] **Step 2: Verificar que fallan** — compilar: errores por `SwordDefinition` inexistente.

- [ ] **Step 3: Implementar la clase** — `Assets/Scripts/Core/Data/SwordDefinition.cs`:

```csharp
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
        Mathf.Clamp01(level * reloadUpgrade.step + treeBonus);
}
```

- [ ] **Step 4: Crear el asset y enlazar a Guts** — con `unity_execute_code`, tras compilar:

```csharp
var sword = ScriptableObject.CreateInstance<SwordDefinition>();
sword.weaponName = "Espada";
sword.range = 3f;
sword.arcDegrees = 120f;
sword.hitDelay = 0.12f;
sword.attackAnimSpeed = 2.6f;
sword.stunSeconds = 1.5f;
sword.isAutomatic = true;       // mantener el clic repite el golpe al ritmo de la cadencia
sword.ownedFromStart = true;
sword.price = 0;
sword.damage = 30;
sword.fireRate = 1.2f;
sword.fireRateUpgrade = new UpgradeStat { step = 0.12f, limit = 0.6f, basePrice = 100, priceIncrease = 50, maxLevel = 5 };
sword.reloadUpgrade = new UpgradeStat { step = 0.06f, limit = 0f, basePrice = 150, priceIncrease = 75, maxLevel = 5 };
sword.damageUpgrade = new UpgradeStat { step = 5f, limit = 0f, basePrice = 150, priceIncrease = 75, maxLevel = 10 };
UnityEditor.AssetDatabase.CreateAsset(sword, "Assets/Data/Weapons/Espada.asset");

var so = new UnityEditor.SerializedObject(sword);
so.FindProperty("id").stringValue = "Espada";
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(sword);

var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
guts.startingWeapons = new WeaponDefinition[] { sword };
guts.description = "El Espadachín Negro, con la Dragonslayer. Lento y resistente. Su golpe de espada pega en área frente a él y puede aturdir a los enemigos.";
UnityEditor.EditorUtility.SetDirty(guts);
UnityEditor.AssetDatabase.SaveAssets();
return "creada=" + (UnityEditor.AssetDatabase.LoadAssetAtPath<SwordDefinition>("Assets/Data/Weapons/Espada.asset") != null)
    + " guts armas=" + guts.startingWeapons.Length;
```
Esperado: `creada=True guts armas=1`.

- [ ] **Step 5: Verificar que pasan** — correr el ejecutor con `filter = "SwordDefinition"`. Esperado: `fallan=0`. Correr también `filter = "Character"` y `filter = "Balance"`: si `CharacterTests` o `BalanceTests` fallan por la espada (por ejemplo, asumen que todo personaje con armas usa munición o tiene un M16), anotar el fallo exacto y corregir la prueba solo si el supuesto era "Guts no tiene armas".

---

### Task 3: `WeaponState` expone la espada y la probabilidad de stun

**Files:**
- Modify: `Assets/Scripts/Gameplay/Weapons/WeaponState.cs` (junto a `Staff`, línea ~45, y a los stats, línea ~49-75)

**Interfaces:**
- Consumes: `SwordDefinition.StunChanceAt`, `TreeBonuses.StunChanceBonus`, `SkillTreeManager.Instance.Bonuses`.
- Produces: `WeaponState.Sword : SwordDefinition` (null si no es espada); `WeaponState.StunChance : float` (0 si no es espada).

No hay test de EditMode aquí (`WeaponState` está en `Game.Runtime`); la fórmula ya está cubierta en la Tarea 2 y el cableado se prueba en Play (Tarea 8).

- [ ] **Step 1: Agregar el acceso a la espada** — después de la propiedad `Staff`:

```csharp
    /// <summary>Datos de la espada si esta arma lo es; null para armas de fuego y bastones.</summary>
    public SwordDefinition Sword => Definition as SwordDefinition;
```

- [ ] **Step 2: Agregar la probabilidad de stun** — después de `BleedPerHit`:

```csharp
    /// <summary>Probabilidad de aturdir a cada enemigo golpeado (solo la espada): nivel de Aturdir más el bono del árbol.</summary>
    public float StunChance
    {
        get
        {
            SwordDefinition sword = Sword;
            if (sword == null) return 0f;

            float treeBonus = SkillTreeManager.Instance != null ? SkillTreeManager.Instance.Bonuses.StunChanceBonus : 0f;
            return sword.StunChanceAt(GetLevel(UpgradeType.Reload), treeBonus);
        }
    }
```

- [ ] **Step 3: Verificar que compila** — `unity_get_compilation_errors`: 0 errores.

---

### Task 4: `EnemyAI.ApplyStun` — el enemigo aturdido se detiene y no ataca

**Files:**
- Modify: `Assets/Scripts/Gameplay/Enemies/EnemyAI.cs` (campos ~línea 31; `Spawn` ~línea 116-160; `ApplySlow` ~198; `Update` ~220)

**Interfaces:**
- Consumes: `StunTimer` (Tarea 1), `EnemyDefinition.IsBoss`.
- Produces: `EnemyAI.ApplyStun(float seconds)` (no hace nada si está muerto, es jefe o `seconds <= 0`); `EnemyAI.IsStunned : bool`.

- [ ] **Step 1: Campo y propiedad** — junto a `slowUntil`:

```csharp
    private readonly StunTimer stun = new StunTimer();
```
y junto a `IsDead`:

```csharp
    public bool IsStunned => stun.IsStunned(Time.time);
```

- [ ] **Step 2: Limpiar el stun al reaparecer** — en `Spawn`, junto a donde se reinician `slowUntil = 0f; slowMultiplier = 1f;` (línea ~131), agregar:

```csharp
        stun.Clear();
```

- [ ] **Step 3: `ApplyStun`** — después de `ApplySlow`:

```csharp
    /// <summary>Lo deja quieto y sin atacar unos segundos. Los jefes son inmunes. No acorta uno que ya dure más.</summary>
    public void ApplyStun(float seconds)
    {
        if (isDead || seconds <= 0f || IsBoss) return;

        stun.Apply(Time.time, seconds);
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
    }

    private bool IsBoss => def != null && def.IsBoss;
```

- [ ] **Step 4: Respetar el stun en `Update`** — justo después del bloque que termina la ralentización y antes de `if (IsControlled) return;`:

```csharp
        // Aturdido: quieto y sin atacar hasta que pase el tiempo (el movimiento se reanuda más abajo).
        if (stun.IsStunned(Time.time))
        {
            agent.isStopped = true;
            return;
        }
```
Al terminar el stun, el resto de `Update` ya hace `agent.isStopped = false` en su camino normal.

- [ ] **Step 5: Verificar que compila** — 0 errores. El comportamiento (quieto, no ataca, se reanuda, jefe inmune) se prueba en Play, Tarea 8.

---

### Task 5: Velocidad de la animación de ataque

**Files:**
- Modify: `Assets/Animation/Guts.controller` (por script de editor)
- Modify: `Assets/Scripts/Gameplay/Player/PlayerBody.cs` (constantes ~línea 19; `PlayAttack` ~línea 235)

**Interfaces:**
- Produces: `PlayerBody.PlayAttack(float speed)`; parámetro float `AttackSpeed` en `Guts.controller` (valor por defecto 1) usado como `speedParameter` del estado `Attack` (cuya `speed` pasa de 1,3 a 1: la velocidad final es `AttackSpeed` × 1).

- [ ] **Step 1: Cambiar el controlador** — `unity_execute_code`:

```csharp
var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/Guts.controller");
bool has = false;
foreach (var p in ctrl.parameters) if (p.name == "AttackSpeed") has = true;
if (!has) ctrl.AddParameter(new UnityEngine.AnimatorControllerParameter { name = "AttackSpeed", type = UnityEngine.AnimatorControllerParameterType.Float, defaultFloat = 1f });

string r = "";
foreach (var layer in ctrl.layers)
    foreach (var cs in layer.stateMachine.states)
        if (cs.state.name == "Attack")
        {
            cs.state.speed = 1f;
            cs.state.speedParameterActive = true;
            cs.state.speedParameter = "AttackSpeed";
            r = "Attack listo: speed=" + cs.state.speed + " param=" + cs.state.speedParameter;
        }
UnityEditor.EditorUtility.SetDirty(ctrl);
UnityEditor.AssetDatabase.SaveAssets();
return r;
```
Esperado: `Attack listo: speed=1 param=AttackSpeed`.

- [ ] **Step 2: `PlayerBody.PlayAttack`** — agregar la constante junto a `AttackParam`:

```csharp
    private static readonly int AttackSpeedParam = Animator.StringToHash("AttackSpeed");
```
y reemplazar `PlayAttack` por:

```csharp
    /// <summary>Ataque cuerpo a cuerpo (el corte de Guts). Solo animación; no hace nada si el controlador no tiene el parámetro "Attack". "speed" acelera el corte (1 = velocidad del clip).</summary>
    public void PlayAttack(float speed = 1f)
    {
        if (dead || !hasAttack) return;

        animator.SetFloat(AttackSpeedParam, Mathf.Max(0.1f, speed));
        animator.SetTrigger(AttackParam);
    }
```
(El parámetro `AttackSpeed` existe en `Guts.controller`; Alucard no tiene `Attack`, así que nunca llega a `SetFloat`.)

- [ ] **Step 3: Verificar** — compila sin errores. La velocidad real se mira en Play (Tarea 8).

---

### Task 6: `Shooting` — `UpdateSword` y el golpe en área

**Files:**
- Modify: `Assets/Scripts/Gameplay/Player/Shooting.cs` (usings; campos ~línea 27-35; `Build` ~línea 92; `Update` ~línea 128-155; `PublishHud` ~línea 417)

**Interfaces:**
- Consumes: `WeaponState.Sword`, `WeaponState.StunChance`, `WeaponState.FireRate`, `WeaponState.Damage`, `MeleeCone.Contains`, `StunRules.Roll`, `EnemyAI.ApplyStun`, `PlayerBody.PlayAttack(float)`.
- Produces: nada que use otra tarea.

- [ ] **Step 1: Usings y campos** — arriba del archivo agregar `using System.Collections.Generic;` (junto a `System.Collections`). Junto a `reloadRoutine`:

```csharp
    // Espada (Guts)
    private Coroutine swordRoutine;
    private readonly Collider[] meleeBuffer = new Collider[64];
    private readonly List<EnemyAI> meleeTargets = new List<EnemyAI>();
    private const float MeleeSearchMargin = 3f; // holgura para enemigos grandes (el radio real se cuenta en MeleeCone)
```

- [ ] **Step 2: Cancelar un golpe pendiente al reconstruir** — en `Build`, justo después del bloque `if (reloadRoutine != null) {...}`:

```csharp
        if (swordRoutine != null)
        {
            StopCoroutine(swordRoutine);
            swordRoutine = null;
        }
```

- [ ] **Step 3: Rama de la espada en `Update`** — la rama "sin armas" queda igual (cambiar solo su comentario a "Un personaje sin armas: el clic solo reproduce el corte, sin daño."). Después de los bloques de cambio de arma y justo antes de `if (!CurrentWeapon.UsesAmmo)`, agregar:

```csharp
        if (CurrentWeapon.Sword != null)
        {
            UpdateSword(CurrentWeapon, input);
            return;
        }
```

- [ ] **Step 4: `UpdateSword`, `SwordHit` y `SwingSword`** — después de `UpdateStaff`:

```csharp
    // Espada: el clic reproduce el corte (rápido) y el daño cae un instante después; el tiempo entre golpes es largo
    // y es lo que mejora la tienda. Mantener el clic repite el golpe al ritmo de la cadencia.
    private void UpdateSword(WeaponState weapon, GameInput input)
    {
        bool triggerPressed = weapon.IsAutomatic ? input.FireHeld : input.FirePressed;
        if (!triggerPressed || Time.time < nextFireTime) return;

        SwordDefinition sword = weapon.Sword;
        nextFireTime = Time.time + weapon.FireRate;

        if (Body != null) Body.PlayAttack(sword.attackAnimSpeed);

        if (swordRoutine != null) StopCoroutine(swordRoutine);
        swordRoutine = StartCoroutine(SwordHit(weapon, sword.hitDelay));
    }

    private IEnumerator SwordHit(WeaponState weapon, float delay)
    {
        yield return new WaitForSeconds(delay);
        swordRoutine = null;

        // Si en este instante ya terminó la partida o está en niebla, el golpe se pierde.
        if (GameState.InputBlocked || abilities.IsMist) yield break;

        SwingSword(weapon);
    }

    // Todos los enemigos vivos dentro del cono reciben el golpe; cada uno tira su propio dado de aturdimiento.
    private void SwingSword(WeaponState weapon)
    {
        SwordDefinition sword = weapon.Sword;

        Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) : transform.forward;
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;

        Vector3 origin = transform.position;

        meleeTargets.Clear();
        int count = Physics.OverlapSphereNonAlloc(origin, sword.range + MeleeSearchMargin, meleeBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = meleeBuffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead || meleeTargets.Contains(enemy)) continue;
            if (!MeleeCone.Contains(origin, forward, enemy.transform.position, sword.range, sword.arcDegrees, enemy.BodyRadius)) continue;

            meleeTargets.Add(enemy);
        }

        int damage = weapon.Damage;
        float stunChance = weapon.StunChance;
        foreach (EnemyAI enemy in meleeTargets)
        {
            enemy.TakeDamage(damage);   // primero el daño: si muere, ApplyStun lo ignora
            if (StunRules.Roll(stunChance, Random.value)) enemy.ApplyStun(sword.stunSeconds);
        }
    }
```

- [ ] **Step 5: El HUD no debe tratar la espada como bastón** — en `PublishHud`, reemplazar la condición del comienzo:

```csharp
        // Sin armas, o con una arma sin munición ni maná (la espada): solo las habilidades del personaje (si tiene).
        if (states.Length == 0 || (!CurrentWeapon.UsesAmmo && CurrentWeapon.Staff == null))
        {
            GameEvents.RaiseResourceModeChanged(false);
            GameEvents.RaiseAbilitiesChanged(abilities.HudInfo());
            return;
        }
```
(Sin este cambio `usesMana = !UsesAmmo` mostraría una barra de maná en Guts.) `ConfigureMana` ya devuelve `mana = null` si `Staff == null`.

- [ ] **Step 6: Verificar** — compila sin errores. Estas ramas dependen de `Time`, de la cámara y de la física: se prueban en Play (Tarea 8).

---

### Task 7: Tienda — etiqueta de Aturdir y comentario de la estación

**Files:**
- Modify: `Assets/Scripts/UI/UpgradeMenuUI.cs` (`Refresh`, ~línea 67; método nuevo junto a `BleedLabel`)
- Modify: `Assets/Scripts/Gameplay/Interaction/UpgradeStation.cs` (comentario)

**Interfaces:**
- Consumes: `WeaponState.Sword`, `WeaponState.StunChance`, `WeaponState.GetLevel/GetMaxLevel/IsMaxLevel/GetUpgradeCost/GetUpgradeLabel`.

- [ ] **Step 1: Mostrar el % bajo "Aturdir"** — en `Refresh`, reemplazar la línea `reloadText.text = UpgradeLabel(weapon, UpgradeType.Reload);` por:

```csharp
        reloadText.text = weapon.Sword != null ? StunLabel(weapon) : UpgradeLabel(weapon, UpgradeType.Reload);
```
y junto a `BleedLabel` agregar:

```csharp
    // La espada usa este espacio para Aturdir: además del nivel y el precio, muestra la probabilidad actual.
    private static string StunLabel(WeaponState weapon)
    {
        string cost = weapon.IsMaxLevel(UpgradeType.Reload) ? "MAX" : "$" + HudFormat.Money(weapon.GetUpgradeCost(UpgradeType.Reload));
        return weapon.GetUpgradeLabel(UpgradeType.Reload)
            + "\nNv " + weapon.GetLevel(UpgradeType.Reload) + "/" + weapon.GetMaxLevel(UpgradeType.Reload)
            + "\nProb.: " + Mathf.RoundToInt(weapon.StunChance * 100f) + "%"
            + "\n" + cost;
    }
```

- [ ] **Step 2: Comentario de la estación** — en `UpgradeStation.cs` cambiar el comentario de `Available` a: `// Los personajes sin armas no tienen nada que mejorar aquí (Guts ya tiene su espada).`

- [ ] **Step 3: Verificar** — compila sin errores. La columna de sangrado ya se oculta sola (`AppliesBleed` es false); la imagen del menú se revisa en Play (Tarea 8).

---

### Task 8: Verificación completa, notas y memoria

- [ ] **Step 1: Suite completa** — compilar sin errores y correr el ejecutor con `filter = ""`. Esperado: `fallan=0` y un total igual a la línea base más los tests nuevos (≈ 320 + 41). Si algo falla fuera de lo nuevo (por ejemplo `BalanceTests` o `CharacterTests` porque ahora Guts tiene un arma), leer el fallo y corregir **la prueba** solo si asumía que Guts no tenía armas; si es un fallo real del código, arreglar el código.

- [ ] **Step 2: Respaldar el guardado** — con PowerShell (respaldar el de este momento, no uno viejo):

```powershell
$d="$env:USERPROFILE\AppData\LocalLow\DefaultCompany\la primera nunca se olvida"
Copy-Item "$d\save.json" "<scratchpad>\save_backup_guts.json" -Force
```

- [ ] **Step 3: Probar en Play** (`unity_play_mode` play; esperar el menú; elegir Guts por código: `CharacterManager.Instance.Apply(<Guts>)` o el método público equivalente que use el menú). El input real no se puede automatizar sin foco: llamar a las funciones y marcar el clic/mando como **no verificado**. Comprobar, con `unity_execute_code` por reflexión y `EditorApplication.Step` (ver D12):
  1. Guts activo: sin barra de maná ni casillas de munición; `Shooting.Instance.WeaponCount == 1` y `CurrentWeapon.Sword != null`.
  2. Poner 5 enemigos con `EnemyPool.Spawn(def, pos, escala)`: 3 dentro del cono (una fila a 1,5 / 2 / 2,8 m al frente), 1 detrás y 1 a 2 m pero a 90° a un lado. Llamar `SwingSword` por reflexión: los 3 del cono pierden `weapon.Damage` de vida; los otros dos no.
  3. Con nivel de Aturdir 5 (`save` de Guts) forzar `StunChance = 0,3` y repetir 200 golpes sobre enemigos de mucha vida: la fracción aturdida ronda el 30% (entre 20% y 40%). Un enemigo aturdido no se mueve durante `stunSeconds` (medir por segundos de juego, no por frames) y luego avanza de nuevo.
  4. Un jefe (`Boss_Invocador`) golpeado recibe daño y **no** queda aturdido.
  5. Golpe sin enemigos en el cono: sin errores en la consola (`unity_console_log`).
  6. Cambiar de personaje (Alucard) con un golpe pendiente en `hitDelay`: no hay daño ni error.
  7. Abrir la estación de mejora de armas con Guts (`UpgradeMenuUI`): muestra "Cadencia / Aturdir / Daño" (3 columnas centradas, sin Sangrado), y **Aturdir** muestra "Prob.: 0%". Comprar niveles: el % sube a 6, 12, … 30; la cadencia baja de 1,20 a 0,60 s. Captura con `unity_screenshot_game`.
  8. Animación: llamar `Body.PlayAttack(2.6)` y confirmar con capturas/estado del Animator que el corte dura ~0,44 s, y que el momento del daño (`hitDelay`) cae sobre el corte. **Si el golpe cae antes o después del tajo, ajustar `hitDelay` en `Espada.asset`** y repetir.
  9. En la consola de Unity no queda ningún error nuevo.

- [ ] **Step 4: Salir de Play y restaurar el guardado** — `unity_play_mode` stop, esperar a que Unity quede quieto y copiar de vuelta **el respaldo de este momento** (nunca uno más viejo). Los `.bak` del juego no se borran.

- [ ] **Step 5: Actualizar notas y memoria** (pedido del usuario):
  - `notas/REGISTRO_DE_CAMBIOS.md`: actualizar "Dónde nos quedamos" (Guts ya ataca; falta árbol, habilidades, balance a mano, `hitDelay` definitivo) y agregar una entrada al historial con lo hecho, lo verificado y lo **no verificado** (input real).
  - `notas/DECISIONES.md`: nueva decisión (siguiente número libre): espada como `SwordDefinition` sin modelo; reutilizar el espacio de "Recarga" para Aturdir; cadencia larga con animación rápida; jefes inmunes; cada enemigo tira su dado.
  - `notas/NOTAS_DEL_JUEGO.md`: estilo de Guts (cono 3 m/120°, stun 30% máx., 50% con árbol).
  - Memoria (`notas-y-registro-de-sesion.md`): actualizar la línea de estado.
  - `ATAJOS_DE_TECLADO.md`: Guts ataca con el clic (mantenerlo repite el golpe).
  - **No commitear.**

- [ ] **Step 6: Informar al usuario** — qué quedó hecho y verificado, qué es "no verificado" (clic/teclas reales, sensación del golpe y balance), y los valores que conviene ajustar jugando (`hitDelay`, `attackAnimSpeed`, `range`/`arcDegrees`, daño, precios).

---

## Self-Review (hecha al escribir)
- **Cobertura del spec:** §3 datos → Tareas 1-2; §4 golpe → Tareas 5-6; §5 stun → Tareas 1 y 4; §6 tienda/UI → Tareas 6 (HUD) y 7; §7 pruebas → Tareas 1, 2 y 8. Los campos `baseStunChance` y el efecto visual del stun **no** se hacen (YAGNI, el spec también los deja fuera).
- **Cambio frente al spec:** los tests "un enemigo aturdido no se mueve ni ataca" y "los jefes no se aturden" no pueden ser de EditMode (`EnemyAI` necesita `NavMeshAgent` y el asmdef de tests no ve `Game.Runtime`). Se cubren con tests de `StunTimer`/`StunRules` (lógica pura) y con la verificación en Play (Tarea 8, pasos 3 y 4).
- **Consistencia de nombres:** `MeleeCone.Contains(origin, forward, point, range, arcDegrees, pointRadius)`, `StunRules.Roll`, `StunTimer.{IsStunned,Apply,Clear}`, `SwordDefinition.{arcDegrees,hitDelay,attackAnimSpeed,stunSeconds,StunChanceAt}`, `WeaponState.{Sword,StunChance}`, `EnemyAI.{ApplyStun,IsStunned}`, `PlayerBody.PlayAttack(float)`, `TreeBonuses.StunChanceBonus`: iguales en todas las tareas.
- **Sin marcadores pendientes.** Los valores a afinar jugando (`hitDelay`, daño, precios) son valores iniciales concretos, ajustables en el asset.
