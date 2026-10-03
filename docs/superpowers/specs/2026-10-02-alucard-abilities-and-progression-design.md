# Habilidades de Alucard, sangrado y progresión por personaje

Fecha: 2026-10-02 · Estado: **aprobado por el usuario. Fase 1 implementada y verificada (149 tests); fase 2 implementada a medias (XP, niveles, puntos, rangos, estación, barra de XP, guardado v3); falta el resumen al morir.**

> **Para retomar:** lo hecho y lo que falta está en `notas/REGISTRO_DE_CAMBIOS.md` (sección "Dónde nos quedamos"); el porqué de cada cosa, en `notas/DECISIONES.md` (D24 a D27).

## 1. Qué se quiere
1. Un **sistema de habilidades general** (3 por personaje: 2 normales y 1 definitiva), con las tres de **Alucard**: disparo pesado, niebla y definitiva.
2. El **sangrado**: pasiva de Alucard, permanente, que se apila y se ve en el enemigo.
3. **Progresión por personaje:** experiencia y nivel propios, puntos para subir los rangos de habilidad y el tope del sangrado, y una **pantalla de resumen al morir**.
4. Una **mejora de dinero** en la tienda de armas: "Aplicación de sangrado".

Fuera de alcance: el árbol y las monedas de prestigio, las otras dos habilidades de Frieren, arte y sonido finales, la cuarta habilidad.

## 2. Reglas generales del sistema (decididas por el usuario)

| Qué | Se gana con | Qué mejora | Dónde |
|---|---|---|---|
| **Dinero** | Matar enemigos | El **arma** del personaje elegido. Esto aplica a **todos** los personajes | Estación de mejoras (la de siempre) |
| **Puntos de personaje** | 1 por nivel del personaje | Rangos de habilidad y el **tope de pilas del sangrado** | Estación nueva, donde estaba la tienda del M16 |
| **Monedas de prestigio** | Prestigiar | El árbol de prestigio (el único árbol del juego) | Más adelante |

- **Cada personaje tiene su propia experiencia y nivel.** Se conservan entre partidas.
- **El prestigio es solo por el personaje que se elija:** mide cuánto avanzó ese personaje y le da monedas de prestigio.
- **No hay árbol por personaje.** Solo el árbol de prestigio.
- **No se mejora nada a mitad de partida** (ni puntos ni dinero): todas las estaciones funcionan solo antes de la primera oleada. Un nivel que se sube durante la partida deja el punto guardado y se avisa en pantalla.
- **Las habilidades se pueden usar solo desde que empieza la primera oleada.** Así `E` no choca con "Interactuar".

## 3. El sangrado

**Modelo:**
- Cada enemigo tiene **pilas de sangrado**, permanentes: duran hasta que muere.
- Cada **1 s** (tick) el enemigo recibe `pilas × daño por pila`.
- **Daño por pila** = `max(1, redondear(daño de una bala × 10%))`: 1 al empezar, 2 con las pistolas al máximo.
- **Tope de pilas por enemigo** = `4 + nivel de sangrado` (5 en el nivel 1, hasta 19 en el nivel 15). El nivel de sangrado se compra con **puntos de personaje**.
- Los ticks matan igual que una bala (dan dinero y experiencia) pero **no curan** con el robo de vida.
- **Cómo se ve:** tinte rojo en el enemigo que se intensifica con `pilas ÷ tope`, y sobre su cabeza el daño de cada tick en rojo (una cifra por segundo y por enemigo, solo si está en pantalla).

**Quién suma pilas:**

| Fuente | Pilas |
|---|---|
| Impacto de disparo básico | `pilas por impacto` del arma (1 al empezar; sube con la mejora de dinero) |
| Disparo pesado | +3 |
| Niebla | +1 a cada enemigo a menos de 3 m, una vez por cada uno en cada uso |
| Definitiva, explosión en área | +2 por impacto |
| Definitiva, disparos básicos | doble de pilas por impacto |
| Definitiva, río de sangre | +1 cada 2 s a los enemigos a menos de 4 m |

