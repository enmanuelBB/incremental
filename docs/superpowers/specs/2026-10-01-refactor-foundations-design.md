# Refactor: orden y cimientos para el futuro

Fecha: 2026-10-01 · Estado: **implementado** (sin commit, a pedido del usuario)

> **Desviaciones respecto al diseño original**, descubiertas al implementar:
> - Un asmdef aplica a toda su carpeta, así que `Data/` y `Save/` quedaron **dentro de `Core/`** (`Core/Data`, `Core/Save`) para compartir `Game.Core`. `Game.Runtime` está en la raíz de `Scripts/` y su alcance excluye la carpeta `Core/`, que tiene su propio asmdef.
> - `GameDefinition` solo tiene `id` (con fallback al nombre del asset). `displayName` vive en cada definición que lo necesita; `WeaponDefinition` conserva su campo `weaponName` para no perder datos de los assets existentes.
> - Se agregó `SaveFile` (lectura/escritura/respaldos) separado de `SaveSystem`, para poder probar el archivo con una carpeta temporal.
> - `WaveBuilder` y `WaveGroup`/`WavePlan` viven en `Core/Data`.
> - No se trabajó en una rama `refactor/foundations`: no se hizo commit, así que los cambios quedan en el árbol de trabajo.
> - Se borró `Assets/InputSystem_Actions.inputactions` tras verificar que ningún asset ni `ProjectSettings` lo referenciaba. `UpgradeStation` se conserva (está en la escena); `WeaponInfoStation`/`WeaponInfoUI` se conservan porque las notas del juego dicen que falta conectar ese panel.

## 1. Objetivo y alcance

Dejar el proyecto ordenado y con la base técnica que piden `NOTAS_DEL_JUEGO.md` (jefes, muchos tipos de enemigo, 3 personajes con nivel, prestigio), **sin cambiar cómo se juega hoy**.

**Dentro del alcance**
- Reordenar carpetas y partir el código en assemblies.
- Pasar a ScriptableObjects los datos que hoy viven en prefabs y en la escena: enemigos, oleadas y personajes.
- Guardado versionado, por personaje, con escritura segura y migración desde el formato actual.
- Quitar código muerto, `Instance` sin uso y acoplamientos innecesarios.
- Tests de lo que más duele si se rompe: migración del guardado y armado de oleadas.

**Fuera del alcance** (viene después, sobre esta base)
- Lógica de jefes, nuevos tipos de enemigo, pantalla de selección de personaje, fórmulas de XP, lógica de prestigio, personajes 2 y 3, armas nuevas.

**Criterio de éxito:** el juego se comporta igual que antes (smoke test de la sección 6), el guardado actual del usuario se conserva, y agregar un enemigo, una oleada o un personaje es crear un asset, no editar código.

## 2. Estado actual (resumen)

33 scripts, ~2.000 líneas, un solo assembly implícito (`Assembly-CSharp`).

- **Acoplamiento por `Instance`:** `Shooting`, `WaveManager`, `MoneyManager`, `EnemyPool`, `GameInput`, `UIManager`, `AudioManager`, `GameOverManager`, `UpgradeMenuUI`.
- **Datos en el lugar equivocado:** las oleadas son clases anidadas serializadas en `WaveManager` (en la escena); los stats del enemigo están en campos de `EnemyAI` (en cada prefab); las armas del jugador están en un array de `Shooting`.
- **Guardado:** `SaveData { money, weapons[] }`, sin `version`, escritura directa a `save.json`, armas identificadas por `definition.name` (el nombre del asset: renombrar un asset rompe el guardado). `WeaponState` lee `SaveSystem.Data` estático.
- **Carpetas:** `Managers` mezcla dinero, UI, estaciones y audio; `WeaponInfo*` está en `Player`; `FloatingText.cs` suelto en `Scripts/`; `debug/` en minúscula.
- **Lo que está bien y se conserva:** el bus `GameEvents`, `EnemyPool`, `WeaponDefinition` como SO, los `ResetStatics` por Enter Play Mode sin recarga de dominio, `GameInput` como único punto de lectura de input (usa `Assets/Settings/GameControls.inputactions`).

## 3. Estructura de carpetas y assemblies

```
Assets/Scripts/
  Core/          GameEvents, GameState, IDamageable
  Data/          GameDefinition (base), WeaponDefinition, UpgradeStat/Type,
                 EnemyDefinition, WaveSet, CharacterDefinition
  Save/          SaveSystem, SaveData (v2), SaveMigrations
  Gameplay/
    Player/      PlayerMovement, PlayerHealth, Shooting, CameraFollow, GameInput
    Weapons/     WeaponState
    Enemies/     EnemyAI, EnemyPool
    Waves/       WaveManager, WaveBuilder
    Economy/     MoneyManager
    Base/        BaseHealth, Health
    Interaction/ InteractableStation, MenuStation, ShopTrigger, StartTrigger,
                 UpgradeStation, WeaponInfoStation
    Audio/       AudioManager
    Debug/       DebugCheats
  UI/            UIManager, MenuPanel, UpgradeMenuUI, WeaponInfoUI,
                 FloatingText, FloatingTextManager, GameOverManager
Assets/Data/     Weapons/  Enemies/  Waves/  Characters/
Assets/Tests/EditMode/
```

