# Guts: E "Embestida" y ulti "Armadura Berserker" — Plan de implementación

> **Para quien ejecute:** SUB-SKILL REQUERIDA: superpowers:executing-plans (el usuario eligió ejecución inline/Native). Los pasos usan casillas (`- [ ]`).
> **Regla del usuario: NO hacer commits.** Él hace los suyos.

**Goal:** La habilidad `E` de Guts (dash con giro y disparo al enemigo más cercano) y su ulti `F` (armadura Berserker como interruptor con drenaje de vida y enfriamiento fijo entre activaciones).

**Architecture:** Lógica pura con tests en `Game.Core` (`TargetPicker`, `DashRules`, `BerserkDrain`, fórmulas en `AbilityDefinition`). `AbilityKind.Dash` (4) y `Berserk` (5) al final del enum. `GutsDash` y `BerserkArmor` son componentes del jugador; `PlayerAbilities.TryCast` los llama. Ganchos pequeños con valores neutros: `PlayerMovement` (movimiento empujado), `PlayerBody` (giro y animaciones), `Health` (`DamageTakenMultiplier`, `Drain`), `WeaponState`/`Shooting` (bonos de la armadura).

**Tech Stack:** Unity 6000.6, C#, NUnit (EditMode), asmdefs `Game.Core` / `Game.Runtime`.

**Specs:** `docs/superpowers/specs/2026-10-04-guts-dash-design.md` y `2026-10-04-guts-berserk-design.md`.

## Global Constraints
- **E:** dash `6 m` en `0,35 s`, invulnerable durante el dash y `0,25 s` al levantarse, dispara una vez al enemigo vivo más cercano en `25 m` con daño = espada × `3` (+0,25 por rango extra), `7 s` de enfriamiento (−0,4 por rango extra), 5 rangos. Furia llena (y hay objetivo): x2 y gasta la barra; sin objetivo no gasta. La E no carga Furia. Los enemigos no bloquean el dash (se ignoran sus colliders durante el dash).
- **Ulti:** interruptor con `F` (casilla 3). Activar solo con el enfriamiento listo; **desactivar siempre se puede**. Enfriamiento fijo `25 s` (−3 s por rango extra) que **empieza al desactivarla** (a mano o sola). Rugido al activar: aturde `1,5 s` a los enemigos a `4 m` (los jefes no). Mientras está puesta: daño x`1,4` (+0,1 por rango extra), tiempo entre golpes x`0,5`, daño recibido x`0,5`, golpe de espada en `360°`, Furia x`2`. Drenaje `2%` de la vida máxima por segundo (−0,5 puntos por rango extra, mínimo 0), **nunca baja de 1 de vida** y la armadura se apaga sola al llegar a 1. Se apaga también al cambiar de personaje, en `GameOver` y si el jugador muere. 3 rangos con las compuertas de nivel existentes.
- `AbilityKind` **solo se agrega al final**: HeavyShot=0, Mist=1, Ultimate=2, FlameBurst=3, **Dash=4, Berserk=5**. No cambiar ningún `id` de asset ni el guardado.
- Los tests de EditMode solo ven `Game.Core`. Escribir archivos con la herramienta Write. Pasar `port: 7890` a las herramientas de Unity; tras editar scripts, esperar compilación (`AssetDatabase.Refresh(ForceUpdate)` desde `unity_execute_code` y luego `unity_get_compilation_errors` con `isCompiling: false`).
- **Pruebas en Play:** respaldar `save.json` **de ese momento** antes y restaurarlo después (Unity fuera de Play y quieto). **Todo bucle con `EditorApplication.Step` con tope de pasos y cortando si `Time.time` no avanza.** No dejar enemigos de vida enorme vivos. `Physics.SyncTransforms()` tras activar enemigos en la misma llamada. `GameObject.Find("XpBar"/"FuryBar")` encuentra el objeto raíz, no la barra del HUD. Al terminar, `Health.Invulnerable` debe quedar en false para pruebas normales (en las mías se pone true a propósito).
- Línea base antes de empezar: **413 tests pasan, 0 fallan**. Para correrlos: el ejecutor por reflexión de `docs/superpowers/plans/2026-10-04-guts-fury.md` (sección "Cómo correr los tests de EditMode"), con `filter` por nombre de clase (`""` = todos).

## Review Focus
1. **Los valores del enum no se mueven** (test) y Alucard sigue funcionando (3 habilidades, comprobar en Play).
2. **Cancelar el dash a mitad** (cambio de personaje o fin de partida): `Invulnerable` vuelve a false, el movimiento empujado termina, el giro termina y los colliders de enemigos se restauran.
3. **Drenaje que nunca mata:** con 1 o 2 de vida la armadura se apaga sola y la vida queda en ≥ 1; las almas siguen curando con la armadura puesta.
4. **Enfriamiento fijo de la ulti:** el mismo tras 2 s o 20 s de uso; no se puede reactivar antes; apagarla durante el enfriamiento no lo reinicia.
5. **Reducción de daño y drenaje:** el drenaje NO se reduce a la mitad por la armadura (usa `Health.Drain`, no `TakeDamage`).

## Mapa de archivos
| Archivo | Acción |
|---|---|
| `Core/Data/TargetPicker.cs`, `DashRules.cs`, `BerserkDrain.cs` | crear |
| `Core/Data/AbilityDefinition.cs` | modificar (Dash, Berserk, campos, fórmulas) |
| `Gameplay/Base/Health.cs` | modificar (`DamageTakenMultiplier`, `Drain`) |
| `Gameplay/Player/PlayerMovement.cs` | modificar (movimiento empujado) |
| `Gameplay/Player/PlayerBody.cs`, `Assets/Animation/Guts.controller` | modificar (giro, `PlayDash`, `PlayPowerUp`) |
| `Gameplay/Player/GutsDash.cs`, `BerserkArmor.cs` | crear |
| `Gameplay/Player/AbilityVfx.cs` | modificar (aura roja) |
| `Gameplay/Player/PlayerAbilities.cs` | modificar (`case`s, interruptor, `StartCooldown`) |
| `Gameplay/Weapons/WeaponState.cs`, `Gameplay/Player/Shooting.cs` | modificar (bonos de la armadura) |
| `Assets/Data/Abilities/Embestida.asset`, `Armadura.asset`; `Assets/Art/Icons/Embestida.png`, `Armadura.png`; `Guts.asset` | crear / modificar |
| `Assets/Tests/EditMode/TargetPickerTests.cs`, `DashRulesTests.cs`, `BerserkDrainTests.cs`, `AbilityDefinitionTests.cs` | crear / modificar |

---

## PARTE A — lógica pura y datos

### Task 1: `TargetPicker`, `DashRules` y `BerserkDrain` (puros)

**Files:** Create `Assets/Scripts/Core/Data/TargetPicker.cs`, `DashRules.cs`, `BerserkDrain.cs`; Test `TargetPickerTests.cs`, `DashRulesTests.cs`, `BerserkDrainTests.cs`.

**Interfaces — Produces:** `TargetPicker.NearestIndex(Vector3 origin, IList<Vector3> candidates, float maxRange) : int`; `DashRules.Direction(Vector3 moveInput, Vector3 aimForward) : Vector3`; `class BerserkDrain { int Advance(float dt, int maxHealth, float fractionPerSecond); void Reset(); static int Allowed(int current, int toLose); static bool ShouldStop(int current); }`.

- [ ] **Step 1: Tests que fallan.**

`TargetPickerTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TargetPickerTests
{
    [Test]
    public void PicksTheNearest()
    {
        var list = new List<Vector3> { new Vector3(10f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(6f, 0f, 0f) };
        Assert.AreEqual(1, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void OutOfRange_ReturnsMinusOne()
    {
        var list = new List<Vector3> { new Vector3(30f, 0f, 0f) };
        Assert.AreEqual(-1, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void TheExactRangeCounts()
    {
        var list = new List<Vector3> { new Vector3(25f, 0f, 0f) };
        Assert.AreEqual(0, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void EmptyOrNull_ReturnsMinusOne()
    {
        Assert.AreEqual(-1, TargetPicker.NearestIndex(Vector3.zero, new List<Vector3>(), 25f));
        Assert.AreEqual(-1, TargetPicker.NearestIndex(Vector3.zero, null, 25f));
    }

    [Test]
    public void ATie_PicksTheFirst()
    {
        var list = new List<Vector3> { new Vector3(4f, 0f, 0f), new Vector3(0f, 0f, 4f) };
        Assert.AreEqual(0, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }

    [Test]
    public void HeightIsIgnored()
    {
        var list = new List<Vector3> { new Vector3(5f, 0f, 0f), new Vector3(4f, 50f, 0f) };
        Assert.AreEqual(1, TargetPicker.NearestIndex(Vector3.zero, list, 25f));
    }
}
```
`DashRulesTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

public class DashRulesTests
{
    [Test]
    public void WithInput_UsesTheInput()
    {
        Vector3 d = DashRules.Direction(new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f));
        Assert.AreEqual(1f, d.x, 1e-4f);
        Assert.AreEqual(0f, d.z, 1e-4f);
    }

    [Test]
    public void WithoutInput_UsesTheAim()
    {
        Vector3 d = DashRules.Direction(Vector3.zero, new Vector3(0f, 0f, 3f));
        Assert.AreEqual(0f, d.x, 1e-4f);
        Assert.AreEqual(1f, d.z, 1e-4f);
    }

    [Test]
    public void ATinyInput_CountsAsNoInput()
    {
        Vector3 d = DashRules.Direction(new Vector3(0.05f, 0f, 0f), new Vector3(0f, 0f, 1f));
        Assert.AreEqual(1f, d.z, 1e-4f);
    }

    [Test]
    public void BothNull_GivesZero()
    {
        Assert.AreEqual(Vector3.zero, DashRules.Direction(Vector3.zero, Vector3.zero));
    }

    [Test]
    public void TheResultIsFlatAndNormalized()
    {
        Vector3 d = DashRules.Direction(Vector3.zero, new Vector3(0f, -0.7f, 0.7f));
        Assert.AreEqual(0f, d.y, 1e-4f);
        Assert.AreEqual(1f, d.magnitude, 1e-4f);
    }

    [Test]
    public void TheInputIsNormalizedToo()
    {
        Vector3 d = DashRules.Direction(new Vector3(3f, 0f, 4f), Vector3.forward);
        Assert.AreEqual(1f, d.magnitude, 1e-4f);
    }
}
```
`BerserkDrainTests.cs`:
```csharp
using NUnit.Framework;

public class BerserkDrainTests
{
    private static int Sum(BerserkDrain drain, float total, float step, int max, float fraction)
    {
        int lost = 0;
        for (float t = 0f; t < total - 1e-4f; t += step) lost += drain.Advance(step, max, fraction);
        return lost;
    }

    [Test]
    public void TwoPercentOf150_IsThreePerSecond()
    {
        Assert.AreEqual(3, Sum(new BerserkDrain(), 1f, 0.1f, 150, 0.02f));
        Assert.AreEqual(6, Sum(new BerserkDrain(), 2f, 0.1f, 150, 0.02f));
    }

    [Test]
    public void SmallSteps_NeverLoseOrDuplicatePoints()
    {
        // 100 de vida y 1%: 1 punto por segundo, en pasos de 0,016 s (como a ~60 fps)
        Assert.AreEqual(10, Sum(new BerserkDrain(), 10f, 0.016f, 100, 0.01f), 1);
    }

    [Test]
    public void OneBigStep_GivesTheWholePoints_AndKeepsTheRest()
    {
        var drain = new BerserkDrain();
        Assert.AreEqual(4, drain.Advance(1.5f, 200, 0.02f));   // 6 puntos en 1,5 s? 200 x 0,02 x 1,5 = 6
    }

    [Test]
    public void InvalidInputs_LoseNothing()
    {
        var drain = new BerserkDrain();
        Assert.AreEqual(0, drain.Advance(0f, 150, 0.02f));
        Assert.AreEqual(0, drain.Advance(-1f, 150, 0.02f));
        Assert.AreEqual(0, drain.Advance(1f, 0, 0.02f));
        Assert.AreEqual(0, drain.Advance(1f, 150, 0f));
    }

    [Test]
    public void Reset_ForgetsTheLeftover()
    {
        var drain = new BerserkDrain();
        drain.Advance(0.2f, 150, 0.02f);   // 0,6 de punto guardado
        drain.Reset();

        Assert.AreEqual(0, drain.Advance(0.2f, 150, 0.02f));
    }

    [TestCase(5, 9, 4)]
    [TestCase(150, 3, 3)]
    [TestCase(1, 5, 0)]
    [TestCase(0, 5, 0)]
    [TestCase(10, -2, 0)]
    public void Allowed_NeverLeavesLessThanOne(int current, int toLose, int expected)
    {
        Assert.AreEqual(expected, BerserkDrain.Allowed(current, toLose));
    }

    [TestCase(2, false)]
    [TestCase(1, true)]
    [TestCase(0, true)]
    public void ShouldStop_AtOneOrLess(int current, bool expected)
    {
        Assert.AreEqual(expected, BerserkDrain.ShouldStop(current));
    }
}
```
**Corrección del test `OneBigStep_...`**: 200 x 0,02 x 1,5 = **6** puntos, así que debe ser `Assert.AreEqual(6, drain.Advance(1.5f, 200, 0.02f));` (el `4` de arriba es un error de borrador: usar 6).

- [ ] **Step 2: Verificar que fallan** — forzar compilación: errores `CS0103/CS0246` de `TargetPicker`, `DashRules`, `BerserkDrain`.

- [ ] **Step 3: Implementar.**

`TargetPicker.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

/// <summary>Elige el objetivo más cercano. Solo geometría, para probarla sin escena.</summary>
public static class TargetPicker
{
    /// <summary>Índice del candidato más cercano (distancia horizontal) dentro del alcance, o -1. En un empate gana el primero.</summary>
    public static int NearestIndex(Vector3 origin, IList<Vector3> candidates, float maxRange)
    {
        if (candidates == null) return -1;

        float limit = maxRange * maxRange;
        float bestSq = float.MaxValue;
        int best = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            float dx = candidates[i].x - origin.x;
            float dz = candidates[i].z - origin.z;
            float sq = dx * dx + dz * dz;
            if (sq <= limit + 1e-6f && sq < bestSq)
            {
                best = i;
                bestSq = sq;
            }
        }
        return best;
    }
}
```
`DashRules.cs`:
```csharp
using UnityEngine;

/// <summary>Reglas del dash de Guts. Solo geometría, para probarla sin escena.</summary>
public static class DashRules
{
    private const float MinInputSqr = 0.01f;   // 0,1 al cuadrado

    /// <summary>Dirección plana y normalizada del dash: la entrada de movimiento si la hay; si no, hacia donde mira. Cero si no hay ninguna.</summary>
    public static Vector3 Direction(Vector3 moveInput, Vector3 aimForward)
    {
        var move = new Vector3(moveInput.x, 0f, moveInput.z);
        if (move.sqrMagnitude >= MinInputSqr) return move.normalized;

        var aim = new Vector3(aimForward.x, 0f, aimForward.z);
        return aim.sqrMagnitude > 1e-6f ? aim.normalized : Vector3.zero;
    }
}
```
`BerserkDrain.cs`:
```csharp
using UnityEngine;

/// <summary>
/// Drenaje de vida de la armadura Berserker: convierte "X% de la vida máxima por segundo" en puntos enteros, guardando el resto
/// para no perder ni duplicar puntos con pasos de tiempo pequeños. Nunca deja menos de 1 de vida.
/// </summary>
public class BerserkDrain
{
    private float carry;

    /// <summary>Puntos enteros que toca perder tras este paso de tiempo.</summary>
    public int Advance(float deltaTime, int maxHealth, float fractionPerSecond)
    {
        if (deltaTime <= 0f || maxHealth <= 0 || fractionPerSecond <= 0f) return 0;

        carry += maxHealth * fractionPerSecond * deltaTime;
        int whole = Mathf.FloorToInt(carry + 1e-4f);
        carry -= whole;
        return whole;
    }

    public void Reset() => carry = 0f;

    /// <summary>Limita lo que se pierde para que la vida nunca baje de 1.</summary>
    public static int Allowed(int current, int toLose) => Mathf.Clamp(toLose, 0, Mathf.Max(0, current - 1));

    /// <summary>Verdadero cuando la vida llegó a 1 o menos: la armadura se apaga sola.</summary>
    public static bool ShouldStop(int current) => current <= 1;
}
```

