# Árbol de habilidades por personaje: plan de implementación

> **Para quien ejecute:** usar superpowers:executing-plans (elegido: ejecución nativa, en esta sesión). Los pasos usan casillas `- [ ]`. **No hacer commits** (pedido del usuario). Skills de apoyo: test-driven-development, unity-scriptableobjects, unity-csharp-scripting, tools-unity-test-framework, verification-before-completion.

**Meta:** que cada personaje tenga un árbol (grafo) comprable con puntos propios que se ganan por oleada, con mejoras de stats y de habilidades, y una pestaña "Árbol" en la estación de habilidades.

**Arquitectura:** datos en un asset por personaje (`SkillTreeDefinition`), reglas y suma de bonos como lógica pura en `Game.Core` (probada en EditMode), un `SkillTreeManager` en la escena que gana puntos por oleada y aplica los bonos, y la pantalla armada por código con `UiKit`.

**Tecnología:** Unity 6000.6, C#, URP, uGUI/TextMeshPro, NUnit (EditMode, corridos por reflexión desde el editor).

**Spec:** `docs/superpowers/specs/2026-10-02-skill-tree-design.md`

## Restricciones globales
- Español en textos de interfaz y comentarios; comentarios solo donde el porqué no es obvio (estilo del repo).
- `Game.Core` no depende de `Game.Runtime`. Las reglas puras van en Core; lo que toca escena va en `Assets/Scripts/Gameplay` o `UI`.
- Cambiar la forma de `SaveData` exige subir `SaveData.CurrentVersion` (a 4) y revisar `SaveMigrations`.
- Un nodo se compra una sola vez; es comprable si es raíz o algún vecino está comprado y alcanzan los puntos.
- Puntos por oleada: `1 + oleada / 5`. Solo se gastan antes de la primera oleada. El reinicio del árbol es gratis.
- Los rangos por nivel de las habilidades no cambian: los bonos del árbol se suman encima. El enfriamiento nunca baja de 1 s.
- Las pruebas en Play modifican el guardado real: respaldar `save.json` antes y restaurarlo **con Unity fuera de Play y ya quieto** (Unity guarda al salir de Play).

## Foco de revisión (casos que el spec no prueba por sí solo)
- Un guardado con ids de nodo que ya no existen en el asset: se ignoran al calcular y no rompen nada (test en Tarea 1).
- Nodo con vecino inexistente en `connections`: no se desbloquea por él, sin excepción (Tarea 1).
- Reiniciar con saldo en 0 y sin nodos: devuelve 0 y no falla (Tarea 1).
- Habilidad con 0 cargas pedidas o `Configure` con enfriamiento 0: no divide ni se queda en bucle (Tarea 2).
- Enemigo del pool que vuelve con ralentización de su vida anterior: `Spawn` la limpia (Tarea 4).
- Terminar la niebla con enemigos ya muertos o desactivados: restaurar colisiones no lanza excepción (Tarea 4).

## Mapa de archivos
- Crear (Core): `Core/Data/SkillTree.cs` (tipos de datos), `Core/Data/TreeBonuses.cs`, `Core/Data/SkillTreeRules.cs`, `Core/Data/AbilityCharges.cs`.
- Modificar (Core): `Core/Save/SaveData.cs` (campos y v4), `Core/Save/SaveMigrations.cs`, `Core/Data/CharacterDefinition.cs` (`skillTree`), `Core/Data/AbilityDefinition.cs` (`mistSlowSeconds`), `Core/GameEvents.cs`.
- Crear (Gameplay): `Gameplay/Characters/SkillTreeManager.cs`.
- Modificar (Gameplay): `CharacterManager.cs`, `WeaponState.cs`, `EnemyAI.cs` (ralentización), `PlayerAbilities.cs` (Q, E, F), `UIManager.cs` y `AbilitySlotUI.cs` (cargas).
- Crear/modificar (UI): `UI/AbilityShopUI.cs` (pestañas), `UI/SkillTreeView.cs` (dibujo del árbol), `UI/SkillNodeButton.cs`.
- Crear (datos): `Assets/Data/Skills/Alucard_Tree.asset`.
- Tests: `Tests/EditMode/SkillTreeTests.cs`, `Tests/EditMode/AbilityChargesTests.cs`.
- Docs: `notas/DECISIONES.md` (D30), `notas/REGISTRO_DE_CAMBIOS.md`, `notas/NOTAS_DEL_JUEGO.md` (si aplica), memoria del proyecto.