**Mejora de dinero "Aplicación de sangrado"** (la 4.ª fila de la estación de mejoras, solo en armas que sangran):
- Base: **1 pila por impacto**; cada nivel suma 1, hasta el nivel 10 (**11 pilas por impacto**).
- **Muy cara y con precio exponencial:** `600 × 1,6^nivel` (600, 960, 1.540, 2.460, 3.930, 6.290, 10.070, 16.110, 25.770 y 41.230; unos 109.000 en total). Para esto `UpgradeStat` gana un factor de crecimiento (`priceGrowth`); si vale 0 se mantiene el precio lineal de siempre, así las demás mejoras no cambian.
- Las armas sin sangrado (el bastón de Frieren) no muestran esta fila.
- **Qué hace de verdad:** el tope de pilas (puntos) limita el total, así que sumar más pilas por impacto sirve sobre todo para **llenar el tope más rápido**: un enemigo que recibe pocos disparos (en un grupo grande) ya sangra a tope con un par de impactos. Contra un solo objetivo, el tope se llena casi enseguida de todos modos. Por eso encaja con la debilidad de Alucard (los grupos), y por eso no tiene sentido pasar de unas 10 pilas por impacto con un tope máximo de 19.

## 4. Las habilidades de Alucard (rango 1)

| | Tecla (mando) | Efecto | Enfriamiento |
|---|---|---|---|
| **Disparo pesado** | `Q` (botón este) | Una bala enorme contra un solo enemigo: 6× el daño de una bala. +3 pilas de sangrado. No gasta munición | 8 s |
| **Niebla** | `E` (`LB`) | Se vuelve niebla durante **2 s**: **invulnerable**, +60% de velocidad, no puede disparar ni usar otras habilidades. +1 pila a los enemigos cercanos | 14 s |
| **Definitiva** | `F` (`RB`) | Durante **8 s**: armadura roja, río de sangre alrededor, **50% de robo de vida del daño directo** (balas y explosiones, no ticks), los disparos explotan en área (radio 2,5 m, 60% del daño a los vecinos), el sangrado de los disparos se duplica | 75 s |

Cada habilidad es un dato propio (con su tabla de números por rango), así las de los demás personajes usan el mismo sistema.

## 5. Niveles, puntos y rangos (estilo League of Legends)
- **Nivel máximo: 30.** Experiencia para el siguiente nivel = `100 × nivel^1,5` (100, 283, 520, 800, 1.118...).
- **Experiencia:** cada tipo de enemigo da la suya (`xpReward`) y completar una oleada da un bono.
- **Puntos:** 1 por nivel. Se gastan antes de la primera oleada.
- **Rangos:** habilidades normales hasta 5, con tope `ceil(nivel ÷ 2)`; definitiva hasta 3, con compuertas en los niveles 6, 12 y 18.
- **Nivel de sangrado:** hasta 15; el nivel 1 es gratis y los 14 siguientes cuestan 1 punto cada uno.
- Total de puntos útiles: 13 (rangos) + 14 (sangrado) = 27 de 30; quedan 3 para una futura cuarta habilidad.
- Cada rango sube cosas distintas según la habilidad (por ejemplo el disparo pesado: más daño y menos enfriamiento; la niebla: más duración y velocidad; la definitiva: más duración y más robo de vida).

## 6. Pantalla de resumen al morir
Se muestra al terminar la partida, antes del botón de reiniciar:
- Oleada alcanzada, enemigos eliminados, dinero ganado.
- Experiencia ganada y **niveles subidos** ("Alucard: nivel 4 → 6").
- **Puntos disponibles** para gastar en la estación.
- Botón de reiniciar (el de siempre; al reiniciar vuelve el menú de personajes).