- [ ] **Step 4: Verificar que pasan** — `filter` = `TargetPicker`, `DashRules`, `BerserkDrain`: `fallan=0`.

### Task 2: `Dash` y `Berserk` en `AbilityDefinition` (+ tests)

**Files:** Modify `Assets/Scripts/Core/Data/AbilityDefinition.cs`; Test `AbilityDefinitionTests.cs`.

**Interfaces — Produces:** `AbilityKind.Dash` (4), `AbilityKind.Berserk` (5); campos `dashDistance`, `dashSeconds`, `riseSeconds`, `shotRange`, `drainFractionPerSecond`, `drainPerRank`, `roarRadius`, `roarStunSeconds`, `damageTakenMultiplier`, `cadenceMultiplier`, `furyGainMultiplier`, `swingArcDegrees`; `int SwordScaledDamageFor(int swordDamage, int rank, float empowerMultiplier = 1f)`; `float BerserkDamageAt(int rank)`; `float BerserkDrainAt(int rank)`; `DescribeRank` para ambos.

- [ ] **Step 1: Tests que fallan** — en `AbilityDefinitionTests.cs`: (a) cambiar en `EnumValues_OfAlucardsKinds_DoNotMove` agregando `Assert.AreEqual(4, (int)AbilityKind.Dash); Assert.AreEqual(5, (int)AbilityKind.Berserk);`; (b) en `LlamaradaAsset_Exists_...` reemplazar `Assert.AreEqual(1, guts.abilities.Length);` por `Assert.GreaterOrEqual(guts.abilities.Length, 1);`; (c) agregar al final de la clase:

```csharp
    // --- Embestida y Armadura de Guts ---

    private static AbilityDefinition Dash()
    {
        var d = ScriptableObject.CreateInstance<AbilityDefinition>();
        d.kind = AbilityKind.Dash;
        d.cooldown = 7f; d.cooldownPerRank = 0.4f;
        d.damageMultiplier = 3f; d.damagePerRank = 0.25f;
        d.dashDistance = 6f; d.dashSeconds = 0.35f; d.riseSeconds = 0.25f; d.shotRange = 25f;
        return d;
    }

    private static AbilityDefinition Berserk()
    {
        var b = ScriptableObject.CreateInstance<AbilityDefinition>();
        b.kind = AbilityKind.Berserk;
        b.cooldown = 25f; b.cooldownPerRank = 3f;
        b.damageMultiplier = 1.4f; b.damagePerRank = 0.1f;
        b.drainFractionPerSecond = 0.02f; b.drainPerRank = 0.005f;
        return b;
    }

    [TestCase(30, 1, 90)]
    [TestCase(80, 1, 240)]
    [TestCase(80, 5, 320)]    // x4 en el rango 5
    [TestCase(0, 1, 1)]
    public void DashShot_IsTheSwordDamageTimesTheRankMultiplier(int sword, int rank, int expected)
    {
        AbilityDefinition d = Dash();
        Assert.AreEqual(expected, d.SwordScaledDamageFor(sword, rank));
        Object.DestroyImmediate(d);
    }

    [Test]
    public void DashShot_WithFury_IsDoubled()
    {
        AbilityDefinition d = Dash();
        Assert.AreEqual(180, d.SwordScaledDamageFor(30, 1, 2f));
        Object.DestroyImmediate(d);
    }

    [TestCase(1, 7f)]
    [TestCase(5, 5.4f)]
    public void DashCooldown_ShortensWithRank(int rank, float expected)
    {
        AbilityDefinition d = Dash();
        Assert.AreEqual(expected, d.CooldownAt(rank), 1e-4f);
        Object.DestroyImmediate(d);
    }

    [Test]
    public void DashDescribeRank_MentionsTheDamageAndTheCooldown()
    {
        AbilityDefinition d = Dash();
        string text = d.DescribeRank(1);

        StringAssert.Contains("x3", text);
        StringAssert.Contains("7", text);
        Object.DestroyImmediate(d);
    }

    [TestCase(1, 1.4f)]
    [TestCase(2, 1.5f)]
    [TestCase(3, 1.6f)]
    public void BerserkDamage_GrowsWithRank(int rank, float expected)
    {
        AbilityDefinition b = Berserk();
        Assert.AreEqual(expected, b.BerserkDamageAt(rank), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [TestCase(1, 0.02f)]
    [TestCase(2, 0.015f)]
    [TestCase(3, 0.01f)]
    public void BerserkDrain_ShrinksWithRank(int rank, float expected)
    {
        AbilityDefinition b = Berserk();
        Assert.AreEqual(expected, b.BerserkDrainAt(rank), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void BerserkDrain_NeverNegative()
    {
        AbilityDefinition b = Berserk();
        b.drainPerRank = 0.05f;
        Assert.AreEqual(0f, b.BerserkDrainAt(3), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [TestCase(1, 25f)]
    [TestCase(2, 22f)]
    [TestCase(3, 19f)]
    public void BerserkCooldown_ShortensWithRank(int rank, float expected)
    {
        AbilityDefinition b = Berserk();
        Assert.AreEqual(expected, b.CooldownAt(rank), 1e-4f);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void BerserkDescribeRank_MentionsDamageDrainAndCooldown()
    {
        AbilityDefinition b = Berserk();
        string text = b.DescribeRank(1);

        StringAssert.Contains("1.4", text);
        StringAssert.Contains("2%", text);
        StringAssert.Contains("25", text);
        Object.DestroyImmediate(b);
    }

    [Test]
    public void DashAndBerserkDefaults_MatchTheDesign()
    {
        Assert.AreEqual(6f, ability.dashDistance, 1e-4f);
        Assert.AreEqual(0.35f, ability.dashSeconds, 1e-4f);
        Assert.AreEqual(0.25f, ability.riseSeconds, 1e-4f);
        Assert.AreEqual(25f, ability.shotRange, 1e-4f);
        Assert.AreEqual(4f, ability.roarRadius, 1e-4f);
        Assert.AreEqual(1.5f, ability.roarStunSeconds, 1e-4f);
        Assert.AreEqual(0.5f, ability.damageTakenMultiplier, 1e-4f);
        Assert.AreEqual(0.5f, ability.cadenceMultiplier, 1e-4f);
        Assert.AreEqual(2f, ability.furyGainMultiplier, 1e-4f);
        Assert.AreEqual(360f, ability.swingArcDegrees, 1e-4f);
    }

    [Test]
    public void EmbestidaAndArmaduraAssets_ExistWithTheDesignValues_AndGutsCarriesThem()
    {
        var dash = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Embestida.asset");
        var armor = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Armadura.asset");
        Assert.IsNotNull(dash, "Falta Embestida.asset");
        Assert.IsNotNull(armor, "Falta Armadura.asset");

        Assert.AreEqual("Embestida", dash.Id);
        Assert.AreEqual(AbilityKind.Dash, dash.kind);
        Assert.AreEqual(7f, dash.cooldown, 1e-4f);
        Assert.AreEqual(3f, dash.damageMultiplier, 1e-4f);
        Assert.AreEqual(6f, dash.dashDistance, 1e-4f);
        Assert.IsNotNull(dash.icon);

        Assert.AreEqual("Armadura", armor.Id);
        Assert.AreEqual(AbilityKind.Berserk, armor.kind);
        Assert.AreEqual(25f, armor.cooldown, 1e-4f);
        Assert.AreEqual(1.4f, armor.damageMultiplier, 1e-4f);
        Assert.AreEqual(0.02f, armor.drainFractionPerSecond, 1e-4f);
        Assert.AreEqual(360f, armor.swingArcDegrees, 1e-4f);
        Assert.IsNotNull(armor.icon);

        var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
        Assert.AreEqual(3, guts.abilities.Length);
        Assert.AreEqual("Llamarada", guts.abilities[0].Id);
        Assert.AreSame(dash, guts.abilities[1]);
        Assert.AreSame(armor, guts.abilities[2]);
    }
```

