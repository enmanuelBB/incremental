# Kit de Frieren: Zoltraak, rayo, campo de flores y definitiva

Fecha: 2026-10-04 · Estado: **para revisión del usuario** (todavía sin plan ni código).

## Objetivo

Darle a Frieren (id `maga`) un kit propio de **maga de control con mucho daño en área** y que **siempre levite**. Hoy solo tiene el disparo básico gratis y el rayo penetrante en la Q (que vive en `StaffDefinition`); la E y la F están vacías y no tiene árbol. Este spec cubre el kit y el levitar. **El árbol de habilidades de Frieren va aparte**, como el de Guts, y se discute después.

Criterio de éxito: Frieren juega distinto a Alucard (tiros y niebla) y a Guts (espada y armadura): carga un hechizo de área, tiene un rayo que atraviesa filas, una zona que la cura y frena a la horda, y una definitiva que paraliza y le da ráfagas sin carga.

## Controles (solo con el bastón)

| Entrada | Con el bastón de Frieren | Con otros personajes |
|---|---|---|
| **Clic izquierdo** (`Fire`) | **Zoltraak**: mantener para cargar, soltar para disparar | sin cambios |
| **Clic derecho** (`Aim`) | **Disparo básico** (el de hoy, gratis, se mantiene apretado). **Ya no hace zoom** | zoom, sin cambios |
| **Q** | Rayo de maná masivo | — |
| **E** | Campo de flores (dos pasos, ver abajo) | — |
| **F** | Definitiva | — |

No se tocan las acciones del `GameControls.inputactions`: `Fire` y `Aim` ya existen y solo cambia lo que hace cada una cuando el arma es un bastón. En mando pasa igual con sus gatillos equivalentes. La cámara deja de hacer zoom con el bastón (`CameraFollow` consulta si el arma activa es un bastón).

## Zoltraak (básico cargado, clic izquierdo)

- **Cargar:** mantener el clic. La carga llena en **1,2 s**; hay una barra o anillo de carga en pantalla. Si no alcanza el maná mínimo, no empieza a cargar.
- **Soltar:** dispara al punto de la mira (el primer golpe del rayo de la cámara, o el final del alcance si no hay nada). Ahí cae una **explosión en área** que daña a todos los enemigos vivos dentro del radio. No necesita línea de tiro hasta cada enemigo; solo hasta el punto de impacto.
- **Escala con la carga:** el daño va de **40% a 100%** y el radio de **2,5 m a 4 m**, según cuánto se cargó.
- **Costo:** **sin maná** (el usuario lo pidió así después de aprobar el spec; antes eran 15). El campo `zoltraakManaCost` queda en el bastón con valor 0. Después de cada disparo hay una pausa de **0,4 s**.
- **Daño base** (a carga completa): **45**, y el nivel de "Poder" de la tienda lo multiplica igual que al rayo y al básico. Un enemigo normal tiene 30 de vida y un tanque 60.
- **Con la definitiva activa:** sale **sin carga**, a carga completa, con cada clic, con una pausa de **0,35 s** entre disparos.

## Q: Rayo de maná masivo (el rayo penetrante de hoy)

Es el rayo que ya existe (40 de daño a todos los que atraviesa, 25 de maná, 3 s de enfriamiento), con otro nombre. **Pasa a ser una habilidad más** (`AbilityDefinition` de clase `ManaBeam`), como las de los otros personajes, para que tenga rango, tecla y árbol igual que ellas. El comportamiento no cambia. **Los números (daño, maná, enfriamiento, alcance, grosor) se quedan en `StaffDefinition`**, porque la tienda (Poder y Maná), `CombatMath`, `CharacterInfo` y los tests de balance ya los leen de ahí; la habilidad solo aporta nombre, icono, tecla y rango (igual que la Llamarada de Guts toma su daño de la espada).

## E: Campo de flores (dos pasos)

1. **Pulsar E** abre el **modo de colocación** y se queda abierto sin mantener la tecla. Se dibuja en el suelo un **círculo del tamaño del campo** (radio 5 m) **donde mira la mira**, y sigue a la vista. Si el punto está más lejos del alcance (25 m), el círculo se queda en el límite. El círculo solo existe en este modo.
2. **Confirmar:** **clic izquierdo** o **pulsar E otra vez**. Ahí se crea el campo en ese punto. El clic de confirmar no empieza a cargar un Zoltraak.
3. **Cancelar:** **clic derecho**. Mientras se coloca, no se dispara ni se carga nada, y no se gasta maná ni se inicia el enfriamiento.

El **maná (20) y el enfriamiento (18 s) se cobran al confirmar**, no al abrir la colocación. Si no hay maná suficiente al pulsar E, no se abre.

**El campo:** dura **6 s** y hay uno a la vez (el enfriamiento es mayor que la duración, así que no se pisan). Cada **0,25 s**:
- a Frieren, si está **dentro del radio**, le **cura 3% de su vida máxima por segundo** (a un solo personaje: no cura la base);
- a cada enemigo dentro lo **ralentiza 40%** (`EnemyAI.ApplySlow`, que ya existe). La ralentización dura 0,5 s desde la última vez que lo toca el campo, así que al salir se recuperan casi de inmediato.

No hace daño. Visual: flores que brotan en el círculo (procedural, sin arte nuevo).

