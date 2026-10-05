# Guts: ulti "Armadura Berserker"

Fecha: 2026-10-04 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (a pedido del usuario, él decide cuándo). 4.ª entrega de las habilidades de Guts (después de la Q, las almas y la E: `2026-10-04-guts-dash-design.md`).

## 1. Qué se quiere
La **ulti** de Guts: se transforma con la armadura Berserker y gana efectos fuertes, a cambio de que la armadura le va **bajando la vida**. Es un **interruptor**: el jugador la activa y la desactiva cuando quiere con la misma tecla. Las almas (cura) son lo que repone la vida que gasta. **Tiene un enfriamiento fijo entre activaciones** (pedido del usuario): empieza al **desactivarla** y dura lo mismo cada vez, sin importar cuánto tiempo la haya tenido puesta.

## 2. Alcance
- **Entra:** el interruptor, los efectos, el drenaje de vida, el enfriamiento, el rugido de activación, el aspecto provisional (tinte y partículas), rangos, icono provisional, pruebas.
- **No entra:** el **modelo real de la armadura** (no está en el proyecto: está en la carpeta de modelos del usuario, en los `.zip` que no se copiaron; cuando exista se cambia sin tocar las reglas), sonidos, el árbol de Guts.

## 3. Reglas
- **Tecla `F`** (casilla 3). Con la armadura **apagada** y el enfriamiento listo: la **activa**. Con la armadura **puesta**: la **desactiva** (siempre se puede, aunque el enfriamiento no haya terminado). Solo tras empezar la primera oleada y con el input libre.
- **Al activarla:** animación `great sword power up`, y un **rugido**: aturde `roarStunSeconds` = **1,5 s** a los enemigos a `roarRadius` = **4 m** (los jefes y minijefes no, como siempre), sin daño.
- **Efectos mientras está puesta** (rango 1):
  - **+40% de daño** de todo lo que hace Guts con la espada, la Q y la E (se suma como multiplicador al daño de la espada: x1,4).
  - **Tiempo entre golpes de espada a la mitad** (x0,5).
  - **Recibe 50% menos daño** (x0,5).
  - El golpe de espada pega en **360°** (alrededor de Guts, con el mismo alcance).
  - **La Furia se carga el doble de rápido** (x2).
- **Drenaje:** pierde `drainFractionPerSecond` = **2% de la vida máxima por segundo** (3 de vida por segundo con 150). **Nunca lo mata:** el drenaje se detiene en 1 de vida y, si llega a 1, la armadura **se desactiva sola**. Las almas y la curación siguen funcionando mientras está puesta.
- **Se desactiva sola** también si termina la partida, si Guts muere o si se cambia de personaje.
- **Enfriamiento fijo:** `cooldown` = **25 s**, empieza **al desactivarla** (a mano o sola), igual cada vez. Mientras está puesta no hay enfriamiento corriendo.
- **Rangos:** 3 (como las demás ultis), con las compuertas de nivel 6, 12 y 18 que ya existen. Por rango extra: el bono de daño sube **+10 puntos** (x1,4 → x1,5 → x1,6), el drenaje baja **−0,5 puntos** (2% → 1,5% → 1%) y el enfriamiento baja **−3 s**.
- **Q y E con la armadura:** se pueden usar; heredan el multiplicador de daño.
- **Aspecto (provisional):** el cuerpo se tiñe de rojo oscuro con un brillo rojo y partículas de humo rojizo mientras está puesta (por código); al desactivarla vuelve el color normal. **Icono:** `Assets/Art/Icons/Armadura.png`, generado por código.

## 4. Estructura
- **Lógica pura (Game.Core, con tests):**
  - `AbilityKind.Berserk` se agrega **al final** del enum (valor 5). `AbilityDefinition` gana `drainFractionPerSecond`, `roarRadius`, `roarStunSeconds`, `damageTakenMultiplier` (0,5), `cadenceMultiplier` (0,5), `furyGainMultiplier` (2), `swingArcDegrees` (360); el bono de daño usa `damageMultiplier` (1,4) con `damagePerRank` (0,1); `drain` por rango con un campo `drainPerRank` (0,005) y `cooldownPerRank` (3). `Progression.MaxRank` ya la trata como ulti (3 rangos).
  - `BerserkDrain` (`BerserkDrain.cs`): acumula la vida a perder: `int Advance(float dt, int maxHealth, float fractionPerSecond)` devuelve los puntos enteros que toca restar (acumula el resto), y `static int Allowed(int current, int toLose)` limita para dejar **mínimo 1**; `static bool ShouldStop(int current)` es verdadero cuando `current ≤ 1`.
  - Fórmulas por rango en `AbilityDefinition`: `BerserkDamageAt(rank)`, `BerserkDrainAt(rank)` (mínimo 0), `DescribeRank`.
