# Frieren: árbol de habilidades

Fecha: 2026-10-06 · Estado: **diseño aprobado por el usuario en conversación; pendiente de que revise este documento.** Sin commit (el usuario decide cuándo). Continúa `2026-10-02-skill-tree-design.md` (árbol de Alucard), `2026-10-04-guts-skill-tree-design.md` (árbol de Guts) y `2026-10-04-frieren-kit-design.md` (el kit).

## 1. Qué se quiere
Un **árbol de mejoras para Frieren**, igual de funcional que los de Alucard y Guts (pestaña "Árbol" de la estación de habilidades, puntos del personaje, nodos conectados), con mejoras para el Zoltraak, el maná y sus tres habilidades. Pedidos concretos del usuario:
- **Grupos de "elegir 1"** al final de las ramas del Zoltraak, del maná, de la Q y de la F: solo se puede tener una opción; las otras quedan desactivadas.
- En la E, **4 nodos divididos en dos mitades**: en cada uno se elige **curar más** o **ralentizar más** (el cuadrado del nodo se ve partido en dos).
- **Cambiar de opción** haciendo clic en otra del mismo grupo, con una **ventana de confirmación** (no hace falta reiniciar el árbol).
- La E **envenena** a los enemigos (en vez de hacer daño directo, curar la base o dar maná).
- El dibujo **se divide como un árbol**, con buen espaciado entre nodos.

## 2. Alcance
- **Entra:** el asset `Frieren_Tree` (56 nodos) enlazado en `Maga.asset`; las reglas de grupos y nodos divididos (compra, cambio, cálculo); la ventana de confirmación y el dibujo de grupos y mitades en la vista; los efectos nuevos y su efecto en el juego; el veneno (`EnemyPoison`); pruebas.
- **No entra:** arte o sonidos nuevos (los efectos visuales son procedurales y sencillos, como los del kit), cambios a los árboles de Alucard y Guts (sus nodos no tienen grupo y siguen igual), cambios al guardado.

## 3. Los nodos (56; se pueden tener 43 a la vez; 151 puntos)
Valores y costos propuestos por mí y aprobados; se ajustan en el asset. "**Elige 1**" = grupo de exclusión (`choiceGroup`).