- [ ] **Step 2: Verificar que fallan** — compilación con errores (`Dash`, `dashDistance`, `SwordScaledDamageFor`...).

- [ ] **Step 3: Implementar** — en `AbilityDefinition.cs`:
  1. Enum: después de `FlameBurst` (agregar la coma):
```csharp
    FlameBurst,
    [Tooltip("Dash con giro que termina disparando al enemigo más cercano (Guts)")]
    Dash,
    [Tooltip("Armadura Berserker: interruptor con bonos fuertes que drena vida (Guts)")]
    Berserk
```
  2. Campos, después del bloque `[Header("Llamarada (Guts)")]`:
```csharp
    [Header("Embestida (Guts)")]
    [Tooltip("Metros que recorre el dash")]
    public float dashDistance = 6f;
    [Min(0.05f), Tooltip("Segundos que dura el dash")]
    public float dashSeconds = 0.35f;
    [Min(0f), Tooltip("Segundos que tarda en levantarse antes de disparar (sigue invulnerable)")]
    public float riseSeconds = 0.25f;
    [Tooltip("Alcance del disparo al enemigo más cercano, en metros")]
    public float shotRange = 25f;

    [Header("Armadura Berserker (Guts)")]
    [Range(0f, 1f), Tooltip("Fracción de la vida máxima que pierde por segundo mientras la lleva puesta (el daño usa 'damageMultiplier')")]
    public float drainFractionPerSecond = 0.02f;
    [Tooltip("Cuánto baja el drenaje por cada rango extra (0,005 = 0,5 puntos)")]
    public float drainPerRank = 0f;
    [Tooltip("Radio del rugido al activarla, en metros")]
    public float roarRadius = 4f;
    [Tooltip("Segundos que aturde el rugido (los jefes son inmunes)")]
    public float roarStunSeconds = 1.5f;
    [Range(0f, 1f), Tooltip("Multiplicador del daño que recibe mientras la lleva (0,5 = la mitad)")]
    public float damageTakenMultiplier = 0.5f;
    [Range(0.1f, 1f), Tooltip("Multiplicador del tiempo entre golpes de espada (0,5 = el doble de rápido)")]
    public float cadenceMultiplier = 0.5f;
    [Min(1f), Tooltip("Multiplicador de la Furia que carga con cada golpe")]
    public float furyGainMultiplier = 2f;
    [Range(0f, 360f), Tooltip("Apertura del golpe de espada mientras la lleva (360 = alrededor)")]
    public float swingArcDegrees = 360f;
```
  3. `DescribeRank`: antes de `default:`:
```csharp
            case AbilityKind.Dash:
                return "Daño x" + DamageMultiplierAt(rank).ToString("0.#") + " al más cercano · " + cd;
            case AbilityKind.Berserk:
                return "Daño x" + BerserkDamageAt(rank).ToString("0.0") + " · drena " + Mathf.RoundToInt(BerserkDrainAt(rank) * 1000f) / 10f + "% por s · " + cd;
```
    Atención al formato: para rango 1 debe contener `"2%"` → usar `(BerserkDrainAt(rank) * 100f).ToString("0.#") + "%"`. Escribirlo así:
```csharp
            case AbilityKind.Berserk:
                return "Daño x" + BerserkDamageAt(rank).ToString("0.0") + " · drena " + (BerserkDrainAt(rank) * 100f).ToString("0.#") + "% por s · " + cd;
```
  4. Fórmulas (junto a `FlameDamageFor`); y hacer que `FlameDamageFor` delegue:
```csharp
    /// <summary>Daño de una habilidad que escala con la espada: daño de la espada x multiplicador del rango (x más con Furia). Mínimo 1.</summary>
    public int SwordScaledDamageFor(int swordDamage, int rank, float empowerMultiplier = 1f) =>
        Mathf.Max(1, Mathf.RoundToInt(swordDamage * DamageMultiplierAt(rank) * empowerMultiplier));

    /// <summary>Multiplicador de daño de la armadura Berserker en un rango (1,4 / 1,5 / 1,6).</summary>
    public float BerserkDamageAt(int rank) => DamageMultiplierAt(rank);

    /// <summary>Fracción de vida máxima que drena por segundo en un rango (nunca negativa).</summary>
    public float BerserkDrainAt(int rank) => Mathf.Max(0f, drainFractionPerSecond - Steps(rank) * drainPerRank);
```
    y cambiar `FlameDamageFor` a `=> SwordScaledDamageFor(swordDamage, rank, empowerMultiplier);`.

- [ ] **Step 4: Verificar** — `filter = "AbilityDefinition"`: todo pasa **excepto** `EmbestidaAndArmaduraAssets_...` (falla por los assets que aún no existen: es su RED; pasa en la Tarea 6).

---

## PARTE B — ganchos del juego

### Task 3: Ganchos neutros (`Health`, `PlayerMovement`, `PlayerBody`)

**Files:** Modify `Gameplay/Base/Health.cs`, `Gameplay/Player/PlayerMovement.cs`, `Gameplay/Player/PlayerBody.cs`. Sin tests de EditMode (runtime); se prueban en Play (Tareas 5 y 6).

- [ ] **Step 1: `Health`** — junto a `Invulnerable`:
```csharp
    /// <summary>Multiplicador del daño que recibe (1 = normal; 0,5 = la mitad). Lo usa la armadura Berserker de Guts.</summary>
    public float DamageTakenMultiplier { get; set; } = 1f;
```
  En `TakeDamage`, después de la línea `if (IsDead || Invulnerable) return;` agregar:
```csharp
        if (!Mathf.Approximately(DamageTakenMultiplier, 1f)) amount = Mathf.Max(1, Mathf.RoundToInt(amount * DamageTakenMultiplier));
```
  Y debajo de `Heal`:
```csharp
    /// <summary>Quita vida sin matar (nunca baja de 1) y sin multiplicadores ni invulnerabilidad: el drenaje de la armadura de Guts.</summary>
    public void Drain(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Max(1, CurrentHealth - amount);
        OnHealthChanged();
    }
```

- [ ] **Step 2: `PlayerMovement`** — junto a `SpeedMultiplier`:
```csharp
    private Vector3 forcedVelocity;
    private float forcedUntil;

    /// <summary>Entrada de movimiento actual (plana, relativa a la cámara, hasta módulo 1). La usa el dash para saber hacia dónde ir.</summary>
    public Vector3 MoveInput => movement;

    /// <summary>Mientras es true la velocidad horizontal la manda el dash y no el teclado.</summary>
    public bool IsForced => Time.time < forcedUntil;

    /// <summary>Empuja al jugador a esta velocidad horizontal durante unos segundos (el dash de Guts).</summary>
    public void BeginForcedMove(Vector3 flatVelocity, float seconds)
    {
        forcedVelocity = flatVelocity;
        forcedUntil = Time.time + seconds;
        jumpRequested = false;
    }

    public void EndForcedMove() => forcedUntil = 0f;
```
  En `FixedUpdate`, reemplazar el bloque que calcula `currentSpeed`, `velocity` y salta (desde `float groundFactor = ...` hasta el cierre del `if (jumpRequested) {...}`) por:
```csharp
        float groundFactor = sprintHeld ? 1f : walkFactor;
        if (wasGrounded) airFactor = groundFactor; // al despegar se conserva el paso con el que se venía

        if (IsForced)
        {
            // Dash: la velocidad horizontal la manda quien empuja; no se salta ni se camina.
            Vector3 pushed = rb.linearVelocity;
            pushed.x = forcedVelocity.x;
            pushed.z = forcedVelocity.z;
            rb.linearVelocity = pushed;
            jumpRequested = false;
        }
        else
        {
            float currentSpeed = (isGrounded ? speed * groundFactor : speed * airFactor * jumpDistance) * SpeedMultiplier;
            Vector3 velocity = rb.linearVelocity;
            velocity.x = movement.x * currentSpeed;
            velocity.z = movement.z * currentSpeed;
            rb.linearVelocity = velocity;

            if (jumpRequested)
            {
                jumpRequested = false;
                if (isGrounded)
                {
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    isGrounded = false;
                }
            }
        }
```
  (la gravedad extra de abajo se queda igual).

- [ ] **Step 3: `PlayerBody`** — constantes junto a `CastParam`: `private static readonly int DashParam = Animator.StringToHash("Dash"); private static readonly int PowerUpParam = Animator.StringToHash("PowerUp");` y campos `private bool hasDash; private bool hasPowerUp; private float spinTimer = -1f; private float spinTotal;`. En la inicialización donde se calcula `hasCast`:
```csharp
        hasDash = System.Array.Exists(animator.parameters, p => p.nameHash == DashParam);
        hasPowerUp = System.Array.Exists(animator.parameters, p => p.nameHash == PowerUpParam);
```
  Métodos junto a `PlayCast`:
```csharp
    /// <summary>Pose agachada del dash de Guts (dura lo que el dash y levantarse). No hace nada si el controlador no tiene "Dash".</summary>
    public void PlayDash()
    {
        if (dead || !hasDash) return;
        animator.SetTrigger(DashParam);
    }

    /// <summary>Gesto de la transformación (la armadura Berserker). No hace nada si el controlador no tiene "PowerUp".</summary>
    public void PlayPowerUp()
    {
        if (dead || !hasPowerUp) return;
        animator.SetTrigger(PowerUpParam);
    }

    /// <summary>Da una vuelta completa sobre el eje vertical durante unos segundos (el giro del dash). En primera persona no gira.</summary>
    public void BeginSpin(float seconds)
    {
        spinTotal = Mathf.Max(0.05f, seconds);
        spinTimer = 0f;
    }

    public void EndSpin() => spinTimer = -1f;
```
  En `LateUpdate`, justo antes de `aimYaw = Mathf.MoveTowardsAngle(...)` agregar:
```csharp
            float spinAngle = 0f;
            if (spinTimer >= 0f)
            {
                spinTimer += Time.deltaTime;
                if (spinTimer >= spinTotal) spinTimer = -1f;
                else if (!first) spinAngle = 360f * spinTimer / spinTotal;
            }
```
  y cambiar la línea `transform.rotation = Quaternion.AngleAxis(aimYaw, Vector3.up) * ...` por `transform.rotation = Quaternion.AngleAxis(aimYaw + spinAngle, Vector3.up) * Quaternion.LookRotation(flat, Vector3.up);`.

- [ ] **Step 4: Verificar** — compila sin errores.

### Task 4: `GutsDash`, `BerserkArmor`, aura, `PlayerAbilities` y bonos

**Files:** Create `Gameplay/Player/GutsDash.cs`, `BerserkArmor.cs`; Modify `AbilityVfx.cs`, `PlayerAbilities.cs`, `Gameplay/Weapons/WeaponState.cs`, `Gameplay/Player/Shooting.cs`.

- [ ] **Step 1: `GutsDash`** — `Assets/Scripts/Gameplay/Player/GutsDash.cs`:

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Embestida de Guts (tecla E): un dash con giro (invulnerable, atraviesa enemigos) y, al levantarse, un disparo del arma de su
/// brazo al enemigo más cercano. Con la Furia llena el disparo sale potenciado (x2) y gasta la barra.
/// </summary>
public class GutsDash : MonoBehaviour
{
    private static readonly Color ShotColor = new Color(1f, 0.15f, 0.1f, 1f);
    private const float ShotHeight = 1.1f;
    private const float IgnoreMargin = 3f;

    private Shooting shooting;
    private PlayerMovement movement;
    private PlayerHealth health;
    private Collider bodyCollider;
    private BeamVfx beam;
    private Coroutine routine;
    private readonly List<Collider> ignored = new List<Collider>();
    private readonly List<EnemyAI> candidates = new List<EnemyAI>();
    private readonly List<Vector3> positions = new List<Vector3>();

    public bool IsDashing => routine != null;

    private void Awake()
    {
        shooting = GetComponent<Shooting>();
        movement = GetComponent<PlayerMovement>();
        health = GetComponent<PlayerHealth>();
        bodyCollider = GetComponent<Collider>();
        beam = BeamVfx.Create("DashShotVfx");
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
        Cancel();
    }

    private void OnDestroy()
    {
        if (beam != null) Destroy(beam.gameObject);
    }

    private void OnGameOver(string message, GameOverCause cause) => Cancel();
    private void OnCharacterChanged(CharacterDefinition character) => Cancel();

    /// <summary>Empieza el dash. False si ya hay uno en curso o el personaje no lleva espada (el daño sale de ella).</summary>
    public bool TryStart(AbilityDefinition ability, int rank)
    {
        if (routine != null || shooting.WeaponCount == 0 || shooting.CurrentWeapon.Sword == null) return false;

        routine = StartCoroutine(Run(ability, rank));
        return true;
    }

    /// <summary>Corta el dash y deja todo como estaba (invulnerabilidad, movimiento, giro y colisiones).</summary>
    public void Cancel()
    {
        if (routine == null) return;

        StopCoroutine(routine);
        routine = null;
        Finish();
    }

    private IEnumerator Run(AbilityDefinition ability, int rank)
    {
        Vector3 aim = Vector3.ProjectOnPlane(shooting.AimRay().direction, Vector3.up);
        Vector3 direction = DashRules.Direction(movement.MoveInput, aim);
        if (direction == Vector3.zero) direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        // Invulnerable, sin chocar con los enemigos, empujado hacia delante y girando.
        health.Invulnerable = true;
        IgnoreEnemiesNear(ability.dashDistance + IgnoreMargin);
        movement.BeginForcedMove(direction * (ability.dashDistance / ability.dashSeconds), ability.dashSeconds);
        if (shooting.Body != null)
        {
            shooting.Body.PlayDash();
            shooting.Body.BeginSpin(ability.dashSeconds);
        }

        yield return new WaitForSeconds(ability.dashSeconds);
        movement.EndForcedMove();

        // Levantándose: sigue invulnerable y quieto.
        yield return new WaitForSeconds(ability.riseSeconds);

        Shoot(ability, rank);
        routine = null;
        Finish();
    }

    private void Finish()
    {
        if (movement != null) movement.EndForcedMove();
        if (shooting != null && shooting.Body != null) shooting.Body.EndSpin();
        if (health != null) health.Invulnerable = false;
        RestoreEnemyCollisions();
    }

