# Menú de personajes y el mago (Frieren)

Fecha: 2026-10-02 · Estado: **implementado y verificado** (excepto lo que se indica en la sección 7)

> **Correcciones y cambios respecto al diseño presentado:**
> - **Error de cálculo corregido:** el M16 al nivel máximo rinde **196,4** de DPS sostenido, no 216 (usé una recarga de 1,0 s; el mínimo real es 1,25 s). Con el dato correcto el mago queda mejor parado: 93% del M16 en fila de 4 al máximo. La tabla de la sección 4 ya está corregida.
> - **Rangos de los tests:** salen de medir 6 puntos de progreso reales (0%–100%): el mago contra un enemigo está entre 86% y 99% de la pistola; en fila de 4 entre 78% y 93% del M16 (el punto bajo es el 40%, donde el M16 ya alcanzó su cadencia mínima). Por eso el rango contra el M16 es 75%–100% y no 80%–100%.
> - **El rayo es horizontal**, a la altura de la punta del bastón (1,1 m), hacia donde apunta la mira. Un rayo que sale de la cámara se clavaría en el suelo y perdería la penetración.
> - **Alcance lateral:** el rayo de radio 0,4 más media caja del enemigo (0,5) da 0,9 m a cada lado del eje, es decir 1,8 m de ancho contra enemigos normales.
> - **Enemigo pegado al bastón:** un `SphereCast` no reporta lo que ya toca la esfera al salir, así que `PiercingBeam` busca esos enemigos aparte con un `OverlapSphere`.
> - **Botones del menú:** el botón de reiniciar tenía `colorMultiplier = 3,66`, que aclaraba todo; se corrigió a 1 en los botones nuevos (y los deshabilitados ahora se ven grises).

## 1. Objetivo
1. Un **menú de selección de personaje** con estadísticas, que se abre al inicio de cada partida y soporta muchos personajes (los 3 principales ahora, más adelante tanques, asesinos, etc.).
2. El segundo personaje principal: un **mago inspirado en Frieren** con bastón, barra de maná y una habilidad de rayo penetrante.
3. Un **hueco reservado** para el tercer personaje principal.
4. Que el juego esté **balanceado**, con los números documentados y protegidos por tests.

Fuera de alcance: modelo y animaciones finales del mago, sonidos propios, el tercer personaje, un sistema de misiones o eventos (solo se deja el gancho), jefes, prestigio.

## 2. Decisiones
- **El disparo básico del mago no gasta maná.** Así siempre tiene un ataque, y el maná es solo para la habilidad.
- **El menú se abre al inicio de cada partida**, antes de la primera horda. No se cambia de personaje a mitad de partida. Se recuerda el último elegido (`SaveData.selectedCharacterId`).
- **Vida y velocidad son de cada personaje** (`CharacterDefinition.maxHealth` y `moveSpeed`). Esto reemplaza la decisión D7 de `DECISIONES.md` ("no cambian"). Alucard y el mago empiezan iguales (100 de vida, velocidad 5); la diferencia está en el kit.
- **Nombre provisional:** id `maga`, `displayName = "Frieren"`. Los nombres finales serán distintos (ver notas del juego).
- **Sin cambio de formato de guardado:** el mago reutiliza los 3 espacios de mejora de las armas. Solo cambian sus etiquetas y su efecto.

## 3. Datos (`Game.Core`)
**`CharacterDefinition`** (ya existe) gana: `maxHealth`, `moveSpeed`, `menuOrder`, `availability` (`Playable` / `ComingSoon`), `unlockKind` (`Free` / `Money` / `Mission` / `Event`), `unlockHint` (texto para los que se desbloquean por misión o evento), `description`, `heldItemPrefab` (el bastón). `unlockPrice` ya existía y solo aplica a `Money`.

**`WeaponDefinition`** gana dos miembros virtuales: `UsesAmmo` (verdadero por defecto) y `UpgradeLabel(UpgradeType)` (el nombre de cada mejora). También `DamageAt(nivel)` y `FireRateAt(nivel)`, con la fórmula actual de las armas, para que la lógica de daño viva en un solo lugar.

**`StaffDefinition : WeaponDefinition`** (nuevo): maná máximo y regeneración, y los datos de la habilidad (nombre, daño, costo de maná, enfriamiento, alcance, radio del rayo). Sobrescribe `UsesAmmo = false` y las etiquetas (Cadencia / Maná / Poder). Sus fórmulas:
- **Cadencia:** igual que en las armas (resta `step` a la cadencia hasta un límite).
- **Poder:** daño × (1 + nivel × `damageUpgrade.step`), tanto el básico como la habilidad.
- **Maná:** regeneración + nivel × `reloadUpgrade.step`, y enfriamiento − nivel × `cooldownStep` hasta un límite.

**Lógica pura, probada en EditMode:**
- `ManaPool`: reserva, regeneración, gasto.
- `AbilityCooldown`: usa un reloj inyectado para poder probarla.
- `CombatMath`: DPS sostenido de un arma o de un bastón (con N enemigos en la fila de la habilidad).
- `CharacterRules`: estado de un personaje (disponible, bloqueado, "próximamente") y desbloqueo con dinero sobre `SaveData`.
- `CharacterInfo`: líneas de estadísticas para el menú.

## 4. Balance
Referencia: la fórmula de DPS sostenido de las armas, `daño × cargador ÷ (cargador × cadencia + recarga)`.

| | Pistola | M16 | Frieren |
|---|---|---|---|
| nv. 0 | 30,8 | 54,5 | 29,3 (1 objetivo) / 48,5 (fila de 4) |
| nv. máx. | 123,1 | 196,4 | 105,6 (1 objetivo) / 182,4 (fila de 4) |