| Rama | Nodos (id: nombre · costo · efecto) |
|---|---|
| **Centro** | `core` Grimorio · 1 · +5% de daño (raíz) |
| **Vitalidad** (raíz) | `v1` I · 1 · +10 vida · `v2` II · 2 · +15 · `v3` III · 3 · +20 |
| **Zancada** (raíz) | `s1` I · 1 · +3% velocidad · `s2` II · 2 · +3% · `s3` III · 3 · +4% |
| **Poder arcano** (raíz) | `d1` I · 1 · +3% de daño · `d2` II · 2 · +3% · `d3` III · 4 · +4% |
| **Zoltraak** | `za1` Zoltraak afinado I · 2 · +15% de daño del Zoltraak · `za2` II · 4 · +15% · `zc1` Canalización rápida I · 2 · −0,15 s de carga · `zc2` II · 4 · −0,15 s (1,2 → 0,9 s) · `zr1` Onda expansiva I · 3 · +0,5 m de radio (mínimo y máximo) · `zr2` II · 5 · +0,5 m (2,5–4 → 3,5–5 m) |
| Zoltraak, **elige 1** (7 c/u) | `zo1` **Sobrecarga**: tras un Zoltraak al 100%, el siguiente sale cargado al instante · `zo2` **Eco**: a carga completa, 0,3 s después cae una segunda explosión con 50% del daño y 70% del radio · `zo3` **Escarcha arcana**: a carga completa, ralentiza 40% durante 3 s a los golpeados |
| **Maná** | `mr1` Reserva I · 2 · +20 de maná máximo · `mr2` II · 4 · +20 (100 → 140) · `mf1` Flujo I · 2 · +0,75/s de regeneración · `mf2` II · 4 · +0,75/s (4 → 5,5/s) |
| Maná, **elige 1** (7 c/u) | `mo1` **Eficiencia**: −40% de costo de maná de Q, E y F · `mo2` **Absorción**: +3 de maná por cada enemigo que muere · `mo3` **Concentración**: regeneración x2 si no recibió daño en los últimos 3 s |
| **Q rayo** | `qi1` Rayo intenso I · 2 · +15% de daño del rayo · `qi2` II · 4 · +15% · `qw` Rayo ancho · 3 · +0,2 m de radio (0,4 → 0,6) · `qc1` Recarga arcana I · 2 · −0,25 s de enfriamiento · `qc2` II · 4 · −0,25 s (**cambiado el 2026-10-07**: antes −0,5 s cada uno; ahora 4,5 s base, 3,5 s con la tienda al máximo y 3 s con el árbol) |
| Q, **elige 1** (7 c/u) | `qo1` **Doble carga**: +1 carga (dos rayos seguidos, se recargan de una en una) · `qo2` **Rayo gélido**: ralentiza **60%** durante 3 s a los que atraviesa · `qo3` **Perforación creciente**: +15% de daño por cada enemigo ya atravesado antes que él, con tope de +90% |
| **E campo** | `ep` Pradera · 2 · +1 m de radio · `ef` Floración larga · 3 · +1,5 s de duración · `eb` Brote rápido · 3 · −3 s de enfriamiento · `ev1` Flores venenosas · 5 · el campo envenena (ver §6) · `ev2` Veneno II · 6 · +50% de daño del veneno |
| E, **4 nodos divididos** | `es1c`/`es1r` · 2 · `es2c`/`es2r` · 3 · `es3c`/`es3r` · 4 · `es4c`/`es4r` · 5. Mitad **Curar** (`…c`): +1 punto de % de cura por segundo (3% → hasta 7%). Mitad **Ralentizar** (`…r`): +10 puntos de ralentización (40% → hasta 80%). Cada nodo se elige por separado (se puede mezclar) |
| **F pulso** | `fp1` Pulso prolongado I · 3 · +1,5 s · `fp2` II · 5 · +1,5 s (9 → 12 s) · `ft` Terror · 4 · +0,5 s de aturdimiento (1,5 → 2 s) · `fr1` Recuperación I · 4 · −8 s de enfriamiento · `fr2` II · 6 · −8 s (60 → 44 s) |
| F, **elige 1** (8 c/u) | `fo1` **Dominio**: el pulso alcanza a todos los enemigos vivos del mapa (aturdir y ralentizar) · `fo2` **Lluvia de Zoltraak**: mientras dura, cada 1 s cae del cielo un Zoltraak a carga completa sobre un enemigo al azar dentro del radio · `fo3` **Explosión final**: al terminar el pulso, todos los enemigos del radio reciben el daño de 3 Zoltraak a carga completa · `fo4` **Marca de maná**: los enemigos tocados por el pulso reciben +30% de daño de todo mientras dure |

Cuenta: 1 + 9 + 9 + 7 + 8 + 13 + 9 = **56**. Sin las alternativas (2 + 2 + 2 + 4 + 3 = 13) quedan **43** comprables a la vez, que cuestan **151 puntos** (base 20, Zoltraak 27, maná 19, Q 22, E 33, F 30), lo mismo que Alucard.

## 4. Disposición: un árbol que se divide
Las mejoras de base son **raíces** que bajan desde el centro; las habilidades son **ramas** que suben y se dividen en sub-ramas, con el abanico de "elige 1" en la punta. Coordenadas en unidades de la vista (x a la derecha, y hacia arriba; 1 unidad = 150 px):

```
   Maná      Q      Zoltraak      E       F          ← copa (ramas)
      \       \        |         /       /
       '-------'--[ GRIMORIO ]--'-------'
                  /     |     \
          Vitalidad   Poder   Zancada                ← raíces
```

