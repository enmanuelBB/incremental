# Espada de Guts y aturdimiento

Fecha: 2026-10-04 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (a pedido del usuario, él decide cuándo).

## 1. Qué se quiere
Guts hoy solo reproduce el corte, sin daño. Se quiere su **ataque básico real**: un golpe de espada **en área frente a él**, mejorable en la tienda de dinero con **daño**, **rapidez entre golpes** y **probabilidad de aturdir (stun)**. Con la tienda al máximo el stun llega al **30%**; más adelante, el árbol de Guts con sus minimejoras lo subirá hasta **50%** en total (+20 puntos).

Pedidos concretos del usuario:
- **No se agrega un arma al diseño del personaje** (ni modelo ni prefab): el modelo ya trae la espada y su ataque va de izquierda a derecha.
- **La respuesta al clic debe sentirse instantánea:** la animación de ataque y el golpe son rápidos. En cambio, el **tiempo entre ataques es largo** y es lo que se acorta con la tienda.

## 2. Alcance
- **Entra:** golpe en área con daño, aturdimiento en enemigos, mejoras de tienda (cadencia, daño, aturdir), pruebas.
- **No entra:** árbol de Guts (hay un campo reservado en `TreeBonuses`, vale 0), habilidades de Guts, combos, bloqueo, efecto visual del stun, sonidos.

## 3. Datos: `SwordDefinition : WeaponDefinition`
Un asset `Assets/Data/Weapons/Espada.asset`. Es solo datos: sin modelo, sin `heldItemPrefab`. `Guts.asset` la lleva en `startingWeapons`.

- `UsesAmmo` = false y `AppliesBleed` = false (como el bastón).
- Campos propios (todos ajustables en el asset):

| Campo | Valor inicial | Qué es |
|---|---|---|
| `range` (ya existe) | 3 m | Alcance del cono |
| `arcDegrees` | 120° | Apertura del cono |
| `hitDelay` | ~0,12 s | Segundos reales desde el clic hasta que se aplica el daño. Se afina en Play viendo el corte |
| `attackAnimSpeed` | 2,6 | Multiplicador de la animación de ataque (hoy el estado `Attack` va a 1,3 y dura ~0,87 s; con 2,6 dura ~0,44 s) |
| `stunSeconds` | 1,5 s | Duración del aturdimiento |

- Las mejoras reutilizan los 3 espacios de `UpgradeType` (igual que el bastón: cambian el nombre, no el enum):

| Mejora | Espacio | Efecto | Valores iniciales |
|---|---|---|---|
| **Cadencia** | `FireRate` | Segundos **entre golpes** (lento al inicio) | base 1,2 s, −0,12 s por nivel, mínimo 0,6 s, 5 niveles |
| **Aturdir** | `Reload` | +6% de probabilidad por nivel | 5 niveles, **30% al máximo** |
| **Daño** | `Damage` | Suma daño por nivel | base 30, +5 por nivel, 10 niveles |

  Las etiquetas salen de `SwordDefinition.UpgradeLabel`. Los niveles se guardan donde ya se guardan (`WeaponSave.upgradeLevels` por personaje y arma), así que **no cambia el guardado**.
- Fórmulas puras en la definición (Game.Core, con tests): `StunChanceAt(level, treeBonus)` = `min(1, nivel × 0,06 + treeBonus)`.
- `TreeBonuses.StunChanceBonus` (float, 0 por ahora). Cuando exista el árbol de Guts, sus nodos lo suman (tope total 50%).

## 4. El golpe
- Hoy `Shooting.Update` manda a Guts a la rama "sin armas" (solo anima). Se reemplaza por `UpdateSword`:
  1. Con el clic y el tiempo entre golpes cumplido (`WeaponState.FireRate`), se llama a `Body.PlayAttack()` (con la velocidad `attackAnimSpeed`) y se agenda el daño `hitDelay` segundos después.
  2. Al cumplirse `hitDelay` se aplica el daño: a todos los enemigos vivos dentro del cono (cada uno recibe **un** golpe).
