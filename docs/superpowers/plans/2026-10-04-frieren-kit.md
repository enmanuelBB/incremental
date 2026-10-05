# Kit de Frieren Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Darle a Frieren su kit completo: Zoltraak cargado (clic izquierdo), disparo básico en el clic derecho, rayo de maná (Q) como habilidad con rango, Campo de flores (E, colocación en dos pasos), Definitiva de maná (F), y que levite siempre (solo visual).

**Architecture:** La lógica pura (carga del Zoltraak, fórmulas del bastón, reglas del campo, cura por tiempo, levitar, cargas más rápidas) va en `Game.Core` con tests de EditMode. El gameplay (`ZoltraakCaster`, `ManaBeam`, `FlowerField`, `ManaPulseEffect`, `PlayerHover`, `StaffHud`) va en `Game.Runtime` y se verifica en Play. Se sigue el patrón de Guts: clases pequeñas que `PlayerAbilities` / `Shooting` crean solas (sin tocar la escena), assets nuevos en `Assets/Data/Abilities/`.

**Tech Stack:** Unity 6000.6, C#, NUnit (EditMode, asmdef `Game.Core` y `Game.Tests.EditMode`), ScriptableObjects.

**Spec:** `docs/superpowers/specs/2026-10-04-frieren-kit-design.md`

## Global Constraints

- **Sin commits** (el usuario hace los suyos). Todo el trabajo queda sin commitear; los pasos "Commit" de otros planes no existen aquí.
- Ejecución nativa, en línea, con TDD en la lógica de `Game.Core`; el gameplay se verifica en Play llamando funciones por reflexión con `EditorApplication.Step` (todo bucle con tope de pasos y corte si `Time.time` no avanza). El input real (clics, mantener, E dos veces) no se puede probar sin foco: decirlo como "no verificado".
- `AbilityKind` y `SkillEffectType`: solo se agrega **al final** (los valores están guardados). Al agregar un `AbilityKind`, revisar `Progression` (`MaxRank`, `RankCapForLevel`, `IsUltimate`).
- Dominio y escena no se recargan al entrar en Play: los estáticos persisten; todo objeto creado por código se comprueba contra `null` de Unity y se limpia en `OnDisable`/al cambiar de personaje.
- Frieren: id `maga`, `Maga.asset`, arma `Baston.asset` (`StaffDefinition`). Las tres habilidades empiezan en rango 0 (se aprenden con el punto de nivel; la F desde el nivel 6).
- Probar en Play modifica el guardado real: respaldar `save.json` **de ese momento** (`%USERPROFILE%/AppData/LocalLow/DefaultCompany/la primera nunca se olvida/`) y restaurarlo al terminar, con Unity fuera de Play y quieto. Esperar ~10 s tras recompilar antes de abrir Play.
- Textos al usuario en español. Mismo estilo de comentarios que el código vecino (español, cortos, explican el porqué).
- Si el editor está en Play al terminar de escribir scripts, el usuario está probando: pedirle que lo detenga antes de compilar.
- Comando de tests (EditMode, runner por reflexión en `unity_execute_code`, puerto 7890; filtrar por nombre de clase y también correr la suite completa al final de cada tarea): ver la nota al pie ("Cómo correr los tests").
- Verificar que **Frieren tiene la Q solo tras gastar el punto** (HUD y lanzamiento exigen rango ≥ 1).

## Review Focus

- Cambiar de personaje (o terminar la partida) con el campo en colocación, con el campo activo, con la definitiva activa o cargando un Zoltraak: todo se corta y se limpia (círculo, flores, barra de carga, ralentizaciones ya aplicadas expiran solas).
- El clic que confirma el campo no debe empezar a cargar un Zoltraak (ni el clic derecho que cancela debe disparar el básico).
- Maná insuficiente: Zoltraak no empieza a cargar; E no abre la colocación; F no se activa; Q no sale. Nada de esto inicia enfriamiento.
- Rango guardado de Frieren en 0 (como hoy): Q/E/F no salen en el HUD ni se pueden lanzar; el punto de nivel 1 se puede gastar en la Q o la E, y la F no se puede subir antes del nivel 6.
- Abrir un menú (estación, pausa) con el círculo de colocación abierto lo cancela; con `GameState.InputBlocked` nada se carga ni se lanza.
- Con Alucard o Guts nada cambia: el clic derecho sigue haciendo zoom y no aparece ningún elemento nuevo de HUD.

---

### Task 1: Lógica pura y datos (Game.Core)

**Files:**
- Create: `Assets/Scripts/Core/Data/ZoltraakCharge.cs`
- Create: `Assets/Scripts/Core/Data/FlowerFieldRules.cs` (clase `FlowerFieldRules` y clase `HealOverTime`)
- Create: `Assets/Scripts/Core/Data/HoverMath.cs`
- Modify: `Assets/Scripts/Core/Data/AbilityCharges.cs` (método `SpeedUp`)
- Modify: `Assets/Scripts/Core/Data/AbilityDefinition.cs` (3 `AbilityKind` al final, campos nuevos, `DescribeRank`)
- Modify: `Assets/Scripts/Core/Data/Progression.cs` (`IsUltimate`)
- Modify: `Assets/Scripts/Core/Data/StaffDefinition.cs` (datos y fórmulas del Zoltraak)
- Modify: `Assets/Scripts/Core/Data/CharacterDefinition.cs` (levitar)
- Test: `Assets/Tests/EditMode/FrierenKitTests.cs`

**Interfaces:**
- Produces:
  - `ZoltraakCharge { bool IsCharging; float Fraction; void Begin(float seconds); void Tick(float dt); float Release(); void Cancel(); }`
  - `FlowerFieldRules.ClampPoint(Vector3 origin, Vector3 point, float range) : Vector3` (limita en horizontal) y `FlowerFieldRules.Contains(Vector3 center, float radius, Vector3 point, float margin = 0f) : bool` (horizontal).
  - `HealOverTime { int Add(int maxHealth, float fractionPerSecond, float dt); void Reset(); }` (acumula el resto).
  - `HoverMath.Offset(float time, float height, float bob, float period) : float` (0 si `height <= 0`).
  - `AbilityCharges.SpeedUp(float seconds)`.
  - `AbilityKind.ManaBeam`, `AbilityKind.FlowerField`, `AbilityKind.ManaPulse` (en ese orden, después de `Berserk`).
  - `AbilityDefinition`: `manaCost`, `fieldHealFractionPerSecond`, `fieldSlowFraction`, `fieldTickSeconds`, `fieldPlaceRange`, `pulseStunSeconds`, `pulseSlowFraction`, `pulseCooldownBoost` (+ reutiliza `radius` y `duration` ya existentes).
  - `StaffDefinition`: `zoltraakDamage`, `zoltraakChargeSeconds`, `zoltraakMinDamageFraction`, `zoltraakMinRadius`, `zoltraakMaxRadius`, `zoltraakManaCost`, `zoltraakPause`, `zoltraakInstantPause`, `zoltraakRange`, `ZoltraakDamageAt(int powerLevel, float charge) : int`, `ZoltraakRadiusAt(float charge) : float`.
  - `CharacterDefinition`: `hoverHeight`, `hoverBob`, `hoverPeriod`.