| Nodo | Posición | Conecta con |
|---|---|---|
| `core` | (0, 0) | — (raíz) |
| `v1` `v2` `v3` | (−1,5, −1,5) (−2,5, −2,8) (−3,5, −4,1) | `core` → `v1` → `v2` → `v3` |
| `d1` `d2` `d3` | (0, −1,5) (0, −3) (0, −4,5) | `core` → `d1` → `d2` → `d3` |
| `s1` `s2` `s3` | (1,5, −1,5) (2,5, −2,8) (3,5, −4,1) | `core` → `s1` → `s2` → `s3` |
| `za1` | (0, 1,8) | `core` |
| `zc1` `zc2` | (−1,5, 3,3) (−1,5, 4,8) | `za1` → `zc1` → `zc2` |
| `za2` | (0, 4,2) | `za1` |
| `zr1` `zr2` | (1,5, 3,3) (1,5, 4,8) | `za1` → `zr1` → `zr2` |
| `zo1` `zo2` `zo3` | (−1,6, 6,5) (0, 6,2) (1,6, 6,5) | cada una con `za2` |
| `qi1` | (−3, 1,6) | `core` |
| `qw` | (−3, 3,3) | `qi1` |
| `qi2` | (−4,6, 3,4) | `qi1` |
| `qc1` `qc2` | (−6, 3) (−6, 4,6) | `qi1` → `qc1` → `qc2` |
| `qo1` `qo2` `qo3` | (−6, 6,2) (−4,6, 5,6) (−3,2, 6,2) | cada una con `qi2` |
| `mr1` | (−3, 0) | `core` |
| `mr2` | (−4,6, −1) | `mr1` |
| `mf1` `mf2` | (−4,6, 0,6) (−6,2, 1) | `mr1` → `mf1` → `mf2` |
| `mo1` `mo2` `mo3` | (−7,6, 2,4) (−8, 0,9) (−7,6, −0,6) | cada una con `mf2` |
| `ep` | (3, 1,6) | `core` |
| `ev1` `ev2` | (3, 3,3) (3, 4,9) | `ep` → `ev1` → `ev2` |
| `es1c`/`es1r` | (4,5, 3,4) | `ep` |
| `es2c`/`es2r` | (4,5, 4,9) | las dos mitades de `es1` |
| `es3c`/`es3r` | (4,5, 6,4) | las dos mitades de `es2` |
| `es4c`/`es4r` | (4,5, 7,9) | las dos mitades de `es3` |
| `ef` `eb` | (6, 3,2) (6,1, 4,8) | `ep` → `ef` → `eb` |
| `fp1` | (3, 0) | `core` |
| `ft` | (4,6, 1,4) | `fp1` |
| `fp2` | (4,8, −0,2) | `fp1` |
| `fr1` `fr2` | (4,4, −1,9) (5,6, −3,3) | `fp1` → `fr1` → `fr2` |
| `fo1` `fo2` `fo3` `fo4` | (7,2, 1,8) (7,6, 0,4) (7,6, −1,1) (7,2, −2,5) | cada una con `fp2` |

- Las dos mitades de un nodo dividido comparten la posición: cada mitad conecta con **las dos mitades** del siguiente, así que cambiar una mitad nunca desconecta a las de arriba.
- Distancia mínima entre centros: **1,4 unidades** (210 px; la regla general de los otros árboles es 1,16). Ninguna conexión mide más de 3,6.
- El árbol mide unas 15,6 × 12,4 unidades: más grande que la pantalla; se usa el desplazamiento y el zoom que ya existen (D35).

## 5. Grupos de "elegir 1" y nodos divididos
**Datos** (`SkillNode`, campos nuevos con valor por defecto, así que los árboles existentes no cambian):
- `choiceGroup` (texto; vacío = sin grupo). Los nodos con el mismo grupo se excluyen: solo uno comprado.
- `half` (`SkillNodeHalf`: `None`, `Left`, `Right`). Un nodo dividido son **dos nodos** con el mismo grupo y la misma posición, uno `Left` (Curar) y otro `Right` (Ralentizar).

**Guardado:** no cambia (sigue siendo la lista de ids comprados). Sin migración ni versión nueva.

