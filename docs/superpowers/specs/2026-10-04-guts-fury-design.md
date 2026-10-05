# Barra de Furia de Guts

Fecha: 2026-10-04 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (a pedido del usuario, él decide cuándo). Continúa `2026-10-04-guts-sword-and-stun-design.md`.

## 1. Qué se quiere
Una barra roja llamada **Furia** que se carga con los ataques básicos de Guts. Al llenarse, **el siguiente básico sale potenciado**: **daño x2** y **aturdimiento seguro a todos los enemigos golpeados** (los jefes reciben el daño x2 pero no el stun, porque son inmunes). Idea del usuario; el diseño base (carga por enemigo golpeado, sin decaimiento) fue aprobado por él.

## 2. Alcance
- **Entra:** la lógica de la barra, su carga con el básico, el básico potenciado, la barra en el HUD (solo con Guts), datos ajustables en `Espada.asset`, pruebas.
- **No entra:** habilidades de Guts y su potenciado (cuando existan usarán la misma barra: ver §4), el árbol de Guts, sonidos y partículas, guardado de la Furia.

## 3. Reglas
- **Barra:** 0 a **100**, `furyMax`. Empieza vacía en cada partida y al elegir/cambiar de personaje. **No decae** y **no se guarda**; al terminar la partida la escena se reinicia y vuelve a 0.
- **Carga:** cada golpe de espada **que conecta** suma **+5 por enemigo golpeado**, con un tope de **+20 por golpe** (`furyPerEnemyHit` = 5, `furyMaxPerSwing` = 20). Un golpe que no toca a nadie no suma. Golpear el cubo de inicio no suma.
- **Llena:** con 100 la barra queda lista (`IsFull`). El **siguiente golpe de espada que conecte con al menos un enemigo** es el potenciado. Si el golpe no toca a nadie, **no gasta la Furia** (sigue llena).
- **Golpe potenciado:**
  - Daño: `furyDamageMultiplier` = **2** sobre el daño normal (que ya incluye mejoras de tienda y bono del árbol).
  - Aturdimiento: **seguro** (probabilidad 100%) a cada enemigo golpeado, con la duración normal (`stunSeconds`). Los jefes y minijefes siguen inmunes al stun (la regla de `EnemyAI.ApplyStun`).
  - **Consume toda la barra** (queda en 0) y **no suma Furia** en ese mismo golpe.
- Los números iniciales son supuestos de diseño: con 5 por enemigo y 100 de barra, contra una horda de 4 o más enemigos se llena en ~5 golpes (~6 s con el tiempo entre golpes de 1,2 s) y contra uno solo en 20 golpes.

## 4. Estructura
- **Lógica pura (Game.Core, con tests):** `FuryMeter` en `Assets/Scripts/Core/Data/FuryMeter.cs`:
  - `FuryMeter(float max)`; `Current`, `Max`, `Fraction`, `IsFull`.
  - `Add(float amount)`: suma sin pasar de `Max` (ignora cantidades ≤ 0).
  - `bool TryConsume()`: si `IsFull`, deja `Current` en 0 y devuelve true; si no, no hace nada y devuelve false.
  - `Reset()`.
  - `static float GainForHits(int enemiesHit, float perEnemy, float maxPerSwing)`: `min(enemiesHit * perEnemy, maxPerSwing)`; 0 si `enemiesHit ≤ 0`.
- **Datos:** `SwordDefinition` gana `furyMax` (100), `furyPerEnemyHit` (5), `furyMaxPerSwing` (20), `furyDamageMultiplier` (2). El stun seguro no lleva campo: es la regla de §3.
- **Juego:** `Shooting` guarda un `FuryMeter fury` (de máximo 0 si el personaje no tiene espada) y lo reconfigura/vacía en `Build`. En `SwingSword`, tras reunir los objetivos: si hay al menos uno y `fury.IsFull`, el golpe es potenciado (daño x2, stun seguro, `fury.TryConsume()`); si no, `fury.Add(GainForHits(...))`. Después publica el estado. Se expone `Shooting.Fury` (solo lectura) para que las habilidades de Guts puedan usar la misma barra más adelante.
- **Evento:** `GameEvents.FuryChanged(float current, float max)`. `max <= 0` significa "este personaje no tiene Furia: oculta la barra" (mismo criterio que `BossHealthChanged` con vida 0). Se publica al construir el personaje (`PublishHud`) y tras cada golpe.
- **HUD:** `FuryBarUI` (`Assets/Scripts/UI/FuryBarUI.cs`), clon de la barra de vida dentro de `PlayerHud/Bars`, **debajo de la de XP**, igual que `XpBarUI`. Relleno rojo, texto "FURIA" (con contorno oscuro para leerse sobre el rojo) y el valor `actual / máximo`. Visible solo si `max > 0`. Con la barra llena el texto pasa a "¡FURIA!" y el relleno pulsa (brillo con una onda seno), para que se note que el siguiente golpe es especial. Se agrega como componente en la escena.

## 5. Pruebas
Tests de EditMode (escritos antes que el código):
- `FuryMeter`: empieza en 0; `Add` suma; no pasa de `Max`; ignora cantidades ≤ 0; `IsFull` en 100; `TryConsume` en barra llena la vacía y devuelve true; `TryConsume` en barra no llena devuelve false y no cambia nada; `Reset` vacía; `Fraction`.
- `GainForHits`: 0 enemigos → 0; 1 → 5; 3 → 15; 10 → 20 (tope); valores negativos → 0.
- Asset: `Espada.asset` tiene `furyMax` 100, `furyPerEnemyHit` 5, `furyMaxPerSwing` 20 y `furyDamageMultiplier` 2.

En Play (con `save.json` respaldado y restaurado, bucles con tope de pasos): con Guts aparece la barra roja y con Alucard no; un golpe a 3 enemigos suma 15; el tope por golpe es 20; al llegar a 100 el siguiente golpe hace daño x2 (60 con el daño base de 30) y aturde a **todos** los golpeados sin dado; un jefe en ese golpe recibe x2 y no se aturde; la barra queda en 0 y ese golpe no suma; un golpe potenciado sin enemigos en el cono no gasta la barra; cambiar de personaje la vacía. El clic real y cómo se ve la barra pulsando son **no verificados** hasta que el usuario los pruebe.

## 6. Riesgos y decisiones abiertas
- **Balance:** daño x2 + stun seguro cada ~6 s puede ser muy fuerte contra hordas; todos los números viven en `Espada.asset` para ajustarlos jugando.
- Que un golpe sin objetivos no gaste la Furia es una decisión mía (más amable); si el usuario prefiere que se gaste siempre, es un cambio de una línea.
- La barra copia la de vida: si cambia el HUD, `FuryBarUI` y `XpBarUI` deben cambiar juntas (el mismo riesgo que ya tiene `XpBarUI`).