## F: Definitiva (pulso de maná dorado)

**Activa por tiempo, no es un interruptor.** Al pulsar F: un pulso de energía con el aura dorada/púrpura de Frieren alrededor de ella. Dura **9 s**. Efectos:
- **Terror:** al activarla, todos los enemigos en un radio grande (**15 m**) quedan **aturdidos 1,5 s** (`EnemyAI.ApplyStun`). Mientras dure, los enemigos en ese radio van **40% más lentos**.
- **Zoltraak instantáneo:** sin carga, sin maná, un disparo por clic (0,35 s de pausa), como se describe arriba.
- **Enfriamientos:** mientras dure, **Q y E se recargan al doble de rápido** (-50% de espera).

**Costo:** 50 de maná. **Enfriamiento:** 60 s, que corre desde que se activa.

Es una habilidad definitiva (`Progression.IsUltimate`), con el rango máximo de las definitivas (3) y desbloqueada desde el nivel 6.

## Levitar (solo visual)

- Frieren **flota siempre**: el cuerpo se eleva **0,45 m** y se balancea suave (**±0,05 m, periodo de 2,5 s**).
- **Solo visual:** el cilindro con la física no cambia, así que camina, choca y pisa igual que ahora. No pasa por encima de obstáculos ni cambia el daño o los efectos del suelo.
- **Cámara:** en primera persona la cámara sube lo mismo (para que el mundo se vea desde arriba); en tercera persona, el foco sube igual.
- Se define en datos: `CharacterDefinition` gana `hoverHeight` (0 para Alucard y Guts, 0,45 para Frieren) y el balanceo. `PlayerBody` lo aplica sobre `FeetLocalY`.
- **Frieren aún no tiene `bodyPrefab`**: hoy solo se vería la cámara más alta y el bastón. Cuando tenga modelo, ya levita sin cambios.

## Datos y arquitectura

- **`AbilityKind`** (solo se agrega al final, los valores están guardados): `ManaBeam` (Q), `FlowerField` (E), `ManaPulse` (F). Revisar `Progression`: `MaxRank`, `RankCapForLevel` e `IsUltimate` (`ManaPulse` es definitiva). Lección anterior: probar con el rango máximo y con el guardado real.
- **Assets nuevos** en `Assets/Data/Abilities/`: `RayoMana`, `CampoFlores`, `PulsoMana`. `Maga.asset` pasa a tener las tres en `abilities`, en el orden Q, E, F.
- **`StaffDefinition`** conserva el maná, el disparo básico y los números del rayo de la Q, y gana los datos del Zoltraak (daño, radio mínimo y máximo, tiempo de carga, costo, pausa).
- **Lógica pura en `Game.Core`** (con tests de EditMode): carga del Zoltraak (fracción de carga a daño y radio), colocación del campo (punto limitado al alcance), cura y ralentización por tick, y balanceo del levitar. El gameplay (cámara, círculo, explosión, flores) en `Game.Runtime`.
- **Elegir la habilidad a nivel 1:** Frieren **empieza sin ninguna habilidad aprendida** (las tres en rango 0), igual que Alucard y Guts. Con el sistema de puntos de siempre (1 punto por nivel, en la estación de habilidades), a **nivel 1 el único punto se gasta en la Q o en la E, a elección**. La definitiva (F) **no se puede elegir** hasta el nivel 6. El Zoltraak y el disparo básico no necesitan punto: son del bastón. Esto ya es lo que hacen las reglas (`Progression.RankCapForLevel`: normales rango 1 a nivel 1, definitivas desde el nivel 6), así que no hay regla nueva: solo hay que quitarle a la Q su "gratis" actual y pasarla a `AbilityDefinition`. Las habilidades sin aprender no salen en el HUD ni se pueden lanzar.
- **Partida guardada de hoy:** Frieren ya está en nivel 1 (10 de XP) con las tres en rango 0. Su Q pasa a pedir el punto, como las demás.
- **Tienda** (Cadencia, Maná, Poder): sigue igual. Cadencia afecta al disparo básico (clic derecho), Maná a la regeneración y a los enfriamientos, y Poder multiplica el daño del básico, del Zoltraak y del rayo.

## Fuera de alcance

El árbol de habilidades de Frieren, su modelo y animaciones, sonidos, la velocidad de ataque de los enemigos (la definitiva solo frena el movimiento), y el balance final (los valores de arriba son iniciales y se ajustan jugando).

## Pruebas

- **EditMode:** Zoltraak (carga 0, mitad y completa; daño y radio; sin maná no empieza), colocación del campo (alcance, límite del punto), cura y ralentización por tick, enfriamiento al doble durante la definitiva, balanceo del levitar, tests de assets (Maga con 3 habilidades, rangos, `IsUltimate` de `ManaPulse`). Revisar los tests de balance por personaje: Frieren ya no está "por debajo" y las bandas pueden necesitar ajuste.
- **Play (por funciones, con tope de pasos):** colocar el campo con E y confirmar con clic y con E; cancelar con clic derecho; curar solo dentro del círculo; ralentizar enemigos; Zoltraak a medias y completo; definitiva (aturdimiento, ralentización, enfriamientos, Zoltraak sin carga); cámara más alta con el levitar.
- **No verificable sin foco del editor:** el input real (clics, mantener, E dos veces). Lo pruebas tú y me avisas.
