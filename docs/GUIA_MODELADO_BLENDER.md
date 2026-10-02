# Guía de modelado en Blender para este proyecto

Contexto del proyecto + reglas para que un modelo hecho en Blender entre a Unity sin retoques.
Sirve para modelar a mano o para pedírselo a Claude con el MCP de Blender (ver el final).

> Estado al 2026-10-02: **no hay ningún modelo propio todavía.** Los enemigos son cápsulas de Unity, el bastón es un cilindro con una esfera y las pistolas de Alucard no tienen modelo. Esta guía es para reemplazar eso.

## 1. El juego en 30 segundos

- Shooter en primera/tercera persona (se cambia con **T**) estilo tower defense incremental. Unity 6 (6000.6), render **URP**.
- Defiendes una base de oleadas de enemigos y mejoras el equipo con el dinero que ganas.
- La cámara en primera persona ve el **objeto de la mano** de muy cerca: ahí el detalle sí se nota.
- En tercera persona se ve el cuerpo del jugador; los enemigos se ven siempre de lejos y en grupo.
- Personajes inspirados en anime, **con nombres y diseños propios** (evitar copyright). No copiar diseños exactos.

## 2. Qué modelos hacen falta (por prioridad)

| # | Modelo | Qué es hoy | Notas de diseño |
|---|--------|-----------|-----------------|
| 1 | **Pistolas de Alucard** (x2, izquierda y derecha) | Nada | Se turnan al disparar. Necesitan un empty `Muzzle` cada una. Tema: gótico/vampírico, rojo oscuro y negro. |
| 2 | **Enemigo normal** | Cápsula (radio 0.5, alto 2) | 30 de vida. Se ven decenas a la vez: silueta clara y barata. |
| 3 | **Enemigo tanque** | Cápsula escalada (~1,24 x 1,54 x 1,24, o sea ~3 m de alto) | 60 de vida. Debe leerse como "más grande y pesado" a distancia. |
| 4 | **Bastón de Frieren** (nombre provisional) | Cilindro + esfera (`Staff.prefab`) | Largo ~1 m, orbe arriba. Empty `Muzzle` en el orbe. Materiales: madera y orbe (emisivo). |
| 5 | **Cuerpo de Alucard** (tercera persona) | Por confirmar si hay | Humanoide, ~1,8 m. Opcional rig. |
| 6 | **Cuerpo de la maga** | Por confirmar | Igual que el anterior. |
| 7 | Base que se defiende, estaciones (mejoras, tienda, info de armas), cubo de inicio | Primitivas en la escena | Solo si se quiere pulir. |
| 8 | Jefes y más tipos de enemigo | No existen | Planeados: jefe cada cierto número de niveles. |

Armadura roja de Alucard (habilidad definitiva "Río de sangre", 8 s) y el rayo de la maga son **efectos visuales** hechos en código; si se modela la armadura, tiene que poder ponerse y quitarse en runtime (objeto aparte, hijo del cuerpo).

## 3. Reglas técnicas (Blender -> Unity)

### Escala y ejes
- **1 unidad de Blender = 1 metro = 1 unidad de Unity.** Jugador/enemigo normal ~1,8–2 m de alto (la cápsula de hoy mide 2).
- Antes de exportar: `Ctrl+A` -> **Apply All Transforms** (escala 1, rotación 0).
- Exportar **FBX** con: *Apply Scalings = FBX Units Scale*, *Forward = -Z Forward*, *Up = Y Up*, *Apply Transform* activado. (Con el exportador glTF también funciona; FBX es lo más directo con Unity.)
- Orientación: el frente del modelo debe quedar mirando a **+Z en Unity**. No lo verifiqué con un export real; probar con el primer modelo y, si sale girado, corregir la dirección en Blender o los ajustes de export.

### Pivote (origen)
- **Personajes y enemigos: origen en los pies**, centrado en X/Y. Así `transform.position` es el suelo (el spawner y el NavMesh lo esperan así).
- **Objetos de mano (pistolas, bastón): origen en la empuñadura**, donde la sujeta la mano.