    private void IgnoreEnemiesNear(float radius)
    {
        foreach (EnemyAI enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead || (enemy.transform.position - transform.position).sqrMagnitude > radius * radius) continue;

            Collider body = enemy.GetComponent<Collider>();
            if (body == null || bodyCollider == null) continue;

            Physics.IgnoreCollision(bodyCollider, body, true);
            ignored.Add(body);
        }
    }

    private void RestoreEnemyCollisions()
    {
        foreach (Collider body in ignored)
        {
            if (body != null && bodyCollider != null && body.gameObject.activeInHierarchy)
                Physics.IgnoreCollision(bodyCollider, body, false);
        }
        ignored.Clear();
    }

    // Al levantarse: un disparo al enemigo vivo más cercano (sin importar hacia dónde mire).
    private void Shoot(AbilityDefinition ability, int rank)
    {
        WeaponState weapon = shooting.CurrentWeapon;
        SwordDefinition sword = weapon.Sword;
        if (sword == null) return;

        candidates.Clear();
        positions.Clear();
        foreach (EnemyAI enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;
            candidates.Add(enemy);
            positions.Add(enemy.transform.position);
        }

        Vector3 origin = transform.position;
        int index = TargetPicker.NearestIndex(origin, positions, ability.shotRange);
        if (index < 0) return;   // sin enemigos al alcance: no dispara y no gasta Furia

        EnemyAI target = candidates[index];
        bool empowered = shooting.Fury.TryConsume();
        float multiplier = empowered ? sword.furyDamageMultiplier : 1f;
        int damage = ability.SwordScaledDamageFor(weapon.Damage, rank, multiplier);

        Vector3 from = origin + Vector3.up * (ShotHeight - 1f);
        Vector3 to = target.transform.position + Vector3.up * 1f;
        beam.Show(from, to, ShotColor, 0.12f, 0.18f);
        AbilityVfx.ExplosionFlash(to, 0.8f);

        target.TakeDamage(damage);
        if (empowered) shooting.PublishFury();
    }
}
```

- [ ] **Step 2: `BerserkArmor`** — `Assets/Scripts/Gameplay/Player/BerserkArmor.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Armadura Berserker de Guts (ulti, tecla F): un interruptor. Puesta, da bonos fuertes (daño, golpes más rápidos, menos daño
/// recibido, golpe en 360°, más Furia) y le va drenando vida sin matarlo; se apaga a mano o sola al llegar a 1 de vida.
/// El enfriamiento lo maneja PlayerAbilities y empieza al apagarla. Los bonos se consultan por los miembros estáticos
/// (neutros cuando está apagada). Aspecto provisional: tinte rojo oscuro y humo rojo.
/// </summary>
public class BerserkArmor : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color ArmorTint = new Color(0.4f, 0.03f, 0.05f);

    public static BerserkArmor Instance { get; private set; }

    private static bool Active => Instance != null && Instance.IsActive;

    /// <summary>Multiplicador del daño de Guts (espada, Q y E). 1 si la armadura está apagada.</summary>
    public static float DamageMultiplier => Active ? Instance.damageMultiplier : 1f;

    /// <summary>Multiplicador del tiempo entre golpes de espada (0,5 = el doble de rápido). 1 si está apagada.</summary>
    public static float CadenceMultiplier => Active ? Instance.cadenceMultiplier : 1f;

    /// <summary>Multiplicador de la Furia que carga cada golpe. 1 si está apagada.</summary>
    public static float FuryGainMultiplier => Active ? Instance.furyGainMultiplier : 1f;

    /// <summary>Apertura del golpe de espada: la de la espada, o la de la armadura (360°) si está puesta.</summary>
    public static float SwingArc(float normalArc) => Active ? Mathf.Max(normalArc, Instance.swingArc) : normalArc;

    private readonly BerserkDrain drain = new BerserkDrain();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private PlayerHealth health;
    private Shooting shooting;
    private AbilityDefinition ability;
    private int rank = 1;
    private Action onDeactivated;
    private ParticleSystem aura;
    private MaterialPropertyBlock block;

    private float damageMultiplier = 1f;
    private float cadenceMultiplier = 1f;
    private float furyGainMultiplier = 1f;
    private float swingArc;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        Instance = this;
        health = GetComponent<PlayerHealth>();
        shooting = GetComponent<Shooting>();
        block = new MaterialPropertyBlock();
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
        ForceOff();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnGameOver(string message, GameOverCause cause) => ForceOff();
    private void OnCharacterChanged(CharacterDefinition character) => ForceOff();

    /// <summary>Se la pone: rugido, bonos y drenaje. "whenOff" se llama al apagarla a mano o sola (no al forzarla).</summary>
    public bool Activate(AbilityDefinition armor, int armorRank, Action whenOff)
    {
        if (IsActive || health == null) return false;

        ability = armor;
        rank = armorRank;
        onDeactivated = whenOff;
        damageMultiplier = armor.BerserkDamageAt(armorRank);
        cadenceMultiplier = armor.cadenceMultiplier;
        furyGainMultiplier = armor.furyGainMultiplier;
        swingArc = armor.swingArcDegrees;
        health.DamageTakenMultiplier = armor.damageTakenMultiplier;
        drain.Reset();
        IsActive = true;

        Roar(armor);
        if (shooting != null && shooting.Body != null) shooting.Body.PlayPowerUp();
        SetTint(true);
        StartAura();
        return true;
    }

    /// <summary>La apaga (a mano o porque la vida llegó a 1) y avisa para que empiece el enfriamiento fijo.</summary>
    public void Deactivate()
    {
        if (!IsActive) return;

        TurnOff();
        Action callback = onDeactivated;
        onDeactivated = null;
        callback?.Invoke();
    }

    /// <summary>La apaga sin enfriamiento (cambio de personaje, fin de partida o al desactivar el componente).</summary>
    public void ForceOff()
    {
        if (!IsActive) return;

        TurnOff();
        onDeactivated = null;
    }

    private void TurnOff()
    {
        IsActive = false;
        if (health != null) health.DamageTakenMultiplier = 1f;
        SetTint(false);
        if (aura != null) aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void Update()
    {
        if (!IsActive) return;

        if (GameState.IsGameOver || health == null || health.IsDead)
        {
            ForceOff();
            return;
        }

        int lose = BerserkDrain.Allowed(health.CurrentHealth, drain.Advance(Time.deltaTime, health.MaxHealth, ability.BerserkDrainAt(rank)));
        if (lose > 0) health.Drain(lose);

        if (BerserkDrain.ShouldStop(health.CurrentHealth)) Deactivate();
    }

    // Rugido al ponérsela: aturde a los enemigos cercanos (los jefes son inmunes por EnemyAI.ApplyStun).
    private void Roar(AbilityDefinition armor)
    {
        float radiusSqr = armor.roarRadius * armor.roarRadius;
        foreach (EnemyAI enemy in UnityEngine.Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead) continue;

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= radiusSqr) enemy.ApplyStun(armor.roarStunSeconds);
        }
    }

    private void StartAura()
    {
        if (aura == null) aura = AbilityVfx.CreateBerserkAura(transform);
        aura.Play();
    }

    private void SetTint(bool on)
    {
        renderers.Clear();
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend is ParticleSystemRenderer || rend is LineRenderer) continue;
            renderers.Add(rend);
        }

        foreach (Renderer rend in renderers)
        {
            if (!on)
            {
                rend.SetPropertyBlock(null);
                continue;
            }

            Material material = rend.sharedMaterial;
            Color original = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, Color.Lerp(original, ArmorTint, 0.7f));
            rend.SetPropertyBlock(block);
        }
    }
}
```

- [ ] **Step 3: Aura roja** — en `AbilityVfx.cs`, junto a `CreateMist`:
```csharp
    /// <summary>Humo rojo alrededor del jugador mientras lleva la armadura Berserker. Quien lo crea lo enciende y lo apaga.</summary>
    public static ParticleSystem CreateBerserkAura(Transform parent)
    {
        var go = new GameObject("BerserkAura");
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startColor = new Color(0.85f, 0.05f, 0.05f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 35f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        Material material = NewMaterial(Color.white);
        material.mainTexture = SoftCircle();
        rend.sharedMaterial = material;

        return ps;
    }
```

- [ ] **Step 4: `PlayerAbilities`** — (a) campos: `private GutsDash dash; private BerserkArmor berserk;`. (b) en `TryCast`, justo después de `AbilityDefinition ability = slots[slot];` y antes de calcular `rank`:
```csharp
        // La armadura de Guts es un interruptor: con ella puesta la tecla la apaga (siempre se puede, aunque haya enfriamiento).
        if (ability.kind == AbilityKind.Berserk && berserk != null && berserk.IsActive)
        {
            berserk.Deactivate();
            return;
        }
```
  (c) en el `switch`, antes de `default`:
```csharp
            case AbilityKind.Dash: cast = EnsureDash().TryStart(ability, rank); break;
            case AbilityKind.Berserk: cast = EnsureBerserk().Activate(ability, rank, () => StartCooldown(slot)); break;
```
  (d) justo después de `if (!cast) return;`:
```csharp
        // La armadura es un interruptor: su enfriamiento fijo empieza al apagarla (StartCooldown), no al encenderla.
        if (ability.kind == AbilityKind.Berserk) return;
```
  (e) métodos nuevos (junto a `TryCast`):
```csharp
    private GutsDash EnsureDash()
    {
        if (dash == null) dash = GetComponent<GutsDash>() ?? gameObject.AddComponent<GutsDash>();
        return dash;
    }

    private BerserkArmor EnsureBerserk()
    {
        if (berserk == null) berserk = GetComponent<BerserkArmor>() ?? gameObject.AddComponent<BerserkArmor>();
        return berserk;
    }

    // Enfriamiento de una habilidad que no empieza al lanzarla (la armadura: al apagarla). Avisa al HUD.
    private void StartCooldown(int slot)
    {
        float now = Time.time;
        if (!charges[slot].TryUse(now)) return;

        publishedCharges[slot] = charges[slot].Available(now);
        GameEvents.RaiseAbilityUsed(slot, now + charges[slot].RechargeRemaining(now));
    }
```
  (f) en `Configure`, junto a `EndUltimate();` agregar `if (dash != null) dash.Cancel(); if (berserk != null) berserk.ForceOff();` (antes de `RefreshBuild()`, que reinicia las cargas).
  (`GetComponent<T>() ?? ...` con componentes de Unity compara con `null` de Unity; si da error de compilación por el operador `??` con objetos Unity, usar `if (x == null) x = ...`.)

- [ ] **Step 5: Bonos de la armadura** — (a) `WeaponState.Damage`: reemplazar
```csharp
            float multiplier = SkillTreeManager.Instance != null ? SkillTreeManager.Instance.Bonuses.DamageMultiplier : 1f;
```
  por
```csharp
            float multiplier = (SkillTreeManager.Instance != null ? SkillTreeManager.Instance.Bonuses.DamageMultiplier : 1f)
                * BerserkArmor.DamageMultiplier;
```
  (b) `Shooting.UpdateSword`: `nextFireTime = Time.time + weapon.FireRate;` → `nextFireTime = Time.time + weapon.FireRate * BerserkArmor.CadenceMultiplier;`. (c) `Shooting.SwingSword`: al inicio, `float arc = BerserkArmor.SwingArc(sword.arcDegrees);` y usar `arc` en las dos llamadas a `MeleeCone.Contains(... sword.arcDegrees ...)` (la del cubo de inicio y la de los enemigos). (d) en `SwingSword`: `if (!empowered) fury.Add(FuryMeter.GainForHits(...));` → `if (!empowered) fury.Add(FuryMeter.GainForHits(meleeTargets.Count, sword.furyPerEnemyHit, sword.furyMaxPerSwing) * BerserkArmor.FuryGainMultiplier);`.

- [ ] **Step 6: Verificar** — compila sin errores.

### Task 5: Animaciones del controlador, assets e iconos (con test)

**Files:** Modify `Assets/Animation/Guts.controller`; Create `Assets/Data/Abilities/Embestida.asset`, `Armadura.asset`, `Assets/Art/Icons/Embestida.png`, `Armadura.png`; Modify `Guts.asset`.

- [ ] **Step 1: RED** — el test `EmbestidaAndArmaduraAssets_...` (Tarea 2) falla por los assets que faltan; correr `filter = "AbilityDefinition"` y confirmarlo.

- [ ] **Step 2: Estados `Dash` y `PowerUp` en el controlador** — `unity_execute_code`:
```csharp
var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animation/Guts.controller");
string r = "";
System.Action<string, string, float> addState = (name, fbx, totalSeconds) =>
{
    bool has = false;
    foreach (var p in ctrl.parameters) if (p.name == name) has = true;
    if (!has) ctrl.AddParameter(name, UnityEngine.AnimatorControllerParameterType.Trigger);

    AnimationClip clip = null;
    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/Guts/Animations/" + fbx + ".fbx"))
        if (o is AnimationClip c && !c.name.StartsWith("__preview")) clip = c;
    if (clip == null) { r += "no se encontró el clip de " + fbx + "\n"; return; }

    var sm = ctrl.layers[0].stateMachine;
    UnityEditor.Animations.AnimatorState locomotion = null, existing = null;
    foreach (var cs in sm.states) { if (cs.state.name == "Locomotion") locomotion = cs.state; if (cs.state.name == name) existing = cs.state; }
    if (existing != null) { r += name + " ya existe\n"; return; }

    var state = sm.AddState(name, new Vector3(300f, name == "Dash" ? 480f : 560f, 0f));
    state.motion = clip;
    state.speed = clip.length / totalSeconds;     // la animación dura lo que dura la acción
    var toState = sm.AddAnyStateTransition(state);
    toState.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, name);
    toState.hasExitTime = false; toState.duration = 0.05f; toState.canTransitionToSelf = false;
    var back = state.AddTransition(locomotion);
    back.hasExitTime = true; back.exitTime = 0.9f; back.duration = 0.1f;
    r += name + " listo: clip=" + clip.name + " len=" + clip.length.ToString("F2") + " velocidad=" + state.speed.ToString("F2") + "\n";
};
addState("Dash", "great sword crouching", 0.6f);      // dash 0,35 s + levantarse 0,25 s
addState("PowerUp", "great sword power up", 1.2f);
UnityEditor.EditorUtility.SetDirty(ctrl);
UnityEditor.AssetDatabase.SaveAssets();
return r;
```

- [ ] **Step 3: Iconos y assets** — `unity_execute_code` (iconos 128x128 por código; provisional), luego revisar los PNG con `Read`:
```csharp
System.Func<string, System.Func<float, float, Color>, Sprite> makeIcon = (path, painter) =>
{
    const int S = 128;
    var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
    for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
            tex.SetPixel(x, y, painter((x + 0.5f) / S * 2f - 1f, (y + 0.5f) / S * 2f - 1f));
    tex.Apply();
    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    UnityEditor.AssetDatabase.ImportAsset(path);
    var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    imp.textureType = UnityEditor.TextureImporterType.Sprite;
    imp.SaveAndReimport();
    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
};

