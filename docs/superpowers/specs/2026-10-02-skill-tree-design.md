# Árbol de habilidades por personaje

Fecha: 2026-10-02 · Estado: **diseño aprobado por el usuario en conversación e implementado el mismo día (sin commit, por pedido suyo).** Los números finales están en el asset; ver `notas/DECISIONES.md` D30 y `notas/REGISTRO_DE_CAMBIOS.md`.

## 1. Qué se quiere
Cada personaje tiene un **árbol de nodos** para mejorarlo, con una moneda propia que se gana jugando con él. Hay nodos pequeños de stats (vida, velocidad, daño, en cantidades bajas) y, repartidos por el árbol, nodos fuertes que cambian una habilidad. Ejemplos del usuario: la **E** (niebla) al atravesar a un enemigo le aplica sangrado y lo ralentiza; la **Q** (disparo pesado) dispara dos balas en vez de una, se puede lanzar 2 veces seguidas y, con otra mejora, 3; la **F** (definitiva) gana duración y pierde enfriamiento.

## 2. Reglas decididas por el usuario
- **Tres sistemas separados:** (1) nivel de personaje: XP y 1 punto por nivel para rangos de habilidad y sangrado, **sin cambios**; (2) este árbol, con los **"puntos de Alucard"** (uno por personaje); (3) el árbol de prestigio, futuro y **global**, sin relación con este.
- **Cómo se ganan los puntos:** al **completar una oleada**, `1 + oleada / 5` (1 en las oleadas 1 a 4, 2 en las 5 a 9, 3 en las 10 a 14...). Si el personaje muere a mitad de una oleada, esa no da puntos. Solo los gana el personaje que se está jugando.
- **No dependen del nivel** y **se conservan tras el prestigio** (el prestigio, cuando exista, no debe tocar `skillPoints` ni `skillNodes`).
- **Forma:** un **grafo**. Un nodo se puede comprar si es **raíz** o si **algún vecino conectado ya está comprado**, y si alcanzan los puntos. Cada nodo se compra **una sola vez**.
- **Reinicio gratis:** un botón devuelve todos los puntos gastados, cuando se quiera (antes de la primera oleada).
- **Dónde:** una pestaña **"Árbol"** en la estación de habilidades (`PointsStation` / `AbilityShopUI`), junto a "Habilidades". Solo antes de la primera oleada, como todas las estaciones. La pestaña no aparece si el personaje no tiene árbol (Frieren por ahora).
- **Datos, no código:** el árbol de cada personaje es un asset (`SkillTreeDefinition`); para otro personaje basta crear otro asset.

## 3. Datos (`Game.Core`)
- `SkillTreeDefinition : GameDefinition` con `SkillNode[] nodes`.
- `SkillNode`: `id` (único), `displayName`, `description`, `cost`, `position` (en unidades de cuadrícula), `connections` (ids de vecinos), `isRoot`, `effects`.
- `SkillEffect`: `type` (`SkillEffectType`) y `value`.
- `SkillEffectType`: `MaxHealth` (suma vida), `MoveSpeedPercent`, `DamagePercent` (fracciones: 0,03 = +3%), `HeavyShotExtraBullets`, `HeavyShotExtraCharges`, `HeavyShotCooldown` (segundos que baja), `MistBleedOnPass` (pilas), `MistSlow` (fracción de velocidad que quita), `MistDuration` (segundos que suma), `MistCooldown`, `UltDuration`, `UltCooldown`, `UltLifeSteal` (suma a la fracción de robo de vida).
- `CharacterDefinition.skillTree` apunta al asset (puede ser null).

## 4. Lógica pura (`Game.Core`, con tests)
- `SkillTreeRules`: `IsUnlocked`, `CanBuy` (`SkillBuyBlock`: `None`, `UnknownNode`, `Owned`, `Locked`, `NoPoints`), `TryBuy`, `Reset` (devuelve la suma de costos de lo comprado y vacía la lista), `Compute` (suma los efectos de los nodos comprados en un `TreeBonuses`; los ids que ya no existen en el asset se ignoran) y `PointsForWave(wave)`.
- `TreeBonuses`: un valor por cada tipo de efecto, con valores neutros por defecto (`DamageMultiplier` = 1 + suma, `SpeedMultiplier`, `MaxHealthBonus`, etc.).
- `AbilityCharges`: cargas con recarga de una en una (`TryUse`, `Available`, `Remaining`, `Configure`). Con 1 carga se comporta como el enfriamiento de hoy.
- `CharacterSave`: `skillPoints` (saldo) y `skillNodes` (ids comprados, sin repetidos).

## 5. Guardado v4
`SaveData.CurrentVersion = 4`. La forma v3 solo gana campos (`skillPoints = 0`, `skillNodes` vacía), así que la migración v3 a v4 es cargar con esos valores por defecto y normalizarlos (como v2 a v3). Copia previa a `save.v3.bak` (mecanismo existente).