Valores base del bastón: disparo básico 8 de daño cada 0,35 s (automático). Maná 100, regeneración 4/s. Habilidad: 40 de daño, 25 de maná, 3 s de enfriamiento, alcance 100, radio del rayo 0,4. Mejoras con los mismos precios y niveles máximos que las armas de Alucard: Cadencia −0,03 s por nivel (límite 0,20 s), Maná +0,8/s y −0,3 s de enfriamiento por nivel (límite 1,5 s), Poder +10% por nivel.

**Reglas de balance que protegen los tests:**
- Contra un objetivo, el mago está entre 85% y 105% de la pistola en cada nivel de mejora.
- Con 4 enemigos en fila, está entre 75% y 100% del M16, y al menos 1,3 veces la pistola.
- Contra un solo objetivo el mago rinde menos del 60% del M16 (así Alucard conserva ese rol).
- La habilidad mata a un enemigo normal de un golpe (40 > 30 de vida) pero no a un tanque (60).
- El cálculo no contempla jefes: contra un único objetivo grande el mago rinde la mitad que Alucard, que es su rol.

Desbloqueo del mago: **$1.500**.

## 5. Runtime (`Game.Runtime`)
- **`GameInput`:** acción nueva `Ability1` (teclado `Q`, gamepad botón este) en `GameControls.inputactions`.
- **`Shooting`:** `SetCharacter(def)` reconstruye las armas con el progreso de ese personaje. Si el arma no usa munición, no hay recarga. Maneja la habilidad del bastón: maná, enfriamiento y el rayo.
- **`PiercingBeam`:** el rayo es un spherecast desde el centro de la pantalla. Daña a todos los enemigos que atraviesa y se detiene en la primera pared; se ignora al jugador.
- **`BeamVfx`:** `LineRenderer` reutilizable para el trazo del disparo básico y del rayo.
- **`CharacterManager`:** guarda el elenco (`CharacterDefinition[]`), aplica el personaje elegido (armas, vida, velocidad, bastón en la mano), resuelve selección y desbloqueo, y abre el menú al iniciar.
- **`PlayerHealth` / `PlayerMovement`:** `SetMaxHealth` y `SetSpeed`.
- **`GameEvents`:** `CharacterChanged`, `ManaChanged`, `AbilityUsed`. La barra de maná y el texto de enfriamiento de la UI solo escuchan eventos.
- **`CharacterSelectUI : MenuPanel`:** lista con scroll de tarjetas, panel de estadísticas y un botón de acción (Jugar, Desbloquear con costo, o deshabilitado con el motivo). Tiene foco inicial para teclado y gamepad.
- **Hueco reservado:** un `CharacterDefinition` en `ComingSoon` aparece como "???" bloqueado en la lista, sin estadísticas.
- **Tienda y mejoras:** la tienda avisa si el arma que vende no existe para el personaje actual. El menú de mejoras usa las etiquetas de la definición.
- **Gancho para el futuro:** los personajes con `unlockKind` `Mission` o `Event` se muestran bloqueados con su `unlockHint`, y se desbloquean desde código con `CharacterRules.Unlock`.

## 6. Verificación
- Tests de EditMode: `ManaPool`, `AbilityCooldown`, fórmulas de `StaffDefinition`, `CharacterRules`, `CharacterInfo` y los tests de paridad de balance de la sección 4.
- Mediciones en Play (con `EditorApplication.Step()` para no depender del foco): el rayo atraviesa 4 enemigos en fila y se detiene en una pared; el maná y el enfriamiento respetan los tiempos; el disparo básico daña igual que lo calculado; el menú completo (se abre al inicio, elegir cambia HUD y estadísticas, desbloquear cobra y se guarda); Alucard sigue igual que antes.
- **No verificable aquí:** que el balance *se sienta* bien. Todos los números son datos editables en los assets.

## 7. Qué se verificó y qué no (2026-10-02)
**Verificado:** compila sin errores; 59 tests de EditMode (los 26 anteriores + 33 nuevos, incluidos los de paridad con los assets reales). En Play: el menú se abre al inicio, lista los 3 personajes y muestra estadísticas con las mejoras ya compradas; el hueco reservado no tiene estadísticas ni se puede elegir; desbloquear cobra $1.500, se guarda y se recuerda entre partidas; elegir a Frieren cambia las armas, el bastón en la mano y el HUD (maná y habilidad en vez de munición); la tienda avisa "No disponible para este personaje". El rayo mata a 4 enemigos en fila, se detiene en la base y no toca lo de detrás, daña sin matar a un tanque, alcanza a un enemigo pegado y respeta el ancho. La habilidad gasta 25 de maná, respeta el enfriamiento de 3 s y regenera 4/s; en 30 s caben 8 rayos. El disparo básico hace 8 de daño y no gasta maná. Las mejoras usan las etiquetas del bastón y hacen efecto en caliente. Alucard sigue igual (vida, pistola, munición, recarga, HUD, tienda) y el menú recuerda la última elección.

**No verificado:**
- **La pulsación real de `Q` y del clic.** Con el editor sin foco Unity descarta el input simulado, y lo mismo pasa con `R`, que ya existía (experimento de control). Lo que sí se comprobó: `Ability1` está habilitada y resuelve a `/Keyboard/q` y al botón este del gamepad, y la ruta `TryCastAbility` funciona. Falta una prueba a mano.
- **Que el balance se sienta bien** y que el modelo provisional del bastón se vea bien en pantalla.
- **La navegación del menú con gamepad.** Hay un elemento con foco inicial, pero no se probó con un mando.