- [ ] **Step 1: Escribir los tests que fallan** — crear `Assets/Tests/EditMode/FrierenKitTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

/// <summary>Lógica pura del kit de Frieren: carga del Zoltraak, fórmulas del bastón, campo de flores, levitar y cargas.</summary>
[TestFixture]
public class FrierenKitTests
{
    // --- Carga del Zoltraak ---

    [Test]
    public void Charge_StartsEmpty_AndFillsOverTheChargeTime()
    {
        var charge = new ZoltraakCharge();
        charge.Begin(1.2f);
        Assert.IsTrue(charge.IsCharging);
        Assert.AreEqual(0f, charge.Fraction, 1e-4f);

        charge.Tick(0.6f);
        Assert.AreEqual(0.5f, charge.Fraction, 1e-4f);

        charge.Tick(5f);
        Assert.AreEqual(1f, charge.Fraction, 1e-4f, "no pasa de 1");
    }

    [Test]
    public void Charge_Release_ReturnsTheFraction_AndResets()
    {
        var charge = new ZoltraakCharge();
        charge.Begin(1f);
        charge.Tick(0.25f);

        Assert.AreEqual(0.25f, charge.Release(), 1e-4f);
        Assert.IsFalse(charge.IsCharging);
        Assert.AreEqual(0f, charge.Release(), 1e-4f, "soltar sin cargar no da nada");
    }

    [Test]
    public void Charge_Cancel_StopsWithoutFiring()
    {
        var charge = new ZoltraakCharge();
        charge.Begin(1f);
        charge.Tick(0.5f);
        charge.Cancel();
        Assert.IsFalse(charge.IsCharging);
        Assert.AreEqual(0f, charge.Fraction, 1e-4f);
    }

    [Test]
    public void Charge_TickWhenNotCharging_DoesNothing()
    {
        var charge = new ZoltraakCharge();
        charge.Tick(3f);
        Assert.AreEqual(0f, charge.Fraction, 1e-4f);
    }

    // --- Fórmulas del bastón ---

    private static StaffDefinition NewStaff()
    {
        var staff = ScriptableObject.CreateInstance<StaffDefinition>();
        staff.zoltraakDamage = 45;
        staff.zoltraakMinDamageFraction = 0.4f;
        staff.zoltraakMinRadius = 2.5f;
        staff.zoltraakMaxRadius = 4f;
        staff.damageUpgrade.step = 0.3f;
        return staff;
    }

    [Test]
    public void Staff_ZoltraakDamage_GoesFromFortyPercentToFull()
    {
        StaffDefinition staff = NewStaff();
        Assert.AreEqual(18, staff.ZoltraakDamageAt(0, 0f));
        Assert.AreEqual(45, staff.ZoltraakDamageAt(0, 1f));
        Assert.AreEqual(Mathf.RoundToInt(45 * 0.7f), staff.ZoltraakDamageAt(0, 0.5f));
    }

    [Test]
    public void Staff_ZoltraakDamage_ScalesWithPower()
    {
        StaffDefinition staff = NewStaff();
        Assert.AreEqual(Mathf.RoundToInt(45 * 1.6f), staff.ZoltraakDamageAt(2, 1f), "Poder 2: +30% por nivel");
    }

    [Test]
    public void Staff_ZoltraakRadius_GrowsWithCharge()
    {
        StaffDefinition staff = NewStaff();
        Assert.AreEqual(2.5f, staff.ZoltraakRadiusAt(0f), 1e-4f);
        Assert.AreEqual(4f, staff.ZoltraakRadiusAt(1f), 1e-4f);
        Assert.AreEqual(3.25f, staff.ZoltraakRadiusAt(0.5f), 1e-4f);
        Assert.AreEqual(4f, staff.ZoltraakRadiusAt(9f), 1e-4f, "la carga se limita a 1");
    }

    // --- Campo de flores ---

    [Test]
    public void Field_ClampPoint_KeepsAPointInsideRange()
    {
        Vector3 origin = new Vector3(1f, 0f, 1f);
        Vector3 point = new Vector3(6f, 3f, 1f);
        Assert.AreEqual(point, FlowerFieldRules.ClampPoint(origin, point, 25f));
    }

    [Test]
    public void Field_ClampPoint_PullsAFarPointToTheRange_KeepingHeightAndDirection()
    {
        Vector3 origin = Vector3.zero;
        Vector3 clamped = FlowerFieldRules.ClampPoint(origin, new Vector3(100f, 2f, 0f), 25f);
        Assert.AreEqual(25f, clamped.x, 1e-3f);
        Assert.AreEqual(0f, clamped.z, 1e-3f);
        Assert.AreEqual(2f, clamped.y, 1e-3f);
    }

    [Test]
    public void Field_Contains_IgnoresHeight_AndCountsTheMargin()
    {
        Vector3 center = Vector3.zero;
        Assert.IsTrue(FlowerFieldRules.Contains(center, 5f, new Vector3(3f, 40f, 4f)), "exactamente 5 m en horizontal");
        Assert.IsFalse(FlowerFieldRules.Contains(center, 5f, new Vector3(6f, 0f, 0f)));
        Assert.IsTrue(FlowerFieldRules.Contains(center, 5f, new Vector3(6f, 0f, 0f), 1.5f), "con el radio del enemigo cuenta");
    }

    [Test]
    public void Heal_AccumulatesTheRemainder_SoSmallTicksStillAddUp()
    {
        var heal = new HealOverTime();
        int total = 0;
        for (int i = 0; i < 4; i++) total += heal.Add(100, 0.03f, 0.25f);   // 1 s a 3% de 100 = 3 de vida
        Assert.AreEqual(3, total);
    }

    [Test]
    public void Heal_Reset_DropsTheRemainder()
    {
        var heal = new HealOverTime();
        Assert.AreEqual(0, heal.Add(100, 0.03f, 0.25f));
        heal.Reset();
        Assert.AreEqual(0, heal.Add(100, 0.03f, 0.25f));
    }

    // --- Levitar ---

    [Test]
    public void Hover_WithNoHeight_IsZeroEvenWithBob()
    {
        Assert.AreEqual(0f, HoverMath.Offset(1.3f, 0f, 0.05f, 2.5f), 1e-6f);
    }

    [Test]
    public void Hover_Oscillates_AroundTheHeight()
    {
        Assert.AreEqual(0.45f, HoverMath.Offset(0f, 0.45f, 0.05f, 2.5f), 1e-4f);
        Assert.AreEqual(0.5f, HoverMath.Offset(0.625f, 0.45f, 0.05f, 2.5f), 1e-4f, "a un cuarto del periodo: cresta");
        Assert.AreEqual(0.4f, HoverMath.Offset(1.875f, 0.45f, 0.05f, 2.5f), 1e-4f, "a tres cuartos: valle");
    }

    // --- Cargas más rápidas (la definitiva acelera Q y E) ---

    [Test]
    public void Charges_SpeedUp_BringsTheRechargeCloser()
    {
        var charges = new AbilityCharges();
        charges.Configure(1, 10f, 0f);
        Assert.IsTrue(charges.TryUse(0f));

        charges.SpeedUp(4f);
        Assert.AreEqual(0, charges.Available(5f));
        Assert.AreEqual(1, charges.Available(6f));
    }

    [Test]
    public void Charges_SpeedUp_WhenFull_DoesNothing()
    {
        var charges = new AbilityCharges();
        charges.Configure(1, 10f, 0f);
        charges.SpeedUp(4f);
        Assert.IsTrue(charges.TryUse(1f));
        Assert.AreEqual(0, charges.Available(10f), "la recarga arranca al usarla: 10 s completos");
        Assert.AreEqual(1, charges.Available(11f));
    }

    // --- Tipos de habilidad y rangos ---

    [Test]
    public void ManaPulse_IsAnUltimate_UnlockedAtLevelSix_WithRankThree()
    {
        Assert.IsTrue(Progression.IsUltimate(AbilityKind.ManaPulse));
        Assert.AreEqual(3, Progression.MaxRank(AbilityKind.ManaPulse));
        Assert.AreEqual(0, Progression.RankCapForLevel(AbilityKind.ManaPulse, 5));
        Assert.AreEqual(1, Progression.RankCapForLevel(AbilityKind.ManaPulse, 6));
    }

    [Test]
    public void ManaBeamAndFlowerField_AreNormalAbilities_LearnableAtLevelOne()
    {
        foreach (AbilityKind kind in new[] { AbilityKind.ManaBeam, AbilityKind.FlowerField })
        {
            Assert.IsFalse(Progression.IsUltimate(kind));
            Assert.AreEqual(5, Progression.MaxRank(kind));
            Assert.AreEqual(1, Progression.RankCapForLevel(kind, 1));
        }
    }

    [Test]
    public void AbilityKinds_NewOnesAreAppendedAfterBerserk()
    {
        Assert.AreEqual(5, (int)AbilityKind.Berserk, "los valores guardados no cambian");
        Assert.AreEqual(6, (int)AbilityKind.ManaBeam);
        Assert.AreEqual(7, (int)AbilityKind.FlowerField);
        Assert.AreEqual(8, (int)AbilityKind.ManaPulse);
    }

    [Test]
    public void DescribeRank_NewKinds_HaveText()
    {
        var field = ScriptableObject.CreateInstance<AbilityDefinition>();
        field.kind = AbilityKind.FlowerField;
        field.duration = 6f;
        field.radius = 5f;
        field.cooldown = 18f;
        StringAssert.Contains("6", field.DescribeRank(1));
        StringAssert.Contains("18", field.DescribeRank(1));

        var pulse = ScriptableObject.CreateInstance<AbilityDefinition>();
        pulse.kind = AbilityKind.ManaPulse;
        pulse.duration = 9f;
        pulse.cooldown = 60f;
        StringAssert.Contains("9", pulse.DescribeRank(1));
        StringAssert.Contains("60", pulse.DescribeRank(1));

        var beam = ScriptableObject.CreateInstance<AbilityDefinition>();
        beam.kind = AbilityKind.ManaBeam;
        Assert.IsNotEmpty(beam.DescribeRank(1));
        Assert.AreEqual("Sin aprender", beam.DescribeRank(0));
    }
}
```

- [ ] **Step 2: Ver que falla** — esperar la recompilación (`unity_get_compilation_errors` con `isCompiling` en false). Expected: errores de compilación por los tipos que no existen (`ZoltraakCharge`, `FlowerFieldRules`, `HealOverTime`, `HoverMath`, `AbilityKind.ManaBeam`, `StaffDefinition.zoltraakDamage`, `AbilityCharges.SpeedUp`). Eso es el RED.

- [ ] **Step 3: Implementar `ZoltraakCharge`** — crear `Assets/Scripts/Core/Data/ZoltraakCharge.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Carga del Zoltraak de Frieren: se mantiene el clic y la fracción sube de 0 a 1 en 'seconds'. Al soltar se obtiene la
/// fracción y la carga se reinicia. Lógica pura: el reloj lo mueve quien la usa.
/// </summary>
public class ZoltraakCharge
{
    private float chargeSeconds = 1f;
    private float held;

    public bool IsCharging { get; private set; }

    public float Fraction => Mathf.Clamp01(held / chargeSeconds);

    public void Begin(float seconds)
    {
        chargeSeconds = Mathf.Max(0.01f, seconds);
        held = 0f;
        IsCharging = true;
    }

    public void Tick(float deltaTime)
    {
        if (IsCharging) held += deltaTime;
    }

    /// <summary>Suelta la carga: devuelve la fracción alcanzada (0 si no se estaba cargando) y se reinicia.</summary>
    public float Release()
    {
        float fraction = IsCharging ? Fraction : 0f;
        Cancel();
        return fraction;
    }

    public void Cancel()
    {
        IsCharging = false;
        held = 0f;
    }
}
```

- [ ] **Step 4: Implementar `FlowerFieldRules` y `HealOverTime`** — crear `Assets/Scripts/Core/Data/FlowerFieldRules.cs`:

```csharp
using UnityEngine;

/// <summary>Reglas del campo de flores de Frieren. Todo se mide en horizontal: la altura no cuenta.</summary>
public static class FlowerFieldRules
{
    /// <summary>Deja el punto donde está si queda a 'range' metros o menos del origen; si no, lo acerca hasta el límite (misma dirección y altura).</summary>
    public static Vector3 ClampPoint(Vector3 origin, Vector3 point, float range)
    {
        Vector3 flat = new Vector3(point.x - origin.x, 0f, point.z - origin.z);
        if (flat.magnitude <= range) return point;

        Vector3 limited = origin + flat.normalized * range;
        limited.y = point.y;
        return limited;
    }

    /// <summary>Si el punto está dentro del círculo (el margen suma al radio: sirve para el tamaño del cuerpo de un enemigo).</summary>
    public static bool Contains(Vector3 center, float radius, Vector3 point, float margin = 0f) =>
        new Vector2(point.x - center.x, point.z - center.z).magnitude <= radius + margin;
}

/// <summary>Cura por tiempo en números enteros: lo que no alcanza a ser 1 punto en un tick se acumula para el siguiente.</summary>
public class HealOverTime
{
    private float carry;

    /// <summary>Puntos de vida (enteros) que toca curar en este tick. 'fractionPerSecond' es fracción de la vida máxima.</summary>
    public int Add(int maxHealth, float fractionPerSecond, float deltaTime)
    {
        carry += maxHealth * fractionPerSecond * deltaTime;

        int whole = Mathf.FloorToInt(carry + 1e-4f);
        carry -= whole;
        return Mathf.Max(0, whole);
    }

    public void Reset() => carry = 0f;
}
```

- [ ] **Step 5: Implementar `HoverMath`** — crear `Assets/Scripts/Core/Data/HoverMath.cs`:

```csharp
using UnityEngine;

/// <summary>Levitar de Frieren: altura sobre el suelo con un balanceo suave. Solo visual.</summary>
public static class HoverMath
{
    /// <summary>Altura (m) en el instante 'time'. Sin altura (0 o menos) no levita: devuelve 0 aunque haya balanceo.</summary>
    public static float Offset(float time, float height, float bob, float period)
    {
        if (height <= 0f) return 0f;

        return height + bob * Mathf.Sin(2f * Mathf.PI * time / Mathf.Max(0.1f, period));
    }
}
```