**Reglas** (lógica pura en `SkillTreeRules`, con tests):
- `CanBuy`: si otro nodo del grupo ya está comprado, devuelve el motivo nuevo **`ChoiceTaken`** (se agrega al final de `SkillBuyBlock`).
- `OwnedRival(tree, owned, nodeId)`: el id del nodo comprado del mismo grupo, o null.
- `CanSwap(tree, save, newId)` → `SkillSwapBlock` (`None`, `UnknownNode`, `NoRival`, `Locked`, `NoPoints`, `WouldDisconnect`). Se puede cambiar si: hay un rival comprado; el nodo nuevo queda desbloqueado sin contar al rival; `skillPoints + costo del rival ≥ costo del nuevo`; y, después del cambio, **todos los nodos comprados siguen conectados a una raíz** por nodos comprados.
- `TrySwap`: devuelve el costo del rival, lo quita, compra el nuevo. Todo o nada.
- `Compute`: si la lista guardada tiene dos nodos del mismo grupo (por ejemplo, editada a mano), **solo cuenta el primero** en el orden de la lista.
- `Reset`: sin cambios (devuelve todo).

**Pantalla** (`SkillTreeView` / `SkillNodeButton`):
- Estado nuevo **Alternativa** (violeta apagado): un nodo de un grupo cuyo rival está comprado. No se ve gris de "bloqueado". El detalle de abajo dice: "Alternativa a *X*. Clic para cambiar: se devuelven *N* puntos y se gastan *M*."
- Cada grupo de "elige 1" lleva un **marco** alrededor de sus nodos con el texto **"Elige 1"**.
- Nodo dividido: un cuadrado partido en dos botones, **Curar** a la izquierda (tono verde) y **Ralentizar** a la derecha (tono celeste). Cada mitad tiene su estado, su selección con teclado y mando, y su detalle.
- **Ventana de confirmación** (nueva, dentro de la pestaña "Árbol", creada por código como el resto de la vista): "¿Cambiar *Eco* por *Sobrecarga*? Se devuelven 7 puntos y se gastan 7." con **Aceptar** y **Cancelar**. Se usa con ratón, teclado y mando (el foco empieza en Cancelar y la navegación queda entre los dos botones; los menús del juego no tienen tecla de "atrás", así que se cierra con Cancelar). Si `CanSwap` no deja, Aceptar sale deshabilitado con el motivo ("Faltan N puntos" o "Desconectaría otros nodos").
- `SkillTreeManager` gana `TrySwap(nodeId)`, que guarda y avisa igual que `TryBuy`.

## 6. Efectos nuevos
Se agregan **al final** de `SkillEffectType` (no se mueven los valores guardados de Alucard y Guts) y se suman en `TreeBonuses` con `SkillTreeRules.Compute`. Los genéricos (`MaxHealth`, `MoveSpeedPercent`, `DamagePercent`) se reutilizan. Las opciones de "elige 1" guardan en su valor el número que se ajusta (Eco 0,5 = fracción del daño; Escarcha 0,4 y Rayo gélido 0,6 = ralentización; Concentración 2 = multiplicador; Lluvia 1 = segundos entre Zoltraaks; Explosión final 3 = multiplicador; Marca 0,3); Sobrecarga y Dominio valen 1.

