# Guts: Llamarada (Q) y quemadura — Plan de implementación

> **Para quien ejecute:** SUB-SKILL REQUERIDA: superpowers:executing-plans (el usuario eligió ejecución inline/Native). Los pasos usan casillas (`- [ ]`).
> **Regla del usuario: NO hacer commits.** Él hace los suyos.

**Goal:** La habilidad `Q` de Guts: un cono de fuego frente a él que daña y deja quemados a los enemigos, potenciada (x2) con la Furia llena.

**Architecture:** Lógica pura de quemadura (`BurnState`) y fórmulas de daño (`AbilityDefinition`) en `Game.Core` con tests. `AbilityKind.FlameBurst` (valor 3, al final del enum). `EnemyBurn` por enemigo (como `EnemyBleed`). `FlameBurst` (clase estática en `Gameplay/Player`) hace el cono; `PlayerAbilities.TryCast` la llama.

**Tech Stack:** Unity 6000.6, C#, NUnit (EditMode), asmdefs `Game.Core` / `Game.Runtime`.

**Spec:** `docs/superpowers/specs/2026-10-04-guts-flame-burst-design.md`

## Global Constraints
- Cono: alcance **5 m**, **90°**. Daño inicial = daño de la espada × **2** (+0,25 por rango extra). Quemadura: **4 s** (+0,5 s por rango extra), tick cada **0,5 s**, **0,6** del daño de la espada por segundo (tick = `max(1, round(daño × 0,6 × 0,5))`). Enfriamiento **8 s** (−0,5 s por rango extra). 5 rangos.
- **Furia llena** (y toca a ≥ 1 enemigo): gasta toda la barra y multiplica por `SwordDefinition.furyDamageMultiplier` (2) el daño inicial **y** el de cada tick. Sin stun. Al aire: no gasta Furia (igual entra en enfriamiento). La Q **no carga Furia**.
- La quemadura no acumula: renueva la duración y conserva el mayor daño por tick. Los jefes se queman.
- `AbilityKind.FlameBurst` **al final** del enum (HeavyShot=0, Mist=1, Ultimate=2 no se mueven). No cambiar ningún `id` de asset. No cambiar el guardado.
- Los tests de EditMode solo ven `Game.Core`. Escribir archivos con la herramienta Write. Pasar `port: 7890` a las herramientas de Unity; tras editar scripts, esperar compilación (`AssetDatabase.Refresh(ForceUpdate)` desde `unity_execute_code` y luego `unity_get_compilation_errors` con `isCompiling: false`).
- **Pruebas en Play:** respaldar `save.json` **de ese momento** antes y restaurarlo después (Unity fuera de Play y quieto). **Todo bucle con `EditorApplication.Step` con tope de pasos y cortando si `Time.time` no avanza.** No dejar enemigos de vida enorme vivos. Llamar `Physics.SyncTransforms()` tras activar enemigos en la misma llamada. `GameObject.Find("XpBar"/"FuryBar")` encuentra el objeto raíz, no la barra del HUD.
- Línea base antes de empezar: **370 tests pasan, 0 fallan**. Para correrlos: el ejecutor por reflexión de `docs/superpowers/plans/2026-10-04-guts-fury.md` (sección "Cómo correr los tests de EditMode"), con `filter` por nombre de clase (`""` = todos).

## Review Focus
1. **Los valores del enum no se mueven:** Alucard sigue funcionando (test de valores + comprobar en Play una habilidad suya).
2. **Enemigo que muere por el daño de la llamarada no se quema ni da error** (`ApplyBurn` ignora muertos; daño antes de quemar).
3. **Enemigo del pool reaparece sin quemadura vieja:** `Spawn` la limpia (test de `Clear`).
4. **Llamarada sin enemigos en el cono:** sin errores, entra en enfriamiento, no gasta Furia.
5. **Quemadura que mata a un enemigo en pleno tick:** el bucle de ticks se corta y no hay error (como el sangrado).

## Mapa de archivos
| Archivo | Acción |
|---|---|
| `Assets/Scripts/Core/Data/BurnState.cs` | crear |
| `Assets/Scripts/Core/Data/AbilityDefinition.cs` | modificar (`FlameBurst`, campos, fórmulas, `DescribeRank`) |
| `Assets/Data/Abilities/Llamarada.asset`, `Assets/Art/Icons/Llamarada.png` | crear (script de editor) |
| `Assets/Data/Characters/Guts.asset` | modificar (`abilities`) |
| `Assets/Scripts/Core/GameEvents.cs` | modificar (`BurnTick`) |
| `Assets/Scripts/UI/FloatingTextManager.cs` | modificar (número naranja) |
| `Assets/Scripts/Gameplay/Enemies/EnemyBurn.cs` | crear |
| `Assets/Scripts/Gameplay/Enemies/EnemyAI.cs` | modificar (`ApplyBurn`, limpiar) |
| `Assets/Scripts/Gameplay/Player/FlameBurst.cs` | crear |
| `Assets/Scripts/Gameplay/Player/AbilityVfx.cs` | modificar (`FlameCone`) |
| `Assets/Scripts/Gameplay/Player/PlayerAbilities.cs` | modificar (`case` en `TryCast`) |
| `Assets/Scripts/Gameplay/Player/Shooting.cs` | modificar (`PublishFury` público) |
| `Assets/Scripts/Gameplay/Player/PlayerBody.cs`, `Assets/Animation/Guts.controller` | modificar (`Cast`) |
| `Assets/Tests/EditMode/BurnStateTests.cs`, `AbilityDefinitionTests.cs` | crear / modificar |