- [ ] **Step 6: `AbilityCharges.SpeedUp`** — en `Assets/Scripts/Core/Data/AbilityCharges.cs`, agregar después de `RechargeRemaining`:

```csharp
    /// <summary>Adelanta la recarga en curso 'seconds' segundos (la definitiva de Frieren acelera sus otras habilidades). No hace nada si están todas las cargas.</summary>
    public void SpeedUp(float seconds)
    {
        if (charges >= max || seconds <= 0f) return;

        rechargeAt -= seconds;
    }
```

- [ ] **Step 6b: `AbilityDefinition`** — en `Assets/Scripts/Core/Data/AbilityDefinition.cs`: (a) ampliar el enum, **después de `Berserk`**:

```csharp
    [Tooltip("Armadura Berserker: interruptor con bonos fuertes que drena vida (Guts)")]
    Berserk,
    [Tooltip("Rayo de maná masivo: línea que atraviesa a todos los enemigos (Frieren). Daño, maná y enfriamiento salen del bastón")]
    ManaBeam,
    [Tooltip("Campo de flores: zona colocada con la mira que cura a Frieren y ralentiza a los enemigos (Frieren)")]
    FlowerField,
    [Tooltip("Pulso de maná: definitiva que aturde, ralentiza, vuelve instantáneo el Zoltraak y acelera las otras habilidades (Frieren)")]
    ManaPulse
```

(b) agregar, antes de `[Header("Mejoras por rango (cada rango sobre el 1.º)")]`:

```csharp
    [Header("Maná")]
    [Min(0f), Tooltip("Maná que gasta al lanzarla (Campo de flores y Pulso de maná; el rayo usa el del bastón)")]
    public float manaCost = 0f;

    [Header("Campo de flores (Frieren)")]
    [Tooltip("Cura a Frieren, mientras esté dentro, esta fracción de su vida máxima por segundo (0,03 = 3%). 'radius' es el radio y 'duration' los segundos que dura")]
    public float fieldHealFractionPerSecond = 0.03f;
    [Range(0f, 0.9f), Tooltip("Fracción de velocidad que quita a los enemigos dentro (0,4 = -40%)")]
    public float fieldSlowFraction = 0.4f;
    [Min(0.05f), Tooltip("Cada cuántos segundos cura y ralentiza")]
    public float fieldTickSeconds = 0.25f;
    [Min(1f), Tooltip("Hasta cuántos metros se puede colocar el campo")]
    public float fieldPlaceRange = 25f;

    [Header("Pulso de maná (Frieren)")]
    [Min(0f), Tooltip("Segundos que aturde a los enemigos del radio al activarlo (los jefes son inmunes). El radio es 'radius' y la duración 'duration'")]
    public float pulseStunSeconds = 1.5f;
    [Range(0f, 0.9f), Tooltip("Fracción de velocidad que quita a los enemigos del radio mientras dura (0,4 = -40%)")]
    public float pulseSlowFraction = 0.4f;
    [Min(1f), Tooltip("Mientras dura, las otras dos habilidades se recargan este número de veces más rápido (2 = el doble)")]
    public float pulseCooldownBoost = 2f;
```

(c) en `DescribeRank`, agregar antes del `default:`:

```csharp
            case AbilityKind.ManaBeam:
                return "Rayo que atraviesa enemigos · daño, maná y enfriamiento del bastón";
            case AbilityKind.FlowerField:
                return "Radio " + radius.ToString("0.#") + " m · " + DurationAt(rank).ToString("0.#") + " s · cura " + (fieldHealFractionPerSecond * 100f).ToString("0.#")
                    + "% por s · enemigos -" + Mathf.RoundToInt(fieldSlowFraction * 100f) + "% · " + cd;
            case AbilityKind.ManaPulse:
                return DurationAt(rank).ToString("0.#") + " s · aturde " + pulseStunSeconds.ToString("0.#") + " s · Zoltraak instantáneo · " + cd;
```

- [ ] **Step 6c: `Progression.IsUltimate`** — en `Assets/Scripts/Core/Data/Progression.cs` cambiar la línea y su comentario:

```csharp
    /// <summary>Las definitivas (la de Alucard, la armadura Berserker de Guts y el pulso de maná de Frieren) llegan a rango 3 y se desbloquean en los niveles 6, 12 y 18.</summary>
    public static bool IsUltimate(AbilityKind kind) =>
        kind == AbilityKind.Ultimate || kind == AbilityKind.Berserk || kind == AbilityKind.ManaPulse;
```

- [ ] **Step 6d: `StaffDefinition`** — en `Assets/Scripts/Core/Data/StaffDefinition.cs`, después del bloque `[Header("Mejora de Maná: enfriamiento")]` (después de `cooldownLimit`) agregar:

```csharp
    [Header("Zoltraak (disparo cargado, clic izquierdo)")]
    [Tooltip("Daño a carga completa (sin mejoras)")]
    public int zoltraakDamage = 45;
    [Min(0.1f), Tooltip("Segundos que hay que mantener el clic para la carga completa")]
    public float zoltraakChargeSeconds = 1.2f;
    [Range(0f, 1f), Tooltip("Fracción del daño con la carga en 0 (suelta al instante)")]
    public float zoltraakMinDamageFraction = 0.4f;
    [Tooltip("Radio de la explosión con la carga en 0, en metros")]
    public float zoltraakMinRadius = 2.5f;
    [Tooltip("Radio de la explosión a carga completa, en metros")]
    public float zoltraakMaxRadius = 4f;
    [Tooltip("Maná que gasta al dispararlo (con la definitiva activa es gratis)")]
    public float zoltraakManaCost = 15f;
    [Tooltip("Pausa tras un Zoltraak antes de poder empezar otro, en segundos")]
    public float zoltraakPause = 0.4f;
    [Tooltip("Pausa entre disparos con la definitiva activa (sin carga)")]
    public float zoltraakInstantPause = 0.35f;
    [Tooltip("Alcance de la mira para colocar la explosión, en metros")]
    public float zoltraakRange = 60f;
```

y, junto a `AbilityDamageAt`:

```csharp
    /// <summary>Daño del Zoltraak: el de carga completa con el Poder de la tienda (+step por nivel), escalado de 'zoltraakMinDamageFraction' a 1 según la carga (0 a 1).</summary>
    public int ZoltraakDamageAt(int powerLevel, float charge) =>
        Mathf.RoundToInt(zoltraakDamage * (1f + powerLevel * damageUpgrade.step) * Mathf.Lerp(zoltraakMinDamageFraction, 1f, Mathf.Clamp01(charge)));

    public float ZoltraakRadiusAt(float charge) =>
        Mathf.Lerp(zoltraakMinRadius, zoltraakMaxRadius, Mathf.Clamp01(charge));
```

- [ ] **Step 6e: `CharacterDefinition`** — en `Assets/Scripts/Core/Data/CharacterDefinition.cs`, antes de `[Header("Almas")]`:

```csharp
    [Header("Levitar (solo visual)")]
    [Min(0f), Tooltip("Metros que sube el cuerpo y la cámara sobre el suelo. 0 = no levita. La física no cambia")]
    public float hoverHeight = 0f;
    [Min(0f), Tooltip("Amplitud del balanceo suave, en metros")]
    public float hoverBob = 0.05f;
    [Min(0.1f), Tooltip("Segundos de un balanceo completo")]
    public float hoverPeriod = 2.5f;
```

- [ ] **Step 7: Ver que pasa** — esperar la compilación y correr `FrierenKitTests` con el runner. Expected: todos pasan (≈20), 0 fallan. Luego correr la suite completa. Expected: 0 fallan (el número exacto sube en ~20 respecto de los 502 de ahora). Si algún test existente falla por `Progression`/`AbilityKind`, es un hallazgo real: corregirlo.

---

### Task 2: Assets de Frieren y tests de assets

**Files:**
- Create: `Assets/Data/Abilities/RayoMana.asset`, `Assets/Data/Abilities/CampoFlores.asset`, `Assets/Data/Abilities/PulsoMana.asset` (con un script de editor de un solo uso en `unity_execute_code`)
- Modify: `Assets/Data/Characters/Maga.asset` (abilities, levitar, descripción)
- Modify: `Assets/Data/Weapons/Baston.asset` (guardar los campos nuevos del Zoltraak)
- Test: `Assets/Tests/EditMode/FrierenAssetTests.cs`

**Interfaces:**
- Consumes: Task 1 (`AbilityKind.*`, campos nuevos).
- Produces: `Maga.abilities = [RayoMana, CampoFlores, PulsoMana]` (casillas Q, E, F); `Maga.hoverHeight = 0.45`.

- [ ] **Step 1: Test que falla** — crear `Assets/Tests/EditMode/FrierenAssetTests.cs`:

```csharp
using NUnit.Framework;
using UnityEditor;

/// <summary>Valida los assets reales de Frieren (el asset del personaje, sus habilidades y el bastón).</summary>
[TestFixture]
public class FrierenAssetTests
{
    private static CharacterDefinition Maga => AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Maga.asset");
    private static StaffDefinition Staff => AssetDatabase.LoadAssetAtPath<StaffDefinition>("Assets/Data/Weapons/Baston.asset");

    [Test]
    public void Frieren_HasThreeAbilities_InQEFOrder()
    {
        AbilityDefinition[] abilities = Maga.abilities;
        Assert.AreEqual(3, abilities.Length);
        Assert.AreEqual(AbilityKind.ManaBeam, abilities[0].kind);
        Assert.AreEqual(AbilityKind.FlowerField, abilities[1].kind);
        Assert.AreEqual(AbilityKind.ManaPulse, abilities[2].kind);
        foreach (AbilityDefinition ability in abilities) Assert.IsFalse(string.IsNullOrEmpty(ability.abilityName));
    }

    [Test]
    public void FlowerField_HasTheSpecValues()
    {
        AbilityDefinition field = Maga.abilities[1];
        Assert.AreEqual(5f, field.radius, 1e-4f);
        Assert.AreEqual(6f, field.duration, 1e-4f);
        Assert.AreEqual(18f, field.cooldown, 1e-4f);
        Assert.AreEqual(20f, field.manaCost, 1e-4f);
        Assert.AreEqual(0.03f, field.fieldHealFractionPerSecond, 1e-4f);
        Assert.AreEqual(0.4f, field.fieldSlowFraction, 1e-4f);
        Assert.AreEqual(25f, field.fieldPlaceRange, 1e-4f);
        Assert.Greater(field.cooldown, field.duration, "el campo no puede pisarse a sí mismo");
    }

    [Test]
    public void ManaPulse_HasTheSpecValues()
    {
        AbilityDefinition pulse = Maga.abilities[2];
        Assert.AreEqual(15f, pulse.radius, 1e-4f);
        Assert.AreEqual(9f, pulse.duration, 1e-4f);
        Assert.AreEqual(60f, pulse.cooldown, 1e-4f);
        Assert.AreEqual(50f, pulse.manaCost, 1e-4f);
        Assert.AreEqual(1.5f, pulse.pulseStunSeconds, 1e-4f);
        Assert.AreEqual(0.4f, pulse.pulseSlowFraction, 1e-4f);
        Assert.AreEqual(2f, pulse.pulseCooldownBoost, 1e-4f);
    }

    [Test]
    public void Staff_Zoltraak_HasTheSpecValues()
    {
        StaffDefinition staff = Staff;
        Assert.AreEqual(45, staff.zoltraakDamage);
        Assert.AreEqual(1.2f, staff.zoltraakChargeSeconds, 1e-4f);
        Assert.AreEqual(0.4f, staff.zoltraakMinDamageFraction, 1e-4f);
        Assert.AreEqual(2.5f, staff.zoltraakMinRadius, 1e-4f);
        Assert.AreEqual(4f, staff.zoltraakMaxRadius, 1e-4f);
        Assert.AreEqual(15f, staff.zoltraakManaCost, 1e-4f);
        Assert.AreEqual(0.4f, staff.zoltraakPause, 1e-4f);
        Assert.AreEqual(0.35f, staff.zoltraakInstantPause, 1e-4f);
    }

    [Test]
    public void Staff_BeamNumbersStayInTheStaff()
    {
        StaffDefinition staff = Staff;
        Assert.AreEqual(40, staff.abilityDamage);
        Assert.AreEqual(25f, staff.abilityManaCost, 1e-4f);
        Assert.AreEqual(3f, staff.abilityCooldown, 1e-4f);
    }

    [Test]
    public void Frieren_Levitates_AndOthersDoNot()
    {
        Assert.AreEqual(0.45f, Maga.hoverHeight, 1e-4f);
        Assert.AreEqual(0.05f, Maga.hoverBob, 1e-4f);
        Assert.AreEqual(2.5f, Maga.hoverPeriod, 1e-4f);
        Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Alucard.asset").hoverHeight);
        Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset").hoverHeight);
    }

    [Test]
    public void Frieren_FirstTwoAbilitiesCanBeLearnedAtLevelOne_ButNotTheUltimate()
    {
        var save = new CharacterSave { level = 1 };
        AbilityDefinition[] abilities = Maga.abilities;

        Assert.AreEqual(UpgradeBlock.None, Progression.CanUpgradeAbility(save, 0, abilities[0].kind));
        Assert.AreEqual(UpgradeBlock.None, Progression.CanUpgradeAbility(save, 1, abilities[1].kind));
        Assert.AreEqual(UpgradeBlock.LevelTooLow, Progression.CanUpgradeAbility(save, 2, abilities[2].kind));
        Assert.AreEqual(1, Progression.PointsAvailable(save), "un solo punto: se elige entre la Q y la E");
    }
}
```

- [ ] **Step 2: Ver que falla** — correr `FrierenAssetTests`. Expected: fallan (`abilities` de Maga está vacío / `NullReference`).

- [ ] **Step 3: Crear los assets** — con `unity_execute_code` (Unity en edición, no en Play):

```csharp
using UnityEditor;
using UnityEngine;

AbilityDefinition Make(string file, string name, AbilityKind kind, System.Action<AbilityDefinition> setup)
{
    string path = "Assets/Data/Abilities/" + file + ".asset";
    var asset = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
    if (asset == null) { asset = ScriptableObject.CreateInstance<AbilityDefinition>(); AssetDatabase.CreateAsset(asset, path); }
    asset.abilityName = name;
    asset.kind = kind;
    setup(asset);
    EditorUtility.SetDirty(asset);
    return asset;
}

var beam = Make("RayoMana", "Rayo de maná masivo", AbilityKind.ManaBeam, a => { a.cooldown = 3f; });
var field = Make("CampoFlores", "Campo de flores", AbilityKind.FlowerField, a =>
{
    a.cooldown = 18f; a.radius = 5f; a.duration = 6f; a.manaCost = 20f;
    a.fieldHealFractionPerSecond = 0.03f; a.fieldSlowFraction = 0.4f; a.fieldTickSeconds = 0.25f; a.fieldPlaceRange = 25f;
    a.cooldownPerRank = 1f; a.durationPerRank = 0.5f;
});
var pulse = Make("PulsoMana", "Pulso de maná", AbilityKind.ManaPulse, a =>
{
    a.cooldown = 60f; a.radius = 15f; a.duration = 9f; a.manaCost = 50f;
    a.pulseStunSeconds = 1.5f; a.pulseSlowFraction = 0.4f; a.pulseCooldownBoost = 2f;
    a.cooldownPerRank = 5f; a.durationPerRank = 0.5f;
});

var maga = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Maga.asset");
maga.abilities = new[] { beam, field, pulse };
maga.hoverHeight = 0.45f; maga.hoverBob = 0.05f; maga.hoverPeriod = 2.5f;
maga.description = "Maga de control y de área. Carga el Zoltraak (clic izquierdo) para explotar zonas enteras, dispara sin gastar maná con el clic derecho, atraviesa filas con el rayo de maná, cura y frena a la horda con el campo de flores y levita siempre.";
EditorUtility.SetDirty(maga);

var staff = AssetDatabase.LoadAssetAtPath<StaffDefinition>("Assets/Data/Weapons/Baston.asset");
EditorUtility.SetDirty(staff);   // escribe los campos nuevos del Zoltraak con sus valores por defecto
AssetDatabase.SaveAssets();
return "ok: " + maga.abilities.Length + " habilidades";
```

- [ ] **Step 4: Ver que pasa** — correr `FrierenAssetTests` y la suite completa. Expected: 0 fallan. Si falla un test de balance o de personaje existente que supone que Frieren no tiene habilidades, ajustarlo con una razón anotada en el registro (no relajar a ciegas).

---

### Task 3: Controles, Zoltraak y disparo básico en el clic derecho

**Files:**
- Modify: `Assets/Scripts/Gameplay/Player/GameInput.cs` (`AimPressed`, `FireReleased`)
- Modify: `Assets/Scripts/Gameplay/Player/CameraFollow.cs` (sin zoom con el bastón)
- Modify: `Assets/Scripts/Gameplay/Player/Shooting.cs` (`UsesStaff`, `UpdateStaff`, públicos auxiliares)
- Modify: `Assets/Scripts/Gameplay/Player/PlayerAbilities.cs` (`PulseActive` y `BlocksFire` provisionales)
- Modify: `Assets/Scripts/Gameplay/Player/AbilityVfx.cs` (`ExplosionFlash` con color opcional)
- Modify: `Assets/Scripts/Gameplay/Weapons/WeaponState.cs` (`ZoltraakDamage`)
- Create: `Assets/Scripts/Gameplay/Player/ZoltraakCaster.cs`
- Create: `Assets/Scripts/UI/StaffHud.cs`

**Interfaces:**
- Consumes: Task 1 (`ZoltraakCharge`, `StaffDefinition.Zoltraak*`).
- Produces:
  - `GameInput.AimPressed`, `GameInput.FireReleased`.
  - `Shooting.UsesStaff : bool`, `Shooting.MuzzlePoint : Vector3`, `Shooting.ShowBeam(Vector3 origin, Vector3 end, Color color, float width, float seconds)`, `Shooting.RefreshMana()`, `Shooting.AimGroundPoint(float range) : Vector3`.
  - `PlayerAbilities.PulseActive : bool` y `PlayerAbilities.BlocksFire : bool` (provisionales en `false`; se completan en las tareas 5 y 6).
  - `WeaponState.ZoltraakDamage(float charge) : int`.
  - `StaffHud.SetCharge(float fraction)` (negativo = ocultar) y `StaffHud.SetHint(string text)` (null o vacío = ocultar).

- [ ] **Step 1: Entradas** — en `GameInput.cs` agregar junto a `AimHeld`/`FirePressed`:

```csharp
    public bool FireReleased => fire.WasReleasedThisFrame();
    public bool AimPressed => aim.WasPressedThisFrame();
```

- [ ] **Step 2: Cámara sin zoom con el bastón** — en `CameraFollow.Update()` reemplazar la línea del FOV:

```csharp
        // Con el bastón el clic derecho es el disparo básico: no hay zoom.
        bool zoomAllowed = Shooting.Instance == null || !Shooting.Instance.UsesStaff;
        float targetFov = input.AimHeld && zoomAllowed ? zoomFOV : normalFOV;
```

- [ ] **Step 3: `Shooting`** — (a) agregar propiedades y métodos públicos (cerca de `Mana`/`AimRay`):

```csharp
    /// <summary>Verdadero si el arma activa es un bastón (Frieren): el clic izquierdo carga el Zoltraak y el derecho es el disparo básico.</summary>
    public bool UsesStaff => states.Length > 0 && CurrentWeapon.Staff != null;

    /// <summary>La punta del bastón (o el pecho del jugador si no hay).</summary>
    public Vector3 MuzzlePoint => MuzzlePosition;

    /// <summary>Dibuja el trazo grueso del rayo de maná (habilidades).</summary>
    public void ShowBeam(Vector3 origin, Vector3 end, Color color, float width, float seconds) =>
        beamVfx.Show(origin, end, color, width, seconds);

    /// <summary>Avisa al HUD del maná actual (las habilidades que lo gastan).</summary>
    public void RefreshMana() => PublishMana(true);

    /// <summary>
    /// Punto del suelo al que apunta la mira: el primer golpe del rayo (o el final del alcance) bajado hasta el piso.
    /// Si no hay piso debajo, se queda a la altura de los pies del jugador.
    /// </summary>
    public Vector3 AimGroundPoint(float range)
    {
        Vector3 point = TryGetHit(range, out RaycastHit hit) ? hit.point : AimRay().GetPoint(range);

        if (Physics.Raycast(point + Vector3.up * 50f, Vector3.down, out RaycastHit ground, 120f, ~0, QueryTriggerInteraction.Ignore)
            && !ground.transform.IsChildOf(transform))
            return ground.point;

        return new Vector3(point.x, transform.position.y + PlayerBody.FeetLocalY, point.z);
    }
```