| Efecto | Valor | Dónde actúa |
|---|---|---|
| `ZoltraakDamagePercent` | +0,15 | daño del Zoltraak (también el del Eco, la Lluvia y la Explosión final) |
| `ZoltraakChargeTime` | −0,15 s | tiempo de carga; nunca baja de **0,3 s** |
| `ZoltraakRadius` | +0,5 m | radio mínimo y máximo |
| `ZoltraakOvercharge` | interruptor | `ZoltraakCaster`: tras soltar uno al 100%, el siguiente sale al 100% sin cargar (un clic) |
| `ZoltraakEcho` | interruptor | a carga completa, segunda explosión 0,3 s después en el mismo punto: 50% del daño, 70% del radio |
| `ZoltraakFrost` | interruptor | a carga completa, `EnemyAI.ApplySlow` 40% durante 3 s |
| `ManaMax` | +20 | maná máximo de `ManaPool` (al aplicar el personaje) |
| `ManaRegen` | +0,75/s | regeneración |
| `ManaCostPercent` | 0,4 | costo de maná de Q, E y F × (1 − valor), redondeado (25 → 15, 20 → 12, 50 → 30) |
| `ManaOnKill` | +3 | maná por cada enemigo que muere (`GameEvents.EnemyDied`; todos los enemigos los mata la jugadora) |
| `ManaFocus` | interruptor | regeneración x2 si pasaron 3 s sin recibir daño |
| `BeamDamagePercent` | +0,15 | daño del rayo de la Q |
| `BeamRadius` | +0,2 m | radio del `SphereCast` del rayo |
| `BeamCooldown` | −0,25 s (antes −0,5) | enfriamiento de la Q (mínimo 1 s, la regla de siempre) |
| `BeamExtraCharges` | +1 | cargas de la Q (`AbilityCharges`, como la Q de Alucard) |
| `BeamFrost` | interruptor | `PiercingBeam`: ralentiza 60% durante 3 s a cada enemigo atravesado |
| `BeamPierceDamage` | 0,15 | `PiercingBeam`: el enemigo número *k* (0 = el primero) recibe x(1 + min(0,15·k, 0,9)) |
| `FieldRadius` | +1 m | radio del campo (y del círculo de colocación) |
| `FieldDuration` | +1,5 s | duración del campo |
| `FieldCooldown` | −3 s | enfriamiento de la E |
| `FieldHeal` | +0,01 | cura por segundo (fracción de la vida máxima) |
| `FieldSlow` | +0,1 | ralentización del campo; el total no pasa de **0,8** |
| `FieldPoison` | 0,5 | el campo envenena: daño por segundo = 0,5 × daño del disparo básico (con Poder y el daño del árbol) |
| `FieldPoisonDamagePercent` | +0,5 | daño del veneno × (1 + valor) |
| `PulseDuration` | +1,5 s | duración del pulso |
| `PulseStun` | +0,5 s | aturdimiento al activarlo |
| `PulseCooldown` | −8 s | enfriamiento de la F |
| `PulseWholeMap` | interruptor | el pulso (aturdir y ralentizar) alcanza a todos los enemigos vivos |
| `PulseZoltraakRain` | interruptor | cada 1 s, un Zoltraak completo cae desde el cielo sobre un enemigo al azar del radio |
| `PulseFinalBlast` | interruptor | al terminar, 3 × daño del Zoltraak completo a todos los enemigos del radio (centrado en Frieren) |
| `PulseMark` | 0,3 | los enemigos tocados reciben +30% de daño de todo mientras dure el pulso |

Detalles:
- **Daño general** (`DamagePercent` de `core` y Poder arcano): multiplica el disparo básico, el Zoltraak (y sus derivados), el rayo y el veneno, igual que Poder de la tienda. Si hoy alguno no lo lee, se conecta.
- **Veneno** (`EnemyPoison`, nuevo, copiado de `EnemyBurn`): cada tick del campo (0,25 s) envenena a los enemigos que están dentro; el veneno hace su daño cada 0,5 s y dura **3 s desde el último toque** (no se acumula: se renueva). Tinte verde y números verdes (evento `PoisonTick`, como `BurnTick`).
- **Lluvia de Zoltraak:** un proyectil visible cae desde ~12 m de altura sobre el enemigo elegido en ~0,4 s y explota como un Zoltraak a carga completa, con todo lo del árbol (daño, radio, Eco y Escarcha). Sin enemigos en el radio, ese segundo no cae nada.
- **Marca de maná:** `EnemyAI` gana un multiplicador de daño recibido con tiempo (como `ApplySlow`), que se aplica en `TakeDamage` a todo (básico, Zoltraak, rayo, veneno).
- **Ralentizaciones que se pisan:** hoy `EnemyAI.ApplySlow` se queda con la última, así que el 40% del campo (cada 0,25 s) borraría el 60% del Rayo gélido. Cambia a **gana la más fuerte mientras dure**; una más débil no la reemplaza ni la alarga (la misma o una más fuerte sí la renuevan).
- **Dominio** reemplaza el radio de 15 m por "todos los enemigos vivos" en el aturdimiento y en la ralentización mientras dura.