// Embestida: disco naranja con tres chevrones blancos apuntando a la derecha
var dashIcon = makeIcon("Assets/Art/Icons/Embestida.png", (dx, dy) =>
{
    float d = Mathf.Sqrt(dx * dx + dy * dy);
    if (d > 0.95f) return new Color(0f, 0f, 0f, 0f);
    Color c = new Color(0.75f, 0.2f, 0.05f, 1f);
    for (int k = 0; k < 3; k++)
    {
        float cx = -0.45f + k * 0.35f;
        float chevron = Mathf.Abs(dy) - (0.55f - (dx - cx) * 1f);   // forma de ">"
        if (dx > cx && dx < cx + 0.55f && Mathf.Abs(Mathf.Abs(dy) - (0.5f - (dx - cx))) < 0.07f) c = Color.white;
    }
    return c;
});

// Armadura: disco rojo oscuro con un yelmo gris (rectángulo con visera)
var armorIcon = makeIcon("Assets/Art/Icons/Armadura.png", (dx, dy) =>
{
    float d = Mathf.Sqrt(dx * dx + dy * dy);
    if (d > 0.95f) return new Color(0f, 0f, 0f, 0f);
    Color c = new Color(0.35f, 0.04f, 0.05f, 1f);
    bool helm = Mathf.Abs(dx) < 0.42f && dy > -0.5f && dy < 0.55f && (dy > 0.1f ? Mathf.Abs(dx) < 0.42f - (dy - 0.1f) * 0.25f : true);
    if (helm) c = new Color(0.62f, 0.64f, 0.7f, 1f);
    if (Mathf.Abs(dx) < 0.3f && dy > -0.05f && dy < 0.12f) c = new Color(0.9f, 0.1f, 0.08f, 1f);   // visera encendida
    return c;
});