## 6. Efectos en el juego
- **`SkillTreeManager`** (escena): escucha `WaveCompleted` y suma `PointsForWave(oleada)` al personaje activo; ofrece `Buy(id)` y `ResetTree()` (guardan, recalculan bonos y reaplican stats); expone `Bonuses`. Publica `GameEvents.SkillPointsChanged` y `SkillPointsGained` (para el aviso "+N puntos de Alucard").
- **Vida y velocidad:** `CharacterManager.Apply` usa `maxHealth + MaxHealthBonus` y `moveSpeed x SpeedMultiplier`; `ReapplyStats()` tras comprar o reiniciar.
- **Daño:** `WeaponState.Damage` se multiplica por `DamageMultiplier` (mínimo el valor base).
- **Q, balas:** con `HeavyShotExtraBullets`, el disparo pesado sale como 1 + N balas separadas 0,12 s, cada una con su daño y su sangrado.
- **Q, cargas:** `HeavyShotExtraCharges` suma cargas (`AbilityCharges`); entre dos usos seguidos hay una pausa mínima de 0,35 s. El HUD muestra el número de cargas cuando son más de una.
- **E (niebla):** durante la niebla el jugador **atraviesa a los enemigos** (se ignoran las colisiones con los enemigos cercanos y se restauran al terminar; es el comportamiento base de la niebla, no un nodo). Al tocar a un enemigo por primera vez en ese uso recibe `MistBleedOnPass` pilas y una ralentización de `MistSlow` durante `mistSlowSeconds` (campo nuevo de `AbilityDefinition`, 2 s). Para esto `EnemyAI.ApplySlow`.
- **F:** `UltDuration`, `UltCooldown` y `UltLifeSteal` se suman a los valores del rango. `HeavyShotCooldown`, `MistDuration` y `MistCooldown` igual (el enfriamiento nunca baja de 1 s).
- **Los rangos por nivel siguen igual;** los bonos del árbol se suman encima.

## 7. Pantalla (`AbilityShopUI`)
- Dos pestañas arriba: "Habilidades" (lo de hoy) y "Árbol" (solo si el personaje tiene árbol).
- El árbol se dibuja por código: cuadrados en la posición de cada nodo (la escala se ajusta sola al espacio disponible), líneas entre conectados, y colores por estado: comprado (amarillo), comprable (azul), bloqueado (gris), sin puntos suficientes (azul apagado).
- Al pasar el ratón o seleccionar con teclado/mando, un panel inferior muestra nombre, descripción y costo. **Clic o Aceptar compra el nodo** (el reinicio es gratis, así que un error no cuesta).
- Arriba: "Puntos de Alucard: N" y el botón "Reiniciar árbol". Con ratón, teclado y mando.

## 8. Árbol inicial de Alucard (19 nodos, 56 puntos en total; números ajustables en el asset `Assets/Data/Skills/Alucard_Tree.asset`)
Raíz al centro, tres ramas (Q a la izquierda, E a la derecha, F abajo) y nodos de stats que las unen, de modo que para llegar a una mejora fuerte se pasa por nodos pequeños.

| Nodo | Efecto | Costo |
|---|---|---|
| Sed de sangre (raíz) | +3% daño | 1 |
| Vitalidad I / II | +5 vida cada uno | 1 / 2 |
| Zancada I / II | +4% velocidad | 1 / 2 |
| Pulso I / II / III | +5% daño | 1 / 2 / 2 |
| Bala gemela (Q) | +1 bala | 3 |
| Doble carga (Q) | +1 carga | 4 |
| Triple carga (Q) | +1 carga | 6 |
| Recarga veloz (Q) | -1,5 s de enfriamiento | 3 |
| Filo de niebla (E) | +2 pilas de sangrado al atravesar | 3 |
| Escarcha (E) | ralentiza 30% | 3 |
| Niebla densa (E) | +1 s de duración | 4 |
| Disipación (E) | -2 s de enfriamiento | 3 |
| Sangre prolongada (F) | +1,5 s de duración | 4 |
| Frenesí (F) | -8 s de enfriamiento | 5 |
| Sed eterna (F) | +15 puntos de robo de vida | 6 |

> Cambios respecto al diseño original, ya implementados: el daño pasó de +2/3% a +3% (raíz) y +5% (Pulsos) y la velocidad a +4%, porque con balas de 10 a 20 de daño un +3% se redondeaba a nada; no hay "Vitalidad III" (con 19 nodos el árbol queda legible).

## 9. Fuera de alcance
Prestigio y su árbol; árboles de otros personajes (el sistema ya lo permite); arte y sonido finales; el resumen al morir.

## 10. Pruebas
Tests de EditMode para las reglas del grafo, el cálculo de bonos, el reinicio, los puntos por oleada, `AbilityCharges` y la migración v3 a v4. Después verificación en Play con el árbol real y con el guardado respaldado.
