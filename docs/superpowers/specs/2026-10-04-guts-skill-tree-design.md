# Guts: árbol de habilidades

Fecha: 2026-10-04 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (a pedido del usuario, él decide cuándo). Continúa `2026-10-02-skill-tree-design.md` (el árbol de Alucard) y las habilidades de Guts (`...-guts-flame-burst`, `...-guts-dash`, `...-guts-berserk`).

## 1. Qué se quiere
Un **árbol de mejoras para Guts**, igual de funcional que el de Alucard (pestaña "Árbol" de la estación de habilidades, puntos del personaje, nodos conectados), con mejoras para sus habilidades, su espada y su Furia. Pedidos concretos del usuario:
- Una mejora de **más daño a los enemigos aturdidos**.
- La **probabilidad de stun máxima es 30% en total** entre tienda y árbol: se **reparte 15% en la tienda y 15% en el árbol**.
- La **E (dash) con dos cargas**.
- Una mejora para que el dash **dispare 2 veces** después de hacerlo y otra para que dispare **3 veces**.
- **Ninguna mejora que baje el drenaje de vida de la armadura** (para que no se pueda tener siempre activa). Por eso también el rango de la armadura deja de bajar el drenaje.

## 2. Alcance
- **Entra:** el asset del árbol (`Guts_Tree`), los efectos nuevos y su efecto en el juego, el cambio de la tienda de stun (3% por nivel, 15% al máximo), el tope total de 30%, la armadura sin reducción de drenaje por rango, pruebas.
- **No entra:** efectos visuales nuevos, el modelo de la armadura, sonidos, cambios a la vista del árbol (ya es genérica: dibuja cualquier árbol con sus posiciones).

## 3. Cambios previos a la tienda y a la armadura
- **Stun:** `SwordDefinition.reloadUpgrade.step` pasa de 0,06 a **0,03** (5 niveles = **15%** en tienda). `StunChanceAt(level, treeBonus)` suma el bono del árbol y **no pasa de `stunChanceCap` = 0,30** (campo nuevo). El árbol da hasta +15% (0,15), así que con todo comprado llega justo a 30%.
- **Armadura:** `Armadura.asset` `drainPerRank` pasa de 0,005 a **0**: el drenaje es **2% en los 3 rangos** (los rangos siguen subiendo el daño y bajando el enfriamiento). No hay efecto de árbol que lo reduzca.

## 4. Los nodos (39)
Posiciones, como en el de Alucard: el **centro** en (0, 0); la rama **Impacto** hacia arriba; la **Q** a la izquierda; la **E** a la derecha; la **F** abajo; Vitalidad, Zancada y Filo en los lados. Los costos van de 1 a 8 (total ≈ 170 puntos). Cada nodo se conecta al anterior de su cadena y los de entrada a un nodo del centro.

| Rama | Nodos (id: nombre · costo · efecto) |
|---|---|
| **Centro** | `core`: Dragonslayer · 1 · +5% de daño (raíz) |
| **Vitalidad** | `v1` I · 1 · +10 vida · `v2` II · 2 · +15 · `v3` III · 3 · +20 |
| **Zancada** | `s1` I · 1 · +3% velocidad · `s2` II · 2 · +3% · `s3` III · 3 · +4% |
| **Filo** (daño) | `d1` · 1 · +3% · `d2` · 2 · +3% · `d3` · 2 · +3% · `d4` · 6 · +4% (el daño de la espada, la Q y la E) |
| **Impacto** (stun) | `i1` · 2 · +3% stun · `i2` · 3 · +3% · `i3` · 4 · +3% · `i4` · 5 · +3% · `i5` · 6 · +3% (**+15% en total**) |
| **Verdugo** | `ve1` Verdugo · 5 · +15% de daño a enemigos aturdidos · `ve2` Verdugo II · 7 · +15% más (**+30%**) |
| **Rabia** (Furia) | `r1` · 4 · +25% de Furia por golpe · `r2` · 6 · +25% más |
| **Alma voraz** | `a1` · 4 · las almas curan +1 punto de % (2% → 3%) · `a2` · 6 · +1 más (4%) |
| **Q Llamarada** | `q1` Brasas · 3 · +2 s de quemadura · `q2` Llama ancha · 3 · +30° de cono · `q3` Infierno · 5 · +50% de daño de la quemadura · `q4` Llama larga · 4 · +2 m de alcance · `q5` Brasas II · 6 · +2 s más · `q6` Infierno II · 8 · +50% más |
| **E Embestida** | `e1` **Doble carga** · 4 · +1 carga (2 cargas, se recargan de una en una) · `e2` **Disparo doble** · 5 · +1 disparo (2) · `e3` **Disparo triple** · 8 · +1 disparo más (3), requiere `e2` · `e4` Zancada de embestida · 3 · +1,5 m de dash · `e5` Recarga veloz · 3 · −1 s de enfriamiento · `e6` Recarga veloz II · 6 · −1 s más |
| **F Armadura** | `f1` Rugido atronador · 4 · +1 s de aturdimiento y +1,5 m de radio · `f2` Ira · 5 · +10 puntos de daño de la armadura (x1,4 → x1,5) · `f3` Piel de hierro · 6 · −10 puntos de daño recibido (x0,5 → x0,4) · `f4` Rugido atronador II · 6 · +1 s y +1,5 m más · `f5` Ira II · 8 · +10 puntos más |

