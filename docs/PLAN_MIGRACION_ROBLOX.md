# Plan de migración a Roblox

Plan para rehacer el juego en Roblox (Luau). **No es un port automático: es una reescritura** que conserva el diseño, los números y las reglas del juego de Unity. Los modelos y las animaciones **no entran en este plan**: se hacen de cero en el formato de Roblox.

El juego de Unity **sigue en desarrollo**. Este plan es un documento vivo: **cada avance importante en Unity se refleja aquí** (ver "Cómo mantener este plan" al final). La sección "Registro de sincronización" dice hasta qué punto de Unity está al día.

---

## 1. Resumen

| | |
|---|---|
| **Código a reescribir** | ~12.000 líneas de C#: `Core/` ~3.000, `Gameplay/` ~6.200, `UI/` ~2.800. Tests: ~5.000 líneas (149 tests) |
| **Complejidad global** | Media-alta |
| **Estimación** | 5 a 8 semanas de una persona para tener lo mismo que hoy en un jugador, con servidor autoritativo. **2 a 4 semanas más** si se quiere cooperativo real |
| **Dónde se va el tiempo** | ~70% en enemigos (rendimiento de la horda), UI y la separación cliente/servidor. La lógica de `Core/` es lo más rápido |
| **Mayor riesgo** | Rendimiento con muchos enemigos a la vez (los `Humanoid` de Roblox son caros) |

---

## 2. Decisiones que hay que tomar antes de empezar

Mientras no se decidan, el plan asume lo marcado como **(por defecto)**.

1. **¿Un jugador o cooperativo?** Cambia la economía, el escalado de oleadas y la sincronización de jefes. **(Por defecto: un jugador por servidor, pero con el código separado en cliente y servidor desde el día 1, para no tener que rehacerlo si después se quiere cooperativo).**
2. **Nombres y diseños propios.** Alucard (Hellsing/Castlevania), Guts y Griffith (Berserk) y Frieren tienen copyright. En Roblox los juegos de fan-art de anime reciben reclamos DMCA, y más si monetizan. Las notas de diseño ya prevén nombres y diseños distintos: en Roblox hay que hacerlo **antes de publicar**, no después. Los `id` internos (`alucard`, `maga`, `guts`) pueden quedarse.
3. **Monetización.** ¿Algún personaje o mejora se desbloquea con Robux (Developer Products / Game Passes)? **(Por defecto: no; todo con el dinero del juego, como hoy).**
4. **Controles táctiles.** Gran parte del público de Roblox juega en celular. Hay que definir los botones en pantalla (disparo, Q/E/F, recarga, correr, cámara). **(Por defecto: se diseñan en la fase 6).**
5. **Librería de UI.** **(Por defecto: Fusion.)** Alternativa: React-lua (Roact). O ScreenGui a mano si se quiere evitar dependencias.

---

## 3. Herramientas y estructura

- **Rojo** + **VS Code** (extensión Luau LSP): el código vive como archivos `.luau` en el repo, con git, igual que hoy. Studio solo se usa para el mapa, los modelos y probar.
- **Wally** como gestor de paquetes.
- Paquetes previstos:
  - **ProfileStore** para el guardado (sesión bloqueada, reintentos, sin pérdida de datos).
  - **Signal** (GoodSignal o similar) para el bus de eventos.
  - **Fusion** para la UI.
  - **Jest-Lua** (o TestEZ) para los tests.
- Tipado estricto de Luau (`--!strict`) en los módulos de `Shared/`, para atrapar errores como hoy lo hace el compilador de C#.

### Estructura de carpetas (equivale a los ensamblados actuales)

