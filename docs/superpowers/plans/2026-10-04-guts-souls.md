# Guts: almas de los enemigos — Plan de implementación

> **Para quien ejecute:** SUB-SKILL REQUERIDA: superpowers:executing-plans (el usuario eligió ejecución inline/Native). Los pasos usan casillas (`- [ ]`).
> **Regla del usuario: NO hacer commits.** Él hace los suyos.

**Goal:** Cuando muere un enemigo y el personaje activo es Guts, deja un alma en el piso que, al recogerla, lo cura un poco.

**Architecture:** `SoulRules` (puro, `Game.Core`, con tests) calcula la curación y el alcance. Los números viven en `CharacterDefinition` (`soulHealFraction`...). `EnemyAI` publica `GameEvents.EnemyDied(position, isBoss)`; `SoulSpawner` coloca las almas (pool de `SoulPickup`).

**Tech Stack:** Unity 6000.6, C#, NUnit (EditMode), asmdefs `Game.Core` / `Game.Runtime`.

**Spec:** `docs/superpowers/specs/2026-10-04-guts-souls-design.md`

## Global Constraints
- Cura **2%** de la vida máxima por alma (mínimo 1; 3 con 150 de vida); jefes y minijefes **x3**. Recoge a **1,5 m** (distancia horizontal). Desaparece a los **10 s**. Solo Guts (`soulHealFraction` 0 en los demás).
- Un jefe que resucita **no** suelta alma al "morir" la primera vez (solo la muerte real publica `EnemyDied`).
- Las almas se retiran al cambiar de personaje y al terminar la partida.
- No cambiar el guardado ni ningún `id`. `EnemyKilled(int)` no se toca.
- Los tests de EditMode solo ven `Game.Core`. Escribir archivos con la herramienta Write. Pasar `port: 7890` a las herramientas de Unity; tras editar scripts, esperar compilación (`AssetDatabase.Refresh(ForceUpdate)` y `unity_get_compilation_errors` con `isCompiling: false`).
- **Pruebas en Play:** respaldar `save.json` de ese momento y restaurarlo después; bucles con `EditorApplication.Step` con tope de pasos y cortando si `Time.time` no avanza; no dejar enemigos de vida enorme vivos; `Physics.SyncTransforms()` tras activar enemigos en la misma llamada.
- Línea base: la suite completa pasa (el total lo deja la entrega de la Llamarada). Para correr tests: el ejecutor por reflexión de `docs/superpowers/plans/2026-10-04-guts-fury.md` (sección "Cómo correr los tests de EditMode").

## Review Focus
1. **Vida llena al recoger:** `Health.Heal` no pasa del máximo; el alma igual se consume.
2. **Jefe que resucita:** no suelta alma al primer "morir".
3. **Muchas almas a la vez:** van en un pool; al cambiar de personaje o terminar la partida se retiran todas sin errores.
4. **Alucard / Frieren:** nunca sueltan almas.
5. **Jugador muerto:** no recoge ni se cura (`Health.IsDead`).

## Mapa de archivos
| Archivo | Acción |
|---|---|
| `Assets/Scripts/Core/Data/SoulRules.cs` | crear |
| `Assets/Scripts/Core/Data/CharacterDefinition.cs` | modificar (4 campos) |
| `Assets/Data/Characters/Guts.asset` | modificar (valores) |
| `Assets/Scripts/Core/GameEvents.cs` | modificar (`EnemyDied`) |
| `Assets/Scripts/Gameplay/Enemies/EnemyAI.cs` | modificar (publicar al morir) |
| `Assets/Scripts/Gameplay/Characters/SoulPickup.cs`, `SoulSpawner.cs` | crear |
| `Assets/Scenes/SampleScene.unity` | modificar (GameObject `Souls`) |
| `Assets/Tests/EditMode/SoulRulesTests.cs` | crear |

---

### Task 1: `SoulRules` y los datos de las almas