---

### Task 1: `BurnState` (lógica pura)

**Files:** Create `Assets/Scripts/Core/Data/BurnState.cs`; Test `Assets/Tests/EditMode/BurnStateTests.cs`.

**Interfaces — Produces:** `class BurnState { bool IsBurning; int DamagePerTick; void Apply(float seconds, int damagePerTick, float tickSeconds); int Advance(float deltaTime); void Clear(); }`. `DamagePerTick` sigue valiendo después de que la quemadura termina (hasta el próximo `Apply` o `Clear`), para que quien llama lo lea justo después de `Advance`.

- [ ] **Step 1: Escribir los tests que fallan** — `BurnStateTests.cs`:

```csharp
using NUnit.Framework;

public class BurnStateTests
{
    private static int Run(BurnState burn, float total, float step)
    {
        int ticks = 0;
        for (float t = 0f; t < total - 1e-4f; t += step) ticks += burn.Advance(step);
        return ticks;
    }

    [Test]
    public void NotBurningByDefault()
    {
        var burn = new BurnState();

        Assert.IsFalse(burn.IsBurning);
        Assert.AreEqual(0, burn.Advance(1f));
    }

    [Test]
    public void FourSeconds_EveryHalfSecond_GivesEightTicks_ThenStops()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.IsTrue(burn.IsBurning);
        Assert.AreEqual(8, Run(burn, 6f, 0.1f));
        Assert.IsFalse(burn.IsBurning);
    }

    [Test]
    public void OneBigStep_GivesTheSameTicks()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.AreEqual(8, burn.Advance(10f));
        Assert.IsFalse(burn.IsBurning);
    }

    [Test]
    public void NoTickBeforeTheFirstInterval()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.AreEqual(0, burn.Advance(0.4f));
        Assert.AreEqual(1, burn.Advance(0.1f));
    }

    [Test]
    public void Reapply_RenewsTheDuration()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);
        burn.Advance(3f);              // quedan 1 s
        burn.Apply(4f, 9, 0.5f);       // vuelve a 4 s

        Assert.AreEqual(8, Run(burn, 6f, 0.1f));
    }

    [Test]
    public void Reapply_KeepsTheHigherDamagePerTick()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);
        burn.Apply(4f, 18, 0.5f);
        Assert.AreEqual(18, burn.DamagePerTick);

        burn.Apply(4f, 5, 0.5f);
        Assert.AreEqual(18, burn.DamagePerTick, "el menor no reemplaza al mayor");
    }

    [Test]
    public void Reapply_DoesNotShortenALongerBurn()
    {
        var burn = new BurnState();
        burn.Apply(6f, 9, 0.5f);
        burn.Apply(2f, 9, 0.5f);

        Assert.AreEqual(12, Run(burn, 8f, 0.1f));
    }

    [Test]
    public void DamagePerTick_StillReadableRightAfterItEnds()
    {
        var burn = new BurnState();
        burn.Apply(1f, 7, 0.5f);

        int ticks = burn.Advance(5f);

        Assert.AreEqual(2, ticks);
        Assert.IsFalse(burn.IsBurning);
        Assert.AreEqual(7, burn.DamagePerTick);
    }

    [Test]
    public void ANewBurnAfterItEnded_UsesItsOwnDamage()
    {
        var burn = new BurnState();
        burn.Apply(1f, 18, 0.5f);
        burn.Advance(5f);
        burn.Apply(1f, 5, 0.5f);

        Assert.AreEqual(5, burn.DamagePerTick);
    }

    [TestCase(0f, 9, 0.5f)]
    [TestCase(-1f, 9, 0.5f)]
    [TestCase(4f, 0, 0.5f)]
    [TestCase(4f, 9, 0f)]
    public void InvalidApply_DoesNothing(float seconds, int damage, float tick)
    {
        var burn = new BurnState();
        burn.Apply(seconds, damage, tick);

        Assert.IsFalse(burn.IsBurning);
    }

    [Test]
    public void Clear_StopsTheBurn()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);
        burn.Clear();

        Assert.IsFalse(burn.IsBurning);
        Assert.AreEqual(0, burn.Advance(1f));
    }

    [Test]
    public void NonPositiveDeltaTime_DoesNothing()
    {
        var burn = new BurnState();
        burn.Apply(4f, 9, 0.5f);

        Assert.AreEqual(0, burn.Advance(0f));
        Assert.AreEqual(0, burn.Advance(-1f));
        Assert.IsTrue(burn.IsBurning);
    }
}
```

- [ ] **Step 2: Verificar que fallan** — forzar compilación: errores `CS0246` de `BurnState`.