```
src/
  Shared/          (ReplicatedStorage)   = Game.Core: lógica pura y datos
    Data/          tablas de personajes, armas, habilidades, enemigos, oleadas, árboles
    Rules/         WaveBuilder, CombatMath, BleedStacks, FuryMeter, SkillTreeRules...
    Net/           definición de los RemoteEvents/RemoteFunctions (un solo lugar)
  Server/          (ServerScriptService) autoridad: oleadas, enemigos, daño, dinero, XP, guardado
  Client/          (StarterPlayerScripts) input, cámara, armas en mano, VFX, predicción, UI
    UI/
tests/             tests de Shared/ (los mismos casos que Assets/Tests/EditMode)
```

**Regla equivalente a la de hoy:** `Shared/` no puede requerir nada de `Server/` ni de `Client/`, igual que `Game.Core` no puede referenciar a `Game.Runtime`.

---

## 4. El cambio grande: cliente y servidor

Hoy todo el código asume que corre en un solo lugar. En Roblox, **todo lo que valga algo vive en el servidor**: dinero, daño, vida, XP, puntos, desbloqueos, guardado. El cliente solo pide ("disparé en esta dirección", "usé la Q") y muestra.

| Qué | Dónde | Cómo |
|---|---|---|
| Input, cámara, animación local, mira, VFX inmediatos | Cliente | Se muestran al instante (predicción) |
| Disparo y habilidades | Cliente → Servidor | El cliente manda `Fire(origen, dirección)`. El servidor valida cadencia, munición y distancia, y hace el raycast de verdad |
| Daño, sangrado, quemadura, stun, muerte | Servidor | Nunca en el cliente |
| Dinero, XP, nivel, puntos, árbol, compras | Servidor | El cliente solo pide `BuyUpgrade(id)` y el servidor comprueba el saldo |
| Oleadas, enemigos, jefes | Servidor | El cliente solo los dibuja |
| HUD | Cliente | Escucha atributos replicados o RemoteEvents (como hoy la UI solo escucha `GameEvents`) |

**`GameEvents` se parte en dos:**
- Eventos **dentro del mismo lado**: un `Signal` local, igual que hoy.
- Eventos que **cruzan la red** (servidor → cliente: `MoneyChanged`, `WaveStarted`, `BossHealthChanged`, `XpChanged`...): RemoteEvents, o mejor **atributos** en el `Player` o en una carpeta replicada cuando es un valor de estado (vida, dinero, maná, furia), porque se replican solos.

Clasificación de los eventos actuales de `GameEvents.cs`:

| Evento | Origen en Roblox | Nota |
|---|---|---|
| `PlayerHealthChanged`, `BaseHealthChanged`, `MoneyChanged`, `ManaChanged`, `FuryChanged`, `XpChanged`, `SkillPointsChanged` | Servidor | **Atributos** replicados |
| `MoneyGained`, `WaveStarted`, `WaveCompleted`, `EnemyKilled`, `EnemyDied`, `EnemyHit`, `BleedTick`, `BurnTick`, `PoisonTick`, `XpGained`, `XpEarned`, `LevelUp`, `SkillPointsGained`, `BossAppeared`, `BossHealthChanged`, `BossDefeated`, `GameStarted`, `GameOver` | Servidor | RemoteEvent → cliente (`EnemyHit`, `BleedTick`, `BurnTick` y `PoisonTick` son muchos: agruparlos por frame. `EnemyHit` es cada golpe directo, para los números de daño) |
| `AbilityUsed`, `AbilityChargesChanged`, `AbilitiesChanged`, `WeaponSlotChanged`, `ResourceModeChanged`, `CharacterChanged` | Servidor confirma, cliente predice | El cliente muestra el enfriamiento al instante; el servidor corrige si lo rechaza |
| `PromptChanged` | Cliente | Mejor con `ProximityPrompt` de Roblox (ver estaciones) |

---

## 5. Equivalencias Unity → Roblox