## 7. Pruebas
Tests de EditMode (primero, antes del código):
- **Reglas de grupos:** comprar una opción bloquea las otras (`ChoiceTaken`); `TrySwap` devuelve y cobra bien; falla con `NoPoints` si no alcanza aun con el reembolso; falla con `WouldDisconnect` si dejaría nodos sueltos (árbol de prueba hecho a mano); cambiar una mitad de `es2` con `es3` y `es4` comprados funciona; `Compute` cuenta solo la primera de dos opciones del mismo grupo; `Reset` devuelve todo; los árboles sin grupos se comportan igual que antes.
- **`Compute`** con cada efecto nuevo: cada uno suma al campo correcto.
- **Fórmulas puras:** carga del Zoltraak con el bono (nunca menos de 0,3 s); radio con el bono; ralentización del campo con tope 0,8; cura con el bono; costo de maná con Eficiencia; multiplicador de Perforación creciente (k = 0, 1, 6, 10 → 1; 1,15; 1,9; 1,9); daño del veneno.
- **Asset `Frieren_Tree`:** 56 nodos; ids únicos; conexiones existentes; todo alcanzable desde la raíz; un solo nodo raíz; costos ≥ 1; cada grupo de "elige 1" con 3 opciones (4 en `pulso`); cada nodo dividido con exactamente una mitad `Left` y otra `Right`, misma posición y mismo costo; los nodos de un grupo final no tienen nodos que dependan de ellos; **151 puntos** contando una opción por grupo (la más cara); `Maga.asset` apunta al árbol; textos no vacíos.
- **Espaciado:** `SkillTreeLayoutTests` se extiende a `Frieren_Tree`, contando las dos mitades como un solo cuadrado, y exige para Frieren 1,4 unidades entre centros.

En Play (respaldando `save.json` y restaurándolo; bucles con tope de pasos):
- La pestaña "Árbol" aparece con Frieren con sus 56 nodos, los marcos "Elige 1" y los nodos partidos; con Alucard y Guts sigue igual.
- Comprar una opción; ver las otras en Alternativa; cambiar con la ventana (los puntos cuadran); Aceptar deshabilitado sin puntos.
- Cada efecto medido: carga y radio del Zoltraak, Sobrecarga, Eco, Escarcha; maná máximo, regeneración, Eficiencia, Absorción, Concentración; daño, ancho, enfriamiento, Doble carga, Rayo gélido y Perforación de la Q; radio, duración, enfriamiento, cura, ralentización y veneno de la E; duración, aturdimiento, enfriamiento, Dominio, Lluvia, Explosión final y Marca de la F.
- Capturas de la vista del árbol (dibujo, espaciado, mitades, ventana).
- **No verificable sin el usuario:** clic real, teclado y mando en la ventana y en las mitades, y cómo se siente el balance.

## 8. Documentación
Al terminar: `notas/DECISIONES.md` (D45), `notas/REGISTRO_DE_CAMBIOS.md`, `notas/ATAJOS_DE_TECLADO.md` si cambia algún control de la vista, y `docs/PLAN_MIGRACION_ROBLOX.md` (sistema nuevo de grupos y nodos divididos, efectos de Frieren) con su registro de sincronización.

## 9. Riesgos y decisiones abiertas
- **Balance:** Lluvia de Zoltraak con Eco son dos explosiones por segundo durante 12 s; Dominio con Terror aturde 2 s a todo el mapa; la ralentización del campo llega a 80%. Los números están en el asset y en `Baston`/`CampoFlores`/`PulsoMana`.
- **Absorción** cuenta cualquier muerte de enemigo (hoy no hay aliados ni otras fuentes de daño). Si algún día las hay, habrá que filtrar.
- Los botones de depuración del árbol ("+1 punto", "Árbol a 0") siguen existiendo y hay que borrarlos al final (D29/D30).
- Los costos y valores de los 56 nodos son supuestos míos, aprobados para empezar.