(b) agregar el campo `private ZoltraakCaster zoltraak;` y reemplazar `UpdateStaff` por:

```csharp
    // Bastón (Frieren): clic izquierdo = Zoltraak cargado, clic derecho = disparo básico gratis, Q/E/F = habilidades con rango.
    private void UpdateStaff(WeaponState weapon, GameInput input)
    {
        if (zoltraak == null)
        {
            zoltraak = GetComponent<ZoltraakCaster>();
            if (zoltraak == null) zoltraak = gameObject.AddComponent<ZoltraakCaster>();
        }

        zoltraak.Tick(weapon, input);

        // Colocando el campo de flores no se dispara; el clic que confirma o cancela tampoco cuenta como disparo.
        if (abilities.BlocksFire || zoltraak.IsCharging) return;

        bool triggerPressed = weapon.IsAutomatic ? input.AimHeld : input.AimPressed;

        if (triggerPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + weapon.FireRate;
            Shoot();
        }
    }
```

(c) en `Build(...)`, junto a los demás reinicios, cortar la carga: `if (zoltraak != null) zoltraak.Cancel();`. (La Q se quita de aquí en la Tarea 4.)

- [ ] **Step 4: `PlayerAbilities` provisionales** — agregar (cerca de `IsMist`):

```csharp
    /// <summary>Verdadero mientras dura el pulso de maná de Frieren (el Zoltraak sale sin carga y gratis). Se completa en la Tarea 6.</summary>
    public bool PulseActive => false;

    /// <summary>Verdadero mientras se coloca el campo de flores y en el fotograma en que se confirma o cancela (el clic no debe disparar). Se completa en la Tarea 5.</summary>
    public bool BlocksFire => false;
```

- [ ] **Step 5: `ExplosionFlash` con color** — en `AbilityVfx.cs` cambiar la firma y la línea del color:

```csharp
    public static void ExplosionFlash(Vector3 position, float radius, Color? tint = null)
    ...
        flash.color = tint ?? new Color(0.9f, 0.05f, 0.05f, 0.45f);
```

- [ ] **Step 6: `WeaponState.ZoltraakDamage`** — junto a `AbilityDamage`:

```csharp
    /// <summary>Daño de un Zoltraak con esa carga (0 a 1): el del bastón con el Poder de la tienda, por el bono de daño del árbol.</summary>
    public int ZoltraakDamage(float charge)
    {
        int baseDamage = Staff.ZoltraakDamageAt(GetLevel(UpgradeType.Damage), charge);
        float multiplier = SkillTreeManager.Instance != null ? SkillTreeManager.Instance.Bonuses.DamageMultiplier : 1f;
        return Mathf.Max(baseDamage, Mathf.RoundToInt(baseDamage * multiplier));
    }
```

- [ ] **Step 7: `StaffHud`** — crear `Assets/Scripts/UI/StaffHud.cs` (barra de carga bajo la mira y una línea de pista; se arma sola en el canvas, sin tocar la escena):

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD propio del bastón de Frieren: una barra fina bajo la mira que muestra la carga del Zoltraak y una línea de pista
/// (por ejemplo al colocar el campo de flores). Se crea sola la primera vez que se usa y se vuelve a crear si el canvas cambió.
/// </summary>
public static class StaffHud
{
    private static readonly Color BackColor = new Color(0.04f, 0.05f, 0.1f, 0.7f);
    private static readonly Color FillColor = new Color(1f, 0.82f, 0.35f, 0.95f);
    private const float BarWidth = 220f;
    private const float BarHeight = 10f;

    private static GameObject root;
    private static RectTransform fill;
    private static GameObject barObject;
    private static TMP_Text hint;

    /// <summary>Muestra la carga (0 a 1) o la oculta con un valor negativo.</summary>
    public static void SetCharge(float fraction)
    {
        if (fraction < 0f)
        {
            if (barObject != null) barObject.SetActive(false);
            return;
        }

        if (!EnsureBuilt()) return;

        barObject.SetActive(true);
        fill.localScale = new Vector3(Mathf.Clamp01(fraction), 1f, 1f);
    }

    /// <summary>Muestra una pista bajo la mira, o la oculta si el texto es nulo o vacío.</summary>
    public static void SetHint(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (hint != null) hint.gameObject.SetActive(false);
            return;
        }

        if (!EnsureBuilt()) return;

        hint.text = text;
        hint.gameObject.SetActive(true);
    }

    private static bool EnsureBuilt()
    {
        if (root != null) return true;

        Transform canvas = UiKit.FindCanvas();
        if (canvas == null) return false;

        RectTransform container = UiKit.Rect("StaffHud", canvas);
        UiKit.Place(container, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(BarWidth, 60f));
        root = container.gameObject;

        Image back = UiKit.Box("ChargeBar", container, BackColor);
        UiKit.Place(back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(BarWidth, BarHeight));
        back.rectTransform.pivot = new Vector2(0.5f, 1f);
        back.raycastTarget = false;
        barObject = back.gameObject;

        Image bar = UiKit.Box("Fill", back.transform, FillColor);
        UiKit.Stretch(bar.rectTransform);
        bar.rectTransform.pivot = new Vector2(0f, 0.5f);
        bar.raycastTarget = false;
        fill = bar.rectTransform;
        barObject.SetActive(false);

        hint = UiKit.Label("Hint", container, "", 24f, TextAlignmentOptions.Center, Color.white);
        UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(900f, 34f));
        hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        hint.raycastTarget = false;
        hint.outlineWidth = 0.2f;
        hint.outlineColor = new Color32(0, 0, 0, 255);
        hint.gameObject.SetActive(false);
        return true;
    }
}
```

(Si `UiKit.Stretch` pone el pivote en el centro, el relleno se escala desde el centro: ajustar `anchorMin/anchorMax/offset` a mano para que crezca desde la izquierda. Se comprueba en el Step 9.)

- [ ] **Step 8: `ZoltraakCaster`** — crear `Assets/Scripts/Gameplay/Player/ZoltraakCaster.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Zoltraak de Frieren (clic izquierdo con el bastón): se mantiene el clic para cargar y al soltar cae una explosión en área
/// donde apunta la mira. Escala con la carga (daño y radio) y gasta maná al soltar. Con el pulso de maná activo no hay carga ni
/// gasto: sale uno por clic. Shooting la crea y la llama cada fotograma con el bastón en la mano.
/// </summary>
public class ZoltraakCaster : MonoBehaviour
{
    private static readonly Color BoltColor = new Color(1f, 0.85f, 0.4f, 0.95f);
    private static readonly Color BlastColor = new Color(0.95f, 0.75f, 0.2f, 0.5f);

    private readonly ZoltraakCharge charge = new ZoltraakCharge();
    private readonly Collider[] buffer = new Collider[96];
    private readonly List<EnemyAI> targets = new List<EnemyAI>();
    private Shooting shooting;
    private PlayerAbilities abilities;
    private float nextAllowed;

    public bool IsCharging => charge.IsCharging;

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        abilities = GetComponent<PlayerAbilities>();
    }

    private void OnDisable() => Cancel();

    /// <summary>Corta la carga sin disparar (cambio de personaje, fin de partida, menú, colocar el campo).</summary>
    public void Cancel()
    {
        if (!charge.IsCharging) return;

        charge.Cancel();
        StaffHud.SetCharge(-1f);
    }

    public void Tick(WeaponState weapon, GameInput input)
    {
        StaffDefinition staff = weapon.Staff;
        if (staff == null) return;

        if (GameState.InputBlocked || abilities.BlocksFire)
        {
            Cancel();
            return;
        }

        // Pulso de maná: sin carga y sin maná, un disparo por clic.
        if (abilities.PulseActive)
        {
            Cancel();
            if (input.FirePressed && Time.time >= nextAllowed) Fire(weapon, 1f, false);
            return;
        }

        if (!charge.IsCharging)
        {
            bool hasMana = shooting.Mana != null && shooting.Mana.Current >= staff.zoltraakManaCost;
            if (input.FirePressed && Time.time >= nextAllowed && hasMana) charge.Begin(staff.zoltraakChargeSeconds);
            return;
        }

        charge.Tick(Time.deltaTime);
        StaffHud.SetCharge(charge.Fraction);

        if (input.FireHeld) return;

        float fraction = charge.Release();
        StaffHud.SetCharge(-1f);
        Fire(weapon, fraction, true);
    }

    private void Fire(WeaponState weapon, float fraction, bool spendMana)
    {
        StaffDefinition staff = weapon.Staff;

        // El maná se cobra al soltar: si en ese momento ya no alcanza, no sale.
        if (spendMana && (shooting.Mana == null || !shooting.Mana.TrySpend(staff.zoltraakManaCost))) return;

        shooting.RefreshMana();
        nextAllowed = Time.time + (abilities.PulseActive ? staff.zoltraakInstantPause : staff.zoltraakPause);

        Vector3 point = shooting.AimGroundPoint(staff.zoltraakRange);
        float radius = staff.ZoltraakRadiusAt(fraction);
        int damage = weapon.ZoltraakDamage(fraction);

        if (shooting.Body != null) shooting.Body.PlayCast();
        shooting.ShowBolt(point, BoltColor, 0.12f, 0.12f);
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

        foreach (EnemyAI enemy in targets) enemy.TakeDamage(damage);
    }
}
```

- [ ] **Step 9: Verificar** — esperar la compilación (0 errores) y la suite (0 fallan). Luego en Play (esperar ~10 s tras compilar): elegir Frieren, empezar la partida y, **por reflexión**:
  1. Dar a Frieren los 3 puntos necesarios no hace falta; llamar `ZoltraakCaster.Fire` por reflexión con carga 1 sobre 3 enemigos activados a <4 m de un punto frente al jugador (`Physics.SyncTransforms()` tras activarlos). Expected: los 3 pierden `ZoltraakDamage(1)` de vida (45 con Poder 0), un enemigo a 6 m no recibe daño, el maná baja 15.
  2. Mismo disparo con carga 0: daño 18 y radio 2,5 (un enemigo a 3 m no recibe daño).
  3. Con maná en 10, `Tick` con `FirePressed` simulado no puede probarse sin input; comprobar `shooting.Mana.Current >= staff.zoltraakManaCost` como condición y que `Fire(…, spendMana: true)` con maná 10 no daña y no cambia el maná.
  4. Captura de pantalla con la barra a mitad (`StaffHud.SetCharge(0.5f)`): crece desde la izquierda; si crece desde el centro, corregir el pivote/anclas del relleno.
  5. Con Alucard seleccionado: `CameraFollow` mantiene el zoom (`AimHeld` real no se puede simular: comprobar que `Shooting.UsesStaff` es falso con Alucard y Guts y verdadero con Frieren).
  **No verificado (input real):** mantener y soltar el clic izquierdo, disparo con el clic derecho, que el clic derecho ya no haga zoom.

---

### Task 4: La Q pasa a ser una habilidad con rango (`ManaBeam`)

**Files:**
- Create: `Assets/Scripts/Gameplay/Player/ManaBeam.cs`
- Modify: `Assets/Scripts/Gameplay/Player/Shooting.cs` (quitar el camino viejo de la Q del bastón; HUD; refrescar al subir Maná)
- Modify: `Assets/Scripts/Gameplay/Player/PlayerAbilities.cs` (`case ManaBeam`; enfriamiento del bastón)

**Interfaces:**
- Consumes: Task 3 (`Shooting.MuzzlePoint`, `ShowBeam`, `RefreshMana`, `TryGetHit`, `AimRay`), Task 2 (assets).
- Produces: `ManaBeam.Cast(AbilityDefinition ability, int rank, Shooting shooting) : bool`.

- [ ] **Step 1: `ManaBeam`** — crear `Assets/Scripts/Gameplay/Player/ManaBeam.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Rayo de maná masivo de Frieren (tecla Q): línea recta que atraviesa y daña a todos los enemigos y se detiene en las paredes.
/// El daño, el maná, el alcance y el grosor salen del bastón (los mismos números de siempre, con el Poder de la tienda).
/// Clase estática, como FlameBurst: PlayerAbilities solo la llama.
/// </summary>
public static class ManaBeam
{
    private static readonly Color BeamColor = new Color(0.75f, 0.92f, 1f, 1f);