**Assemblies (2, no 4):**

| Assembly | Contiene | Referencias |
|---|---|---|
| `Game.Core` | `Core/`, `Data/`, `Save/` | solo Unity |
| `Game.Runtime` | `Gameplay/`, `UI/` | `Game.Core`, `Unity.InputSystem`, `Unity.TextMeshPro`, `UnityEngine.UI` |
| `Game.Tests.EditMode` | tests | `Game.Core`, Test Framework |

Se descartó separar `Gameplay` y `UI`: las estaciones (Gameplay) abren menús (UI) y la UI consulta `Shooting`/`MoneyManager` (Gameplay), lo que crearía una dependencia circular. Dividir en dos assemblies sí aísla lo importante: datos y guardado se compilan y se prueban sin el resto.

`Game.Core` no puede referenciar tipos de `Game.Runtime`. Por eso `EnemyDefinition` guarda el prefab como `GameObject` (no como `EnemyAI`), y el pool hace `GetComponent<EnemyAI>()` como ya hace hoy.

**Movimiento de archivos:** siempre con su `.meta` (git mv del par), para que la escena y los prefabs conserven las referencias por GUID.

## 4. Datos como ScriptableObjects

Todos heredan de `GameDefinition : ScriptableObject` con `id` (string estable, único, editable) y `displayName`.

**Por qué `id` explícito:** hoy el guardado usa el nombre del asset. Con `id` se puede renombrar o reorganizar assets sin romper progresos. Migración: el `id` de las armas existentes se inicializa con el nombre actual del asset (`M16`, `Pistola`).

### 4.1 `EnemyDefinition`
`prefab (GameObject)`, `maxHealth`, `speed`, `moneyReward`, `damageToBase`, `damageToPlayer`, `damageInterval`, `attackReach`, `detectionRange`, `chaseGiveUpTime`, `giveUpCooldown`, `directionTolerance`, `repathInterval`, `isBoss (bool, solo etiqueta)`.

- `EnemyAI` deja de tener esos campos serializados: recibe la definición en `Init`/`Spawn` y la lee.
- El prefab conserva `NavMeshAgent`, collider y `EnemyAI`. Un mismo prefab puede servir a varias definiciones (variantes de stats sin prefab nuevo).
- Se crean 2 assets (`Enemy_Normal`, `Enemy_Tank`) con **exactamente los valores que hoy tienen los prefabs** `enemy.prefab` y `EnemyTank.prefab`.
- `EnemyPool` pasa a indexar por `EnemyDefinition`.
- `ChooseTarget` (anti-kiting) no cambia. La IA sigue siendo la misma máquina implícita; separar comportamientos por tipo se hará cuando exista el segundo comportamiento real (hoy sería especulativo).

