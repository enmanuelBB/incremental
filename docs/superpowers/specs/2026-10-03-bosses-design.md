# Minijefes y jefes

Fecha: 2026-10-03 · Estado: **diseño aprobado por el usuario en conversación; se implementa a continuación (sin commit, por pedido suyo).** El estado real está en `notas/REGISTRO_DE_CAMBIOS.md` y `notas/DECISIONES.md` (D33).

## 1. Qué se quiere
Minijefes y jefes que rompan el ritmo de las oleadas: más vida y tamaño, premios grandes y **comportamientos que cambian a mitad de vida**. Esta entrega: **dos minijefes (oleadas 5 y 15) y dos jefes (10 y 20)**. Los comportamientos pedidos por el usuario que quedan para después: barrera que cae al destruir torretas, bombas, y las ideas extra (golpe sísmico, división, curandero, armadura por fases).

## 2. Estructura
- **Datos:** `EnemyDefinition` gana `tier` (`Normal`, `MiniBoss`, `Boss`), `treePointsReward` y una lista `abilities` de `BossAbility`. Cada habilidad lleva su tipo (`Charge`, `Summon`, `Enrage`, `Revive`, `Shoot`), su ventana de vida (`minHealthFraction`, `maxHealthFraction`: activa si `min < vida <= max`, así en cada momento una sola de dos fases consecutivas está activa) y sus números.
- **Lógica pura (Game.Core, con tests):** `BossRules` (ventanas de fase, cuándo se dispara algo una sola vez, vida al resucitar), `AbilityTimers` (temporizadores por habilidad), `BossPhaseState` (disparos únicos).
- **Oleadas:** `WaveSet.bosses` (oleada de 1 en adelante + jefe) y `bossCycleLength` (20): pasada la 20 el ciclo se repite (25 = oleada 5...). `WaveBuilder.Build` pone el jefe en `WavePlan.Boss`.
- **Juego:** `BossController` (en los prefabs de jefe) ejecuta las habilidades; `EnemyAI` ofrece lo que necesita (vida y fracción, `SetControlled`, `Enrage`, intercepción de la muerte para resucitar, multiplicador de daño). `WaveManager` saca al jefe al inicio de su oleada por el centro de la zona de aparición y cuenta las criaturas invocadas (`RegisterSummoned`) para no cerrar la oleada antes de matarlas.
- **Pantalla:** `BossHealthBarUI`: barra grande arriba al centro con el nombre, una marca en el 50% y aviso "¡Llegó el jefe: X!". Eventos: `BossAppeared`, `BossHealthChanged` (0 = murió o desapareció), `BossDefeated(puntos de árbol)`.

## 3. Los cuatro jefes (vida base; el juego ya multiplica la vida +15% por oleada desde la 4)
| Oleada | Jefe | Vida | Tamaño | Habilidades |
|---|---|---|---|---|
| 5 | Minijefe **Embestidor** | 600 | x1,6 | **Embestida** cada 8 s: se detiene 1 s (pulsa en rojo) y se lanza en línea recta 1,2 s a 14 m/s (~17 m) hacia el jugador si está a menos de 30 m, si no hacia la base; 25 de daño al jugador si lo atropella. |
| 10 | Jefe **Invocador** | 1.800 | x2,2 | **Invocar** cada 10 s a 3 enemigos normales; por debajo del 50% cada 8 s a 4 tanques y **se enfurece** (+40% de velocidad, +30% de daño). |
| 15 | Minijefe **Coloso** | 2.500 | x2,0 | Tanque lento. **Enfurecer** por debajo del 50%: +60% de velocidad y +50% de daño. |
| 20 | Jefe **Tirador** | 4.500 | x2,4 | **Disparar** al jugador cada 2 s (15 de daño, proyectil a 12 m/s); por debajo del 50%, ráfagas de 3. **Resucitar** una vez con la mitad de la vida (1 s de invulnerabilidad). |

Premios (dinero / XP / puntos del árbol): minijefes $150 / 120 XP / 2; jefes $400 / 300 XP / 4. Cada uno con prefab propio (tamaño y color: naranja, morado, rojo oscuro, azul eléctrico) para que el tamaño coincida con la navegación.

## 4. Fuera de alcance
Barrera con torretas, bombas, golpe sísmico, división y curandero; sonido y partículas; el balance fino con el mapa nuevo.

## 5. Pruebas
Tests de EditMode: ventanas de fase (en el borde exacto del 50% solo una de las dos fases está activa), disparos únicos, vida al resucitar, temporizadores, ciclo de las oleadas de jefe. En Play, un jefe por vez forzado: embiste, invoca (la oleada espera a las invocadas), dispara, resucita, y la barra aparece y desaparece.