    /// <summary>Lanza el rayo. False (sin gastar nada ni empezar el enfriamiento) si no hay bastón o no alcanza el maná.</summary>
    public static bool Cast(AbilityDefinition ability, int rank, Shooting shooting)
    {
        if (shooting.WeaponCount == 0 || shooting.Mana == null) return false;

        WeaponState weapon = shooting.CurrentWeapon;
        StaffDefinition staff = weapon.Staff;
        if (staff == null) return false;
        if (!shooting.Mana.TrySpend(staff.abilityManaCost)) return false;

        Vector3 origin = shooting.MuzzlePoint;
        Vector3 end = PiercingBeam.Cast(new Ray(origin, Direction(shooting, origin, staff.abilityRange)),
            staff.abilityRange, staff.abilityBeamRadius, weapon.AbilityDamage, shooting.transform);

        shooting.ShowBeam(origin, end, BeamColor, staff.abilityBeamRadius * 1.5f, 0.25f);
        shooting.RefreshMana();
        return true;
    }

    // Sale horizontal, a la altura del bastón, hacia donde apunta la mira: recorre el campo a la altura de los enemigos
    // en vez de clavarse en el suelo, y atraviesa filas enteras.
    private static Vector3 Direction(Shooting shooting, Vector3 origin, float range)
    {
        Ray aim = shooting.AimRay();
        Vector3 target = shooting.TryGetHit(range, out RaycastHit hit) ? hit.point : aim.GetPoint(range);

        Vector3 direction = target - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = aim.direction;
            direction.y = 0f;
        }

        return direction.normalized;
    }
}
```

- [ ] **Step 2: `PlayerAbilities`** — (a) en `TryCast`, agregar el caso: `case AbilityKind.ManaBeam: cast = ManaBeam.Cast(ability, rank, shooting); break;`.

(b) En `RefreshBuild()`, reemplazar `float cooldown = ability != null ? EffectiveCooldown(ability, rank, bonuses) : 1f;` por:

```csharp
            float cooldown = ability != null ? CooldownFor(ability, rank, bonuses) : 1f;
```

y agregar:

```csharp
    // El rayo de maná usa el enfriamiento del bastón (baja con "Maná" de la tienda); las demás, el de su rango y el árbol.
    private float CooldownFor(AbilityDefinition ability, int rank, TreeBonuses bonuses)
    {
        if (ability.kind == AbilityKind.ManaBeam && shooting.WeaponCount > 0 && shooting.CurrentWeapon.Staff != null)
            return shooting.CurrentWeapon.AbilityCooldownTime;

        return EffectiveCooldown(ability, rank, bonuses);
    }
```

- [ ] **Step 3: `Shooting`** — (a) borrar el método `TryCastAbility` y `BeamDirection`, y el campo `abilityCooldown`/propiedad `AbilityCooldown` solo si ningún otro código los usa (`grep` primero; si algo los usa, dejarlos). (b) Reemplazar `BuildAbilityHud()` entero por una llamada directa a `abilities.HudInfo()` (borrar el método y usar `abilities.HudInfo()` en sus dos usos). (c) Al final de `Build(...)` (después de `ConfigureMana()`), agregar `abilities.RefreshBuild();` (en `Configure` los estados del arma todavía no estaban listos). (d) En `BuyUpgrade`, dentro del `if (bought && index == currentIndex)`, agregar `abilities.RefreshBuild();` (subir "Maná" baja el enfriamiento del rayo).

- [ ] **Step 4: Verificar** — compilar (0 errores) + suite (0 fallan). En Play con Frieren: con rangos [0,0,0] `HudInfo()` devuelve las 3 casillas vacías y `TryCast(0)` no sale; con rango [1,0,0] la casilla 0 muestra "Rayo de maná masivo"; lanzar por reflexión `TryCast(0)` con 3 enemigos en línea frente al bastón (<30 m). Expected: los 3 pierden `AbilityDamage` (40 con Poder 0), el maná baja 25, un segundo `TryCast(0)` inmediato no sale (enfriamiento 3 s), y con el maná en 10 no sale ni inicia enfriamiento. Subir "Maná" a nivel 1 (`BuyUpgrade`) cambia el enfriamiento a 2,7 s. Restaurar el guardado.

---

### Task 5: Campo de flores (colocación en dos pasos y campo activo)

**Files:**
- Create: `Assets/Scripts/Gameplay/Player/FlowerField.cs`
- Modify: `Assets/Scripts/Gameplay/Player/PlayerAbilities.cs` (`case FlowerField`, confirmar con la segunda E, `BlocksFire`)

**Interfaces:**
- Consumes: Task 1 (`FlowerFieldRules`, `HealOverTime`, campos de `AbilityDefinition`), Task 3 (`Shooting.AimGroundPoint`, `StaffHud.SetHint`, `AimPressed`).
- Produces: `FlowerField.Begin(AbilityDefinition ability, int rank, System.Action onConfirmed) : bool`, `FlowerField.Confirm()`, `FlowerField.Cancel()`, `FlowerField.ForceEnd()`, `FlowerField.IsPlacing`, `FlowerField.ClosedFrame`.

- [ ] **Step 1: `FlowerField`** — crear `Assets/Scripts/Gameplay/Player/FlowerField.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Campo de flores de Frieren (tecla E), en dos pasos. 1) Al pulsar E se abre la colocación: un círculo del tamaño del campo sigue la
/// mira sobre el suelo, sin mantener la tecla. 2) Clic izquierdo o E otra vez lo confirman; clic derecho lo cancela. El maná y el
/// enfriamiento se cobran al confirmar. El campo cura a Frieren (si está dentro) y ralentiza a los enemigos dentro. Sin daño.
/// </summary>
public class FlowerField : MonoBehaviour
{
    private static readonly Color RingColor = new Color(1f, 0.78f, 0.92f, 0.95f);
    private static readonly Color FloorColor = new Color(1f, 0.62f, 0.82f, 0.25f);
    private static readonly Color[] PetalColors =
    {
        new Color(1f, 0.55f, 0.75f), new Color(1f, 0.9f, 0.45f), new Color(0.75f, 0.6f, 1f), new Color(1f, 1f, 1f)
    };
    private const int RingSegments = 64;
    private const int FlowerCount = 48;
    private const float RingHeight = 0.08f;
    private const string PlacingHint = "Clic izquierdo o E: colocar el campo  ·  Clic derecho: cancelar";

    private readonly HealOverTime heal = new HealOverTime();
    private readonly Collider[] buffer = new Collider[128];

    private Shooting shooting;
    private PlayerHealth health;
    private AbilityDefinition ability;
    private int rank = 1;
    private Action onConfirmed;
    private LineRenderer ring;
    private GameObject fieldRoot;
    private Vector3 fieldCenter;
    private float fieldEnd;
    private float nextTick;
    private Material petalMaterial;

    public bool IsPlacing { get; private set; }

    /// <summary>Fotograma en que se cerró la colocación (confirmar o cancelar): ese clic no debe disparar ni cargar nada.</summary>
    public int ClosedFrame { get; private set; } = -1;