| Unity | Roblox |
|---|---|
| `MonoBehaviour` | Script / LocalScript + ModuleScript |
| `ScriptableObject` (`Assets/Data/`) | ModuleScript que devuelve una tabla |
| `GameEvents` (eventos estáticos) | Signal local + RemoteEvent / atributos |
| `NavMesh` + `NavMeshAgent` | `PathfindingService` (limitado) o rutas propias por waypoints |
| `Physics.Raycast` / `OverlapSphere` | `workspace:Raycast`, `workspace:GetPartBoundsInRadius`, `GetPartsInPart` |
| `CharacterController` / movimiento propio | `Humanoid` del jugador (se ajusta `WalkSpeed`, `JumpPower`) |
| Cámara propia (`CameraFollow`) | `workspace.CurrentCamera` con `CameraType.Scriptable`, o el módulo de cámara por defecto modificado |
| Input System (`.inputactions`) | `ContextActionService` (crea botones táctiles solo) + `UserInputService` |
| Animator / Mecanim | `Animator` + `AnimationTrack` (se hacen de cero) |
| `ParticleSystem`, LineRenderer | `ParticleEmitter`, `Beam`, `Trail` |
| uGUI + `UiKit` | `ScreenGui` + Fusion |
| Navegación de menú con mando | `GuiService.SelectedObject` y `NextSelection*` |
| `save.json` con `.tmp` y `.bak` | DataStore con ProfileStore |
| `Time.timeScale` (cámara lenta del Game Over) | **No existe global en Roblox.** Se simula en el cliente (cámara y animaciones más lentas) |
| `Object pool` | Igual: reciclar modelos en vez de `Clone`/`Destroy` |
| EditMode tests | Jest-Lua / TestEZ |
| Controles de depuración (`O`, `P`, `N`, `U`) | Comandos solo en Studio (`RunService:IsStudio()`) |

---

## 6. Inventario archivo por archivo

Dificultad: 🟢 traducción casi 1:1 · 🟡 hay que adaptarlo · 🟠 hay que rediseñarlo · 🔴 alto riesgo.
Lado: **S** = Shared, **Sv** = Server, **C** = Client.

### 6.1 `Core/Data` — lógica pura y definiciones (→ `Shared/`)

| Archivo C# | Destino | Dif. | Nota |
|---|---|---|---|
| `CombatMath`, `TargetPicker`, `MeleeCone`, `StunRules`, `DashRules`, `HoverMath`, `MovePhase`, `SpawnArea`, `FrierenTreeMath`, `SlowRules`, `BeamAim` | `Shared/Rules/` | 🟢 | Matemática pura. `BeamAim`: la Q de Frieren va del bastón al punto de la mira con su altura (desde el 2026-10-07). `Vector3` existe igual en Luau. `SlowRules`: mientras dura una ralentización, solo la reemplaza otra igual o más fuerte |
| `PackFormation` | `Shared/Rules/` | 🟢 | Manadas en filas: ancho de cada fila al azar (`RowWidths`, 3 a 5) y puesto de cada enemigo con cada fila centrada (`Slot`) |
| `EnemyStackRules` | `Shared/Rules/` | 🟢 | Apilado estilo Megabonk: quién está subido encima de quién (de abajo hacia arriba, superposición en planta, tope de altura, quién bloquea al que no cabe). Lo corre el servidor sobre las posiciones lógicas |
| `BleedStacks`, `BurnState`, `FuryMeter`, `BerserkDrain`, `ManaPool`, `AbilityCooldown`, `AbilityCharges`, `TimedEffect`, `ZoltraakCharge`, `BarrelMagazines`, `FlowerFieldRules`, `SoulRules` | `Shared/Rules/` | 🟢 | Pasan a "clases" Luau (tabla + metatabla). El tiempo debe venir como parámetro (`os.clock()` / `workspace:GetServerTimeNow()`), no leído adentro |
| `WaveBuilder`, `WaveSet`, `BossRules` | `Shared/Rules/` | 🟢 | Las oleadas salen en manadas mixtas: `Interleave` (tipos intercalados), `CountRemaining` (lo que pasa a la siguiente al saltar) |
| `Progression`, `CharacterRules`, `CharacterInfo`, `SkillTreeRules`, `TreeBonuses`, `RunStats`, `UpgradeType`, `HudFormat`, `RecoilSettings` | `Shared/Rules/` | 🟢 | `HudFormat`: revisar el formato de números (separador de miles). `SkillTreeRules` tiene grupos de "elige 1" y nodos divididos (`choiceGroup`, `half`): `CanSwap`/`TrySwap` cambian una opción por otra con reembolso y comprobando que todo siga conectado; el servidor debe validar el cambio igual que una compra |
| `AbilityDefinition`, `CharacterDefinition`, `EnemyDefinition`, `WeaponDefinition`, `StaffDefinition`, `SwordDefinition`, `SkillTreeDefinition`, `GameDefinition` | `Shared/Data/` (forma de la tabla + validación) | 🟢 | Los campos que apuntan a prefabs, sonidos o iconos pasan a ser nombres de assets o `rbxassetid://` |