### Tamaño del collider y del NavMesh (importante para enemigos)
`EnemyAI` calcula el radio y la altura del `NavMeshAgent` **a partir de los bounds del renderer** del modelo (`radio = max(extents.x, extents.z)`, `alto = extents.y * 2`).
- Mantener el modelo **compacto**: brazos abiertos, capas largas o armas que sobresalen **agrandan el radio** del agente y lo hacen atascarse.
- Enemigo normal: que quepa en ~1 m de ancho x 2 m de alto. Tanque: ~1,25 m de radio máximo x ~3 m de alto.
- El prefab trae su propio `CapsuleCollider`; ajustarlo en Unity a la silueta, no en Blender.

### Objetos de mano y disparos
- `CharacterManager` instancia `heldItemPrefab` como hijo de la cámara/`Shooting` y busca un hijo llamado **exactamente `Muzzle`** (con mayúscula). De ahí salen la bala y el rayo.
- En Blender: crear un **Empty** llamado `Muzzle` en la boca del cañón (o en el orbe del bastón), con el eje **+Z (Unity) apuntando hacia donde dispara**. Debe sobrevivir al export (es un nodo hijo del modelo).
- El bastón actual está a `(0.7, -0.5, 0.5)` respecto a la cámara en primera persona: **queda abajo a la derecha**. Las pistolas deberían ir a ambos lados (izquierda y derecha).
- Dos pistolas: modelar `Pistola_L` y `Pistola_R` (pueden ser la misma malla espejada con *Mirror* aplicado, o dos prefabs). Cada una con su `Muzzle`.

### Presupuesto de polígonos (guía, no norma)
| Modelo | Triángulos |
|--------|-----------|
| Enemigo normal | 500 – 1.500 (hay muchos en pantalla) |
| Tanque / jefe | 1.500 – 4.000 |
| Arma / bastón en mano | 1.500 – 5.000 (se ve de cerca) |
| Personaje jugable | 5.000 – 15.000 |

Preferir **low poly con colores planos o texturas pequeñas**: es lo más barato y lo más rápido de hacer.

### Materiales y texturas (URP)
- Usar **Principled BSDF** simple: Base Color, Metallic, Roughness, y Emission si brilla (orbe, ojos). Unity los convierte a URP/Lit al importar; después se reasignan en Unity.
- Sin nodos exóticos (procedurales, mix de mapas complejos): **no se exportan**. Si hay textura, hornearla (bake) a PNG y guardarla junto al FBX.
- Un material por parte con sentido (madera, orbe, metal, tela). Menos materiales = menos draw calls.
- Nombres de material claros (`Staff_Wood`, `Staff_Orb`, `Enemy_Skin`); en Unity ya existen `Staff_Wood.mat` y `Staff_Orb.mat`.
- Normales: *Shade Smooth* + *Auto Smooth/Weighted Normal* (o *Shade Flat* en estilo facetado). Revisar que no haya caras invertidas (Overlay -> Face Orientation: todo azul).

### Rig y animación (solo si se hace)
- Hoy **no hay Animator** en enemigos ni jugador. Un modelo estático funciona.
- Si se rigea: esqueleto **Humanoid** compatible con Unity (pelvis, columna, brazos, piernas; T-pose o A-pose), un solo armature, huesos con nombres sin espacios. Animaciones mínimas para enemigos: `Idle`, `Walk` (o `Run`), `Attack`, `Death`.

## 4. Convenciones de nombres y carpetas

- Archivo `.blend` fuente: `Art/Blender/<Nombre>.blend` (fuera de `Assets/` para que Unity no lo importe; **o** dentro si se quiere que Unity lo lea directo, pero entonces necesita Blender instalado).
- Export a `Assets/Art/Models/<Nombre>.fbx`; texturas en `Assets/Art/Models/Textures/`.
- Ya existen `Assets/Art/Icons/` (iconos de habilidades) y `Assets/Prefabs/`.
- Nombres de objeto en inglés o español, **sin espacios ni tildes**: `Enemy_Normal`, `Pistola_L`, `Muzzle`.
- Un FBX por modelo; las partes que cambian en runtime (armadura roja, orbe) como objetos hijos con nombre propio.

## 5. Paleta y estilo