- [ ] **Step 3: Implementar** — `Assets/Scripts/Core/Data/BurnState.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Quemadura de un enemigo: daño cada cierto tiempo durante unos segundos. Solo lógica (el tiempo se pasa desde
/// fuera), para poder probarla sin escena. Aplicarla de nuevo renueva la duración y conserva el mayor daño por tick.
/// </summary>
public class BurnState
{
    private float remaining;
    private float untilNextTick;
    private float tickSeconds = 0.5f;

    public bool IsBurning => remaining > 0f;

    /// <summary>Daño de cada tick. Sigue valiendo justo después de que la quemadura termina (hasta el próximo Apply o Clear).</summary>
    public int DamagePerTick { get; private set; }

    public void Apply(float seconds, int damagePerTick, float tick)
    {
        if (seconds <= 0f || damagePerTick <= 0 || tick <= 0f) return;

        if (!IsBurning)
        {
            remaining = seconds;
            DamagePerTick = damagePerTick;
            tickSeconds = tick;
            untilNextTick = tick;
            return;
        }

        remaining = Mathf.Max(remaining, seconds);
        DamagePerTick = Mathf.Max(DamagePerTick, damagePerTick);
    }

    /// <summary>Gasta tiempo y devuelve cuántos ticks tocan. Nunca da más ticks de los que caben en la duración.</summary>
    public int Advance(float deltaTime)
    {
        if (!IsBurning || deltaTime <= 0f) return 0;

        float step = Mathf.Min(deltaTime, remaining);
        remaining -= step;
        untilNextTick -= step;

        int ticks = 0;
        while (untilNextTick <= 1e-4f)
        {
            ticks++;
            untilNextTick += tickSeconds;
        }

        if (remaining <= 1e-5f) remaining = 0f;
        return ticks;
    }

    public void Clear()
    {
        remaining = 0f;
        DamagePerTick = 0;
    }
}
```

- [ ] **Step 4: Verificar que pasan** — compilar y correr `filter = "BurnState"`. Esperado: `fallan=0`.

---

### Task 2: `AbilityKind.FlameBurst`, campos y fórmulas en `AbilityDefinition`

**Files:** Modify `Assets/Scripts/Core/Data/AbilityDefinition.cs`; Test `Assets/Tests/EditMode/AbilityDefinitionTests.cs`.

**Interfaces — Produces:** `AbilityKind.FlameBurst` (= 3); `AbilityDefinition.{coneDegrees, burnDamageFractionPerSecond, burnTickSeconds}`; `int FlameDamageFor(int swordDamage, int rank, float empowerMultiplier = 1f)`; `int BurnTickDamageFor(int swordDamage, float empowerMultiplier = 1f)`; `float BurnSecondsAt(int rank)`; `DescribeRank` para `FlameBurst`.

- [ ] **Step 1: Tests que fallan** — al final de la clase `AbilityDefinitionTests` (antes de la llave de cierre) agregar:

```csharp
    // --- Llamarada de Guts ---

    private static AbilityDefinition Flame()
    {
        var flame = ScriptableObject.CreateInstance<AbilityDefinition>();
        flame.kind = AbilityKind.FlameBurst;
        flame.cooldown = 8f; flame.range = 5f; flame.coneDegrees = 90f;
        flame.damageMultiplier = 2f; flame.damagePerRank = 0.25f;
        flame.duration = 4f; flame.durationPerRank = 0.5f; flame.cooldownPerRank = 0.5f;
        flame.burnDamageFractionPerSecond = 0.6f; flame.burnTickSeconds = 0.5f;
        return flame;
    }

    [Test]
    public void EnumValues_OfAlucardsKinds_DoNotMove()
    {
        Assert.AreEqual(0, (int)AbilityKind.HeavyShot);
        Assert.AreEqual(1, (int)AbilityKind.Mist);
        Assert.AreEqual(2, (int)AbilityKind.Ultimate);
        Assert.AreEqual(3, (int)AbilityKind.FlameBurst);
    }

    [TestCase(30, 1, 60)]
    [TestCase(30, 5, 90)]    // x3 en el rango 5
    [TestCase(80, 1, 160)]
    [TestCase(0, 1, 1)]
    public void FlameDamage_IsTheSwordDamageTimesTheRankMultiplier(int sword, int rank, int expected)
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(expected, flame.FlameDamageFor(sword, rank));
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void FlameDamage_WithFury_IsDoubled()
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(120, flame.FlameDamageFor(30, 1, 2f));
        Object.DestroyImmediate(flame);
    }

    [TestCase(30, 9)]    // 30 x 0,6 x 0,5
    [TestCase(80, 24)]
    [TestCase(1, 1)]     // mínimo 1
    public void BurnTick_IsSixtyPercentOfTheSwordPerSecond(int sword, int expected)
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(expected, flame.BurnTickDamageFor(sword));
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void BurnTick_WithFury_IsDoubled()
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(18, flame.BurnTickDamageFor(30, 2f));
        Object.DestroyImmediate(flame);
    }

    [TestCase(1, 4f, 8f)]
    [TestCase(3, 5f, 7f)]
    [TestCase(5, 6f, 6f)]
    public void Ranks_LengthenTheBurn_AndShortenTheCooldown(int rank, float burn, float cooldown)
    {
        AbilityDefinition flame = Flame();
        Assert.AreEqual(burn, flame.BurnSecondsAt(rank), 1e-4f);
        Assert.AreEqual(cooldown, flame.CooldownAt(rank), 1e-4f);
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void DescribeRank_Flame_MentionsDamageBurnAndCooldown()
    {
        AbilityDefinition flame = Flame();
        string text = flame.DescribeRank(1);

        StringAssert.Contains("x2", text);
        StringAssert.Contains("quema 4", text);
        StringAssert.Contains("8", text);
        Object.DestroyImmediate(flame);
    }

    [Test]
    public void FlameDefaults_MatchTheDesign()
    {
        Assert.AreEqual(90f, ability.coneDegrees, 1e-4f);
        Assert.AreEqual(0.6f, ability.burnDamageFractionPerSecond, 1e-4f);
        Assert.AreEqual(0.5f, ability.burnTickSeconds, 1e-4f);
    }
```