---

## Estado de la ejecución (ledger)
Ejecutado en línea por una sola sesión, sin commits ni worktree (pedido del usuario). Tests: de 195 a **238**, todos en verde; verificación en Play con el guardado respaldado y restaurado.
- Tarea 1: completa (27 tests nuevos). Ruling: `IsUnlocked` recibe el árbol (`IsUnlocked(tree, node, owned)`) para poder ignorar vecinos que no existen.
- Tarea 2: completa (10 tests nuevos).
- Tarea 3: completa. Ruling: la lectura de bonos en `WeaponState.Damage` usa `SkillTreeManager.Instance` (sin él, multiplicador 1).
- Tarea 4: completa. Ruling: atravesar enemigos es comportamiento base de la niebla, no un nodo; `EnemyAI.Spawn` restaura la colisión con el jugador.
- Tarea 5: completa (6 tests de datos del asset). Ruling: daño +3% (raíz) y +5% (Pulsos), velocidad +4%, por el redondeo; 19 nodos, sin "Vitalidad III".
- Tarea 6: completa; verificada con capturas. Ruling: clic o Aceptar compra el nodo directamente (el reinicio es gratis).
- Tarea 7: completa; documentos y memoria al día. Revisión final: autorrevisión (no se lanzó un subagente porque no se pidió).

---

### Tarea 1: Datos, reglas, bonos y guardado v4 (TDD)

**Archivos:** crear `SkillTree.cs`, `TreeBonuses.cs`, `SkillTreeRules.cs`; modificar `SaveData.cs`, `SaveMigrations.cs`; test `SkillTreeTests.cs`.

**Interfaces:**
- Produce: `enum SkillEffectType`; `struct SkillEffect { SkillEffectType type; float value; }`; `class SkillNode { string id, displayName, description; int cost; Vector2 position; string[] connections; bool isRoot; SkillEffect[] effects; }`; `class SkillTreeDefinition : GameDefinition { SkillNode[] nodes; SkillNode Find(string id); }`; `enum SkillBuyBlock { None, UnknownNode, Owned, Locked, NoPoints }`; `static class SkillTreeRules { bool IsUnlocked(SkillNode, ICollection<string>); SkillBuyBlock CanBuy(SkillTreeDefinition, CharacterSave, string); bool TryBuy(SkillTreeDefinition, CharacterSave, string); int Reset(SkillTreeDefinition, CharacterSave); TreeBonuses Compute(SkillTreeDefinition, IEnumerable<string>); int PointsForWave(int) }`; `CharacterSave.skillPoints`, `CharacterSave.skillNodes`; `struct TreeBonuses` con `MaxHealthBonus`, `SpeedMultiplier`, `DamageMultiplier`, `HeavyShotExtraBullets`, `HeavyShotExtraCharges`, `HeavyShotCooldownReduction`, `MistBleedOnPass`, `MistSlow`, `MistDurationBonus`, `MistCooldownReduction`, `UltDurationBonus`, `UltCooldownReduction`, `UltLifeStealBonus`, y `static TreeBonuses None`.

- [ ] **Paso 1: tests que fallan** en `Tests/EditMode/SkillTreeTests.cs`: `PointsForWave` (1,4,5,9,10 → 1,1,2,2,3); `IsUnlocked` (raíz sí; nodo con vecino comprado sí; sin vecino no; vecino inexistente no, sin excepción); `CanBuy` en cada motivo; `TryBuy` descuenta y agrega; `Reset` devuelve la suma de costos, vacía la lista y con saldo 0 y sin nodos devuelve 0; `Compute` suma efectos (vida +10 con dos nodos, daño 1,05) e ignora ids que no existen; migración v3 a v4 (JSON v3 sin los campos carga con `skillPoints = 0` y lista vacía); `Validate` corrige `skillPoints` negativo, lista nula y ids repetidos.
- [ ] **Paso 2:** correr los tests por reflexión; deben fallar por compilación (tipos inexistentes).
- [ ] **Paso 3:** implementar los tipos y reglas mínimos (`SkillNode.connections` nulo se trata como vacío; `Compute` usa `Find` y salta nulos).
- [ ] **Paso 4:** subir `SaveData.CurrentVersion` a 4, agregar los campos a `CharacterSave` y normalizarlos en `NormalizeProgress` (saldo ≥ 0, lista sin nulos ni repetidos).
- [ ] **Paso 5:** compilar sin errores y correr todos los tests: deben pasar los 195 anteriores y los nuevos.