**Files:** Create `Assets/Scripts/Core/Data/SoulRules.cs`; Modify `CharacterDefinition.cs`, `Guts.asset`; Test `Assets/Tests/EditMode/SoulRulesTests.cs`.

**Interfaces — Produces:** `SoulRules.HealAmount(int maxHealth, float fraction, bool isBoss, float bossMultiplier) : int`; `SoulRules.IsWithinReach(Vector3 playerPosition, Vector3 soulPosition, float radius) : bool`; `CharacterDefinition.{soulHealFraction, soulBossMultiplier, soulLifetime, soulPickupRadius}`.

- [ ] **Step 1: Tests que fallan** — `SoulRulesTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SoulRulesTests
{
    [TestCase(150, 0.02f, false, 3f, 3)]
    [TestCase(150, 0.02f, true, 3f, 9)]
    [TestCase(40, 0.02f, false, 3f, 1)]    // mínimo 1
    [TestCase(100, 0.05f, false, 3f, 5)]
    public void HealAmount_IsAFractionOfMaxHealth_WithBossMultiplier(int max, float fraction, bool boss, float mult, int expected)
    {
        Assert.AreEqual(expected, SoulRules.HealAmount(max, fraction, boss, mult));
    }

    [TestCase(0f)]
    [TestCase(-0.5f)]
    public void HealAmount_ZeroOrNegativeFraction_GivesNothing(float fraction)
    {
        Assert.AreEqual(0, SoulRules.HealAmount(150, fraction, false, 3f));
        Assert.AreEqual(0, SoulRules.HealAmount(150, fraction, true, 3f));
    }

    [Test]
    public void IsWithinReach_InsideAndOutside()
    {
        Assert.IsTrue(SoulRules.IsWithinReach(Vector3.zero, new Vector3(1f, 0f, 0f), 1.5f));
        Assert.IsFalse(SoulRules.IsWithinReach(Vector3.zero, new Vector3(2f, 0f, 0f), 1.5f));
    }

    [Test]
    public void IsWithinReach_TheExactEdgeCounts()
    {
        Assert.IsTrue(SoulRules.IsWithinReach(Vector3.zero, new Vector3(1.5f, 0f, 0f), 1.5f));
    }

    [Test]
    public void IsWithinReach_IgnoresHeight()
    {
        Assert.IsTrue(SoulRules.IsWithinReach(new Vector3(0f, 1f, 0f), new Vector3(1f, 0.2f, 0f), 1.5f));
        Assert.IsTrue(SoulRules.IsWithinReach(Vector3.zero, new Vector3(0.5f, 40f, 0f), 1.5f));
    }

    [Test]
    public void GutsAsset_HasTheDesignValues()
    {
        var guts = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");

        Assert.AreEqual(0.02f, guts.soulHealFraction, 1e-4f);
        Assert.AreEqual(3f, guts.soulBossMultiplier, 1e-4f);
        Assert.AreEqual(10f, guts.soulLifetime, 1e-4f);
        Assert.AreEqual(1.5f, guts.soulPickupRadius, 1e-4f);
    }

    [TestCase("Assets/Data/Characters/Alucard.asset")]
    [TestCase("Assets/Data/Characters/Maga.asset")]
    public void OtherCharacters_DoNotDropSouls(string path)
    {
        var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);

        Assert.AreEqual(0f, character.soulHealFraction, 1e-4f);
    }
}
```

- [ ] **Step 2: Verificar que fallan** — compilación con errores (`SoulRules`, `soulHealFraction`...).

- [ ] **Step 3: Implementar** — `Assets/Scripts/Core/Data/SoulRules.cs`:

```csharp
using UnityEngine;

/// <summary>Reglas de las almas de Guts: cuánto curan y cuándo se recogen. Solo lógica, para probarla sin escena.</summary>
public static class SoulRules
{
    /// <summary>Vida que cura un alma: fracción de la vida máxima (x multiplicador si viene de un jefe). Mínimo 1; 0 si la fracción es ≤ 0.</summary>
    public static int HealAmount(int maxHealth, float fraction, bool isBoss, float bossMultiplier)
    {
        if (fraction <= 0f) return 0;

        float amount = maxHealth * fraction * (isBoss ? bossMultiplier : 1f);
        return Mathf.Max(1, Mathf.RoundToInt(amount));
    }

    /// <summary>¿Está el alma al alcance del jugador? Distancia horizontal ≤ radio; la altura no cuenta.</summary>
    public static bool IsWithinReach(Vector3 playerPosition, Vector3 soulPosition, float radius)
    {
        float dx = soulPosition.x - playerPosition.x;
        float dz = soulPosition.z - playerPosition.z;
        return dx * dx + dz * dz <= radius * radius + 1e-6f;
    }
}
```
En `CharacterDefinition.cs`, después de `public GameObject bodyPrefab;`:

```csharp

    [Header("Almas")]
    [Min(0f), Tooltip("Cuando muere un enemigo deja un alma que cura esta fracción de la vida máxima (0,02 = 2%). 0 = el personaje no suelta almas")]
    public float soulHealFraction = 0f;
    [Min(1f), Tooltip("Las almas de jefes y minijefes curan esto veces más")]
    public float soulBossMultiplier = 3f;
    [Min(1f), Tooltip("Segundos que dura un alma en el piso si no se recoge")]
    public float soulLifetime = 10f;
    [Min(0.1f), Tooltip("A cuántos metros (horizontales) se recoge sola")]
    public float soulPickupRadius = 1.5f;
```

- [ ] **Step 4: Valores en Guts** — tras compilar, `unity_execute_code`:

```csharp
var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
guts.soulHealFraction = 0.02f; guts.soulBossMultiplier = 3f; guts.soulLifetime = 10f; guts.soulPickupRadius = 1.5f;
UnityEditor.EditorUtility.SetDirty(guts);
UnityEditor.AssetDatabase.SaveAssets();
return "guts almas=" + guts.soulHealFraction + "/x" + guts.soulBossMultiplier + "/" + guts.soulLifetime + "s/" + guts.soulPickupRadius + "m";
```

- [ ] **Step 5: Verificar que pasan** — `filter = "SoulRules"`: `fallan=0`; luego suite completa sin fallos.

---

### Task 2: Evento, `SoulPickup`, `SoulSpawner` y la escena

**Files:** Modify `GameEvents.cs`, `EnemyAI.cs`; Create `Assets/Scripts/Gameplay/Characters/SoulPickup.cs`, `SoulSpawner.cs`; Modify la escena (script de editor).

**Interfaces — Consumes:** `SoulRules`, `CharacterDefinition.soul*` (Tarea 1), `Shooting.Instance.Character`, `PlayerHealth` (`Heal`, `MaxHealth`, `IsDead`), `GameEvents.{CharacterChanged, GameOver}`. **Produces:** `GameEvents.EnemyDied(Vector3 position, bool isBoss)` + `RaiseEnemyDied`.

- [ ] **Step 1: Evento** — `GameEvents.cs`: `public static event Action<Vector3, bool> EnemyDied;   // posición donde murió y si era jefe o minijefe (solo la muerte real: un jefe que resucita no lo publica)` y `public static void RaiseEnemyDied(Vector3 position, bool isBoss) => EnemyDied?.Invoke(position, isBoss);`.

- [ ] **Step 2: Publicarlo al morir** — en `EnemyAI.TakeDamage`, en la rama de muerte real, junto a `GameEvents.RaiseEnemyKilled(def.moneyReward);` (después del bloque de `TryRevive`):

```csharp
        GameEvents.RaiseEnemyDied(transform.position, IsBoss);
```

- [ ] **Step 3: `SoulPickup`** — `Assets/Scripts/Gameplay/Characters/SoulPickup.cs`:

```csharp
using System;
using UnityEngine;

/// <summary>
/// Alma que deja un enemigo al morir con Guts: flota con un vaivén y, si Guts pasa cerca, lo cura y desaparece.
/// Si nadie la recoge, se va sola al terminar su duración. SoulSpawner la crea y la recicla (pool).
/// </summary>
public class SoulPickup : MonoBehaviour
{
    private const float HoverHeight = 0.6f;
    private const float BobAmplitude = 0.12f;
    private const float BobSpeed = 3f;

    private Transform player;
    private PlayerHealth health;
    private Action<SoulPickup> release;
    private float timeLeft;
    private float radius;
    private int healAmount;
    private float baseY;

    public void Init(Vector3 position, int heal, float lifetime, float pickupRadius, float scale,
        Transform playerTransform, PlayerHealth playerHealth, Action<SoulPickup> releaseToPool)
    {
        transform.position = new Vector3(position.x, position.y + HoverHeight, position.z);
        transform.localScale = Vector3.one * scale;
        baseY = transform.position.y;
        healAmount = heal;
        timeLeft = lifetime;
        radius = pickupRadius;
        player = playerTransform;
        health = playerHealth;
        release = releaseToPool;
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            release(this);
            return;
        }

        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(Time.time * BobSpeed) * BobAmplitude;
        transform.position = p;

        if (player == null || health == null || health.IsDead) return;
        if (!SoulRules.IsWithinReach(player.position, transform.position, radius)) return;

        health.Heal(healAmount);   // Health.Heal no pasa de la vida máxima; el alma se consume igual
        release(this);
    }
}
```

- [ ] **Step 4: `SoulSpawner`** — `Assets/Scripts/Gameplay/Characters/SoulSpawner.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Coloca un alma donde muere cada enemigo mientras el personaje activo suelte almas (Guts). Las almas van en un pool
/// y se retiran todas al cambiar de personaje o al terminar la partida. Aspecto provisional: esfera azul brillante.
/// </summary>
public class SoulSpawner : MonoBehaviour
{
    private const float SoulScale = 0.35f;
    private const float BossSoulScale = 0.7f;
    private static readonly Color SoulColor = new Color(0.45f, 0.8f, 1f, 1f);

    private ObjectPool<SoulPickup> pool;
    private readonly List<SoulPickup> active = new List<SoulPickup>();
    private Material material;

    private void Awake()
    {
        Shader shader = Shader.Find("Sprites/Default");
        material = new Material(shader) { color = SoulColor };

        pool = new ObjectPool<SoulPickup>(
            createFunc: Create,
            actionOnGet: soul => soul.gameObject.SetActive(true),
            actionOnRelease: soul => soul.gameObject.SetActive(false),
            actionOnDestroy: soul => Destroy(soul.gameObject));
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void OnEnable()
    {
        GameEvents.EnemyDied += OnEnemyDied;
        GameEvents.CharacterChanged += OnCharacterChanged;
        GameEvents.GameOver += OnGameOver;
    }

    private void OnDisable()
    {
        GameEvents.EnemyDied -= OnEnemyDied;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        GameEvents.GameOver -= OnGameOver;
        ReleaseAll();
    }

    private SoulPickup Create()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Soul";
        Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(transform);
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        return sphere.AddComponent<SoulPickup>();
    }

    private void OnEnemyDied(Vector3 position, bool isBoss)
    {
        Shooting shooting = Shooting.Instance;
        CharacterDefinition character = shooting != null ? shooting.Character : null;
        if (character == null || character.soulHealFraction <= 0f) return;

        PlayerHealth health = shooting.GetComponent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        int heal = SoulRules.HealAmount(health.MaxHealth, character.soulHealFraction, isBoss, character.soulBossMultiplier);
        SoulPickup soul = pool.Get();
        soul.Init(position, heal, character.soulLifetime, character.soulPickupRadius,
            isBoss ? BossSoulScale : SoulScale, shooting.transform, health, Release);
        active.Add(soul);
    }

    private void Release(SoulPickup soul)
    {
        if (!active.Remove(soul)) return;   // ya estaba retirada
        pool.Release(soul);
    }

    private void OnCharacterChanged(CharacterDefinition character) => ReleaseAll();
    private void OnGameOver(string message, GameOverCause cause) => ReleaseAll();

    private void ReleaseAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) pool.Release(active[i]);
        active.Clear();
    }
}
```
Nota: `Health.MaxHealth` ya existe (`public int MaxHealth => maxHealth;`); `OnDisable` llama a `ReleaseAll` y el pool puede estar ya destruido al cerrar la escena: no pasa nada (los GameObjects se destruyen con la escena).