- [ ] **Step 2: Verificar que fallan** — compilación con errores (`FlameBurst`, `coneDegrees`, `FlameDamageFor`... no existen).

- [ ] **Step 3: Implementar** — en `AbilityDefinition.cs`:
  1. Al **final** del enum `AbilityKind` (después de `Ultimate`, añadiendo la coma a `Ultimate`):

```csharp
    Ultimate,
    [Tooltip("Cono de fuego frente al jugador que daña y deja quemados a los enemigos (Guts)")]
    FlameBurst
```
  2. Después del bloque `[Header("Definitiva")]` (antes de `[Header("Mejoras por rango ...")]`):

```csharp
    [Header("Llamarada (Guts)")]
    [Tooltip("Apertura del cono de fuego frente al jugador, en grados. El alcance es 'range'; los segundos de quemadura son 'duration'")]
    public float coneDegrees = 90f;
    [Tooltip("Quemadura: fracción del daño de la espada que hace por segundo")]
    public float burnDamageFractionPerSecond = 0.6f;
    [Min(0.1f), Tooltip("Quemadura: cada cuántos segundos hace daño")]
    public float burnTickSeconds = 0.5f;
```
  3. En `DescribeRank`, antes del `default:` del `switch`:

```csharp
            case AbilityKind.FlameBurst:
                return "Daño x" + DamageMultiplierAt(rank).ToString("0.#") + " · quema " + DurationAt(rank).ToString("0.#") + " s · " + cd;
```
  4. Al final de la clase (junto a `DamageFor`):

```csharp
    /// <summary>Daño inicial de la llamarada: daño de la espada x multiplicador del rango (x2 más con Furia). Mínimo 1.</summary>
    public int FlameDamageFor(int swordDamage, int rank, float empowerMultiplier = 1f) =>
        Mathf.Max(1, Mathf.RoundToInt(swordDamage * DamageMultiplierAt(rank) * empowerMultiplier));

    /// <summary>Daño de cada tick de la quemadura: fracción por segundo del daño de la espada x el intervalo. Mínimo 1.</summary>
    public int BurnTickDamageFor(int swordDamage, float empowerMultiplier = 1f) =>
        Mathf.Max(1, Mathf.RoundToInt(swordDamage * burnDamageFractionPerSecond * burnTickSeconds * empowerMultiplier));

    /// <summary>Segundos que dura la quemadura en un rango.</summary>
    public float BurnSecondsAt(int rank) => DurationAt(rank);
```

- [ ] **Step 4: Verificar que pasan** — `filter = "AbilityDefinition"`: `fallan=0`. Luego `filter = ""`: el total debe ser 370 + 13 (BurnState) + las pruebas nuevas de esta tarea, sin fallos (Alucard intacto).

---

### Task 3: Quemadura en el juego (`EnemyBurn`, `ApplyBurn`, evento y número naranja)

**Files:** Modify `GameEvents.cs`, `FloatingTextManager.cs`, `EnemyAI.cs`; Create `Assets/Scripts/Gameplay/Enemies/EnemyBurn.cs`.

**Interfaces — Produces:** `GameEvents.BurnTick(Vector3 worldPosition, int damage)` + `RaiseBurnTick`; `EnemyAI.ApplyBurn(float seconds, int damagePerTick, float tickSeconds)`.

Sin test de EditMode (runtime); se prueba en Play (Tarea 6).

- [ ] **Step 1: Evento** — `GameEvents.cs`, junto a `BleedTick`:

```csharp
    public static event Action<Vector3, int> BurnTick;          // posición sobre el enemigo y daño del tick de quemadura
```
y junto a `RaiseBleedTick`:

```csharp
    public static void RaiseBurnTick(Vector3 worldPosition, int damage) => BurnTick?.Invoke(worldPosition, damage);
```

- [ ] **Step 2: Número naranja** — `FloatingTextManager.cs`: junto a `BleedColor` agregar `private static readonly Color BurnColor = new Color(1f, 0.55f, 0.1f);`; en `OnEnable` `GameEvents.BurnTick += ShowBurnTick;`; en `OnDisable` `GameEvents.BurnTick -= ShowBurnTick;`; y debajo de `ShowBleedTick`:

```csharp
    // Número naranja sobre el enemigo que se quema (igual que el sangrado, pero de otro color).
    private void ShowBurnTick(Vector3 worldPosition, int damage)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f) return;

        pool.Get().Show(damage.ToString(), screen, pool.Release, BurnColor);
    }
```