    private float Radius => ability != null ? ability.radius : 0f;

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        GameEvents.GameOver += OnGameOver;
        GameEvents.CharacterChanged += OnCharacterChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= OnGameOver;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        ForceEnd();
    }

    private void OnGameOver(string message, GameOverCause cause) => ForceEnd();
    private void OnCharacterChanged(CharacterDefinition character) => ForceEnd();

    /// <summary>Abre la colocación. False si ya estaba abierta o no alcanza el maná.</summary>
    public bool Begin(AbilityDefinition definition, int abilityRank, Action confirmed)
    {
        if (IsPlacing || shooting.Mana == null || shooting.Mana.Current < definition.manaCost) return false;

        ability = definition;
        rank = abilityRank;
        onConfirmed = confirmed;
        IsPlacing = true;

        if (ring == null) ring = CreateRing();
        ring.gameObject.SetActive(true);
        StaffHud.SetHint(PlacingHint);
        return true;
    }

    /// <summary>Cierra la colocación y crea el campo en el círculo. Cobra el maná y empieza el enfriamiento.</summary>
    public void Confirm()
    {
        if (!IsPlacing) return;

        Vector3 center = PlacementPoint();
        if (!shooting.Mana.TrySpend(ability.manaCost))
        {
            Cancel();
            return;
        }

        shooting.RefreshMana();
        CloseRing();
        StartField(center);
        if (shooting.Body != null) shooting.Body.PlayCast();
        onConfirmed?.Invoke();
    }

    /// <summary>Cierra la colocación sin gastar nada ni empezar el enfriamiento.</summary>
    public void Cancel()
    {
        if (!IsPlacing) return;

        CloseRing();
    }

    /// <summary>Corta todo: la colocación y el campo activo (cambio de personaje, fin de partida).</summary>
    public void ForceEnd()
    {
        IsPlacing = false;
        StaffHud.SetHint(null);
        if (ring != null) ring.gameObject.SetActive(false);
        EndField();
    }

    private void CloseRing()
    {
        IsPlacing = false;
        ClosedFrame = Time.frameCount;
        StaffHud.SetHint(null);
        if (ring != null) ring.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsPlacing)
        {
            if (GameState.InputBlocked || health == null || health.IsDead)
            {
                Cancel();
            }
            else
            {
                DrawRing(PlacementPoint());

                GameInput input = GameInput.Instance;
                if (input.FirePressed) Confirm();
                else if (input.AimPressed) Cancel();
            }
        }

        if (fieldRoot == null) return;

        if (Time.time >= fieldEnd)
        {
            EndField();
            return;
        }

        if (Time.time >= nextTick) TickField();
    }

    // Donde mira la mira, en el suelo y a no más de 'fieldPlaceRange' de Frieren.
    private Vector3 PlacementPoint()
    {
        Vector3 point = shooting.AimGroundPoint(ability.fieldPlaceRange + 20f);
        return FlowerFieldRules.ClampPoint(transform.position, point, ability.fieldPlaceRange);
    }

    private void StartField(Vector3 center)
    {
        EndField();

        fieldCenter = center;
        fieldEnd = Time.time + ability.DurationAt(rank);
        nextTick = Time.time;
        heal.Reset();
        fieldRoot = BuildFlowers(center, Radius);
    }

    private void EndField()
    {
        if (fieldRoot != null) Destroy(fieldRoot);
        fieldRoot = null;
    }

    private void TickField()
    {
        float step = ability.fieldTickSeconds;
        nextTick = Time.time + step;

        // Cura a Frieren si está dentro del círculo.
        if (health != null && !health.IsDead && FlowerFieldRules.Contains(fieldCenter, Radius, transform.position))
        {
            int amount = heal.Add(health.MaxHealth, ability.fieldHealFractionPerSecond, step);
            if (amount > 0) health.Heal(amount);
        }

        // Ralentiza a cada enemigo dentro. La ralentización dura un poco más que el tick: al salir se recuperan casi enseguida.
        int count = Physics.OverlapSphereNonAlloc(fieldCenter, Radius + 3f, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead) continue;
            if (!FlowerFieldRules.Contains(fieldCenter, Radius, enemy.transform.position, enemy.BodyRadius)) continue;

            enemy.ApplySlow(ability.fieldSlowFraction, step * 2f);
        }
    }

    // --- Aspecto provisional (sin arte): anillo de línea, disco rosado y flores de colores ---

    private LineRenderer CreateRing()
    {
        var go = new GameObject("FlowerFieldRing");
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = RingSegments;
        line.startWidth = line.endWidth = 0.14f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = new Material(Shader.Find("Sprites/Default")) { color = RingColor };
        line.startColor = line.endColor = RingColor;
        go.SetActive(false);
        return line;
    }

    private void DrawRing(Vector3 center)
    {
        float radius = Radius;
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, RingHeight, Mathf.Sin(angle) * radius));
        }
    }

    private GameObject BuildFlowers(Vector3 center, float radius)
    {
        var root = new GameObject("FlowerField");
        root.transform.position = center;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Ground";
        Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(root.transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        disc.GetComponent<Renderer>().sharedMaterial = NewMaterial(FloorColor);
        disc.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        for (int i = 0; i < FlowerCount; i++)
        {
            // Reparto uniforme en el disco (raíz cuadrada para que no se junten en el centro).
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float distance = Mathf.Sqrt(UnityEngine.Random.value) * (radius - 0.3f);

            GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flower.name = "Flower";
            Destroy(flower.GetComponent<Collider>());
            flower.transform.SetParent(root.transform, false);
            flower.transform.localPosition = new Vector3(Mathf.Cos(angle) * distance, 0.14f, Mathf.Sin(angle) * distance);
            flower.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.22f, 0.4f);

            Renderer r = flower.GetComponent<Renderer>();
            r.sharedMaterial = NewMaterial(PetalColors[i % PetalColors.Length]);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return root;
    }

    private static Material NewMaterial(Color color) => new Material(Shader.Find("Sprites/Default")) { color = color };
}
```

(`petalMaterial` no se usa: borrar el campo si queda sin uso; no dejar warnings.)

- [ ] **Step 2: `PlayerAbilities`** — (a) agregar el campo `private FlowerField field;` y el creador:

```csharp
    private FlowerField EnsureField()
    {
        if (field == null)
        {
            field = GetComponent<FlowerField>();
            if (field == null) field = gameObject.AddComponent<FlowerField>();
        }
        return field;
    }
```

(b) En `TryCast`, justo después del bloque de la armadura (`berserk.Deactivate(); return;`) agregar: la segunda E confirma, aunque haya enfriamiento (aún no empezó):

```csharp
        // El campo de flores: con la colocación abierta, la misma tecla confirma.
        if (ability.kind == AbilityKind.FlowerField && field != null && field.IsPlacing)
        {
            field.Confirm();
            return;
        }
```

(c) En el `switch`: `case AbilityKind.FlowerField: cast = EnsureField().Begin(ability, rank, () => StartCooldown(slot)); break;` y cambiar el retorno anticipado: `if (ability.kind == AbilityKind.Berserk || ability.kind == AbilityKind.FlowerField) return;` (el comentario explica: "el enfriamiento empieza al confirmar el campo / al apagar la armadura").

(d) Reemplazar `BlocksFire` por: `public bool BlocksFire => field != null && (field.IsPlacing || field.ClosedFrame == Time.frameCount);` y en `Configure(...)` agregar `if (field != null) field.ForceEnd();`.

(e) Al abrir la colocación, cancelar una carga de Zoltraak en curso: lo hace `ZoltraakCaster.Tick` (por `BlocksFire`).

- [ ] **Step 3: Verificar** — compilar (0 errores) + suite (0 fallan). En Play con Frieren (rangos [1,1,0] en el guardado de prueba; nivel ≥ 2 para tener 2 puntos), partida empezada, por reflexión:
  1. `TryCast(1)` abre la colocación (`IsPlacing` verdadero, `BlocksFire` verdadero, pista visible); maná intacto y sin enfriamiento todavía (`AbilityCharges.Available == 1`). Captura: círculo rosado en el suelo.
  2. Segundo `TryCast(1)`: confirma; maná -20; `IsPlacing` falso; hay `FlowerField` en la escena; enfriamiento de 18 s en marcha; `BlocksFire` verdadero solo ese fotograma.
  3. Cancelar (`Cancel()`) con maná 100: sin gasto ni enfriamiento.
  4. Con maná 10: `TryCast(1)` no abre la colocación.
  5. Campo activo, con Frieren a 1 punto de vida dentro del círculo: tras `Step`s hasta pasar 1 s de juego, cura 3 (3% de 100) ± 1; fuera del círculo no cura.
  6. 3 enemigos activos: uno dentro y uno fuera del radio; el de dentro queda con velocidad ×0,6 (`agent.speed`), el de fuera sin cambio; ~0,5 s tras sacar al de dentro, vuelve a su velocidad.
  7. A los 6 s el campo desaparece (objetos destruidos). Cambiar de personaje con el campo activo y con la colocación abierta: todo se corta (sin círculo ni flores).
  **No verificado (input real):** E una vez/dos veces, clic izquierdo para confirmar, clic derecho para cancelar y que ese clic no dispare.

---

### Task 6: Pulso de maná (definitiva)

**Files:**
- Create: `Assets/Scripts/Gameplay/Player/ManaPulseEffect.cs`
- Modify: `Assets/Scripts/Gameplay/Player/PlayerAbilities.cs` (`case ManaPulse`, `PulseActive`, aceleración de Q y E)

**Interfaces:**
- Consumes: Task 1 (`AbilityCharges.SpeedUp`, campos `pulse*`), Task 3 (`PulseActive` provisional).
- Produces: `ManaPulseEffect.Activate(AbilityDefinition ability, int rank, Shooting shooting) : bool`, `ManaPulseEffect.IsActive`, `ManaPulseEffect.ForceOff()`.

- [ ] **Step 1: `ManaPulseEffect`** — crear `Assets/Scripts/Gameplay/Player/ManaPulseEffect.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pulso de maná de Frieren (definitiva, tecla F): un pulso dorado aturde a los enemigos de un radio grande y, mientras dura,
/// los ralentiza, vuelve instantáneo (sin carga ni maná) el Zoltraak y acelera las otras dos habilidades. No es un interruptor:
/// dura su tiempo. PlayerAbilities la crea, la activa y le pide la aceleración de las otras casillas.
/// </summary>
public class ManaPulseEffect : MonoBehaviour
{
    private const float TickSeconds = 0.25f;
    private static readonly Color PulseColor = new Color(1f, 0.82f, 0.35f, 0.4f);

    private readonly TimedEffect effect = new TimedEffect();
    private readonly Collider[] buffer = new Collider[192];
    private Shooting shooting;
    private AbilityDefinition ability;
    private float nextTick;
    private ParticleSystem aura;

    public bool IsActive => effect.IsActive(Time.time);

    /// <summary>Multiplicador de velocidad de recarga de las otras habilidades (2 = el doble). 1 si el pulso no está activo.</summary>
    public float CooldownBoost => IsActive && ability != null ? ability.pulseCooldownBoost : 1f;

    private void Awake() => shooting = GetComponent<Shooting>();

    private void OnEnable()
    {
        GameEvents.GameOver += OnGameOver;
        GameEvents.CharacterChanged += OnCharacterChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameOver -= OnGameOver;
        GameEvents.CharacterChanged -= OnCharacterChanged;
        ForceOff();
    }

    private void OnGameOver(string message, GameOverCause cause) => ForceOff();
    private void OnCharacterChanged(CharacterDefinition character) => ForceOff();

    /// <summary>Activa el pulso. False (sin gastar nada) si no alcanza el maná.</summary>
    public bool Activate(AbilityDefinition definition, int rank, Shooting owner)
    {
        if (owner.Mana == null || !owner.Mana.TrySpend(definition.manaCost)) return false;

        ability = definition;
        shooting = owner;
        owner.RefreshMana();

        effect.Start(Time.time, definition.DurationAt(rank));
        nextTick = Time.time;

        // Terror: aturde (una sola vez) a todos los enemigos del radio. Los jefes son inmunes (EnemyAI.ApplyStun).
        foreach (EnemyAI enemy in EnemiesInRadius())
            enemy.ApplyStun(definition.pulseStunSeconds);

        AbilityVfx.ExplosionFlash(transform.position, definition.radius, PulseColor);
        if (owner.Body != null) owner.Body.PlayPowerUp();
        return true;
    }

