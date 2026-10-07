# Alucard: árbol de habilidades ampliado (estilo Frieren)

Fecha: 2026-10-07 · Estado: **BORRADOR de brainstorming, a medias.** Falta que el usuario apruebe la sección 1 y se escriban las secciones 2 en adelante. Sin código ni commit. Continúa `2026-10-02-skill-tree-design.md` (árbol actual de Alucard) y `2026-10-06-frieren-skill-tree-design.md` (el modelo a seguir).

## 0. Dónde quedamos
- Ruta elegida: **arquitectónica** (spec escrito → plan con `writing-plans` → implementación).
- Sección 1 (forma y "Elige 1") **presentada, esperando respuesta del usuario**. Para retomar: preguntarle qué opciones quita, cambia o agrega, y si las ramas nuevas (Pistolas y Sangrado) van bien.
- Siguiente (sección 2): coordenadas del dibujo, nodos nuevos con costo, efectos nuevos (`SkillEffectType`, `TreeBonuses`), dónde actúa cada efecto en el código y pruebas.

## 1. Qué se quiere (respuestas del usuario)
Rehacer el árbol de Alucard con la misma forma que el de Frieren. Eligió traer:
- **Más nodos y forma de árbol** (raíces de stats abajo, ramas que suben y se dividen, buen espaciado).
- **Rama nueva de Sangrado y Pistolas.**
- **Grupos "Elige 1"** (con la ventana de confirmación para cambiar, que ya existe).
- **No** pidió nodos partidos (los de dos mitades de la E de Frieren).
- **Conservar los ids actuales** de los nodos y ampliar, para no romper los guardados. Puede cambiar el valor o la posición de algún nodo viejo.

## 2. Punto de partida (verificado leyendo el código y los assets)
- `Assets/Data/Skills/Alucard_Tree.asset` tiene **35 nodos** (el spec de 2026-10-02 decía 19; creció con los nodos "II"). Ids vistos: `core`, `v1`–`v3`, `s1`–`s2`, `d1`–`d3`, `q1`–`q7`, `e1`–`e8`, `f1`–`f6` (más algunos que no se leyeron; revisar el asset completo).
- El sistema de grupos y cambio **ya existe** (`SkillNode.choiceGroup`, `SkillTreeRules.CanSwap`/`TrySwap`, `SkillTreeManager.Swap`, vista con marcos "Elige 1" y ventana). Alucard y Guts no usan grupos hoy.
- Efectos actuales de Alucard en `SkillEffectType`: `HeavyShotExtraBullets/ExtraCharges/Cooldown`, `MistBleedOnPass/Slow/Duration/Cooldown`, `UltDuration/Cooldown/LifeSteal`, más los genéricos. Los nuevos se agregan **al final del enum** (no mover valores guardados).
- Código donde actúan las habilidades: `PlayerAbilities.cs` (`CastHeavyShot`, `CastMist`/`TickMist`, `CastUltimate`/`TickRiver`, `AfterBulletHit`, `HealFromDamage`). Sangrado: `BleedStacks`, `EnemyBleed`, `Shooting.BleedCap`.
- Reglas del sangrado: ticks cada 1 s, tope de pilas `4 + nivel`, daño por pila `max(1, round(bala × 10%))`, los ticks no curan.

## 3. Sección 1 del diseño (propuesta, pendiente de aprobación)
**Forma:** centro "Sed de sangre" y raíces de stats (Vitalidad, Zancada, Pulso) hacia abajo. Cinco ramas hacia arriba, de izquierda a derecha: **Pistolas · Q · Sangrado · E · F**. Cada una termina en un abanico de "Elige 1". Las cadenas actuales de Q, E y F quedan como base de su rama. Total: unos **60 nodos**, unos 45 comprables a la vez.

| Rama | Opciones "Elige 1" |
|---|---|
| **Q Disparo pesado** | **Perforante**: atraviesa y daña a todos los enemigos de la línea · **Explosiva**: explota en 3 m con 60% del daño y 2 pilas · **Ejecución**: +100% de daño a enemigos con menos del 30% de vida; si lo mata, se reinicia el enfriamiento |
| **E Niebla** | **Estela sangrienta**: rastro que da 1 pila por segundo a quien lo pise · **Emboscada**: el primer disparo básico tras la niebla hace x3 · **Bandada**: al terminar, estalla en murciélagos (4 m): daño, 3 pilas y aturde 1 s |
| **F Río de sangre** | **Sed insaciable**: cada muerte durante la definitiva suma 0,5 s de duración (tope +6 s) · **Marea roja**: el río crece x1,75 y además hace daño · **Pacto de sangre**: los ticks de sangrado van al doble de rápido · **Segundo aliento**: si mueres durante la definitiva, revives una vez con 30% de vida |
| **Sangrado** (rama nueva: tope de pilas, daño por pila, pilas por impacto) | **Hemorragia**: al llenar el tope, el enemigo sufre de golpe 5 ticks · **Contagio**: al morir un enemigo sangrando, pasa la mitad de sus pilas a los que estén a 4 m · **Banquete**: cada enemigo sangrando que muere te cura 3 de vida |
| **Pistolas** (rama nueva: cadencia, cargador, recarga) | **Balas de plata**: cada 5.º disparo hace x3 · **Rebote**: la bala rebota a otro enemigo cercano con 50% del daño · **Fuego rápido**: +40% de cadencia y −15% de daño |

Riesgos señalados al usuario: **Segundo aliento** toca la muerte del jugador y **Rebote** toca el disparo básico (el núcleo de Alucard); se pueden cambiar por algo más simple. Todos los números son supuestos míos, a ajustar en el asset.

## 4. Pendiente (secciones por escribir)
- Nodos nuevos de las ramas Sangrado y Pistolas (ids, nombres, costos, valores) y de los stats que falten.
- Coordenadas del dibujo (unidades de la vista, 1 unidad = 150 px; distancia mínima entre centros ≥ 1,4 como en Frieren) y a qué nodo se conecta cada opción "Elige 1".
- Lista de efectos nuevos (`SkillEffectType` al final del enum, `TreeBonuses`, fórmulas puras en Core) y dónde actúa cada uno.
- Pruebas: reglas de grupos con el asset de Alucard, `Compute` por efecto, asset (ids únicos, conexiones, alcanzable desde la raíz, grupos de 3 o 4, ids viejos intactos), espaciado (`SkillTreeLayoutTests`), y verificación en Play con `save.json` respaldado.
- Documentación al terminar: `notas/DECISIONES.md`, `notas/REGISTRO_DE_CAMBIOS.md`, `docs/PLAN_MIGRACION_ROBLOX.md` con su registro de sincronización.
- Recordatorio: los botones de depuración del árbol siguen existiendo y hay que borrarlos al final (D29/D30).