(Los valores son supuestos de diseño, ajustables en el asset. La armadura **no tiene** nodos de drenaje ni de enfriamiento.)

## 5. Efectos nuevos
Se agregan **al final** de `SkillEffectType` (los valores guardados de Alucard no se mueven) y se suman en `TreeBonuses` por `SkillTreeRules.Compute`:

| Efecto | Campo de `TreeBonuses` | Dónde actúa |
|---|---|---|
| `StunChance` | `StunChanceBonus` (ya existe) | `WeaponState.StunChance` (con el tope de 30%) |
| `StunnedDamagePercent` | `StunnedDamagePercent` | espada, Q y E: el daño a un enemigo ya aturdido x(1 + bono) |
| `FuryGainPercent` | `FuryGainPercent` | `Shooting.SwingSword`: la Furia que carga cada golpe x(1 + bono) |
| `SoulHealBonus` | `SoulHealBonus` | `SoulSpawner`: fracción de curación + bono |
| `FlameBurnSeconds` | `FlameBurnSecondsBonus` | `FlameBurst`: segundos de quemadura |
| `FlameCone` | `FlameConeBonus` | `FlameBurst`: grados del cono |
| `FlameBurnDamagePercent` | `FlameBurnDamagePercent` | `FlameBurst`: daño por tick x(1 + bono) |
| `FlameRange` | `FlameRangeBonus` | `FlameBurst`: alcance |
| `DashExtraCharges` | `DashExtraCharges` | `PlayerAbilities.RefreshBuild`: cargas de la E |
| `DashExtraShots` | `DashExtraShots` | `GutsDash`: disparos tras el dash |
| `DashDistance` | `DashDistanceBonus` | `GutsDash`: metros |
| `DashCooldown` | `DashCooldownReduction` | `EffectiveCooldown` de la E |
| `BerserkDamage` | `BerserkDamageBonus` | `BerserkArmor`: multiplicador de daño |
| `BerserkDamageTaken` | `BerserkDamageTakenReduction` | `BerserkArmor`: multiplicador de daño recibido (mínimo 0,1) |
| `RoarStun` / `RoarRadius` | `RoarStunBonus` / `RoarRadiusBonus` | `BerserkArmor.Roar` |

## 6. Las dos cargas y los disparos múltiples de la E
- **Cargas:** `RefreshBuild` da `1 + DashExtraCharges` cargas a la casilla de la E (como la Q de Alucard con su árbol); se recargan **una a una** cada enfriamiento. El dash no se superpone: no se puede lanzar otro mientras uno está en curso.
- **Disparos:** al levantarse dispara `1 + DashExtraShots` veces (hasta 3), con **0,12 s** entre disparo y disparo. **Cada disparo apunta a un enemigo distinto**: el 1.º más cercano, el 2.º más cercano, el 3.º; si hay menos enemigos que disparos, los que sobran repiten sobre el más cercano. **Cada disparo hace el daño completo** (espada × 3 por rango). Sin enemigos, no dispara.
- **Furia llena:** potencia **toda la salva** (todos los disparos x2) y gasta la barra una sola vez; sin objetivo no la gasta.
- **Lógica pura (Game.Core, con tests):** `TargetPicker.PlanShots(Vector3 origin, IList<Vector3> candidates, float maxRange, int shots)` devuelve los índices de los objetivos en orden (distintos del más cercano al más lejano; los sobrantes repiten el más cercano) o una lista vacía si no hay ninguno al alcance.