- [ ] **Step 3: `EnemyBurn`** — `Assets/Scripts/Gameplay/Enemies/EnemyBurn.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Quemadura de un enemigo: aplica el daño de cada tick, avisa a la UI (número naranja) y tiñe al enemigo.
/// Un solo Update por enemigo que arde, sin corrutinas. EnemyAI lo agrega solo la primera vez que hace falta.
/// Nota: usa el mismo MaterialPropertyBlock de color que EnemyBleed; Guts no sangra, así que no chocan en la práctica.
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public class EnemyBurn : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color BurnColor = new Color(1f, 0.45f, 0.05f);

    [SerializeField, Range(0f, 1f), Tooltip("Cuánto se acerca al naranja mientras arde")]
    private float tintAmount = 0.6f;

    private readonly BurnState burn = new BurnState();
    private EnemyAI enemy;
    private Collider body;
    private Renderer[] renderers;
    private Color[] baseColors;
    private MaterialPropertyBlock block;
    private bool tinted;

    private void Awake()
    {
        enemy = GetComponent<EnemyAI>();
        body = GetComponent<Collider>();
        block = new MaterialPropertyBlock();

        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].sharedMaterial;
            baseColors[i] = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
        }
    }

    public bool IsBurning => burn.IsBurning;

    /// <summary>Quema o renueva la quemadura (conserva el mayor daño por tick).</summary>
    public void Apply(float seconds, int damagePerTick, float tickSeconds)
    {
        burn.Apply(seconds, damagePerTick, tickSeconds);
        if (burn.IsBurning) SetTint(true);
    }

    /// <summary>Apaga la quemadura y devuelve el color original (al morir o al reutilizar del pool).</summary>
    public void Clear()
    {
        burn.Clear();
        SetTint(false);
    }

    private void Update()
    {
        if (!burn.IsBurning || GameState.IsGameOver) return;

        int ticks = burn.Advance(Time.deltaTime);
        int damage = burn.DamagePerTick;
        for (int i = 0; i < ticks && !enemy.IsDead; i++)
        {
            // El número se publica ANTES de dañar: si el tick mata, el enemigo vuelve al pool y ya no habría dónde mostrarlo.
            GameEvents.RaiseBurnTick(HeadPosition(), damage);
            enemy.TakeDamage(damage);
        }

        if (!burn.IsBurning) SetTint(false);
    }

    private Vector3 HeadPosition()
    {
        float top = body != null ? body.bounds.max.y : transform.position.y + 1f;
        return new Vector3(transform.position.x, top + 0.25f, transform.position.z);
    }

    private void SetTint(bool on)
    {
        if (renderers == null || tinted == on) return;
        tinted = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!on)
            {
                renderers[i].SetPropertyBlock(null);
                continue;
            }

            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(baseColors[i], BurnColor, tintAmount));
            renderers[i].SetPropertyBlock(block);
        }
    }
}
```

- [ ] **Step 4: `EnemyAI`** — campo junto a `private EnemyBleed bleed;`: `private EnemyBurn burn;`. En `Spawn`, junto a `if (bleed != null) bleed.Clear();`: `if (burn != null) burn.Clear();`. En `TakeDamage`, en la rama de muerte junto a `if (bleed != null) bleed.Clear();`: `if (burn != null) burn.Clear();`. Y después de `ApplyStun`:

```csharp
    /// <summary>Lo deja ardiendo unos segundos (daño cada tick). Renueva y conserva el mayor daño. No hace nada si ya murió.</summary>
    public void ApplyBurn(float seconds, int damagePerTick, float tickSeconds)
    {
        if (isDead || seconds <= 0f || damagePerTick <= 0) return;

        if (burn == null)
        {
            burn = GetComponent<EnemyBurn>();
            if (burn == null) burn = gameObject.AddComponent<EnemyBurn>();
        }

        burn.Apply(seconds, damagePerTick, tickSeconds);
    }
```

- [ ] **Step 5: Verificar** — compila sin errores.

---

### Task 4: La habilidad en el juego (`FlameBurst`, VFX, animación, `PlayerAbilities`)

**Files:** Create `Assets/Scripts/Gameplay/Player/FlameBurst.cs`; Modify `AbilityVfx.cs`, `PlayerAbilities.cs`, `Shooting.cs`, `PlayerBody.cs`, `Assets/Animation/Guts.controller` (script de editor).

**Interfaces — Consumes:** `AbilityDefinition.{range, coneDegrees, FlameDamageFor, BurnTickDamageFor, BurnSecondsAt, burnTickSeconds}`, `EnemyAI.ApplyBurn`, `Shooting.{WeaponCount, CurrentWeapon, Fury, AimRay(), Body, PublishFury}`, `WeaponState.{Sword, Damage}`, `MeleeCone.Contains`. **Produces:** `FlameBurst.Cast(AbilityDefinition ability, int rank, Shooting shooting) : bool`; `PlayerBody.PlayCast()`; `AbilityVfx.FlameCone(...)`.

- [ ] **Step 1: `PublishFury` público** — en `Shooting.cs` cambiar `private void PublishFury()` por `public void PublishFury()` (con comentario `/// <summary>Avisa a la barra de Furia del estado actual (la llamarada también la gasta).</summary>`).

