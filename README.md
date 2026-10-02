# incremental

Shooter en primera/tercera persona con estilo de tower defense incremental (Unity 6).

## Estructura del código

```
Assets/Scripts/
  Core/            Game.Core (no depende de nada del juego)
    Data/          ScriptableObjects: WeaponDefinition, StaffDefinition, EnemyDefinition,
                   WaveSet, CharacterDefinition + lógica pura: WaveBuilder, CombatMath,
                   CharacterRules, CharacterInfo, ManaPool, AbilityCooldown
    Save/          SaveData (v2), SaveMigrations, SaveFile, SaveSystem
    GameEvents.cs  bus de eventos; GameState.cs; IDamageable.cs
  Gameplay/        Game.Runtime
    Player/ Weapons/ Enemies/ Waves/ Economy/ Base/ Interaction/ Characters/ Audio/ Debug/
  UI/              Game.Runtime (HUD, menús, game over)
Assets/Data/       Weapons/  Enemies/  Waves/  Characters/   (los assets de datos)
Assets/Tests/EditMode/   tests del guardado y de las hordas
```

- **Agregar un enemigo:** Create > Game > Enemy, asignarle un prefab (con `NavMeshAgent` y `EnemyAI`) y ponerlo en un grupo de un `WaveSet`.
- **Cambiar las hordas:** editar `Assets/Data/Waves/Waves_Default.asset`. Después de la última horda definida se generan solas (ver `WaveBuilder`).
- **Agregar un arma o personaje:** Create > Game > Weapon / Staff / Character. El `id` del asset es lo que usa el guardado; no lo cambies cuando ya existan partidas.
- **Agregar un personaje al menú:** crear su `CharacterDefinition` (vida, velocidad, armas, cómo se desbloquea) y ponerlo en el elenco de `CharacterManager` en la escena; el menú de selección se arma solo.
- **Balance:** los números de armas y personajes son datos de los assets. `BalanceTests` (Assets/Tests/EditMode) comprueba que ningún personaje se descompense; córrelos después de cambiar un valor.
- **Comunicación:** los sistemas se hablan por `GameEvents` (la UI solo escucha eventos). `Game.Core` no puede referenciar a `Game.Runtime`.

## Guardado

`save.json` en `Application.persistentDataPath`, con campo `version`. Al cargar un formato viejo se migra y se deja una copia previa en `save.v1.bak`. Se escribe a un `.tmp` y se reemplaza, dejando el anterior como `save.json.bak`; si el principal está dañado se recupera el `.bak`.

## Controles de desarrollo (solo Editor / Development build)

`O` dinero · `P` borrar progreso · `N` saltar horda · `U` desbloquear todos los personajes