### 4.2 `WaveSet`
- `waves[]`: cada una con `name`, `groups[] { EnemyDefinition enemy, int count }`, `spawnInterval`.
- `scaling`: `healthMultiplierPerWave`, `extraEnemiesPerWave`, `infiniteSpawnInterval` (los valores actuales).
- `WaveBuilder` (C# puro, sin `MonoBehaviour`) recibe `WaveSet` + índice y devuelve `{ groups, healthScale, interval }`, incluyendo el caso "después de la última horda". También contiene `MergeGroups` (arrastre de enemigos al saltar horda).
- `WaveManager` queda con la corrutina de spawn, el conteo de enemigos vivos y los eventos. Mismo comportamiento, pero la matemática de oleadas queda **probable en EditMode**.
- El modelo "infinito" actual sigue siendo el comportamiento por defecto. Los jefes por nivel se agregarán como un campo/regla de `WaveSet` cuando se implementen; con este diseño es agregar datos a un asset, no reescribir el manager.

### 4.3 `CharacterDefinition`
`id`, `displayName`, `unlockPrice`, `startingWeapons[] (WeaponDefinition)`.

- Se crea el asset `Alucard` con las armas actuales.
- `Shooting` toma sus armas del `CharacterDefinition` activo en lugar de su array propio. **La selección de personaje queda como un campo serializado** (`Alucard` asignado en la escena); la UI de selección es trabajo futuro.
- `PlayerHealth` y `PlayerMovement` **no cambian**: siguen con los valores del Inspector. Los stats por personaje (vida, velocidad, etc.) se agregan a `CharacterDefinition` cuando se diseñen los personajes 2 y 3; agregarlos hoy sería un campo que nadie lee.

### 4.4 `WeaponState` desacoplado
`new WeaponState(WeaponDefinition def, WeaponSave save)`: ya no busca en `SaveSystem.Data`. Quien lo crea (`Shooting`, vía el personaje) le entrega el `WeaponSave` correspondiente. Esto permite que cada personaje tenga sus armas y niveles.

## 5. Guardado v2

```
SaveData {
  int   version = 2
  int   money                      // global
  int   prestigeCoins              // 0; reservado, sin lógica todavía
  string selectedCharacterId       // "alucard"
  List<CharacterSave> characters
}
CharacterSave { string id; bool unlocked; int level; List<WeaponSave> weapons }
WeaponSave    { string id; bool owned; int[] upgradeLevels }
```

- **Migración v1 → v2** (`SaveMigrations`, funciones puras): el formato actual (sin `version`, `money` + `weapons[]`) se convierte en un único `CharacterSave` `alucard` con `unlocked = true` y esas armas. Se mantiene el soporte para progreso aún más viejo en PlayerPrefs y los ids `legacy_{n}`.
- **Un solo respaldo previo:** antes de migrar por primera vez se copia `save.json` a `save.v1.bak` (nunca se sobrescribe).
- **Versión mayor que la conocida:** no se carga ni se sobrescribe; se avisa en consola y se juega con un guardado vacío en memoria sin tocar el archivo.
- **Escritura segura:** escribir `save.json.tmp` → `File.Replace(tmp, save.json, save.json.bak)` si existe el archivo (o `File.Move` si no). En Windows el reemplazo no es atómico del todo, pero `.bak` garantiza recuperación.
- **Carga defensiva:** `save.json` → si falla, `save.json.bak` → si falla, vacío. Validar rangos (dinero ≥ 0, niveles dentro de `maxLevel`).
- **API pública sin cambios** (`SaveSystem.Data`, `Save()`, `Delete()`), así `MoneyManager`, `GameOverManager` y `DebugCheats` no se tocan por esto. `JsonUtility` se mantiene (sin dependencias nuevas).
- **Puntos de guardado** iguales a hoy: fin de horda, compra, game over, pausa/salida.
- **`Delete()`** (debug P) borra también `.bak` y `.tmp`.

## 6. Limpieza, riesgos y verificación

**Limpieza**
- Auditar con grep cada `Instance`; quitar los que no usa nadie. El aviso de la tienda (`ShopTrigger` → `UIManager`) pasa a un evento `GameEvents.PromptChanged(string)`, en línea con el HUD guiado por eventos.
- `UpgradeStation` y `WeaponInfoStation` (5 líneas): **no se borran sin confirmar** en la escena (por GUID) que no estén asignados; si son marcadores de `InteractableStation`, se quedan.
- `Assets/InputSystem_Actions.inputactions` parece la plantilla de Unity sin uso (`GameInput` usa `GameControls`). Se verifica que ningún asset lo referencie por GUID y **se lista antes de borrar**.
- `DebugCheats` sigue compilado solo en Editor/Development build.

**Riesgos y mitigaciones**
- *Referencias rotas en la escena/prefabs al mover o cambiar campos serializados.* → Mover siempre con `.meta`; los valores de `EnemyAI` y de `WaveManager` se migran con un script de editor de un solo uso que copia los datos a los assets antes de borrar los campos; todo en una rama; compilar y abrir la escena tras cada fase.
- *Perder el progreso del usuario.* → Respaldo `save.v1.bak` antes de migrar + tests de migración con el JSON real.
- *Cambiar el comportamiento sin querer.* → Los valores iniciales de los assets se copian de los prefabs/escena, no se reescriben; smoke test manual al cierre de cada fase.

**Verificación (por fase y al final)**
1. Sin errores de compilación (`unity_get_compilation_errors`) y sin errores en la consola al entrar a Play.
2. EditMode: migración v1→v2 (incluye legacy y versión mayor), carga con `.bak`, `WaveBuilder` (hordas definidas, hordas infinitas, arrastre al saltar).
3. Smoke test en Play: disparar al cubo de inicio → matar enemigos (dinero +10 y texto flotante) → completar horda → comprar M16 → mejorar cadencia → salir y volver a entrar (el progreso persiste) → perder la base (Game Over y reinicio) → saltar horda (N).
4. Abrir la escena con el `save.json` real del usuario y comprobar dinero y armas intactos.

## 7. Orden de trabajo (cada fase deja el juego funcionando)

0. Rama `refactor/foundations`, respaldo del `save.json`, compilación base limpia.
1. Mover archivos y crear asmdefs (sin cambiar código).
2. Evento de aviso de tienda, limpieza de `Instance` sin uso.
3. Guardado v2 + migraciones + tests.
4. `GameDefinition`/ids, `EnemyDefinition` + assets, `WaveSet` + `WaveBuilder` + tests, `CharacterDefinition` y `WeaponState` desacoplado.
5. Código muerto, `README` con la arquitectura, actualizar el grafo de graphify.