- **Juego:**
  - `BerserkArmor` (componente en el jugador, `Assets/Scripts/Gameplay/Player/BerserkArmor.cs`, con `Instance`): `IsActive`, `TryToggle(ability, rank)`, `Deactivate()`, y los multiplicadores vigentes (`DamageMultiplier`, `CadenceMultiplier`, `FuryGainMultiplier`, `DamageTakenMultiplier`, `SwingArcOverride`) que valen 1/1/1/1/sin cambio cuando está apagada. `PlayerAbilities.TryCast` la llama con un `case AbilityKind.Berserk` (con su regla propia: desactivar siempre permitido; activar solo con el enfriamiento listo; el enfriamiento arranca al desactivar).
  - **Ganchos** (pequeños, con valores neutros si no hay armadura): `WeaponState.Damage` multiplica por `BerserkArmor.DamageMultiplier`; `Shooting.UpdateSword` usa la cadencia x0,5 (`FireRate × CadenceMultiplier`) y `SwingSword` el arco override y la carga de Furia x2; `Health.DamageTakenMultiplier` (nuevo, por defecto 1) aplicado en `TakeDamage`.
  - `Assets/Data/Abilities/Armadura.asset` (id "Armadura"); `Guts.asset` pasa a `abilities = [Llamarada, Embestida, Armadura]`.
  - Efecto visual y tinte en `BerserkArmor` (partículas por código, como `AbilityVfx`).
- **Datos:** todos los números en `Armadura.asset`.

## 5. Pruebas
Tests de EditMode (primero):
- `BerserkDrain`: 150 de vida y 2% → 3 por segundo; `Advance` en pasos pequeños acumula el resto sin perder ni duplicar puntos; con otro máximo y fracción; `Allowed` nunca deja menos de 1 (current 5, perder 9 → 4; current 1 → 0); `ShouldStop` en 1 y 0.
- `AbilityDefinition` para Berserk: `BerserkDamageAt` (1,4 / 1,5 / 1,6), `BerserkDrainAt` (2% / 1,5% / 1%), `CooldownAt` (25 / 22 / 19), `DescribeRank`, valores del asset y que Guts lleva la ulti en la casilla 2 (tercera); el enum (0 a 5) no se mueve.

En Play (con `save.json` respaldado y restaurado; bucles con tope de pasos; sin dejar enemigos de vida enorme):
- Activar: el rugido aturde a los normales a 4 m y no a los jefes ni a los de más lejos.
- Mientras está puesta: el daño de la espada x1,4; el tiempo entre golpes a la mitad; el golpe alcanza a un enemigo detrás de Guts (360°); el daño recibido a la mitad (probar con `TakeDamage`); la Furia sube el doble por golpe.
- Drenaje: ~3 de vida por segundo (medido por segundos de juego) y **nunca baja de 1**: llegando a 1 se desactiva sola.
- Desactivar a mano en cualquier momento; el enfriamiento de 25 s empieza ahí, es el mismo con 2 s o con 20 s de uso, y no se puede reactivar antes.
- Se desactiva al cambiar de personaje y al terminar la partida; el color vuelve al normal.
- Alucard y la Q y la E de Guts siguen funcionando.
- **No verificado hasta que lo pruebe el usuario:** la tecla `F` real, cómo se ve el tinte/partículas y el balance.

## 6. Riesgos y decisiones abiertas
- **Balance:** x1,4 de daño, golpes al doble de rápido y la mitad de daño recibido es muy fuerte; el freno es el drenaje de vida y el enfriamiento de 25 s. Con las almas curando, puede mantenerse más tiempo del previsto; números en el asset.
- **Valores por rango** (+0,1 de daño, −0,5% de drenaje, −3 s de enfriamiento) y el aturdimiento del rugido (1,5 s a 4 m) son supuestos míos.
- **Armadura sin modelo:** el tinte y las partículas son provisionales; con el modelo real habrá que reemplazar el cuerpo (el reemplazo se hace en `BerserkArmor`, sin tocar las reglas).
- **Multiplicadores globales:** `BerserkArmor.Instance` expone los bonos como un singleton más (como `SkillTreeManager`); si más personajes tuvieran transformaciones, convendría un sistema de "modificadores" genérico (fuera de alcance).
