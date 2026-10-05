# Guts: habilidad Q "Llamarada" y quemadura

Fecha: 2026-10-04 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (a pedido del usuario, él decide cuándo). Es la 1.ª de tres entregas de las habilidades de Guts (Q, E, ulti); las almas tienen su propio spec (`2026-10-04-guts-souls-design.md`). Continúa `2026-10-04-guts-fury-design.md`.

## 1. Qué se quiere
La habilidad `Q` de Guts: una **llamarada** que sale de su guante en un **cono frente a él**, daña a todos los enemigos dentro y los deja **quemados unos segundos**. Con la **Furia llena** sale potenciada (más daño). Reutiliza el sistema de habilidades que ya existe (rangos, puntos de nivel, enfriamiento, HUD).

Cambio del usuario durante el diseño: la Furia **no** cura con las habilidades (se descartó); lo que cura son las almas (otro spec). La Furia sí sigue potenciando las habilidades con más daño.

## 2. Alcance
- **Entra:** la habilidad Q de Guts, el estado de quemadura de los enemigos, su aspecto (tinte, número naranja, partículas del cono), icono provisional, pruebas.
- **No entra:** la E (dash con disparo), la ulti (armadura), las almas, el árbol de Guts, sonidos finales.

## 3. La habilidad
- **Cono** frente a Guts: alcance `range` = **5 m**, apertura `coneDegrees` = **90°**, con la misma geometría pura que el golpe de espada (`MeleeCone`). Daña a todos los enemigos vivos dentro (cada uno una vez) y los quema.
- **Daño inicial:** `daño de la espada × damageMultiplier` (**2**), con las mejoras de tienda y el bono del árbol (`WeaponState.Damage`). Mínimo 1.
- **Enfriamiento:** `cooldown` = **8 s**. Se lanza solo tras empezar la primera oleada, como las de Alucard.
- **Rangos:** 5 (como las normales de Alucard), comprados con los puntos de nivel. Por rango extra sobre el 1.º: daño del multiplicador `+0,25` (`damagePerRank`), quemadura `+0,5 s` (`durationPerRank`), enfriamiento `−0,5 s` (`cooldownPerRank`).
- **Con Furia llena** (`Shooting.Fury.IsFull` y la llamarada toca a al menos un enemigo): gasta toda la barra y el daño inicial **y el de cada tick de la quemadura** de esa llamarada se multiplican por `SwordDefinition.furyDamageMultiplier` (**2**). Sin stun: el stun seguro es solo del golpe de espada. Si no toca a nadie, **no gasta la Furia** y igual entra en enfriamiento.
- La Q **no carga Furia** (solo la carga el golpe de espada), para que las habilidades no se alimenten solas.
- **Animación:** `spell cast` (nuevo trigger `Cast` en `Guts.controller`, vía `PlayerBody.PlayCast()`); el daño se aplica en el instante del lanzamiento (sin retraso), para que responda rápido.
- **Visual:** un cono de partículas de fuego (naranja) hecho por código, parecido a los efectos de Alucard (`Sprites/Default`); provisional.
- **Icono:** `Assets/Art/Icons/Llamarada.png`, generado por código; provisional.

## 4. La quemadura
- Hace daño cada `burnTickSeconds` = **0,5 s** durante `duration` = **4 s** (rango 1). Daño por segundo = `burnDamageFractionPerSecond` = **0,6** del daño de la espada; daño por tick = `max(1, round(dañoEspada × 0,6 × 0,5))`. Con Furia llena el daño por tick también se multiplica.
- **No acumula:** una llamarada nueva sobre un enemigo ya quemado **renueva la duración** y deja el **mayor** daño por tick entre el actual y el nuevo.
- Los jefes y minijefes también se queman (solo son inmunes al stun).
- Al morir o al volver al pool se limpia.
- **Aspecto:** el enemigo se tiñe de naranja mientras arde y cada tick muestra un **número naranja** sobre él (como el sangrado, que es rojo).