- [ ] **Step 2: `FlameBurst`** — `Assets/Scripts/Gameplay/Player/FlameBurst.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Llamarada de Guts (tecla Q): un cono de fuego frente a él que daña y quema a todos los enemigos dentro.
/// Con la Furia llena sale potenciada (x2 en el daño inicial y en cada tick de la quemadura) y gasta la barra.
/// Clase estática para que PlayerAbilities, que es de Alucard, no crezca con la lógica de Guts.
/// </summary>
public static class FlameBurst
{
    private const float SearchMargin = 3f;     // holgura para enemigos grandes (el radio real se cuenta en MeleeCone)
    private const float VfxHeight = 1.1f;

    private static readonly Collider[] Buffer = new Collider[64];
    private static readonly List<EnemyAI> Targets = new List<EnemyAI>();

    /// <summary>Lanza la llamarada. Devuelve false si no se puede (sin espada); true aunque no toque a nadie (entra en enfriamiento).</summary>
    public static bool Cast(AbilityDefinition ability, int rank, Shooting shooting)
    {
        if (shooting.WeaponCount == 0) return false;

        WeaponState weapon = shooting.CurrentWeapon;
        SwordDefinition sword = weapon.Sword;
        if (sword == null) return false;   // el daño de la llamarada sale del de la espada

        Transform player = shooting.transform;
        Vector3 forward = Vector3.ProjectOnPlane(shooting.AimRay().direction, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = player.forward;
        forward.Normalize();

        Vector3 origin = player.position;
        AbilityVfx.FlameCone(origin + Vector3.up * (VfxHeight - 1f), forward, ability.range, ability.coneDegrees);
        if (shooting.Body != null) shooting.Body.PlayCast();

        Targets.Clear();
        int count = Physics.OverlapSphereNonAlloc(origin, ability.range + SearchMargin, Buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = Buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead || Targets.Contains(enemy)) continue;
            if (!MeleeCone.Contains(origin, forward, enemy.transform.position, ability.range, ability.coneDegrees, enemy.BodyRadius)) continue;

            Targets.Add(enemy);
        }

        // Sin enemigos en el cono: ni daño ni Furia gastada (igual entra en enfriamiento).
        if (Targets.Count == 0) return true;

        // Furia llena: la llamarada sale potenciada y gasta toda la barra.
        bool empowered = shooting.Fury.TryConsume();
        float multiplier = empowered ? sword.furyDamageMultiplier : 1f;

        int swordDamage = weapon.Damage;
        int damage = ability.FlameDamageFor(swordDamage, rank, multiplier);
        int tick = ability.BurnTickDamageFor(swordDamage, multiplier);
        float seconds = ability.BurnSecondsAt(rank);

        foreach (EnemyAI enemy in Targets)
        {
            enemy.TakeDamage(damage);   // primero el daño: si muere, ApplyBurn lo ignora
            enemy.ApplyBurn(seconds, tick, ability.burnTickSeconds);
        }

        if (empowered) shooting.PublishFury();
        return true;
    }
}
```

- [ ] **Step 3: Partículas del cono** — en `AbilityVfx.cs` agregar el campo `private ParticleSystem flameCone;` junto a `private int nextFlash;`, y estos métodos (dentro de la clase, antes de `NewMaterial`):

```csharp
    /// <summary>Chorro de fuego naranja en un cono (la llamarada de Guts). Provisional, por código.</summary>
    public static void FlameCone(Vector3 origin, Vector3 forward, float range, float coneDegrees)
    {
        AbilityVfx vfx = Instance;
        if (vfx.flameCone == null) vfx.flameCone = vfx.BuildFlameCone();

        Transform t = vfx.flameCone.transform;
        t.position = origin;
        t.rotation = Quaternion.LookRotation(forward);

        var main = vfx.flameCone.main;
        main.startSpeed = range / main.startLifetime.constant;   // llega al alcance justo al terminar su vida

        var shape = vfx.flameCone.shape;
        shape.angle = coneDegrees * 0.5f;

        vfx.flameCone.Emit(90);
    }

    private ParticleSystem BuildFlameCone()
    {
        var go = new GameObject("FlameConeVfx");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.4f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startColor = new Color(1f, 0.5f, 0.08f, 0.85f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.15f;
        shape.angle = 45f;

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f), new GradientColorKey(new Color(0.9f, 0.15f, 0.02f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var grow = ps.sizeOverLifetime;
        grow.enabled = true;
        grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));

        var rend = go.GetComponent<ParticleSystemRenderer>();
        Material material = NewMaterial(Color.white);
        material.mainTexture = SoftCircle();
        rend.sharedMaterial = material;

        return ps;
    }
```

- [ ] **Step 4: `PlayerBody.PlayCast`** — junto a las constantes: `private static readonly int CastParam = Animator.StringToHash("Cast");` y un campo `private bool hasCast;`; en el `Awake`/inicialización donde se calcula `hasAttack` (línea con `System.Array.Exists(animator.parameters, p => p.nameHash == AttackParam)`) agregar la línea `hasCast = System.Array.Exists(animator.parameters, p => p.nameHash == CastParam);`. Método junto a `PlayAttack`:

```csharp
    /// <summary>Lanzar una habilidad con la mano (la llamarada de Guts). Solo animación; no hace nada si el controlador no tiene el parámetro "Cast".</summary>
    public void PlayCast()
    {
        if (dead || !hasCast) return;
        animator.SetTrigger(CastParam);
    }
```

- [ ] **Step 5: `PlayerAbilities`** — en `TryCast`, dentro del `switch`, antes de `default`:

```csharp
            case AbilityKind.FlameBurst: cast = FlameBurst.Cast(ability, rank, shooting); break;
```
(`shooting` ya es el campo de la clase.)

- [ ] **Step 6: Estado `Cast` en `Guts.controller`** — con `unity_execute_code` (el controlador tiene el estado `Attack` con parámetro `Attack` de tipo Trigger; se copia su forma):