### 6.2 `Core` — resto

| Archivo C# | Destino | Dif. | Nota |
|---|---|---|---|
| `GameEvents` | `Shared/Signal` + `Shared/Net/` | 🟡 | Ver la sección 4 |
| `GameState` | `Sv` (autoridad) + atributo replicado | 🟢 | |
| `IDamageable` | Convención: módulo `Damage.apply(target, amount, source)` en `Sv` | 🟢 | Luau no tiene interfaces; se usa un registro de "dañables" |
| `Save/SaveData` | `Sv/Save/` | 🟡 | La forma de los datos se conserva (versión 4 hoy) |
| `Save/SaveMigrations` | `Sv/Save/` | 🟢 | La lógica de migración se conserva. **Empezar en Roblox con la versión actual de Unity** y migrar solo desde ahí en adelante |
| `Save/SaveFile`, `Save/SaveSystem` | Se reemplazan por ProfileStore | 🟡 | El `.tmp` y el `.bak` ya no hacen falta; sí hace falta guardar al salir y cada cierto tiempo, y respetar los límites del DataStore |

### 6.3 `Gameplay/Player`

| Archivo C# (líneas) | Destino | Dif. | Nota |
|---|---|---|---|
| `PlayerMovement` (132) | C | 🟡 | Humanoid + `WalkSpeed`. Caminar/correr con Shift, multiplicador de la niebla (lo valida el servidor) |
| `PlayerHover` (23) | C | 🟢 | Levitación visual de Frieren: solo animación/offset |
| `PlayerBody` (554) | C | 🟠 | Animaciones e inclinación del torso hacia la mira. Con modelos nuevos se rehace; la inclinación se replica a los demás si hay cooperativo |
| `CameraFollow` (196) | C | 🟠 | Primera/tercera persona con `T`, zoom, animación de Game Over. Primera persona necesita viewmodel propio (brazos y arma pegados a la cámara) |
| `GameInput` (76) | C | 🟡 | `ContextActionService`; agregar botones táctiles |
| `Shooting` (595) | C (predicción) + Sv (validación) | 🔴 | El archivo más grande del jugador: disparo, recarga con punto de no retorno, cambio de arma, maná del bastón. Partirlo en módulos por tipo de arma |
| `PlayerAbilities` (647) | C + Sv | 🔴 | Las 9 habilidades. Conviene un módulo por habilidad (`Abilities/DisparoPesado.luau`...) con la parte de cliente y la de servidor |
| `ZoltraakCaster` (126), `ManaBeam` (58), `PiercingBeam` (64), `BeamVfx` (65) | C + Sv | 🟡 | Raycasts y área en el servidor; `Beam` visual en el cliente |
| `FlowerField` (272) | C (colocar) + Sv (curar y frenar) | 🟡 | La zona se comprueba en el servidor con `GetPartBoundsInRadius` |
| `ManaPulseEffect` (96), `FlameBurst` (72), `AbilityVfx` (286) | C | 🟡 | Rehacer con `ParticleEmitter`; se elimina el problema de `Shader.Find` |
| `GutsDash` (185), `BerserkArmor` (193), `SwordGrip` (50) | C + Sv | 🟡 | Dash con invulnerabilidad validada por el servidor |
| `PlayerHealth` (12) | Sv | 🟢 | Vida como atributo |