## 5. Estructura
- **Lógica pura (Game.Core, con tests):**
  - `BurnState` (`Assets/Scripts/Core/Data/BurnState.cs`): `IsBurning`, `Apply(float seconds, int damagePerTick, float tickSeconds)` (renueva y conserva el mayor daño por tick), `int Advance(float deltaTime)` (devuelve cuántos ticks tocan y gasta tiempo; nunca más ticks de los que caben en el tiempo restante), `DamagePerTick`, `Clear()`.
  - `AbilityKind.FlameBurst` se agrega **al final** del enum (valor 3) para no mover los valores guardados en los assets de Alucard.
  - `AbilityDefinition` gana `coneDegrees` y `burnDamageFractionPerSecond` y `burnTickSeconds`; `range`, `duration`, `damageMultiplier`, `cooldown` y los `...PerRank` ya existen y se reutilizan. `DescribeRank` y `DurationAt` cubren el tipo nuevo ("Daño xN · quema N s · enfriamiento N s"). `Progression.MaxRank` ya lo trata como normal (5 rangos).
  - Cálculos puros del daño de la llamarada y del tick (con y sin Furia) en `AbilityDefinition` (como `DamageFor`).
- **Juego:**
  - `EnemyBurn` (`Assets/Scripts/Gameplay/Enemies/EnemyBurn.cs`): igual que `EnemyBleed` (un `Update` por enemigo que arde, tinte naranja, publica el tick **antes** de dañar), agregado por `EnemyAI` la primera vez; `EnemyAI.ApplyBurn(seconds, damagePerTick, tickSeconds)` y limpieza en `Spawn`/al morir.
  - `FlameBurst` (`Assets/Scripts/Gameplay/Player/FlameBurst.cs`): clase que hace el cono, el daño, la quemadura y la Furia; `PlayerAbilities.TryCast` la llama con un `case AbilityKind.FlameBurst` (así `PlayerAbilities`, que es de Alucard, no crece con la lógica de Guts).
  - `GameEvents.BurnTick(Vector3, int)` y `FloatingTextManager` lo muestra en naranja.
  - `Guts.asset`: `abilities = [Llamarada]`. Asset nuevo `Assets/Data/Abilities/Llamarada.asset` (`id` "Llamarada").
  - `PlayerBody.PlayCast()` y el parámetro/estado `Cast` en `Guts.controller`.
- **Datos:** todos los números viven en `Llamarada.asset` y en `Espada.asset` (Furia).

## 6. Pruebas
Tests de EditMode (primero):
- `BurnState`: `Apply` + `Advance` da el número correcto de ticks (0,5 s → 8 ticks en 4 s) y se apaga; un tick parcial no se pierde ni se duplica entre llamadas; renovar extiende la duración; el menor daño no reemplaza al mayor; el mayor sí; `Clear` apaga; tiempo/ticks no positivos no hacen nada.
- `AbilityDefinition` para FlameBurst: `DamageFor` (x2, con rango, mínimo 1), daño por tick de la quemadura (con y sin Furia), `DescribeRank`, `CooldownAt`/`DurationAt` por rango.
- Valores del asset `Llamarada.asset` y que `Guts.asset` la lleva en la casilla 0.
- `AbilityKind`: los valores de HeavyShot/Mist/Ultimate no cambian (0, 1, 2) y FlameBurst es 3.

En Play (con `save.json` respaldado y restaurado; bucles con tope de pasos; sin dejar enemigos de vida enorme):
- El cono pega y quema a los de adentro y no a los de atrás ni a los de 90°.
- Los jefes se queman; una segunda llamarada renueva la quemadura (no acumula).
- Los ticks bajan la vida con el tiempo (medido por segundos de juego) y el enemigo se tiñe; el número naranja aparece.
- Con Furia llena: x2 de daño inicial y de tick, la barra queda en 0; al aire no gasta Furia.
- El enfriamiento de 8 s y el HUD de la casilla Q.
- Con Alucard sus habilidades siguen funcionando igual (sin regresiones).
- **No verificado hasta que lo pruebe el usuario:** la tecla `Q` real, cómo se ve el fuego y el balance.

## 7. Riesgos y decisiones abiertas
- **Balance:** quemar a toda una horda con daño x2 y quemadura cada 8 s puede ser fuerte; los números están en el asset. Los valores por rango (+0,25 / +0,5 s / −0,5 s) son supuestos míos.
- **Primera elección:** con 1 punto Guts aprende la Q (única habilidad), igual que la principal de Alucard.
- El icono y las partículas son provisionales (`Shader.Find("Sprites/Default")` podría faltar en una build final, el mismo riesgo que ya tienen los efectos de Alucard).
- Las habilidades 2 y 3 de Guts (E y ulti) quedan como casillas vacías hasta sus entregas.