```csharp
var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/Guts.controller");
bool has = false;
foreach (var p in ctrl.parameters) if (p.name == "Cast") has = true;
if (!has) ctrl.AddParameter("Cast", UnityEngine.AnimatorControllerParameterType.Trigger);

AnimationClip clip = null;
foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/Guts/Animations/spell cast.fbx"))
    if (o is AnimationClip c && !c.name.StartsWith("__preview")) clip = c;
if (clip == null) return "no se encontró el clip spell cast";

var layer = ctrl.layers[0];
var sm = layer.stateMachine;
UnityEditor.Animations.AnimatorState attack = null, cast = null, locomotion = null;
foreach (var cs in sm.states)
{
    if (cs.state.name == "Attack") attack = cs.state;
    if (cs.state.name == "Cast") cast = cs.state;
    if (cs.state.name == "Locomotion") locomotion = cs.state;
}
if (cast == null)
{
    cast = sm.AddState("Cast", new Vector3(attack != null ? 300f : 0f, 400f, 0f));
    cast.motion = clip;
    cast.speed = 1.6f;
    var toCast = sm.AddAnyStateTransition(cast);
    toCast.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Cast");
    toCast.hasExitTime = false; toCast.duration = 0.05f; toCast.canTransitionToSelf = false;
    var back = cast.AddTransition(locomotion);
    back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.1f;
}
UnityEditor.EditorUtility.SetDirty(ctrl);
UnityEditor.AssetDatabase.SaveAssets();
return "Cast listo: clip=" + clip.name + " len=" + clip.length;
```
Esperado: `Cast listo: clip=... len=...`. Si el nombre del archivo o el clip difieren, listar los clips del FBX y ajustar (ruling en el registro).

- [ ] **Step 7: Verificar** — compila sin errores.

---

### Task 5: Asset de la habilidad, icono y enlace con Guts (con tests)

**Files:** Create `Assets/Data/Abilities/Llamarada.asset`, `Assets/Art/Icons/Llamarada.png`; Modify `Assets/Data/Characters/Guts.asset`; Test `AbilityDefinitionTests.cs` (agregar).

- [ ] **Step 1: Tests que fallan** — al final de `AbilityDefinitionTests` agregar:

```csharp
    [Test]
    public void LlamaradaAsset_Exists_WithTheDesignValues_AndGutsCarriesIt()
    {
        var flame = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Llamarada.asset");
        Assert.IsNotNull(flame, "Falta Assets/Data/Abilities/Llamarada.asset");

        Assert.AreEqual("Llamarada", flame.Id);
        Assert.AreEqual(AbilityKind.FlameBurst, flame.kind);
        Assert.AreEqual(5f, flame.range, 1e-4f);
        Assert.AreEqual(90f, flame.coneDegrees, 1e-4f);
        Assert.AreEqual(2f, flame.damageMultiplier, 1e-4f);
        Assert.AreEqual(4f, flame.duration, 1e-4f);
        Assert.AreEqual(8f, flame.cooldown, 1e-4f);
        Assert.AreEqual(0.25f, flame.damagePerRank, 1e-4f);
        Assert.AreEqual(0.5f, flame.durationPerRank, 1e-4f);
        Assert.AreEqual(0.5f, flame.cooldownPerRank, 1e-4f);
        Assert.IsNotNull(flame.icon, "Falta el icono");

        var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
        Assert.AreEqual(1, guts.abilities.Length);
        Assert.AreSame(flame, guts.abilities[0]);
    }
```

- [ ] **Step 2: Verificar que falla** — correr `filter = "AbilityDefinition"`: falla por `Falta ...Llamarada.asset`.

- [ ] **Step 3: Icono y asset** — `unity_execute_code` (genera un PNG de llama 128x128 por código, lo importa como Sprite, crea el asset y enlaza a Guts):

```csharp
const int S = 128;
var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
for (int y = 0; y < S; y++)
    for (int x = 0; x < S; x++)
    {
        float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        Color c = new Color(0.55f, 0.08f, 0.04f, 1f);                       // fondo rojo oscuro
        if (d > 0.95f) c = new Color(0f, 0f, 0f, 0f);
        else
        {
            // llama: gota apuntada hacia arriba
            float fy = (dy + 0.55f) / 1.5f;                                   // 0 abajo, 1 arriba
            float half = Mathf.Max(0f, 0.55f * (1f - fy) * (fy < 0f ? 0f : 1f)) * (1f - 0.25f * Mathf.Sin(fy * 9f));
            if (fy >= 0f && fy <= 1f && Mathf.Abs(dx) <= half) c = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.35f, 0.05f), fy);
            if (fy >= 0.1f && fy <= 0.7f && Mathf.Abs(dx) <= half * 0.45f) c = new Color(1f, 0.95f, 0.6f);
        }
        tex.SetPixel(x, y, c);
    }
tex.Apply();
System.IO.File.WriteAllBytes("Assets/Art/Icons/Llamarada.png", tex.EncodeToPNG());
UnityEditor.AssetDatabase.ImportAsset("Assets/Art/Icons/Llamarada.png");
var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Art/Icons/Llamarada.png");
imp.textureType = UnityEditor.TextureImporterType.Sprite;
imp.SaveAndReimport();
var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Icons/Llamarada.png");

var a = ScriptableObject.CreateInstance<AbilityDefinition>();
a.abilityName = "Llamarada";
a.icon = sprite;
a.kind = AbilityKind.FlameBurst;
a.cooldown = 8f; a.cooldownPerRank = 0.5f;
a.range = 5f; a.coneDegrees = 90f;
a.damageMultiplier = 2f; a.damagePerRank = 0.25f;
a.duration = 4f; a.durationPerRank = 0.5f;
a.burnDamageFractionPerSecond = 0.6f; a.burnTickSeconds = 0.5f;
UnityEditor.AssetDatabase.CreateAsset(a, "Assets/Data/Abilities/Llamarada.asset");
var so = new UnityEditor.SerializedObject(a);
so.FindProperty("id").stringValue = "Llamarada";
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(a);

var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
guts.abilities = new AbilityDefinition[] { a };
UnityEditor.EditorUtility.SetDirty(guts);
UnityEditor.AssetDatabase.SaveAssets();
return "asset=" + (UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Llamarada.asset") != null) + " icono=" + (sprite != null) + " guts habilidades=" + guts.abilities.Length;
```
Esperado: `asset=True icono=True guts habilidades=1`. (Revisar el icono con `Read` del PNG; si se ve mal, ajustar la forma: es provisional.)