- [ ] **Step 5: GameObject en la escena** — con el editor fuera de Play, `unity_execute_code`:

```csharp
if (UnityEngine.Object.FindFirstObjectByType<SoulSpawner>(FindObjectsInactive.Include) != null) return "ya existe";
var go = new GameObject("Souls");
go.AddComponent<SoulSpawner>();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(go.scene);
return "Souls agregado y escena guardada";
```

- [ ] **Step 6: Verificar que compila** — 0 errores.

---

### Task 3: Verificación en Play, notas y memoria

- [ ] **Step 1: Respaldar el guardado de este momento** (`save_backup_souls.json` en el scratchpad).

- [ ] **Step 2: Play (pausado; funciones por reflexión; tope de pasos)**:
  1. Guts activo; `GameEvents.RaiseGameStarted()` no hace falta. Bajar la vida: `PlayerHealth.TakeDamage(100)` (150 → 50).
  2. Matar un enemigo normal (`TakeDamage(1000000)`) a 6 m del jugador: aparece **1 alma** en su lugar (`SoulSpawner` activas = 1; posición ≈ la del enemigo + 0,6 m de altura).
  3. Mover al jugador junto al alma (`rb.position`/`transform.position` + `Physics.SyncTransforms()`) y avanzar un frame (`Step`): la vida sube **3** (50 → 53) y el alma desaparece.
  4. Con la vida llena, recoger otra: la vida no pasa de 150 y el alma se consume.
  5. Sin recoger: dejar un alma lejos y avanzar **10 s de juego** (por `Time.time`, con tope de pasos): desaparece.
  6. Jefe (`Boss_Invocador`): matarlo (si tiene revivir, ver que el primer "morir" no suelta; usar `Boss_Tirador` que resucita: el primer golpe letal no deja alma, el segundo sí): su alma es mayor (escala 0,7) y cura **9**.
  7. Muerte por quemadura (`ApplyBurn` en un enemigo de poca vida): también suelta alma.
  8. Con Alucard (`TrySelect`): matar un enemigo **no** deja alma; con Frieren tampoco.
  9. Cambiar de personaje con almas en el piso: se retiran todas. `GameOver` también.
  10. Captura con un alma visible. Consola sin errores nuevos.

- [ ] **Step 3: Salir de Play y restaurar el guardado de este momento** (Unity quieto).

- [ ] **Step 4: Notas y memoria** (**no commitear**): entrada en `REGISTRO_DE_CAMBIOS.md`, decisión **D40** en `DECISIONES.md` (almas de Guts; pool; números en `Guts.asset`; la Furia no cura), "Dónde nos quedamos", línea de estado de la memoria.

- [ ] **Step 5: Informar al usuario** — qué quedó hecho y verificado; **no verificado**: cómo se ve y se siente recoger almas, y el balance de la curación (en `Guts.asset`).

---

## Self-Review (hecha al escribir)
- **Cobertura del spec:** §3 reglas → Tareas 1 y 2; §4 estructura → Tareas 1 y 2; §5 pruebas → Tareas 1 y 3.
- **Consistencia:** `SoulRules.{HealAmount,IsWithinReach}`, `CharacterDefinition.{soulHealFraction,soulBossMultiplier,soulLifetime,soulPickupRadius}`, `GameEvents.{EnemyDied,RaiseEnemyDied}`, `SoulPickup.Init(...)`, `SoulSpawner`: iguales en todas las tareas.
- **Sin marcadores pendientes.**
