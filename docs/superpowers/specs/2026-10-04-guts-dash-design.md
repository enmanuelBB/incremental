# Guts: habilidad E "Embestida" (dash con giro y disparo)

Fecha: 2026-10-04 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (a pedido del usuario, él decide cuándo). 3.ª entrega de las habilidades de Guts (después de la Q y las almas; antes de la ulti: `2026-10-04-guts-berserk-design.md`).

## 1. Qué se quiere
La habilidad `E` de Guts: un **dash que gira** y, cuando Guts se levanta, **dispara el arma de su brazo al enemigo más cercano**. Con la Furia llena sale potenciada (más daño).

## 2. Alcance
- **Entra:** el dash (movimiento, invulnerabilidad, giro del cuerpo), el disparo automático al más cercano, rangos, icono provisional, pruebas.
- **No entra:** la ulti, sonidos, un modelo del cañón (el disparo es un rayo rojo con un destello), el árbol de Guts.

## 3. La habilidad
- **Secuencia:** (1) el dash dura `dashSeconds` = **0,35 s** y recorre `dashDistance` = **6 m**; (2) al terminar se **levanta** durante `riseSeconds` = **0,25 s** (quieto); (3) al levantarse **dispara** una vez al enemigo vivo más cercano dentro de `shotRange` = **25 m** (sin importar hacia dónde mire). Si no hay enemigos, no dispara.
- **Dirección:** hacia donde se mueve el jugador (WASD, relativo a la cámara); si está quieto, hacia donde mira (la cámara, aplanada).
- **Invulnerable** durante el dash y mientras se levanta (`Health.Invulnerable`); se restaura siempre al terminar o si se cancela.
- **Giro:** en tercera persona el cuerpo da **una vuelta completa** durante el dash (giro del modelo con código, en el eje vertical) y usa la pose `great sword crouching` al levantarse; **no gira la cámara** en primera persona (para no marear): ahí solo se ve el avance.
- **Disparo:** daño = daño de la espada × `damageMultiplier` = **3** (+0,25 por rango extra), con mejoras de tienda y bono del árbol. Rayo rojo (`BeamVfx`) desde Guts hasta el enemigo y destello. Golpea a **un** enemigo.
- **Con Furia llena** (y hay un objetivo): gasta toda la barra y el daño se multiplica por `SwordDefinition.furyDamageMultiplier` (**2**), sin stun. Si no hay objetivo, no gasta Furia. La E **no carga Furia**.
- **Enfriamiento:** **7 s** (−0,4 s por rango extra), 5 rangos, comprados con los puntos de nivel. Empieza al lanzarla. Solo tras empezar la primera oleada, como las demás.
- **No se puede lanzar** si ya está en un dash o si el input está bloqueado (menús, fin de partida) y se cancela limpiamente si termina la partida o se cambia de personaje a mitad.
- **Con la armadura puesta (ulti)** funciona igual.
- **Icono:** `Assets/Art/Icons/Embestida.png`, generado por código; provisional.

## 4. Estructura
- **Lógica pura (Game.Core, con tests):**
  - `TargetPicker.NearestIndex(Vector3 origin, IList<Vector3> candidates, float maxRange)` (en `Assets/Scripts/Core/Data/TargetPicker.cs`): índice del candidato más cercano (distancia horizontal) dentro del alcance, o −1.
  - `DashRules.Direction(Vector3 moveInput, Vector3 aimForward)` (en `DashRules.cs`): aplana, usa la entrada si su módulo ≥ 0,1, si no la mira; normalizado; vector cero si ambos son nulos.
  - `AbilityKind.Dash` se agrega **al final** del enum (valor 4; Berserk será 5). `AbilityDefinition` gana `dashDistance`, `dashSeconds`, `riseSeconds` y `shotRange`; `damageMultiplier`, `cooldown` y los `...PerRank` se reutilizan. `DescribeRank` y fórmula `DashShotDamageFor(int swordDamage, int rank, float empowerMultiplier = 1f)`.
- **Juego:**
  - `GutsDash` (componente en el jugador, `Assets/Scripts/Gameplay/Player/GutsDash.cs`): máquina de estados por corrutina (dash → levantarse → disparo), con `bool TryStart(AbilityDefinition ability, int rank)` y `Cancel()`. `PlayerAbilities.TryCast` la llama con un `case AbilityKind.Dash`.
  - **Gancho en `PlayerMovement`:** un modo "empujado" (`BeginForcedMove(Vector3 velocity, float seconds)` / `EndForcedMove()`), porque hoy `FixedUpdate` fija la velocidad horizontal cada frame. Mientras está activo se usa esa velocidad.
  - **Gancho en `PlayerBody`:** giro del modelo (`BeginSpin(float seconds)` / `EndSpin()`), y el trigger `Crouch`/`Rise` del controlador (parámetros nuevos en `Guts.controller`, clip `great sword crouching`).
  - `Assets/Data/Abilities/Embestida.asset` (id "Embestida"); `Guts.asset` pasa a `abilities = [Llamarada, Embestida]`.
- **Datos:** todos los números en `Embestida.asset`.

## 5. Pruebas
Tests de EditMode (primero):
- `TargetPicker`: el más cercano gana; fuera de alcance → −1; lista vacía → −1; empate estable; la altura se ignora.
- `DashRules.Direction`: con entrada usa la entrada; sin entrada usa la mira; ambas nulas → cero; resultado normalizado y plano.
- `AbilityDefinition` para Dash: daño del disparo (x3, por rango, con Furia x2, mínimo 1), `CooldownAt` por rango (7 → 5,4), `DescribeRank`, valores del asset y que Guts lleva la E en la casilla 1; el enum de Alucard y `FlameBurst` (0 a 3) no se mueve y `Dash` vale 4.

En Play (con `save.json` respaldado y restaurado; bucles con tope de pasos):
- El dash recorre ~6 m en 0,35 s; el jugador es invulnerable y deja de serlo después; si no hay enemigos no dispara; con varios, dispara al más cercano (aunque no esté al frente) y le quita el daño esperado; con Furia llena x2 y la barra queda en 0; sin objetivo no gasta Furia.
- Cancelar (cambio de personaje o fin de partida a mitad) restaura la invulnerabilidad y el movimiento.
- Enfriamiento de 7 s y HUD de la casilla E. Alucard y la Q de Guts siguen funcionando.
- **No verificado hasta que lo pruebe el usuario:** la tecla `E` real (con el input real hay que revisar el conflicto con "Interactuar", que antes de la primera oleada usa también `E`), cómo se ve el giro y la sensación del dash.

## 6. Riesgos y decisiones abiertas
- **Colisiones:** el dash usa la física del jugador; atravesar enemigos o paredes depende de colisiones del `Rigidbody` (se detiene contra un muro). Los enemigos bloquean el dash salvo que se ignoren sus colliders durante el dash (decisión a tomar al implementar: se ignoran, como la niebla de Alucard).
- **Giro del modelo:** sin animación de voltereta, el giro es procedural y puede verse tosco; el arte final lo reemplazará.
- **Balance:** x3 de daño a un objetivo cada 7 s con invulnerabilidad es fuerte; números en el asset.