- **Alucard** (tirador): negro, rojo sangre y hueso. La armadura de la definitiva es **roja** (armadura de Vlad Tepes). El sangrado se muestra como tinte rojo creciente sobre el enemigo, así que el **enemigo conviene de color neutro/claro** (gris, verde apagado) para que el tinte se lea.
- **Maga:** tonos fríos, madera clara y orbe emisivo (azul/cian; el rayo y el bolt usan ese color).
- Diferenciar enemigo normal y tanque por **silueta y tamaño**, no solo por color.
- Los enemigos deben leerse bien de lejos y contra el suelo de la escena.

## 6. Cómo entra el modelo al juego (después de exportar)

1. Copiar el FBX (y texturas) a `Assets/Art/Models/`.
2. En el Inspector del FBX: *Scale Factor 1*, *Convert Units* activado, *Generate Colliders* desactivado; Rig: *None* (o *Humanoid* si hay esqueleto).
3. Extraer/asignar materiales (URP/Lit).
4. **Enemigo:** abrir `enemy.prefab` o `EnemyTank.prefab`, reemplazar el `MeshFilter`/`MeshRenderer` de la cápsula por el modelo (como hijo, manteniendo `NavMeshAgent`, `EnemyAI` y collider en la raíz). Los stats **no** están en el prefab sino en `Assets/Data/Enemies/*.asset`.
5. **Arma/bastón en mano:** crear un prefab con el modelo y su `Muzzle` y asignarlo en `heldItemPrefab` de `Assets/Data/Characters/Alucard.asset` o `Maga.asset`.
6. Probar en Play y ajustar posición (`Staff.prefab` usa `(0.7, -0.5, 0.5)` como referencia).

> Aviso: probar en Play modifica el guardado real (dinero y mejoras). Respaldar `save.json` antes (ver `notas/REGISTRO_DE_CAMBIOS.md`).

## 7. Usar Claude con el MCP de Blender

Hay un MCP de Blender conectado (`mcp__blender__*`). Para pedir un modelo, dar:

1. **Qué es** y de qué personaje/arma (sección 2).
2. **Dimensiones reales** en metros y tope de triángulos (sección 3).
3. **Paleta y estilo** (sección 5).
4. Si lleva `Muzzle`, rig o partes separadas.
5. Dónde guardar: `Assets/Art/Models/<Nombre>.fbx`.

Plantilla de pedido:

```
Modela <nombre> para el juego (ver docs/GUIA_MODELADO_BLENDER.md).
- Tipo: enemigo | arma en mano | personaje
- Altura/largo: X m, máximo N triángulos
- Estilo: low poly, colores planos, paleta <...>
- Extras: empty "Muzzle" en <lugar> / partes separadas <...>
- Exportar como FBX a Assets/Art/Models/
```

Notas para quien use el MCP:
- Para una pieza **simple y a medida** (pistola, bastón, enemigo low poly) es mejor **modelar con `execute_blender_code`** (primitivas + modifiers) que generar con IA: se controlan escala, pivote, `Muzzle` y nombres.
- Los generadores (Hyper3D, Hunyuan3D, Tripo) y las descargas (Sketchfab, Poly Pizza) dan mallas con topología pesada y sin pivote ni `Muzzle`; sirven de referencia o para un jefe, pero hay que limpiarlas. Revisar licencia (los CC-BY piden crédito).
- Después de cada cambio: `get_viewport_screenshot` y `get_scene_info`; verificar `world_bounding_box` contra las medidas de la sección 3.
- Exportar con `export_scene` o `bpy.ops.export_scene.fbx(...)` usando los ajustes de la sección 3.

## 8. Checklist antes de exportar

- [ ] Escala aplicada (1,1,1), rotación (0,0,0), tamaño real en metros.
- [ ] Origen en los pies (personaje/enemigo) o en la empuñadura (arma).
- [ ] Empty `Muzzle` presente y orientado, si es un arma.
- [ ] Sin caras invertidas, sin vértices sueltos, sin objetos ocultos olvidados.
- [ ] Materiales con Principled BSDF y nombres claros; texturas horneadas si las hay.
- [ ] Triángulos dentro del presupuesto.
- [ ] Modelo compacto (para enemigos: no sobresale del radio previsto).
- [ ] Exportado a `Assets/Art/Models/` y probado en un prefab.