### Tarea 2: Cargas de habilidad (TDD)

**Archivos:** crear `AbilityCharges.cs`; test `AbilityChargesTests.cs`.

**Interfaces:** `class AbilityCharges { void Configure(int max, float rechargeSeconds, float now); bool TryUse(float now); int Available(float now); float RechargeRemaining(float now); int Max {get;} void Reset(float now); }`.

- [ ] **Paso 1: tests:** con 1 carga y 8 s se comporta como el enfriamiento actual (usa, queda en 0 hasta pasar 8 s); con 3 cargas permite 3 usos seguidos y el cuarto falla; recarga de una en una (tras 8 s hay 1, tras 16 s hay 2); no pasa del máximo; `Configure` con máximo menor que 1 se trata como 1 y con enfriamiento 0 no se queda en bucle; `RechargeRemaining` es 0 con las cargas llenas.
- [ ] **Paso 2:** ver que fallan.
- [ ] **Paso 3:** implementar con `charges`, `rechargeAt` y un `Refresh(now)` perezoso (suma cargas mientras `now >= rechargeAt`, con tope en el máximo y protección si `recharge <= 0`).
- [ ] **Paso 4:** pasan.

### Tarea 3: Manager, puntos por oleada, stats y daño

**Archivos:** crear `SkillTreeManager.cs`; modificar `GameEvents.cs`, `CharacterDefinition.cs` (`public SkillTreeDefinition skillTree;`), `CharacterManager.cs`, `WeaponState.cs`.

**Interfaces:** `SkillTreeManager.Instance`; `TreeBonuses Bonuses`; `SkillTreeDefinition Tree`; `CharacterSave ActiveSave`; `bool Buy(string id)`; `int ResetTree()`; `void Recompute()`; eventos `GameEvents.SkillPointsChanged(int total)` y `GameEvents.SkillPointsGained(string characterName, int amount)`; `CharacterManager.ReapplyStats()`.

- [ ] **Paso 1:** `SkillTreeManager` (escucha `WaveCompleted` y `CharacterChanged`; suma `PointsForWave` al personaje activo; `Recompute` lee el árbol del personaje activo y llama `SkillTreeRules.Compute`; `Buy` y `ResetTree` guardan, recalculan, llaman `CharacterManager.ReapplyStats()` y publican).
- [ ] **Paso 2:** `CharacterManager.Apply` usa vida y velocidad con bonos; `ReapplyStats()` repite solo eso.
- [ ] **Paso 3:** `WeaponState.Damage` = `Mathf.Max(base, round(base x DamageMultiplier))` leyendo `SkillTreeManager.Instance`.
- [ ] **Paso 4:** compilar y verificar por código en Play (con respaldo del guardado) que completar una oleada suma puntos y que comprar un nodo de vida cambia la vida máxima.

### Tarea 4: Habilidades (Q, E, F) y ralentización

**Archivos:** modificar `EnemyAI.cs`, `AbilityDefinition.cs` (`mistSlowSeconds = 2`), `PlayerAbilities.cs`, `UIManager.cs`, `AbilitySlotUI.cs`, `GameEvents.cs` (`AbilityChargesChanged(int slot, int charges, int max)`).

**Interfaces:** `EnemyAI.ApplySlow(float fraction, float seconds)`; `PlayerAbilities` usa `AbilityCharges` por casilla con `HeavyShotExtraCharges`.