### 6.4 `Gameplay/Enemies` y `Waves` — **el mayor riesgo**

| Archivo C# (líneas) | Destino | Dif. | Nota |
|---|---|---|---|
| `EnemyAI` (498) | Sv | 🔴 | Ir a la base o perseguir al jugador, anti-kiting, ataque por distancia. **No usar un `Humanoid` por enemigo.** Opción recomendada: el servidor mueve una posición lógica por enemigo (rutas precalculadas por waypoints o `PathfindingService` cacheado por zona) y el cliente dibuja y anima el modelo. Prototipo con 100+ enemigos antes de seguir. Desde el 2026-10-07: **volador** (altura fija `flyHeight` con vaivén, mismo camino que los de tierra), **ataque a distancia** (`ranged`: se detiene a `attackReach` y lanza `EnemyProjectile`) y altura de apilado; cerca de su objetivo deja de esquivar para meterse entre los otros y trepar. En Unity el `NavMeshAgent` escala sus medidas con el objeto (se le pasan en unidades locales); en Roblox no aplica |
| `EnemyPool` (52) | Sv + C | 🟡 | Pool de modelos en el cliente; IDs de enemigo en el servidor. Agrega `EnemyCrowd` |
| `EnemyCrowd` (73) | Sv | 🟡 | Apilado estilo Megabonk cada frame con `EnemyStackRules`: sube/baja suave (6 y 10 m/s), tope 5 m y empuja de lado al que no cabe. Con 100+ enemigos es O(n²): en Roblox hacerlo cada 2-3 frames o por celdas. La altura viaja al cliente con la posición |
| `EnemyBleed` (92), `EnemyBurn` (96), `EnemyPoison` (96) | Sv (tick) + C (tinte y número) | 🟢 | El tinte rojo con `Highlight` o cambiando el color. El veneno (campo de flores de Frieren) es la misma lógica que la quemadura (`BurnState`), en verde. `EnemyAI` también guarda la Marca de maná (daño recibido +30% por un tiempo) |
| `BossController` (260), `EnemyProjectile` (95) | Sv | 🟠 | Embestida, invocar, enfurecer, resucitar, disparar. `EnemyProjectile` (antes `BossProjectile`) lo usan el jefe Tirador y el Lanzador; apunta a un collider (jugador o base) con color y tamaño propios. Proyectiles: simulados en el servidor, dibujados en el cliente |
| `WaveManager` (196), `SpawnZone` (48) | Sv | 🟢 | Manadas de 2 filas de frente a la base, de 3 a 5 enemigos al azar cada una, 2,5 m entre cada uno (`PackFormation`), cada `spawnInterval` |

### 6.5 `Gameplay` — resto

| Archivo C# | Destino | Dif. | Nota |
|---|---|---|---|
| `Characters/CharacterManager` (151) | Sv | 🟡 | Aplica vida, velocidad y armas del personaje elegido |
| `Characters/ProgressionManager` (109), `SkillTreeManager` (151), `RunSummaryTracker` (64) | Sv | 🟢 | No migrar `DebugLevelUp`, `DebugResetLevel`, `DebugAddPoints` ni `DebugClearTree` (solo depuración) |
| `Characters/SoulPickup` (55), `SoulSpawner` (96) | Sv + C | 🟡 | Las almas de Guts: recogida validada en el servidor |
| `Economy/MoneyManager` (63) | Sv | 🟢 | |
| `Base/BaseHealth` (12), `Base/Health` (66) | Sv | 🟢 | |
| `Weapons/WeaponState` (117) | S + Sv | 🟢 | |
| `Weapons/HeldGuns` (86), `GunModel` (77) | C | 🟡 | Armas en mano; se rehacen con los modelos nuevos |
| `Interaction/*` (estaciones, `StartTrigger`, `ShopTrigger`) | C + Sv | 🟢 | `ProximityPrompt` de Roblox reemplaza el trigger + texto + "Interactuar". El servidor valida "solo antes de la primera oleada" |
| `Audio/AudioManager` (26) | C | 🟢 | `SoundService` |
| `Debug/DebugCheats` (46) | Sv, solo en Studio | 🟢 | |

