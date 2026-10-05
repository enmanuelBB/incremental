# Guts: almas de los enemigos

Fecha: 2026-10-04 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (a pedido del usuario, él decide cuándo). Es la 2.ª entrega de las habilidades de Guts (después de la Q: `2026-10-04-guts-flame-burst-design.md`).

## 1. Qué se quiere
Cuando muere un enemigo y el personaje activo es **Guts**, deja un **alma en el piso** y, si Guts la recoge, lo **cura un poco** ("no mucho", palabras del usuario). Es la forma de recuperar vida de Guts (la Furia ya no cura). Más adelante la ulti de Guts (armadura) le va bajando vida, y las almas son lo que la repone.

## 2. Alcance
- **Entra:** las almas (aparecer, recogerse, desaparecer), la curación, sus números ajustables, pruebas.
- **No entra:** efectos de sonido, un contador de almas en el HUD, almas para otros personajes, habilidades que usen las almas.

## 3. Reglas
- **Cuándo suelta:** cada vez que un enemigo **muere** (por espada, quemadura, la Q o cualquier causa) **y el personaje activo suelta almas** (solo Guts por ahora). Un jefe que resucita no suelta alma al "morir" la primera vez, solo cuando muere de verdad.
- **Dónde:** en el lugar donde murió, flotando a ~0,6 m del piso, con un vaivén suave.
- **Cómo se recoge:** automático, cuando Guts pasa a **1,5 m** o menos (distancia horizontal). Sin pulsar nada.
- **Cuánto cura:** `soulHealFraction` = **2%** de la vida máxima de Guts por alma (3 con 150 de vida), **mínimo 1**. Los **jefes y minijefes** sueltan un alma mayor que cura **x3** (`soulBossMultiplier`). No se pasa de la vida máxima (`Health.Heal` ya lo limita).
- **Duración:** si no se recoge, desaparece a los `soulLifetime` = **10 s**.
- **Con otros personajes:** Alucard y Frieren no sueltan almas (`soulHealFraction` = 0). Las almas que ya estén en el piso al cambiar de personaje o al terminar la partida se retiran.
- **Aspecto (provisional, por código):** esfera pequeña azulada y brillante, el alma de jefe más grande. Sin sonido.

## 4. Estructura
- **Datos:** `CharacterDefinition` gana `soulHealFraction` (0 = no suelta almas), `soulBossMultiplier`, `soulLifetime` y `soulPickupRadius`. `Guts.asset` los lleva con los valores de arriba.
- **Lógica pura (Game.Core, con tests):** `SoulRules` (`Assets/Scripts/Core/Data/SoulRules.cs`):
  - `HealAmount(int maxHealth, float fraction, bool isBoss, float bossMultiplier)`: `max(1, round(vidaMax × fracción × (jefe ? multiplicador : 1)))`; 0 si `fraction ≤ 0`.
  - `IsWithinReach(Vector3 playerPosition, Vector3 soulPosition, float radius)`: distancia horizontal ≤ radio (la altura no cuenta).
- **Evento:** `GameEvents.EnemyDied(Vector3 position, bool isBoss)`, publicado por `EnemyAI.TakeDamage` en la rama de muerte real (después de `TryRevive`, junto a `RaiseEnemyKilled`). `EnemyKilled(int)` no se toca.
- **Juego:**
  - `SoulSpawner` (`Assets/Scripts/Gameplay/Characters/SoulSpawner.cs`): se suscribe a `EnemyDied`; si el personaje activo tiene `soulHealFraction > 0` coloca un alma (pool con `ObjectPool<SoulPickup>`); se retira todo al cambiar de personaje (`CharacterChanged`) y al terminar la partida (`GameOver`). Un GameObject raíz `Souls` en la escena.
  - `SoulPickup` (`Assets/Scripts/Gameplay/Characters/SoulPickup.cs`): vaivén, cuenta regresiva de duración y chequeo de alcance con `SoulRules.IsWithinReach`; al recogerla llama a `PlayerHealth.Heal(SoulRules.HealAmount(...))` y vuelve al pool. Esfera primitiva con material emisivo azul creado por código.

## 5. Pruebas
Tests de EditMode (primero):
- `SoulRules.HealAmount`: 150 de vida y 2% → 3; vida baja → mínimo 1; jefe x3 → 9; fracción 0 → 0; negativa → 0.
- `SoulRules.IsWithinReach`: dentro, fuera, borde exacto, y la altura se ignora.
- Valores de `Guts.asset` (2%, x3, 10 s, 1,5 m) y que Alucard/Frieren tienen `soulHealFraction` 0.

En Play (con `save.json` respaldado y restaurado; bucles con tope de pasos):
- Matar un enemigo con Guts deja un alma en su lugar; con Alucard no.
- Pasar cerca la recoge y cura 3 (con 150 de vida y vida baja); con la vida llena no pasa del máximo.
- Sin recogerla, desaparece a los 10 s (medido por segundos de juego).
- Un jefe suelta un alma que cura 9.
- Matar por quemadura también suelta alma; un jefe que resucita no suelta alma al primer "morir".
- Cambiar de personaje o terminar la partida retira las almas.
- **No verificado hasta que lo pruebe el usuario:** cómo se ve y se siente recoger almas jugando, y el balance de la curación.

## 6. Riesgos y decisiones abiertas
- **Balance:** con 2% por alma y oleadas grandes la curación se acumula rápido; todo está en `Guts.asset` para ajustarlo. Si resulta mucha, se puede bajar el %, la duración o hacer que solo suelte alma 1 de cada N enemigos (por ahora sueltan todos, a pedido del diseño aprobado).
- **Rendimiento:** las oleadas grandes pueden dejar decenas de almas vivas a la vez; por eso van en un pool y duran 10 s.
- El aspecto es provisional (esfera + emisión); no hay sonido.