- [ ] **Paso 1:** `EnemyAI.ApplySlow` guarda `slowMultiplier` y `slowUntil`; en `Update` aplica `agent.speed = def.speed x multiplicador`; `Spawn` limpia la ralentización.
- [ ] **Paso 2 (Q):** reemplazar `AbilityCooldown` de la casilla por `AbilityCharges`; el enfriamiento de recarga = `max(1, CooldownAt(rango) - HeavyShotCooldownReduction)`; pausa mínima de 0,35 s entre usos; con `HeavyShotExtraBullets`, lanzar 1 + N balas separadas 0,12 s en una corrutina (cada una con daño y sangrado).
- [ ] **Paso 3 (E):** durante la niebla, cada fotograma ignorar colisiones con los enemigos a menos de 3 m y, a menos de 1,2 m y una vez por uso, aplicar `MistBleedOnPass` pilas y `ApplySlow(MistSlow, mistSlowSeconds)`; al terminar restaurar las colisiones (tolerando enemigos muertos o nulos). Duración y enfriamiento suman los bonos del árbol.
- [ ] **Paso 4 (F):** `UltDurationBonus`, `UltCooldownReduction` y `UltLifeStealBonus` se suman a los valores del rango.
- [ ] **Paso 5 (HUD):** `AbilitySlotUI.SetCharges(int, int)` crea un texto pequeño en la esquina cuando hay más de una carga; `UIManager` lo conecta al evento.
- [ ] **Paso 6:** compilar; correr todos los tests; verificar en Play lanzando Q con 2 cargas y E sobre un enemigo (sangrado y velocidad).

### Tarea 5: Asset del árbol de Alucard y escena

**Archivos:** crear `Assets/Data/Skills/Alucard_Tree.asset` (por `execute_code`, con los 19 nodos de la sección 8 del spec y sus conexiones y posiciones), enlazarlo en `Alucard.asset`, agregar el objeto `SkillTreeManager` a la escena y guardarla.

- [ ] **Paso 1:** crear el asset con los nodos del spec (ids en minúsculas sin espacios: `core`, `v1`, `s1`, `d1`, `q1`..`q4`, `v2`, `e1`..`e4`, `s2`, `f1`..`f3`, `d2`, `d3`, `v3`).
- [ ] **Paso 2:** validar con un test de datos (EditMode, sobre el asset cargado) que todos los ids son únicos, todas las conexiones existen, hay al menos una raíz y todo nodo es alcanzable desde una raíz.
- [ ] **Paso 3:** enlazar al personaje y agregar el manager a la escena; guardar la escena.

### Tarea 6: Pantalla (pestañas y árbol)

**Archivos:** modificar `AbilityShopUI.cs`; crear `SkillTreeView.cs` y `SkillNodeButton.cs`.

- [ ] **Paso 1:** pestañas "Habilidades" y "Árbol" (la segunda solo si el personaje tiene árbol); al cambiar de pestaña se muestran u ocultan la lista de habilidades y la vista del árbol.
- [ ] **Paso 2:** `SkillTreeView` dibuja nodos como cuadrados en su posición (escala ajustada al espacio), líneas entre conectados, colores por estado, panel inferior con nombre, descripción y costo, texto "Puntos de X: N" y botón "Reiniciar árbol".
- [ ] **Paso 3:** `SkillNodeButton` (ratón y selección) actualiza el panel; clic o Aceptar llama a `SkillTreeManager.Buy`.
- [ ] **Paso 4:** verificar en Play con capturas: nodos, colores, compra, reinicio, y que sin puntos los nodos quedan apagados.

### Tarea 7: Verificación final y documentos

- [ ] **Paso 1:** compilar sin errores y correr todos los tests (los anteriores más los nuevos).
- [ ] **Paso 2:** Play completo con el guardado respaldado: ganar puntos, comprar un camino hasta una mejora de Q, E y F, comprobar sus efectos y reiniciar. Restaurar el guardado al final (con Unity quieto).
- [ ] **Paso 3:** actualizar `notas/DECISIONES.md` (D30), `notas/REGISTRO_DE_CAMBIOS.md` (estado, pendientes, entrada nueva), `notas/NOTAS_DEL_JUEGO.md`, el spec (estado real) y la memoria del proyecto. No commitear.