## 7. Qué se construye (3 fases, cada una se verifica antes de pasar a la siguiente)
**Fase 1: habilidades y sangrado**
- Sacar el rayo de Frieren del bastón a datos de habilidad propios. Tres casillas en el HUD. Acciones de entrada `Ability2` y `Ability3`.
- Sistema de sangrado (con un solo temporizador central, no una corrutina por enemigo) y su aspecto en pantalla.
- Mejora de dinero "Aplicación de sangrado" (nuevo tipo de mejora en `UpgradeType`, el guardado ya rellena con ceros los niveles que faltan).
- Las tres habilidades de Alucard a rango 1: invulnerabilidad, curación y multiplicador de velocidad en vida y movimiento del jugador; transformación visual.

**Fase 2: progresión**
- Experiencia y nivel por personaje, puntos, rangos y tope de sangrado. Guardado **v3** con migración desde v2.
- La estación de puntos (reemplaza la tienda del M16).
- Pantalla de resumen al morir.

**Fase 3 (fuera de esta especificación):** prestigio por personaje y su árbol.

## 8. Balance
- Con 5 pilas de 1 al empezar, un enemigo sangra 5 por segundo: ~13% del daño por segundo de Alucard (38). Al máximo (19 pilas de 2) son 38 por segundo por enemigo, ~25% de su daño máximo (152).
- Alucard sube bastante, sobre todo contra grupos (niebla y definitiva castigan a varios). **Frieren queda por debajo** hasta que tenga sus otras dos habilidades.
- Los tests de balance pasan a comparar **builds de referencia** (inicial, intermedia y máxima) de cada personaje, con el kit completo, y hay que relajar las bandas actuales mientras Frieren tenga una sola habilidad.
- Todos los números de esta sección son una estimación y se ajustan jugando; viven en datos.

## 9. Provisional
Armadura roja (cambio de color con brillo), río de sangre (disco rojo en el suelo), niebla (partículas oscuras), iconos de las habilidades (generados por código) y sin sonidos.

## 11. Diferencias entre lo diseñado y lo implementado (fase 2)
- **Hecho:** XP y nivel por personaje, puntos (1 por nivel), rangos de habilidad con las compuertas del diseño, nivel de sangrado, estación de puntos en el objeto `shop`, barra de XP para todos los personajes y guardado v3. Detalle en `notas/DECISIONES.md`, D29.
- **Las habilidades empiezan en rango 0** (hay que gastar el punto del nivel 1 para tener la primera). El diseño de los totales (13 + 14 puntos útiles) lo daba a entender.
- **Hecho el 2026-10-03:** la pantalla de resumen al morir (sección 6), con cámara de muerte y título animado; ver `notas/DECISIONES.md` D31. **Falta:** el nivel en el menú de personajes.
- **Valores por rango** (estimados, en los assets de `Assets/Data/Abilities/`): ver D29.

## 10. Diferencias entre lo diseñado y lo implementado (fase 1)
- **Frieren no se migró** a `AbilityDefinition`: su rayo sigue en `StaffDefinition` y ocupa la casilla 1. Se migra cuando reciba sus 2 habilidades restantes.
- **Mando:** la sección 4 decía `LB`/`RB` para la niebla y la definitiva, pero esos botones ya cambian de arma. Quedaron en la **cruceta** (arriba = niebla, abajo = definitiva). El teclado es el planeado: Q, E, F.
- **Nivel de sangrado:** fijo en 1 (tope de 5 pilas) hasta la fase 2 (`Shooting.BleedLevel`).
- **Número de tests:** el plan hablaba de balance con "builds de referencia"; **eso sigue pendiente** (`BalanceTests` solo compara el disparo básico).
- **Precio de la mejora "Sangrado":** se implementó tal cual (600 x 1,6^nivel, 10 niveles, 11 pilas por impacto).
- **Supuestos no confirmados por el usuario:** en niebla no se dispara; el robo de vida es solo de daño directo; el disparo pesado entra en enfriamiento aunque falle.