- [ ] **Step 4: Verificar que pasa** — `filter = "AbilityDefinition"`: `fallan=0`; luego suite completa: sin fallos (esperado ≈ 370 + 13 + 13 + 1).

---

### Task 6: Verificación en Play, notas y memoria

- [ ] **Step 1: Respaldar el guardado de este momento** (PowerShell, copiar `save.json` al scratchpad como `save_backup_flame.json`).

- [ ] **Step 2: Play (pausado; llamar a las funciones por reflexión; tope de pasos)**:
  1. Guts activo: `PlayerAbilities.HasAbility(0)`; para poder lanzarla, dar rango 1: `SaveSystem.Data.GetCharacter("guts").abilityRanks[0] = 1` y `RefreshBuild()`; `GameEvents.RaiseGameStarted()`.
  2. Cono: 3 enemigos al frente (1,5 / 2,5 / 4,5 m), uno a 90° y uno detrás (2 m), todos con vida alta (escala 200). Llamar `PlayerAbilities.TryCast(0)` (reflexión). Esperado: los 3 de adelante pierden `2 × daño de la espada` (80 → 160) y quedan `EnemyBurn.IsBurning`; los otros dos intactos.
  3. Quemadura con el tiempo (Step con tope): el enemigo quemado pierde vida cada 0,5 s (tick = `round(daño × 0,3)`: 24 con daño 80) durante 4 s (≈ 8 ticks) y luego se apaga y vuelve el color.
  4. Segunda llamarada sobre uno ya quemado: renueva (no suma el doble de ticks).
  5. Jefe (`Boss_Invocador`) en el cono: recibe daño y se quema.
  6. Furia: `Fury.Add(100)` y publicar; llamarada con enemigos: daño inicial y tick x2 y la barra queda en 0. Llamarada al aire con la Furia llena: no la gasta.
  7. Enfriamiento: segunda `TryCast(0)` inmediata no lanza; HUD de la casilla.
  8. Con Alucard: `TryCast` de su Q sigue funcionando (sin regresión) y el enum no se movió.
  9. Captura con las partículas del cono y el tinte naranja (avanzar frames con `Step`, con tope). Consola sin errores nuevos.

- [ ] **Step 3: Salir de Play y restaurar el guardado de este momento** (Unity quieto).

- [ ] **Step 4: Notas y memoria** (**no commitear**): entrada nueva en `REGISTRO_DE_CAMBIOS.md` (hecho, verificado, no verificado), decisión **D39** en `DECISIONES.md` (Llamarada y quemadura; `FlameBurst` al final del enum; la Furia potencia habilidades con daño; las almas curan, la Furia no), actualizar "Dónde nos quedamos", `ATAJOS_DE_TECLADO.md` (Q con Guts) y la línea de estado de la memoria.

- [ ] **Step 5: Informar al usuario** — qué quedó hecho y verificado, qué es "no verificado" (la tecla `Q` real, cómo se ve el fuego, balance), y los números a ajustar en `Llamarada.asset`.

---

## Self-Review (hecha al escribir)
- **Cobertura del spec:** §3 la habilidad → Tareas 2, 4 y 5; §4 la quemadura → Tareas 1 y 3; §5 estructura → Tareas 1 a 5; §6 pruebas → Tareas 1, 2, 5 y 6.
- **Consistencia:** `BurnState.{IsBurning,DamagePerTick,Apply,Advance,Clear}`, `AbilityDefinition.{coneDegrees,burnDamageFractionPerSecond,burnTickSeconds,FlameDamageFor,BurnTickDamageFor,BurnSecondsAt}`, `EnemyAI.ApplyBurn`, `EnemyBurn.{Apply,Clear}`, `GameEvents.{BurnTick,RaiseBurnTick}`, `FlameBurst.Cast`, `PlayerBody.PlayCast`, `AbilityVfx.FlameCone`, `Shooting.PublishFury`: iguales en todas las tareas.
- **Sin marcadores pendientes.** Los valores por rango y los números del cono son iniciales y se ajustan en el asset.