## 7. Daño a aturdidos
Un enemigo cuenta como aturdido si `EnemyAI.IsStunned`. El bono se aplica al calcular el daño a **cada enemigo** en la espada, la Q y la E, justo antes de `TakeDamage`. En el golpe de espada el aturdimiento de ese mismo golpe se aplica **después** del daño, así que el bono cuenta para enemigos que ya estaban aturdidos de antes (incluida la Furia llena de un golpe anterior).

## 8. Pruebas
Tests de EditMode (primero):
- `TargetPicker.PlanShots`: 1 enemigo y 3 disparos → [0,0,0]; 3 enemigos y 3 disparos → los 3 en orden de cercanía; 2 enemigos y 3 → [más cercano, 2.º, más cercano]; sin enemigos o fuera de alcance → vacío; `shots` ≤ 0 → vacío.
- `SwordDefinition`: stun 3% por nivel (0, 3, 15% al nivel 5); con el bono del árbol de 15% → 30%; nunca pasa de 30% aunque el bono sea mayor; valores del asset (`stunChanceCap` 0,3).
- `SkillTreeRules.Compute` con los efectos nuevos (cada uno suma al campo correcto; efectos desconocidos se ignoran como ya pasa).
- **Asset `Guts_Tree`** (como los tests de Alucard): ids únicos, conexiones válidas, todos alcanzables desde la raíz, costos ≥ 1, un solo nodo raíz, **la suma de los `StunChance` es exactamente 0,15** (así con la tienda al máximo el total es 30%), **ningún nodo reduce el drenaje de la armadura** (no existe ese efecto), `e3` requiere `e2` (cadena), `Guts.asset` apunta al árbol, y el texto de cada nodo no está vacío.
- `Armadura.asset`: `drainPerRank` = 0 y `BerserkDrainAt(rank)` = 2% en los rangos 1, 2 y 3.
- Los tests existentes que citaban el stun de 6% (30% al nivel 5) se actualizan a 3% (15%).

En Play (con `save.json` respaldado y restaurado; bucles con tope de pasos):
- La pestaña "Árbol" aparece con Guts (el árbol carga, 39 nodos) y con Alucard sigue igual.
- Comprar `i1`..`i5` y la tienda al máximo: la probabilidad de stun es 30% y no pasa de ahí.
- `ve1`: el daño a un enemigo aturdido sube 15%, a uno no aturdido no.
- `e1`: la E tiene 2 cargas (dos dashes seguidos, el tercero se bloquea hasta que se recarga una).
- `e2` y `e3`: tras el dash salen 2 y 3 disparos a enemigos distintos (con 3 enemigos, uno cada uno; con 1, los tres al mismo); con Furia llena todos x2 y la barra queda en 0.
- Las ramas de Q (quemadura más larga, cono y alcance mayores), Rabia, Alma voraz y las de la F (rugido, daño, daño recibido) hacen lo que dicen.
- La armadura drena 2% (3 por segundo con 150) en los rangos 1 y 3.
- **No verificado hasta que lo pruebe el usuario:** la vista del árbol con las posiciones (cómo se ve y se desplaza), clic real en los nodos y el balance.

## 9. Riesgos y decisiones abiertas
- **Balance:** tres disparos de espada × 3 a tres enemigos con dos cargas es mucho daño en área concentrada; los números (y que cada disparo haga el daño completo) son ajustables en el asset y en `GutsDash`.
- **Los 15% de stun del árbol son 5 nodos de 2 a 6 puntos** (20 puntos en total): llegar al 30% completo cuesta bastante a propósito.
- Los costos y valores de los 39 nodos son supuestos míos.
- La armadura sin reducción de drenaje queda como una decisión explícita; si más adelante se quiere reducirlo, es un efecto nuevo (no existe hoy).
