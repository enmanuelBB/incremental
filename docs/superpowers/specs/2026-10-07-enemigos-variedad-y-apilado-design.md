# Enemigos: más grandes, dos tipos nuevos, manadas, mapa más corto y apilado (2026-10-07)

Pedido del usuario: los enemigos son pequeños y hay pocos tipos; quiere uno volador, uno que lance proyectiles, más
enemigos por oleada, que salgan más juntos, un mapa más corto (se tarda mucho en llegar a donde aparecen) y que los
enemigos **se suban unos encima de otros y se apilen, como en Megabonk**. Respuestas del usuario: todas las opciones
recomendadas (volador bajo, tirador que se detiene, tamaño x1,6, mapa ~60 x 80 con manadas).

## 1. Tamaño
- Todos los prefabs de enemigo x1,6: el normal pasa de 1 m a 1,6 m; el tanque, de 1,24 x 1,54 a ~2 x 2,5.
- Jefes x1,3 sobre su tamaño actual para seguir destacando (Embestidor 2,1; Invocador 2,9; Coloso 2,6; Tirador 3,1).
- `EnemyAI` calcula radio y altura del agente desde el collider. **Arreglado:** el `NavMeshAgent` multiplica radio,
  alto y `baseOffset` por la escala del objeto, y se le pasaban en metros del mundo; con x1,6 el normal flotaba 0,5 m
  y el tanque 1,8 m (el tanque ya flotaba ~0,4 m antes de este cambio). Ahora se pasan en unidades locales.

## 2. Volador (`Enemy_Flyer`, "Volador")
- Flota a ~2 m del suelo (`EnemyDefinition.flyHeight`) con un leve vaivén. Usa el mismo NavMesh y la misma IA del
  normal (va a la base, persigue al jugador cerca); solo cambia la altura del cuerpo (`baseOffset` del agente).
- Rápido y frágil: velocidad 3,2, 18 de vida, 8 de daño. Premio $12 / 12 XP.
- Al estar elevado hay que apuntar arriba; la espada de Guts lo alcanza igual (el cono ignora la altura).
- No se apila (ni sube ni sirve de apoyo).
- Prefab de primitivas: cuerpo aplanado celeste con dos alas.

## 3. Tirador (`Enemy_Shooter`, "Lanzador")
- Camina hacia la base como el normal. Si el jugador está a menos de 16 m lo busca, y a 14 m o menos se detiene, lo
  mira y le lanza un proyectil esquivable cada 2,5 s (8 de daño, 11 m/s, con adelanto de puntería). Al llegar a 14 m
  de la base, se queda ahí y la ataca a distancia.
- Datos nuevos en `EnemyDefinition`: `ranged` y `projectileSpeed`. El daño y el intervalo son los de siempre
  (`damageToPlayer`, `damageToBase`, `damageInterval`); el alcance es `attackReach` (14).
- El proyectil del jefe Tirador pasa a ser genérico (`EnemyProjectile`): apunta a un collider (jugador o base), con
  color y tamaño propios. El del Lanzador es verde y más chico.
- 25 de vida, velocidad 1,8, $15 / 15 XP. Prefab: cubo verde con un cañón.

## 4. Mapa y zona de aparición
- Suelo de 60 x 130 m (z -25 a 105) a **60 x 80 m** (z -25 a 55). Base, estaciones y jugador no se mueven.
- Zona de aparición de z=94 a **z=46**, de 52 x 12 a **36 x 8 m**. Recorrido hasta la base: de ~107 m a ~60 m
  (~30 s el normal en vez de 55). NavMesh rehecho.

## 5. Oleadas: más enemigos y en manadas
- Cada oleada sale en **manadas**: cada `spawnInterval` segundos aparece, alrededor de un punto al azar de la zona,
  una manada de **2 filas de frente a la base, cada fila con 3 a 5 enemigos al azar** (6 a 10 por manada), con
  **2,5 m** entre cada uno (`WaveSet.packRows`, `packRowMin/Max`, `packSpacing`; `PackFormation.RowWidths` y `Slot`,
  cada fila centrada). Solo la última manada de la oleada puede quedar más corta. Los tipos se intercalan para que
  las manadas sean mixtas (`WaveBuilder.Interleave`).
  (Versiones anteriores del mismo día: 3 a 6 en un círculo de 2,5 m; después 4 a 8 en filas fijas de 4.)
- Saltar oleada: lo que no alcanzó a salir pasa a la siguiente, igual que antes (`WaveBuilder.CountRemaining`).
- Oleadas: 1 = 20 normales (cada 3 s); 2 = 28 normales + 8 voladores (3 s); 3 = 24 normales + 6 tanques + 6
  voladores + 4 lanzadores (3 s; plantilla de las infinitas). Infinitas: +1 por tipo y oleada, una manada cada 2,5 s.
  Antes: 10, 20 y 20, con +2 por tipo y oleada.

## 6. Apilado (estilo Megabonk)
- Cada enemigo terrestre que se superpone (en planta) con otro que ya está debajo **se sube encima**: su altura de
  apoyo es la parte de arriba del más alto que pisa. Si el de abajo se va o muere, el de arriba cae.
- Lógica pura `EnemyStackRules.TargetLifts`: se procesa de abajo hacia arriba (altura actual y, a igualdad, el que
  está más cerca de su objetivo queda abajo). Se superponen si la distancia en planta es menor que 0,8 x la suma de
  los radios. **Tope: nadie apoya los pies por encima de 5 m** (unos 3 pisos de normales).
- **El que no cabe arriba se corre de lado** (3 m/s, lejos del enemigo que lo bloquea) y durante 1,5 s vuelve a
  esquivar: así el montón se ensancha en vez de crecer en una sola columna. (Primera versión, en Play: tope 8 m sin
  empuje; en la base, que mide 1 m de ancho, todos llegaban al mismo punto y se formaba una sola torre de 8 m con los
  sobrantes metidos unos dentro de otros.)
- Para que se apilen tienen que empujarse: lejos de su objetivo esquivan a los demás como hoy; **a menos de 6 m de su
  objetivo** (jugador o base) dejan de esquivar y se meten entre los otros, y así trepan. Se forman montones alrededor
  del jugador y de la base.
- La altura sube a 6 m/s y baja a 10 m/s (suavizada). Los jefes no trepan, pero los demás pueden subirse a ellos.
  Los voladores quedan fuera.
- Un enemigo en lo alto de un montón ataca igual si está al alcance en planta (el alcance ya ignora la altura).
- Lo corre `EnemyCrowd` (uno por escena, se agrega solo junto al `EnemyPool`) en `LateUpdate` con la lista de
  enemigos activos.

## 7. Balance y pruebas
- `BossBalanceTests` usa las oleadas reales: con +1 por tipo y oleada la oleada 20 se limpia en ~44 s (antes 46),
  dentro del límite de 50 s. Su comentario dice ~55 s de llegada: se actualiza a ~30 s, y se avisa al usuario que
  esa prueba es una estimación gruesa.
- Pruebas nuevas: `EnemyStackRulesTests` (apilado) y casos de `WaveBuilder` (intercalar y contar lo que falta).
- En Play: tamaño y apoyo en el suelo, volador a su altura, Lanzador disparando y dañando, manadas, montones.

## Fuera de alcance
- Modelos y animaciones reales (siguen siendo primitivas), sonidos, física real entre enemigos.