    public void ForceOff()
    {
        effect.Cancel();
        ability = null;
        if (aura != null) Destroy(aura.gameObject);
        aura = null;
    }

    private void Update()
    {
        if (ability == null) return;

        if (effect.TryFinish(Time.time))
        {
            ForceOff();
            return;
        }

        if (Time.time < nextTick) return;
        nextTick = Time.time + TickSeconds;

        // Mientras dura, los enemigos del radio van más lentos (la ralentización caduca sola poco después de salir).
        foreach (EnemyAI enemy in EnemiesInRadius())
            enemy.ApplySlow(ability.pulseSlowFraction, TickSeconds * 2f);
    }

    private readonly List<EnemyAI> found = new List<EnemyAI>();

    private List<EnemyAI> EnemiesInRadius()
    {
        found.Clear();
        int count = Physics.OverlapSphereNonAlloc(transform.position, ability.radius, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            EnemyAI enemy = buffer[i].GetComponentInParent<EnemyAI>();
            if (enemy != null && !enemy.IsDead && !found.Contains(enemy)) found.Add(enemy);
        }
        return found;
    }
}
```

(Comprobar que `PlayerBody` tenga `PlayPowerUp()`; si no existe con ese nombre, usar el de la animación que usa la definitiva de Alucard o `PlayCast()`. El `aura` queda para un efecto visual simple: si no se agrega ninguno en esta tarea, borrar el campo y su limpieza.)

- [ ] **Step 2: `PlayerAbilities`** — (a) campo `private ManaPulseEffect pulse;` y creador `EnsurePulse()` (mismo patrón que `EnsureDash`). (b) En el `switch` de `TryCast`: `case AbilityKind.ManaPulse: cast = EnsurePulse().Activate(ability, rank, shooting); break;`. (c) Reemplazar `PulseActive` por `public bool PulseActive => pulse != null && pulse.IsActive;`. (d) En `Configure(...)` agregar `if (pulse != null) pulse.ForceOff();`. (e) En `Update()` (antes del `return` por `InputBlocked`), mientras el pulso esté activo, acelerar las casillas con habilidades que **no** sean el pulso:

```csharp
        // Pulso de maná: las otras habilidades se recargan más rápido (el pulso no se acelera a sí mismo).
        if (pulse != null && pulse.IsActive)
        {
            float extra = Time.deltaTime * (pulse.CooldownBoost - 1f);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || slots[i].kind == AbilityKind.ManaPulse) continue;
                charges[i].SpeedUp(extra);
            }
        }
```

El HUD de enfriamiento de cada casilla se actualiza con `RaiseAbilityUsed(slot, readyAt)` solo al lanzar: para que el reloj del HUD no se quede atrasado mientras se acelera, republicar en `PublishChargeChanges`-estilo cada 0,25 s mientras el pulso está activo, para cada casilla con recarga pendiente: `GameEvents.RaiseAbilityUsed(i, now + charges[i].RechargeRemaining(now))`. **Comprobar en Play** que `AbilitySlotUI` no parpadea ni reinicia animaciones con esa republicación; si lo hace, omitirla y anotar en el registro que el reloj visual no refleja la aceleración (la lógica sí).

- [ ] **Step 3: Verificar** — compilar (0 errores) + suite (0 fallan). En Play con Frieren (rangos [1,1,1] con nivel 6 en el guardado de prueba), por reflexión:
  1. Maná 100, 3 enemigos activos a 5, 10 y 20 m: `TryCast(2)`. Expected: maná -50; los de 5 y 10 m con `IsStunned` verdadero ~1,5 s (a los 1,6 s ya no); el de 20 m sin aturdir; los de dentro con velocidad ×0,6 durante los 9 s; `PulseActive` verdadero 9 s y luego falso.
  2. Con maná 30: `TryCast(2)` no se activa y no inicia enfriamiento.
  3. Enfriamiento de la F: 60 s desde que se activa (`charges[2].RechargeRemaining`).
  4. Con la Q usada (3 s de enfriamiento) y el pulso activo, la recarga dura ~1,5 s en vez de 3 s (avanzar con `Step`).
  5. `ZoltraakCaster.Fire(…, spendMana: false)` mientras `PulseActive` no gasta maná y su pausa es 0,35 s.
  6. Con un jefe cerca: no se aturde (inmune) pero sí se ralentiza.
  7. Cambiar de personaje con el pulso activo lo apaga (`PulseActive` falso, enemigos recuperan velocidad al expirar).
  **No verificado (input real):** F con la tecla y clics seguidos del Zoltraak instantáneo.

---

### Task 7: Levitar (solo visual)

**Files:**
- Create: `Assets/Scripts/Gameplay/Player/PlayerHover.cs`
- Modify: `Assets/Scripts/Gameplay/Player/Shooting.cs` (crear y configurar `PlayerHover`)
- Modify: `Assets/Scripts/Gameplay/Player/PlayerBody.cs` (subir el cuerpo)
- Modify: `Assets/Scripts/Gameplay/Player/CameraFollow.cs` (subir la cámara)

**Interfaces:**
- Consumes: Task 1 (`HoverMath`, `CharacterDefinition.hover*`), Task 2 (Maga con 0,45).
- Produces: `PlayerHover.Configure(CharacterDefinition)`, `PlayerHover.Offset : float` (metros, ya con el balanceo).

- [ ] **Step 1: `PlayerHover`** — crear `Assets/Scripts/Gameplay/Player/PlayerHover.cs`:

```csharp
using UnityEngine;

/// <summary>
/// Levitar de Frieren: guarda la altura (con balanceo suave) que sube el cuerpo y la cámara. Solo visual: no toca el collider
/// ni el movimiento. Shooting la crea y la configura con el personaje; PlayerBody y CameraFollow leen 'Offset'.
/// Usa tiempo sin escala para que el balanceo siga igual en la cámara lenta.
/// </summary>
public class PlayerHover : MonoBehaviour
{
    private float height;
    private float bob;
    private float period = 2.5f;

    /// <summary>Metros que sube el cuerpo y la cámara en este instante (0 si el personaje no levita).</summary>
    public float Offset => HoverMath.Offset(Time.unscaledTime, height, bob, period);

    public void Configure(CharacterDefinition character)
    {
        height = character != null ? character.hoverHeight : 0f;
        bob = character != null ? character.hoverBob : 0f;
        period = character != null ? character.hoverPeriod : 2.5f;
    }
}
```

- [ ] **Step 2: `Shooting`** — en `Awake()` antes de `Build(character)`: `hover = GetComponent<PlayerHover>(); if (hover == null) hover = gameObject.AddComponent<PlayerHover>();` (campo `private PlayerHover hover;`), y en `Build(...)`, después de `character = def;`: `hover.Configure(def);`.

- [ ] **Step 3: `PlayerBody`** — agregar el campo `private PlayerHover hover;` y en `LateUpdate`, al empezar (junto al `cam == null`): `if (hover == null) hover = GetComponentInParent<PlayerHover>();`. Reemplazar la línea `transform.localPosition = new Vector3(0f, FeetLocalY, 0f);` por:

```csharp
        // Los pies del modelo quedan sobre el suelo; los personajes que levitan (Frieren) suben con su balanceo.
        transform.localPosition = new Vector3(0f, FeetLocalY + (hover != null ? hover.Offset : 0f), 0f);
```

(La cámara de primera persona se alinea con la del jugador, así que el cuerpo sube y la cámara también: la sube `CameraFollow`.)

- [ ] **Step 4: `CameraFollow`** — agregar `private PlayerHover hover;`. En `LateUpdate()`, antes de calcular la posición: `if (hover == null && target != null) hover = target.GetComponent<PlayerHover>();` y `Vector3 lift = Vector3.up * (hover != null ? hover.Offset : 0f);`. Sumar `lift` a la posición de primera persona (`target.position + firstPersonOffset + lift`) y, en tercera persona, a `ResolveThirdPersonPosition` (sumar `lift` a `pivot` y a `desired`: pasar `lift` como parámetro).

- [ ] **Step 5: Verificar** — compilar (0 errores) + suite (0 fallan). En Play con Frieren: `PlayerHover.Offset` oscila entre 0,40 y 0,50 con periodo 2,5 s (tres lecturas con `Step` y tiempo avanzado); con Alucard y Guts es 0. La cámara en primera persona (`CameraFollow.IsFirstPerson` por reflexión) queda `Offset` más alta que con Alucard a la misma altura del jugador; en tercera persona también sube el pivote. Captura de las dos vistas. El collider no cambia (`CapsuleCollider`/`Collider.bounds` iguales antes y después). **No verificado:** cómo se ve el cuerpo levitando (Frieren aún no tiene `bodyPrefab`).

---

### Task 8: Notas, suite completa y revisión final

**Files:**
- Modify: `notas/REGISTRO_DE_CAMBIOS.md`, `notas/DECISIONES.md` (D44: kit de Frieren), `notas/NOTAS_DEL_JUEGO.md` (kit de Frieren), `notas/ATAJOS_DE_TECLADO.md` (clic izquierdo/derecho del bastón, E de dos pasos)
- Modify: memoria `notas-y-registro-de-sesion.md`

- [ ] **Step 1: Suite completa** — correr todos los tests de EditMode. Expected: 0 fallan (≈ 502 + los nuevos de las tareas 1 y 2).
- [ ] **Step 2: Prueba integrada en Play** — con un guardado de prueba (respaldo hecho): Frieren nivel 1 con el punto sin gastar → Q y E aparecen solo tras gastarlo y la F no se puede subir; ciclo completo E → Q → F con enemigos reales, sin excepciones nuevas en la consola. Cambiar entre Frieren, Alucard y Guts: ningún elemento de HUD nuevo en los otros dos y el zoom del clic derecho vuelve. Restaurar `save.json`.
- [ ] **Step 3: Revisión del conjunto** — revisar el diff completo contra el spec y la sección "Review Focus" (sin subagentes si el usuario no lo pidió; decir que es una autorevisión).
- [ ] **Step 4: Notas y memoria** — registrar en el historial lo hecho, las decisiones (números iniciales, "Q con datos en el bastón") y **lo que queda sin verificar**: input real (clic izquierdo mantenido, clic derecho, E dos veces, confirmar/cancelar), aspecto del cuerpo levitando (sin modelo), sonidos, balance, y el árbol de habilidades de Frieren (siguiente bloque).

---

### Cómo correr los tests

Runner por reflexión (el mismo de las sesiones anteriores) con `unity_execute_code`, puerto 7890; con `Unity` quieto (no en Play). Filtro por clase con `type.Name.Contains("FrierenKit")`; sin filtro corre la suite entera y debe devolver `pasan=N fallan=0`.