- **Velocidad de la animación:** el controlador `Guts.controller` gana el parámetro float `AttackSpeed`, usado como `speedParameter` del estado `Attack` (misma técnica que `ReloadSpeed` en Alucard). `PlayerBody.PlayAttack(speed)` lo fija antes de disparar el trigger.
- **Daño:** `WeaponState.Damage` (ya incluye el multiplicador de daño del árbol).
- **Cono (lógica pura, Game.Core):** `MeleeCone.Contains(origen, direccionPlana, punto, alcance, arcoGrados)`: distancia horizontal ≤ alcance + radio del enemigo y ángulo respecto a la mira ≤ arco/2. Se buscan candidatos con `Physics.OverlapSphere` y se filtra con el cono. La dirección es la de la mira, aplanada.
- **Cancelaciones:** si Guts muere, se cambia de personaje o se pausa/bloquea el input durante `hitDelay`, el golpe pendiente se descarta.
- En niebla (`abilities.IsMist`) y con `GameState.InputBlocked` no se ataca, como ya pasa con el resto de las armas.

## 5. Aturdimiento (`EnemyAI`)
- `ApplyStun(seconds)`: igual que `ApplySlow`. No acumula, vale el último (extiende hasta `Time.time + seconds` si es mayor).
- Mientras dura: `agent.isStopped = true`, no ataca a la base ni al jugador, y no se mueve por sí mismo. Al terminar retoma lo que hacía.
- **Cada enemigo golpeado tira su propio dado** con la probabilidad de `StunChanceAt`. Los **jefes y minijefes son inmunes** (`EnemyDefinition.IsBoss`).
- Sin efecto visual por ahora.

## 6. Tienda y UI
- `UpgradeStation.Available` ya depende de `WeaponCount > 0`: se abre sola para Guts. Se actualiza el comentario de la clase.
- `UpgradeMenuUI`: muestra las tres columnas con los nombres de la espada y, bajo "Aturdir", el % actual (como las pilas en la columna de sangrado).
- Hay que revisar que el HUD de munición, el cambio de arma y el panel de información no asuman que "sin munición = bastón" (`WeaponState.Staff` puede ser null para la espada).

## 7. Pruebas
Tests de EditMode, escritos antes que el código:
- `StunChanceAt`: niveles 0, 1 y 5 (0%, 6%, 30%); nunca pasa de 100%; el bono del árbol suma (30% + 20% = 50%).
- Cadencia: nivel 0 = 1,2 s, nivel 5 = 0,6 s (nunca menos que el mínimo); daño por nivel.
- `MeleeCone`: dentro, fuera por distancia, fuera por ángulo, borde del arco, detrás, y el radio del enemigo cuenta.
- Aturdimiento: un enemigo con stun no se mueve ni ataca; el stun termina; los jefes no se aturden.
- Guardado: los niveles de la espada se guardan y cargan sin migración.

En Play (con `save.json` respaldado y restaurado): golpe real contra una fila de enemigos (todos los del cono reciben daño y los de atrás no), el momento del daño coincide con el corte, la animación se ve rápida pero no cortada, el stun detiene a los enemigos, y la tienda sube los niveles y baja el tiempo entre golpes. El clic y las teclas reales no se pueden automatizar sin foco del editor: se marcan como **no verificados** y se prueba llamando a las funciones.

## 8. Riesgos y decisiones abiertas
- **Cuándo ocurre el golpe dentro del clip:** `hitDelay` es un valor fijo, no un evento de animación; se afina viéndolo en Play.
- **Mejora de cadencia vs. animación:** la animación va a una velocidad fija y rápida; el intervalo entre golpes es independiente. Con 0,6 s de intervalo mínimo y ~0,44 s de animación no se solapan.
- **Balance:** los números iniciales son supuestos (un golpe de 30 mata a un enemigo normal de 30 de vida en la oleada 1, en área). Hay que ajustarlos jugando; si se cambian, correr `BalanceTests`.
- Los valores de stun (6% por nivel, 1,5 s, jefes inmunes) son supuestos del diseño, no pedidos explícitos del usuario.