### 6.6 `UI` (→ `Client/UI/`, todo 🟡/🟠)

| Archivo C# (líneas) | Dif. | Nota |
|---|---|---|
| `UIManager` (196), `AmmoPanelUI`, `StaffHud`, `FuryBarUI`, `XpBarUI`, `BossHealthBarUI`, `AbilitySlotUI`, `HudPunch` | 🟡 | HUD. Escala con `UIScale` / `UIAspectRatioConstraint`; respetar la zona segura del celular |
| `FloatingText` + `FloatingTextManager` | 🟡 | `BillboardGui` con pool. Ahora solo el "+X" del dinero |
| `DamageNumbersUI` (+ `DamageNumberStyle` en `Shared/Rules/`) | 🟡 | Números de daño (desde el 2026-10-07): golpe directo grande con pop, rojo que pasa a blanco; ticks más chicos de su color; suben y se desvanecen en 0,9 s. En Roblox: `BillboardGui` con pool anclado al punto del golpe; el borde, la sombra y el relieve del material TMP se imitan con `UIStroke` y un `UIGradient` |
| `CharacterSelectUI` (190) | 🟡 | |
| `UpgradeMenuUI` (133), `WeaponCardUI`, `WeaponInfoUI` | 🟡 | |
| `AbilityShopUI` (396) | 🟠 | Dos pestañas (rangos y árbol) |
| `SkillTreeView` (551), `SkillTreePanZoom` (38), `SkillNodeButton` (21) | 🟠 | Pan y zoom: `ScrollingFrame` o un `Frame` con arrastre propio; zoom con pellizco en el celular. Además: marcos "Elige 1", nodos partidos en dos botones (curar / ralentizar) y una ventana de confirmación para cambiar de opción |
| `GameOverManager` (114), `GameOverScreenUI` (285) | 🟡 | Resumen de la partida. La cámara lenta se simula en el cliente |
| `MenuPanel`, `UiKit` | 🟡 | Se reemplazan por componentes de Fusion |

### 6.7 Datos (`Assets/Data/` → `Shared/Data/`)

Copiar **los valores** de cada asset a tablas Luau, sin cambiar los `id`. Conviene un script (en Unity o en Python) que lea los `.asset` (YAML) y genere los `.luau`, para poder **regenerarlos cada vez que cambie el balance en Unity** en vez de copiarlos a mano.

| Carpeta | Assets hoy |
|---|---|
| `Characters/` | `Alucard`, `Maga` (Frieren), `Guts` |
| `Weapons/` | `Pistola`, `Baston`, `Espada`, `M16` (apagado) |
| `Abilities/` | `DisparoPesado`, `Niebla`, `Definitiva` (Alucard); `RayoMana`, `CampoFlores`, `PulsoMana` (Frieren); `Llamarada`, `Embestida`, `Armadura` (Guts) |
| `Enemies/` | `Enemy_Normal`, `Enemy_Tank`, `Enemy_Flyer` (Volador), `Enemy_Shooter` (Lanzador), `Mini_Embestidor`, `Mini_Coloso`, `Boss_Invocador`, `Boss_Tirador` |
| `Skills/` | `Alucard_Tree` (35 nodos), `Guts_Tree` (39 nodos), `Frieren_Tree` (56 nodos: 4 grupos "elige 1" y 4 nodos divididos) |
| `Waves/` | `Waves_Default` |