AbilityDefinition Make(string name, AbilityKind kind, Sprite icon, System.Action<AbilityDefinition> fill)
{
    var a = ScriptableObject.CreateInstance<AbilityDefinition>();
    a.abilityName = name; a.icon = icon; a.kind = kind;
    fill(a);
    UnityEditor.AssetDatabase.CreateAsset(a, "Assets/Data/Abilities/" + name + ".asset");
    var so = new UnityEditor.SerializedObject(a);
    so.FindProperty("id").stringValue = name;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(a);
    return a;
}

var dash = Make("Embestida", AbilityKind.Dash, dashIcon, a =>
{
    a.cooldown = 7f; a.cooldownPerRank = 0.4f;
    a.damageMultiplier = 3f; a.damagePerRank = 0.25f;
    a.dashDistance = 6f; a.dashSeconds = 0.35f; a.riseSeconds = 0.25f; a.shotRange = 25f;
});
var armor = Make("Armadura", AbilityKind.Berserk, armorIcon, a =>
{
    a.cooldown = 25f; a.cooldownPerRank = 3f;
    a.damageMultiplier = 1.4f; a.damagePerRank = 0.1f;
    a.drainFractionPerSecond = 0.02f; a.drainPerRank = 0.005f;
    a.roarRadius = 4f; a.roarStunSeconds = 1.5f;
    a.damageTakenMultiplier = 0.5f; a.cadenceMultiplier = 0.5f; a.furyGainMultiplier = 2f; a.swingArcDegrees = 360f;
});

var guts = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Guts.asset");
var flame = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Llamarada.asset");
guts.abilities = new AbilityDefinition[] { flame, dash, armor };
UnityEditor.EditorUtility.SetDirty(guts);
UnityEditor.AssetDatabase.SaveAssets();
return "habilidades de Guts=" + guts.abilities.Length + " iconos=" + (dashIcon != null) + "/" + (armorIcon != null);
```
  Esperado: `habilidades de Guts=3 iconos=True/True`. Mirar los dos PNG (`Read`); si se ven mal, ajustar la forma (es provisional).

- [ ] **Step 4: GREEN** — `filter = "AbilityDefinition"`: `fallan=0`; luego la suite completa sin fallos (413 + los tests nuevos).

---

## PARTE C — verificación

### Task 6: Verificación en Play, notas y memoria

- [ ] **Step 1: Respaldar `save.json` de este momento** (PowerShell, al scratchpad como `save_backup_berserk.json`).

- [ ] **Step 2: Play (pausado; funciones por reflexión; tope de pasos).** Preparación: `TrySelect(Guts)`, `abilityRanks[1] = 1; abilityRanks[2] = 1`, `Shooting.RefreshBuild()`, `GameEvents.RaiseGameStarted()`, `health.Invulnerable = true` solo mientras no se prueba el daño. `TryCast(slot)` por reflexión (privado en `PlayerAbilities`).
  **E:**
  1. `HasAbility(1)`; lanzar sin enemigos: el dash ocurre (posición cambia ~6 m en ~0,35 s de juego), no dispara y la Furia llena no se gasta.
  2. Durante el dash `health.Invulnerable == true`; tras ≈ 0,6 s vuelve a `false` y `IsForced == false`; los colliders de enemigos cercanos se restauran.
  3. Con 3 enemigos (1 al frente a 5 m, otro **detrás** a 3 m, otro a 12 m): al terminar, el **más cercano** (el de 3 m, aunque esté detrás) pierde `3 × daño de la espada`; los otros intactos.
  4. Con Furia llena: x2 (6 × espada) y la barra en 0; sin objetivo no la gasta.
  5. Cancelar: lanzar la E y cambiar de personaje a mitad → `Invulnerable == false`, `IsForced == false`.
  6. Enfriamiento de 7 s y el HUD de la casilla E; la Q sigue funcionando.
  **Ulti:**
  7. `TryCast(2)`: se activa (`BerserkArmor.Instance.IsActive`), el rugido aturde a un enemigo normal a 3 m y a ninguno a 6 m, y no a un jefe a 3 m.
  8. Con ella puesta: `WeaponState.Damage == espada × 1,4`; el tiempo entre golpes (`nextFireTime`) es la mitad; `BerserkArmor.SwingArc(120) == 360` y un enemigo **detrás** recibe el golpe de `SwingSword`; `health.TakeDamage(10)` quita 5; la Furia sube el doble por golpe (+10 por enemigo con 2 enemigos = 20 → tope por golpe... verificar con 1 enemigo: +10).
  9. Drenaje: con 150 de vida, ~3 por segundo (medido por segundos de juego con `Step` y tope), **sin** reducción por la armadura; con vida baja (por ejemplo 5) se apaga sola en 1 de vida y la vida queda en 1 (nunca 0); `DamageTakenMultiplier` vuelve a 1.
  10. Desactivar a mano con `TryCast(2)` (no debe lanzar de nuevo): empieza el enfriamiento de 25 s; intentar activar de nuevo antes → no se activa; el enfriamiento es el mismo habiéndola tenido 2 s o 20 s; apagar a mano o sola dan el mismo enfriamiento.
  11. Cambiar de personaje con la armadura puesta: se apaga sin enfriamiento y el color vuelve al normal. Con Alucard: sus 3 habilidades siguen funcionando.
  12. Captura (avanzando frames con `Step` y tope) con el tinte rojo y el humo; consola sin errores nuevos.

- [ ] **Step 3: Salir de Play y restaurar el guardado de este momento** (Unity quieto).

- [ ] **Step 4: Notas y memoria** (**no commitear**): entrada en `REGISTRO_DE_CAMBIOS.md`, decisiones **D41** (Embestida) y **D42** (Armadura Berserker: interruptor, drenaje que no mata, enfriamiento fijo al apagarla, bonos por singleton `BerserkArmor`, `Health.DamageTakenMultiplier`/`Drain`), "Dónde nos quedamos", `ATAJOS_DE_TECLADO.md` (E y F con Guts), línea de estado de la memoria.

- [ ] **Step 5: Informar al usuario** — qué quedó hecho y verificado, qué es **no verificado** (teclas `E`/`F` reales, cómo se ve el giro y la armadura, balance) y los números a ajustar en `Embestida.asset` y `Armadura.asset`.

---

## Self-Review (hecha al escribir)
- **Cobertura de los specs:** dash §3/§4 → Tareas 1 a 5; ulti §3/§4 → Tareas 1 a 5; pruebas → Tareas 1, 2 y 6.
- **Consistencia de nombres:** `TargetPicker.NearestIndex`, `DashRules.Direction`, `BerserkDrain.{Advance,Reset,Allowed,ShouldStop}`, `AbilityDefinition.{SwordScaledDamageFor,BerserkDamageAt,BerserkDrainAt, dashDistance, dashSeconds, riseSeconds, shotRange, drainFractionPerSecond, drainPerRank, roarRadius, roarStunSeconds, damageTakenMultiplier, cadenceMultiplier, furyGainMultiplier, swingArcDegrees}`, `Health.{DamageTakenMultiplier,Drain}`, `PlayerMovement.{MoveInput,IsForced,BeginForcedMove,EndForcedMove}`, `PlayerBody.{PlayDash,PlayPowerUp,BeginSpin,EndSpin}`, `GutsDash.{TryStart,Cancel}`, `BerserkArmor.{Activate,Deactivate,ForceOff,IsActive, DamageMultiplier, CadenceMultiplier, FuryGainMultiplier, SwingArc}`, `AbilityVfx.CreateBerserkAura`, `PlayerAbilities.StartCooldown`: iguales en todas las tareas.
- **Sin marcadores pendientes** (el error de borrador del test `OneBigStep_...` está corregido en el mismo bloque: el valor correcto es 6).