### 6.8 Tests

Los 149 tests de `Assets/Tests/EditMode` prueban casi todo `Core/`. **Se traducen junto con su módulo** en la fase 1: son la prueba de que la traducción a Luau da los mismos números que Unity. `BalanceTests` y `BossBalanceTests` son los más valiosos.

---

## 7. Fases

Cada fase termina con algo que se puede probar en Studio.

### Fase 0 · Montaje (2-3 días)
- Proyecto Rojo + Wally + Luau LSP + Jest-Lua. Repo separado o carpeta `roblox/` en este repo (decidir).
- Mapa de prueba en Studio: base, zona de spawn, una ruta.
- **Listo cuando:** `rojo serve` sincroniza y un test vacío corre.

### Fase 1 · `Shared/`: lógica y datos (1 semana)
- Traducir todo `Core/Data` y los tests correspondientes.
- Script que genera `Shared/Data/*.luau` desde los `.asset`.
- **Listo cuando:** los tests traducidos pasan con los mismos números que en Unity.

### Fase 2 · Prototipo de horda (3-5 días) — **decide si el proyecto sigue**
- Servidor mueve 100-200 enemigos hacia la base; el cliente los dibuja con un modelo simple.
- Medir FPS del cliente y tiempo del servidor (MicroProfiler) en PC y en un celular de gama media.
- **Listo cuando:** 150 enemigos a 60 FPS en PC y 30+ en celular. Si no se llega, ajustar el máximo de enemigos vivos en `WaveBuilder` antes de seguir.

### Fase 3 · Jugador y un personaje (1-1,5 semanas)
- Movimiento, cámara primera/tercera persona, input con teclado, mando y táctil.
- **Alucard completo:** dos pistolas que se turnan, recarga con punto de no retorno, sangrado, Q/E/F, validación en el servidor.
- Vida del jugador y de la base, Game Over.
- **Listo cuando:** se juega una partida entera con Alucard contra la horda.

### Fase 4 · Oleadas, enemigos y jefes (1 semana)
- `WaveManager`, IA completa (base o jugador, anti-kiting), tanque, volador, Lanzador, manadas, apilado, los 4 jefes y minijefes con sus comportamientos.
- Mapa de 60 x 80 m (desde el 2026-10-07): zona de aparición a z=46 (36 x 8 m), ~30 s hasta la base.

### Fase 5 · Resto de personajes (1-1,5 semanas)
- Frieren (Zoltraak, rayo, campo de flores, pulso, levitación).
- Guts (espada en cono, stun, Furia, llamarada, quemadura, almas, embestida, armadura).

### Fase 6 · Economía, progresión, guardado y UI (1,5-2 semanas)
- Dinero, estación de mejoras, XP y nivel, rangos, árboles, menú de personajes, resumen al morir.
- ProfileStore con la forma de `SaveData` actual.
- Toda la UI con Fusion, navegación con mando y botones táctiles.

### Fase 7 · Pulido y publicación
- Sonidos, VFX finales, modelos y animaciones nuevos.
- Nombres y diseños propios (ver la sección 2).
- Quitar los controles de depuración.
- Pruebas en celular, consola y PC; servidor privado para testers.

### (Opcional) Fase 8 · Cooperativo
- Varios jugadores defendiendo la misma base, escalado de vida de enemigos por jugador, reparto de dinero y XP.

---

## 8. Riesgos

| Riesgo | Impacto | Qué hacer |
|---|---|---|
| Rendimiento de la horda | Alto | Fase 2 antes que nada; enemigos sin `Humanoid`; limitar enemigos vivos. Desde el 2026-10-07 hay más por oleada (20 a 40 en las tres primeras, +4 por oleada) y el apilado agrega un cálculo O(n²) por frame: incluirlo en el prototipo de la fase 2 |
| Exploits (dinero, daño, velocidad) | Alto | Todo lo valioso en el servidor; validar cadencia, alcance y enfriamientos |
| Reclamos de copyright | Alto si se publica | Nombres y diseños propios antes de publicar |
| Latencia: el disparo "no pega" | Medio | Predicción en el cliente y validación tolerante en el servidor (margen de distancia y tiempo) |
| Unity sigue cambiando mientras se migra | Medio | Este plan se actualiza con cada avance (sección 10) y los datos se regeneran con el script de la fase 1 |
| Pérdida de datos del jugador | Medio | ProfileStore; no escribir el DataStore en cada cambio |
| UI en celular | Medio | Diseñar para pantalla chica desde la fase 6, no adaptarla al final |

---

## 9. Qué cosas de Unity NO se migran tal cual

- `save.json`, `.tmp`, `.bak` y las migraciones viejas (v1 a v3): en Roblox nadie tiene partidas viejas. Se empieza en la versión que tenga Unity en ese momento.
- `Time.timeScale` y la cámara lenta global.
- Los arreglos específicos de los modelos de Unity (ángulos de la espada de Guts, agarre de las pistolas, rig Generic de Frieren, etc.): se rehacen con los modelos nuevos.
- Los botones y teclas de depuración.
- Los workarounds de pruebas en el editor (reflexión, `EditorApplication.Step`).

---

## 10. Cómo mantener este plan

**Regla:** con **cada avance importante en Unity** (sistema nuevo, personaje, habilidad, jefe, cambio de guardado, cambio grande de balance o de UI), se actualiza este plan en la misma sesión:

1. Agregar o modificar la fila del archivo en la sección 6 (archivo nuevo, destino en Roblox, dificultad, nota).
2. Si cambia `GameEvents`, actualizar la tabla de la sección 4.
3. Si hay assets nuevos en `Assets/Data/`, agregarlos a la sección 6.7.
4. Si el cambio mueve la estimación o agrega un riesgo, actualizar las secciones 1, 7 u 8.
5. Agregar una línea al registro de abajo con la fecha y el commit de Unity.

Cambios chicos (un número de balance, un arreglo visual) **no** hace falta anotarlos: los datos se regeneran con el script.

### Registro de sincronización

Lo más nuevo arriba.

| Fecha | Commit de Unity | Qué se reflejó |
|---|---|---|
| 2026-10-07 | sin commit (sobre `94476e8`) | **Números de daño** (`DamageNumbersUI`, `DamageNumberStyle`, evento `EnemyHit`, `EnemyAI.TakeTickDamage` para los ticks). 615 tests |
| 2026-10-07 | sin commit (sobre `94476e8`) | Enemigos: x1,6 (jefes x1,3), **Volador** y **Lanzador** nuevos, `EnemyProjectile` genérico (antes `BossProjectile`), manadas mixtas de 2 filas de 3 a 5 (`WaveBuilder.Interleave/CountRemaining`, `PackFormation`), mapa 60 x 80 m, **apilado estilo Megabonk** (`EnemyStackRules`, `EnemyCrowd`), arreglo de la escala del `NavMeshAgent`. 609 tests |
| 2026-10-06 | sin commit (sobre `77da566`) | Árbol de Frieren (56 nodos): grupos "elige 1", nodos divididos y cambio de opción (`SkillTreeRules`, `SkillTreeView`), 31 efectos nuevos, `FrierenTreeMath`, `SlowRules`, `EnemyPoison`, evento `PoisonTick`, Marca de maná en `EnemyAI`. 589 tests |
| 2026-10-06 | `e886693` | Plan inicial: Alucard, Frieren y Guts con sus 3 habilidades, árboles de Alucard (19 nodos) y Guts (39), 4 jefes/minijefes, guardado v4, 149 tests |
