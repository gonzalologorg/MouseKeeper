# Mouse Keeper · Informe de balance de ocho Auto-run con semilla ×5

Corte de lectura: **2026-11-25T15:04:29-03:00 (Buenos Aires)**. Lote: `AutoRun-seeded-2026-11-25-144429`. Se leyó una instantánea de cada JSON publicado. Se verificaron los ocho procesos originales por PID y ruta del ejecutable antes del cierre.

**118 expediciones únicas:** 78 victorias, 30 derrotas, 10 interrumpidas y 0 parciales con estado in_progress. Tasa entre terminadas: **72,2 %**. Se excluyeron de las tasas las partidas abiertas, interrumpidas o con comandos de depuración. Errores o duplicados: 0. Solo se analizaron archivos finales `run_*.json`.

## Cobertura por instancia

| Instancia | JSON | Victorias | Derrotas | Interrumpidas | Parciales |
| --- | --- | --- | --- | --- | --- |
| instancia-1 | 12 | 7 | 3 | 2 | 0 |
| instancia-2 | 14 | 11 | 2 | 1 | 0 |
| instancia-3 | 14 | 9 | 4 | 1 | 0 |
| instancia-4 | 16 | 9 | 6 | 1 | 0 |
| instancia-5 | 19 | 12 | 6 | 1 | 0 |
| instancia-6 | 14 | 11 | 2 | 1 | 0 |
| instancia-7 | 13 | 9 | 2 | 2 | 0 |
| instancia-8 | 16 | 10 | 5 | 1 | 0 |

Configuraciones observadas (versión, esquema, ratas base, intervalo de spawn, tamaño base): `[('0.1.0', 3, 5, 0.35, 18.0, 14.0)]`. Mediana juego/tiempo real en terminadas: **5,00×**. El JSON no identifica commit ni velocidad nominal; sí identifica semilla y repetición. La comparación con el informe ×3 y con el lote ×5 sin semillas es descriptiva, con composición, mapas y cambios de código sin controlar.

## Resultados por dificultad

| Dificultad | Terminadas | Victorias | Juego medio s | Mediana s | Bajas/partida | Daño recibido/partida | $/partida | Puntaje medio | Derrotas P1/P2/P3 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fácil | 27 | 23/27 (85,2 %) | 330 | 321 | 157 | 323 | 47 | 2.822 | 2/1/1 |
| Normal | 36 | 27/36 (75,0 %) | 359 | 380 | 230 | 489 | 67 | 4.556 | 3/4/2 |
| Difícil | 22 | 13/22 (59,1 %) | 429 | 429 | 312 | 418 | 87 | 6.113 | 3/3/3 |
| Enjambre | 23 | 15/23 (65,2 %) | 518 | 524 | 544 | 533 | 148 | 12.103 | 0/7/1 |

| Dificultad | ×3 (informe anterior) | ×5 (este lote) |
| --- | --- | --- |
| Fácil | 8/11 (73 %) | 23/27 (85,2 %) |
| Normal | 10/13 (77 %) | 27/36 (75,0 %) |
| Difícil | 8/14 (57 %) | 13/22 (59,1 %) |
| Enjambre | 1/6 (17 %) | 15/23 (65,2 %) |

Puntaje medio: **6.776** en victoria y **4.151** en derrota. No comparar puntaje bruto entre grupos con oportunidades distintas de bajas, tiempo y botín.

## Enemigos y fuentes de daño

Daño efectivo por eventos: **47.763,4**; suma de fichas de agentes: **47.763,4**. La fuente usa `source`; el campo `detail=unknown` también puede aparecer cuando el enemigo sí está identificado.

| Fuente | Daño efectivo | % | Eventos | Apariciones del preset |
| --- | --- | --- | --- | --- |
| Boss | 11.373,6 | 23,8 % | 1721 | 321 |
| poison_cloud | 8.512,6 | 17,8 % | 1705 | — |
| Normal | 8.325,8 | 17,4 % | 1937 | 13655 |
| unknown | 5.506,8 | 11,5 % | 372 | — |
| Explosive | 4.396,0 | 9,2 % | 607 | 1645 |
| Speedy | 3.564,5 | 7,5 % | 771 | 2805 |
| Jumping | 2.408,3 | 5,0 % | 371 | 2259 |
| MussromRat | 2.150,8 | 4,5 % | 573 | 7933 |
| Tanky | 981,1 | 2,1 % | 258 | 2560 |
| PoisonRat | 544,0 | 1,1 % | 140 | 1173 |

| Preset / variante de registro | Apariciones | Bajas | Daño efectivo a enemigos |
| --- | --- | --- | --- |
| Normal | 13655 | 13487 | 413.938 |
| MussromRat | 7933 | 7861 | 107.388 |
| Speedy | 2805 | 2768 | 87.687 |
| Tanky | 2560 | 2494 | 157.550 |
| Jumping | 2259 | 2248 | 106.013 |
| Explosive | 1645 | 1602 | 76.861 |
| PoisonRat | 1173 | 1147 | 72.452 |
| Boss | 321 | 268 | 370.627 |

Hongo por generación: **{0: 1265, 1: 2416, 2: 2170, 3: 2082}**. Ratios observados dos hijas por división: G1/G0 95,5 %, G2/G1 44,9 %, G3/G2 48,0 %. No equivalen a probabilidad configurada si hubo límites o enemigos pendientes.

## Ritmo, salas y jefes

| Dificultad | Combate | Visitas | Media s | P50 s | P90 s | Spawns/visita | Caídas | Marcadas limpias |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fácil | Normal | 182 | 7,9 | 7,0 | 12,1 | 10,9 | 3 | 180 |
| Fácil | Jefe | 81 | 32,8 | 23,2 | 70,2 | 28,7 | 12 | 72 |
| Normal | Normal | 273 | 10,1 | 9,4 | 16,4 | 19,2 | 2 | 262 |
| Normal | Jefe | 114 | 29,5 | 26,1 | 46,0 | 27,9 | 39 | 89 |
| Difícil | Normal | 135 | 15,4 | 13,6 | 22,6 | 31,3 | 1 | 130 |
| Difícil | Jefe | 63 | 57,0 | 43,8 | 101,8 | 43,5 | 27 | 48 |
| Enjambre | Normal | 202 | 17,7 | 16,1 | 25,7 | 47,9 | 9 | 191 |
| Enjambre | Jefe | 63 | 55,5 | 39,6 | 119,2 | 47,3 | 16 | 54 |

Unidad: visita Combat con spawns positivos, incluso visitas incompletas o repetidas. Tiempo de combate de la visita, en segundos de juego. Un promedio bajo puede reflejar derrotas tempranas.

## Especialidades y agentes

| Grupo | Agentes | Victorias del grupo | DPS efectivo | Daño recibido medio | Curación dada media | Con caída | Caídas | Revivieron |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Bardo | 65 | 51 | 16,4 | 126,9 | 90,0 | 14 | 15 | 1 |
| Cerrajero | 62 | 40 | 21,8 | 141,5 | 0,0 | 26 | 30 | 4 |
| DPS | 59 | 39 | 26,3 | 102,0 | 0,0 | 23 | 25 | 4 |
| Médico | 72 | 49 | 15,3 | 83,8 | 100,5 | 25 | 26 | 1 |
| Tanque | 66 | 55 | 23,4 | 283,2 | 0,0 | 14 | 14 | 0 |

| Grupo | Agentes | Victorias del grupo | DPS efectivo | Daño recibido medio | Curación dada media | Con caída | Caídas | Revivieron |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Bruno | 18 | 15 | 27,0 | 105,0 | 11,7 | 3 | 3 | 0 |
| Cleo | 11 | 11 | 22,5 | 154,2 | 25,3 | 0 | 0 | 0 |
| Dante | 27 | 23 | 43,4 | 137,6 | 12,7 | 5 | 5 | 0 |
| Kira | 27 | 20 | 23,9 | 154,6 | 21,2 | 9 | 12 | 3 |
| Luna | 26 | 17 | 17,0 | 137,2 | 2,0 | 10 | 10 | 0 |
| Milo | 22 | 13 | 16,3 | 147,5 | 59,8 | 10 | 11 | 1 |
| Nico | 26 | 14 | 36,3 | 245,5 | 0,0 | 12 | 12 | 0 |
| Nora | 22 | 13 | 15,7 | 204,6 | 69,3 | 14 | 18 | 6 |
| Olivia | 19 | 18 | 19,3 | 76,1 | 74,3 | 1 | 1 | 0 |
| Pipo | 21 | 11 | 13,5 | 143,3 | 38,1 | 12 | 12 | 0 |
| Rocco | 20 | 16 | 13,1 | 99,0 | 60,5 | 4 | 4 | 0 |
| Sasha | 26 | 20 | 11,8 | 162,0 | 114,4 | 6 | 6 | 0 |
| Teo | 14 | 9 | 12,2 | 126,8 | 55,8 | 5 | 5 | 0 |
| Tito | 24 | 14 | 12,3 | 194,8 | 45,9 | 10 | 10 | 0 |
| Vera | 21 | 20 | 21,3 | 71,3 | 24,1 | 1 | 1 | 0 |

| Composición | Terminadas | Victorias |
| --- | --- | --- |
| DPS + Médico + Tanque | 15 | 13/15 |
| Bardo + Cerrajero + Médico | 13 | 9/13 |
| Bardo + Cerrajero + Tanque | 13 | 10/13 |
| Bardo + Médico + Tanque | 12 | 12/12 |
| Cerrajero + Médico + Tanque | 11 | 8/11 |
| Cerrajero + DPS + Médico | 11 | 3/11 |
| Bardo + DPS + Médico | 10 | 4/10 |
| Bardo + DPS + Tanque | 9 | 9/9 |
| Bardo + Cerrajero + DPS | 8 | 7/8 |
| Cerrajero + DPS + Tanque | 6 | 3/6 |

DPS agregado = daño efectivo / suma de segundos de combate vivo. Especialidad, arma, mapa y dificultad varían. «Revivieron» suma eventos de reanimación registrados por agente.

## Armas

| Arma | Equipamientos | Combate s | DPS efectivo | Activaciones | Impactos/activación | Críticos | Bajas | Sobredaño |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PulseBlaster | 48 | 8.082 | 50,9 | 20019 | 2,53 | 6182 | 8294 | 37.807 |
| RustyBlade | 10 | 1.077 | 50,3 | 1574 | 1,60 | 209 | 1169 | 12.551 |
| MouseSweeper | 39 | 5.569 | 27,4 | 7591 | 1,15 | 922 | 3566 | 42.815 |
| Blower | 35 | 6.160 | 22,1 | 6200 | 1,46 | 1519 | 3313 | 37.137 |
| FoamLauncher | 37 | 7.255 | 20,5 | 20155 | 0,56 | 1605 | 3973 | 34.105 |
| MouseBlaster | 54 | 9.264 | 18,5 | 32903 | 0,40 | 1479 | 4140 | 31.703 |
| MouseFlamer | 35 | 5.015 | 18,0 | 19270 | 0,35 | 1005 | 2099 | 16.268 |
| Shotgun | 34 | 4.883 | 17,9 | 5194 | 0,91 | 474 | 1930 | 26.068 |
| GasGrenade | 37 | 5.531 | 11,7 | 2240 | 5,35 | 1362 | 1541 | 4.424 |
| Tazer | 24 | 2.212 | 9,1 | 1006 | 3,37 | 491 | 315 | 1.620 |
| Whip | 30 | 7.091 | 5,6 | 3300 | 0,99 | 375 | 1023 | 8.232 |
| Cheese | 28 | 5.268 | 0,0 | 1221 | 0,00 | 0 | 0 | 0 |

Misma inclusión que el informe anterior: más de 3 segundos de combate equipado por agente/arma. El DPS incluye daño atribuido; los impactos por activación pueden superar 1 y no representan puntería. El daño no mide utilidad de control, protección o señuelo.

## Economía, curación y puntuación

| purchase | Eventos | Monto efectivo |
| --- | --- | --- |
| Health | 92 | 460,0 |
| Passive | 83 | 786,0 |
| Weapon | 22 | 322,0 |

| heal | Eventos | Monto efectivo |
| --- | --- | --- |
| medic_regeneration | 953 | 894,6 |
| room_clear | 872 | 6.510,5 |
| medic | 829 | 6.341,7 |
| bard | 151 | 5.850,0 |
| health_pickup | 123 | 2.310,6 |
| passive_max_health | 56 | 1.400,0 |
| inhaler | 23 | 265,0 |
| health_balance | 9 | 170,4 |

| money_collected | Eventos | Monto efectivo |
| --- | --- | --- |
| — | 8991 | 8.991,0 |

| money_spent | Eventos | Monto efectivo |
| --- | --- | --- |
| — | 1027 | 2.407,0 |

| money_inheritance | Eventos | Monto efectivo |
| --- | --- | --- |
| — | 119 | 1.009,0 |

En terminadas: **8.991** dólares recogidos, **2.407** gastados y **6.584** en saldos finales individuales. No inferir una tasa de recogida: el lote no registra toda la oferta de dinero ni un contrafactual sin espera.

## Navegación y calidad de telemetría

| Evento | Eventos (todas) | Expediciones afectadas | Eventos (terminadas) | Principales fuentes |
| --- | --- | --- | --- | --- |
| auto_run_stalled | 2 | 2 | 0 | no_progress: 2 |
| navigation_replan | 59 | 17 | 58 | door_stall: 59 |
| agent_unstuck | 27 | 15 | 27 | escape: 25, auto_navigation: 1, auto_loot: 1 |
| room_escape | 59 | 43 | 57 | locksmith: 59 |
| auto_run_loot_unclaimed | 18 | 4 | 18 | no_eligible_agent: 18 |
| emergency_weapon_spawned | 7 | 7 | 7 | no_offensive_weapon: 7 |
| agent_down | 110 | 39 | 110 | —: 110 |
| agent_revived | 10 | 8 | 10 | squad_revival: 10 |

| Instancia | Run | Dificultad | Piso | Sala | Tiempo del atasco s |
| --- | --- | --- | --- | --- | --- |
| instancia-1 | 8 | Difícil | 3 | 3:2,-1 | 713,6 |
| instancia-7 | 7 | Difícil | 1 | 1:3,-1 | 200,8 |

Comparación descriptiva con las cuatro instancias ×5: 9/104 atascos, 44 replanteos de puerta y 20 reubicaciones. La composición, los mapas y múltiples reglas cambiaron entre lotes.

## Control de semillas y repeticiones

| Tanda | Semilla | Dificultad | Intentos | Victorias | Derrotas | Interrumpidos | Abiertos | Series con 3 resultados | Mapas distintos P1/P2/P3 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 20261125 | Normal | 24 | 21 | 3 | 0 | 0 | 8 | 1/1/1 |
| 2 | 20365854 | Fácil | 24 | 23 | 1 | 0 | 0 | 8 | 1/1/1 |
| 3 | 20470583 | Difícil | 24 | 13 | 9 | 2 | 0 | 8 | 1/1/1 |
| 4 | 20575312 | Enjambre | 24 | 15 | 8 | 1 | 0 | 8 | 1/1/1 |
| 5 | 20680041 | Normal | 16 | 6 | 6 | 4 | 0 | 3 | 1/1/1 |
| 6 | 20784770 | Fácil | 5 | 0 | 3 | 2 | 0 | 1 | 1/1/0 |
| 7 | 20889499 | Difícil | 1 | 0 | 0 | 1 | 0 | 0 | 1/0/0 |

| Tanda | Instancia | Equipo inicial (agente / clase / arma / suerte) | Resultados R1 / R2 / R3 |
| --- | --- | --- | --- |
| 1 | instancia-1 | Tito/Dps/Whip/1; Teo/Locksmith/MouseBlaster/9; Nico/Tank/FoamLauncher/7 | defeat / defeat / completed |
| 1 | instancia-2 | Vera/Tank/PulseBlaster/1; Dante/Dps/Tazer/5; Sasha/Bard/Cheese/10 | completed / completed / completed |
| 1 | instancia-3 | Rocco/Bard/Tazer/2; Nora/Dps/MouseSweeper/1; Pipo/Locksmith/GasGrenade/3 | completed / completed / completed |
| 1 | instancia-4 | Kira/Locksmith/Blower/3; Tito/Tank/GasGrenade/7; Nora/Medic/MouseSweeper/6 | completed / completed / defeat |
| 1 | instancia-5 | Olivia/Medic/MouseBlaster/4; Teo/Tank/FoamLauncher/3; Tito/Bard/MouseSweeper/9 | completed / completed / completed |
| 1 | instancia-6 | Nico/Locksmith/Shotgun/5; Dante/Bard/MouseSweeper/9; Rocco/Medic/Tazer/2 | completed / completed / completed |
| 1 | instancia-7 | Sasha/Bard/MouseFlamer/6; Nora/Tank/Blower/4; Olivia/Locksmith/FoamLauncher/5 | completed / completed / completed |
| 1 | instancia-8 | Luna/Dps/FoamLauncher/7; Vera/Medic/MouseBlaster/10; Dante/Tank/Blower/7 | completed / completed / completed |
| 2 | instancia-1 | Vera/Tank/MouseSweeper/1; Cleo/Dps/MouseFlamer/4; Bruno/Bard/Shotgun/1 | completed / completed / completed |
| 2 | instancia-2 | Rocco/Bard/Whip/2; Kira/Dps/Blower/10; Tito/Locksmith/PulseBlaster/3 | completed / completed / completed |
| 2 | instancia-3 | Kira/Locksmith/PulseBlaster/3; Sasha/Tank/FoamLauncher/5; Rocco/Medic/MouseFlamer/6 | completed / completed / completed |
| 2 | instancia-4 | Olivia/Medic/Tazer/4; Pipo/Tank/MouseSweeper/1; Nico/Bard/MouseBlaster/9 | completed / completed / completed |
| 2 | instancia-5 | Nico/Locksmith/Blower/5; Cleo/Bard/GasGrenade/7; Dante/Medic/RustyBlade/2 | completed / completed / completed |
| 2 | instancia-6 | Sasha/Bard/MouseBlaster/6; Kira/Tank/FoamLauncher/3; Milo/Locksmith/Whip/5 | completed / completed / completed |
| 2 | instancia-7 | Luna/Dps/Shotgun/7; Sasha/Medic/MouseBlaster/9; Cleo/Tank/Whip/8 | completed / completed / completed |
| 2 | instancia-8 | Pipo/Medic/MouseFlamer/7; Nora/Locksmith/Blower/5; Rocco/Dps/Shotgun/1 | defeat / completed / completed |
| 3 | instancia-1 | Rocco/Bard/Cheese/2; Luna/Locksmith/Tazer/8; Sasha/Dps/GasGrenade/4 | defeat / interrupted / completed |
| 3 | instancia-2 | Kira/Medic/MouseSweeper/3; Bruno/Locksmith/MouseFlamer/4; Luna/Tank/Blower/7 | defeat / completed / defeat |
| 3 | instancia-3 | Olivia/Medic/Whip/4; Vera/Tank/Blower/10; Milo/Bard/MouseSweeper/9 | completed / completed / completed |
| 3 | instancia-4 | Nico/Locksmith/PulseBlaster/5; Olivia/Bard/FoamLauncher/6; Bruno/Medic/MouseBlaster/2 | defeat / completed / completed |
| 3 | instancia-5 | Sasha/Bard/Tazer/6; Luna/Tank/MouseSweeper/1; Vera/Locksmith/FoamLauncher/5 | completed / completed / completed |
| 3 | instancia-6 | Dante/Dps/Blower/7; Bruno/Tank/GasGrenade/7; Kira/Medic/FoamLauncher/8 | completed / completed / completed |
| 3 | instancia-7 | Pipo/Medic/MouseBlaster/7; Rocco/Locksmith/Whip/3; Sasha/Dps/Cheese/1 | interrupted / defeat / defeat |
| 3 | instancia-8 | Milo/Bard/Shotgun/8; Nico/Dps/MouseBlaster/9; Luna/Medic/GasGrenade/4 | defeat / defeat / defeat |
| 4 | instancia-1 | Kira/Medic/MouseBlaster/3; Teo/Locksmith/Cheese/3; Cleo/Tank/GasGrenade/7 | completed / completed / interrupted |
| 4 | instancia-2 | Olivia/Medic/Shotgun/4; Dante/Tank/FoamLauncher/8; Vera/Bard/MouseFlamer/10 | completed / completed / completed |
| 4 | instancia-3 | Nico/Locksmith/MouseFlamer/5; Milo/Bard/MouseSweeper/4; Teo/Medic/MouseBlaster/3 | defeat / defeat / defeat |
| 4 | instancia-4 | Sasha/Bard/Whip/6; Tito/Tank/GasGrenade/10; Nico/Locksmith/RustyBlade/5 | completed / defeat / defeat |
| 4 | instancia-5 | Dante/Dps/PulseBlaster/6; Teo/Tank/FoamLauncher/6; Luna/Medic/Whip/8 | completed / completed / completed |
| 4 | instancia-6 | Pipo/Medic/Tazer/7; Dante/Locksmith/MouseBlaster/2; Nora/Dps/Cheese/1 | defeat / completed / defeat |
| 4 | instancia-7 | Milo/Bard/Blower/8; Bruno/Dps/PulseBlaster/7; Tito/Medic/GasGrenade/4 | completed / completed / completed |
| 4 | instancia-8 | Nora/Dps/MouseBlaster/9; Vera/Locksmith/Whip/3; Kira/Tank/PulseBlaster/7 | completed / completed / defeat |
| 5 | instancia-2 | Nico/Locksmith/MouseBlaster/5; Rocco/Bard/Blower/3; Pipo/Medic/MouseFlamer/3 | completed / interrupted / — |
| 5 | instancia-3 | Sasha/Bard/Shotgun/6; Nico/Tank/FoamLauncher/9; Nora/Locksmith/RustyBlade/6 | defeat / interrupted / — |
| 5 | instancia-4 | Dante/Dps/MouseFlamer/6; Pipo/Tank/MouseBlaster/4; Tito/Medic/Tazer/9 | completed / defeat / defeat |
| 5 | instancia-5 | Pipo/Medic/FoamLauncher/7; Tito/Dps/GasGrenade/10; Kira/Locksmith/Shotgun/1 | defeat / defeat / defeat |
| 5 | instancia-6 | Milo/Bard/GasGrenade/8; Kira/Dps/Whip/6; Olivia/Medic/Blower/4 | completed / interrupted / — |
| 5 | instancia-7 | Nora/Dps/RustyBlade/9; Sasha/Locksmith/MouseBlaster/2; Nico/Tank/GasGrenade/7 | interrupted / — / — |
| 5 | instancia-8 | Bruno/Tank/Cheese/10; Milo/Dps/PulseBlaster/8; Luna/Bard/MouseSweeper/10 | completed / completed / completed |
| 6 | instancia-4 | Pipo/Medic/Shotgun/7; Nico/Dps/FoamLauncher/9; Dante/Locksmith/Whip/2 | interrupted / — / — |
| 6 | instancia-5 | Milo/Bard/MouseFlamer/8; Luna/Dps/MouseBlaster/5; Nora/Medic/Shotgun/5 | defeat / defeat / defeat |
| 6 | instancia-8 | Tito/Bard/RustyBlade/1; Nico/Locksmith/Tazer/2; Rocco/Dps/MouseBlaster/3 | interrupted / — / — |
| 7 | instancia-5 | Nora/Dps/Shotgun/9; Olivia/Locksmith/Whip/9; Kira/Tank/GasGrenade/8 | interrupted / — / — |

Equipo inicial y Suerte coinciden dentro de las **43 series observadas**. **10** series incluyen victoria y derrota bajo la misma configuración. Una repetición comparte mapa y equipo; las acciones en tiempo real pueden divergir. Las diferencias entre dificultades mezclan semillas y escuadrones distintos.

## Pulse Blaster por modo y tamaño del grupo

| Modo | Enemigos vivos | Activaciones | Bajas | Energía | Daño efectivo | Energía/baja | Daño/activación |
| --- | --- | --- | --- | --- | --- | --- | --- |
| primary | Todos | 10193 | 1005 | 10.193,0 | 74.183,7 | 10,14 | 7,28 |
| secondary | Todos | 9826 | 7289 | 39.304,0 | 337.271,2 | 5,39 | 34,32 |

| Modo | Enemigos vivos | Activaciones | Bajas | Energía | Daño efectivo | Energía/baja | Daño/activación |
| --- | --- | --- | --- | --- | --- | --- | --- |
| primary | 0 | 20 | 0 | 20,0 | 0,0 | — | 0,00 |
| primary | 1 | 3069 | 209 | 3.069,0 | 15.050,3 | 14,68 | 4,90 |
| primary | 2 | 5497 | 459 | 5.497,0 | 43.727,8 | 11,98 | 7,95 |
| primary | 3 | 950 | 112 | 950,0 | 9.280,9 | 8,48 | 9,77 |
| primary | 4 | 333 | 73 | 333,0 | 3.289,1 | 4,56 | 9,88 |
| primary | 5 | 131 | 44 | 131,0 | 1.258,7 | 2,98 | 9,61 |
| primary | 6 | 71 | 34 | 71,0 | 618,3 | 2,09 | 8,71 |
| primary | 7 | 29 | 12 | 29,0 | 286,3 | 2,42 | 9,87 |
| primary | 8 | 36 | 24 | 36,0 | 265,4 | 1,50 | 7,37 |
| primary | 9 | 27 | 19 | 27,0 | 211,7 | 1,42 | 7,84 |
| primary | 10 | 14 | 7 | 14,0 | 77,5 | 2,00 | 5,54 |
| primary | 11 | 6 | 5 | 6,0 | 55,1 | 1,20 | 9,18 |
| primary | 12 | 5 | 3 | 5,0 | 28,3 | 1,67 | 5,67 |
| primary | 13 | 3 | 2 | 3,0 | 9,1 | 1,50 | 3,02 |
| primary | 14 | 1 | 1 | 1,0 | 11,8 | 1,00 | 11,81 |
| primary | 15 | 1 | 1 | 1,0 | 13,2 | 1,00 | 13,22 |
| secondary | 1 | 360 | 7 | 1.440,0 | 2.963,6 | 205,71 | 8,23 |
| secondary | 2 | 337 | 45 | 1.348,0 | 5.382,8 | 29,96 | 15,97 |
| secondary | 3 | 3171 | 1100 | 12.684,0 | 72.320,5 | 11,53 | 22,81 |
| secondary | 4 | 2109 | 1229 | 8.436,0 | 63.884,4 | 6,86 | 30,29 |
| secondary | 5 | 1275 | 1074 | 5.100,0 | 46.809,1 | 4,75 | 36,71 |
| secondary | 6 | 841 | 910 | 3.364,0 | 37.074,5 | 3,70 | 44,08 |
| secondary | 7 | 542 | 647 | 2.168,0 | 27.835,8 | 3,35 | 51,36 |
| secondary | 8 | 429 | 574 | 1.716,0 | 24.855,0 | 2,99 | 57,94 |
| secondary | 9 | 298 | 523 | 1.192,0 | 19.417,3 | 2,28 | 65,16 |
| secondary | 10 | 207 | 432 | 828,0 | 14.621,3 | 1,92 | 70,63 |
| secondary | 11 | 112 | 264 | 448,0 | 8.596,1 | 1,70 | 76,75 |
| secondary | 12 | 68 | 192 | 272,0 | 5.497,7 | 1,42 | 80,85 |
| secondary | 13 | 35 | 98 | 140,0 | 3.092,7 | 1,43 | 88,36 |
| secondary | 14 | 15 | 51 | 60,0 | 1.431,2 | 1,18 | 95,41 |
| secondary | 15 | 9 | 35 | 36,0 | 936,3 | 1,03 | 104,03 |
| secondary | 16 | 4 | 28 | 16,0 | 417,3 | 0,57 | 104,31 |
| secondary | 17 | 3 | 27 | 12,0 | 262,3 | 0,44 | 87,43 |
| secondary | 18 | 4 | 32 | 16,0 | 506,1 | 0,50 | 126,52 |
| secondary | 19 | 3 | 3 | 12,0 | 527,0 | 4,00 | 175,67 |
| secondary | 23 | 2 | 4 | 8,0 | 419,6 | 2,00 | 209,78 |
| secondary | 24 | 1 | 2 | 4,0 | 211,0 | 2,00 | 211,03 |
| secondary | 29 | 1 | 12 | 4,0 | 209,7 | 0,33 | 209,72 |

Los cocientes se calculan a partir de los totales, y las filas sin bajas muestran —. Tamaño = enemigos vivos en la sala al activar. La IA favorece el pulso cuando hay varios enemigos; los grupos por modo no son un A/B.

## Cierre de la recolección

Se esperó el desenlace de la expedición que cada instancia tenía en curso al ordenar el cierre. **8/8** publicaron victoria o derrota. El registro confirma la salida de los ocho procesos. Como Auto-run reinicia de inmediato, algunos procesos llegaron a publicar un archivo de la expedición siguiente. Las partidas parciales y todas las interrupciones quedan en el inventario y fuera de las tasas de victoria. Cierres forzados: **0**. [Registro de cierre](BalanceRuns/AutoRun-seeded-2026-11-25-144429/shutdown.txt)

## Prioridades para proseguir

| Prioridad | Evidencia | Próxima prueba |
| --- | --- | --- |
| P0 · Nubes tóxicas | 8.513 de daño efectivo (**17,8 %**), frente a 2.501 y 6,7 % en la tanda ×5 anterior. | Comparar la evasión previa con la salida tras el primer impacto en el mismo mapa. Medir tiempo expuesto, impactos por nube, tiempo hasta salir y fallos de ruta. Mantener daño y radio durante esta prueba. |
| P0 · Atribución | El daño sin fuente suma 5.507 (**11,5 %**). | Etiquetar causa, arma y responsable en los caminos de daño sin atacante, incluido daño entre aliados si corresponde. |
| P1 · Puertas y atascos | 59 replanteos de puerta, 2 atascos y 0 reubicaciones atribuidas al cruce. En el lote previo hubo 44 replanteos y 9 atascos. | Mantener el cruce en fila como control. Medir espera y replanteos por puerta cruzada. Reproducir los casos del inventario de atascos y registrar objetivo, línea de visión, última posición y ruta. |
| P1 · Jefes | P90 de combate en jefe: 101,8 s en Difícil y 119,2 s en Enjambre. | Separar tiempo hasta matar al jefe de la limpieza posterior. Comparar el ritmo de refuerzos anterior y actual dejando fija la vida del jefe. Medir caídas y enemigos simultáneos en cada fase. |
| P1 · Enjambre | 7/8 derrotas ocurrieron en el segundo piso. | Usar ×2,5 como control y probar ×2,0 en los mismos mapas y equipos. Dificultad, semilla y composición cambiaron juntas en esta tanda. |
| P1 · Pulse Blaster | Primario: 10,14 energía/baja y 7,28 daño/activación. Secundario: 5,39 y 34,32. | Mantener costo y cadencia. La IA usa más el secundario en grupos grandes. Comparar modos en grupos de 3, 4, 5 y 8 con la misma vida y suerte. |
| P2 · Cuchilla y Escopeta | El 75,5 % del sangrado fue contra jefes, frente a 91,7 % previo, con distinta exposición de la Cuchilla. La Escopeta obtuvo 17,9 DPS. | Medir sangrado por segundo equipado contra el mismo jefe y daño de escopeta por banda de distancia. Mantener ambos valores actuales como control. |
| P2 · Tienda y armas de apoyo | Se gastó el **26,8 %** del efectivo recogido, frente al 19,7 % anterior. | Medir ofertas, compras bloqueadas, donaciones y saldo al entrar a tienda. Para Queso y Sopladora, medir tiempo de espera, despliegues, control y daño evitado. |

El lote registró **15.624 críticos**. Fijar Suerte en pruebas de armas: el lote previo arrancó con Suerte 1. Los resultados por semilla muestran **10 series** con al menos una victoria y una derrota bajo la misma configuración; no conviene leer una sola repetición como resultado del equipo.

### Diseño de la próxima tanda

1. Configurar un límite de tandas y cerrar cada instancia al terminar su última repetición. Guardar también la revisión de build y la variante experimental en el JSON.
2. Reusar las mismas semillas, equipos, dificultad y Suerte entre control y variante. Comenzar por evasión de veneno y navegación; cambiar una variable por vez.
3. Revisar tasas y tiempos por serie antes de agrupar. Completar las dificultades con menos observaciones y contrastar los hallazgos con partidas humanas.

Referencias: [informe anterior de cuatro instancias ×5](Reporte_balance_AutoRun_x5_2026-11-25.md) y [definición de métricas](BALANCE.md). Este informe propone experimentos; no se aplicaron cambios de balance.

## Apéndice · inventario de runs

| Instancia | N.º | Dificultad | Resultado | Piso | Salas | Bajas | Juego s | Real s | Puntaje | Archivo |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| instancia-1 | 1 | Normal | defeat | 2 | 7 | 168 | 293,4 | 59,4 | 2.668 | [20261125_174457_430_6bdf9fb6](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_174457_430_6bdf9fb6.json) |
| instancia-1 | 2 | Normal | defeat | 2 | 7 | 145 | 293,9 | 58,9 | 2.360 | [20261125_174556_531_44f248ff](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_174556_531_44f248ff.json) |
| instancia-1 | 3 | Normal | completed | 3 | 11 | 287 | 423,0 | 84,7 | 5.758 | [20261125_174655_450_4c2a563e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_174655_450_4c2a563e.json) |
| instancia-1 | 4 | Fácil | completed | 3 | 10 | 168 | 362,1 | 72,5 | 2.678 | [20261125_174820_178_bb52e14b](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_174820_178_bb52e14b.json) |
| instancia-1 | 5 | Fácil | completed | 3 | 10 | 160 | 391,3 | 78,3 | 1.752 | [20261125_174932_607_49692f37](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_174932_607_49692f37.json) |
| instancia-1 | 6 | Fácil | completed | 3 | 10 | 162 | 433,5 | 86,8 | 2.005 | [20261125_175050_947_8d9952a9](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_175050_947_8d9952a9.json) |
| instancia-1 | 7 | Difícil | defeat | 2 | 5 | 196 | 571,8 | 114,4 | 1.275 | [20261125_175217_709_608ae13a](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_175217_709_608ae13a.json) |
| instancia-1 | 8 | Difícil | interrupted | 3 | 6 | 265 | 713,6 | 142,8 | 2.035 | [20261125_175412_111_8b88f8f4](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_175412_111_8b88f8f4.json) |
| instancia-1 | 9 | Difícil | completed | 3 | 10 | 483 | 735,0 | 147,0 | 8.100 | [20261125_175635_012_68a4ef39](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_175635_012_68a4ef39.json) |
| instancia-1 | 10 | Enjambre | completed | 3 | 13 | 729 | 737,5 | 147,5 | 14.880 | [20261125_175901_996_fe51d458](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_175901_996_fe51d458.json) |
| instancia-1 | 11 | Enjambre | completed | 3 | 13 | 743 | 758,7 | 151,7 | 15.380 | [20261125_180129_547_d646b0fd](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_180129_547_d646b0fd.json) |
| instancia-1 | 12 | Enjambre | interrupted | 1 | 0 | 0 | 11,7 | 2,3 | 890 | [20261125_180401_147_c8834bae](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-1/run_20261125_180401_147_c8834bae.json) |
| instancia-2 | 1 | Normal | completed | 3 | 11 | 286 | 425,2 | 85,8 | 5.865 | [20261125_174457_427_3ca00e41](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_174457_427_3ca00e41.json) |
| instancia-2 | 2 | Normal | completed | 3 | 11 | 310 | 398,9 | 79,8 | 6.338 | [20261125_174622_839_3c3ec9df](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_174622_839_3c3ec9df.json) |
| instancia-2 | 3 | Normal | completed | 3 | 11 | 277 | 393,5 | 78,9 | 5.382 | [20261125_174742_658_b284c2a2](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_174742_658_b284c2a2.json) |
| instancia-2 | 4 | Fácil | completed | 3 | 10 | 188 | 321,1 | 64,3 | 3.622 | [20261125_174901_581_e7e78136](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_174901_581_e7e78136.json) |
| instancia-2 | 5 | Fácil | completed | 3 | 10 | 175 | 327,7 | 65,6 | 3.680 | [20261125_175005_828_3f113720](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_175005_828_3f113720.json) |
| instancia-2 | 6 | Fácil | completed | 3 | 10 | 196 | 312,3 | 62,5 | 3.955 | [20261125_175111_410_2e4d0087](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_175111_410_2e4d0087.json) |
| instancia-2 | 7 | Difícil | defeat | 3 | 9 | 339 | 406,7 | 81,4 | 7.088 | [20261125_175213_917_5798604c](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_175213_917_5798604c.json) |
| instancia-2 | 8 | Difícil | completed | 3 | 10 | 349 | 425,8 | 85,2 | 7.718 | [20261125_175335_306_608f4b23](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_175335_306_608f4b23.json) |
| instancia-2 | 9 | Difícil | defeat | 3 | 9 | 343 | 397,5 | 79,5 | 7.370 | [20261125_175500_574_06288177](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_175500_574_06288177.json) |
| instancia-2 | 10 | Enjambre | completed | 3 | 13 | 612 | 573,1 | 114,7 | 14.540 | [20261125_175620_096_03077546](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_175620_096_03077546.json) |
| instancia-2 | 11 | Enjambre | completed | 3 | 13 | 587 | 577,1 | 115,4 | 14.272 | [20261125_175814_865_2a6fe4f3](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_175814_865_2a6fe4f3.json) |
| instancia-2 | 12 | Enjambre | completed | 3 | 13 | 593 | 555,2 | 111,0 | 14.025 | [20261125_180010_295_e6cd4bb1](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_180010_295_e6cd4bb1.json) |
| instancia-2 | 13 | Normal | completed | 3 | 11 | 224 | 374,4 | 74,9 | 4.845 | [20261125_180201_332_bd1b1a65](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_180201_332_bd1b1a65.json) |
| instancia-2 | 14 | Normal | interrupted | 1 | 0 | 0 | 9,3 | 1,9 | 910 | [20261125_180316_156_35d8f9d7](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-2/run_20261125_180316_156_35d8f9d7.json) |
| instancia-3 | 1 | Normal | completed | 3 | 11 | 274 | 439,6 | 88,7 | 5.142 | [20261125_174457_432_99279660](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_174457_432_99279660.json) |
| instancia-3 | 2 | Normal | completed | 3 | 11 | 259 | 455,0 | 91,1 | 5.200 | [20261125_174625_775_d17dc51e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_174625_775_d17dc51e.json) |
| instancia-3 | 3 | Normal | completed | 3 | 11 | 225 | 371,9 | 74,4 | 4.645 | [20261125_174756_883_7dbfc721](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_174756_883_7dbfc721.json) |
| instancia-3 | 4 | Fácil | completed | 3 | 10 | 148 | 285,2 | 57,1 | 3.325 | [20261125_174911_230_0be8cae3](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_174911_230_0be8cae3.json) |
| instancia-3 | 5 | Fácil | completed | 3 | 10 | 152 | 281,3 | 56,3 | 3.185 | [20261125_175008_333_993187b9](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_175008_333_993187b9.json) |
| instancia-3 | 6 | Fácil | completed | 3 | 10 | 149 | 292,5 | 58,6 | 3.565 | [20261125_175104_663_0173128e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_175104_663_0173128e.json) |
| instancia-3 | 7 | Difícil | completed | 3 | 10 | 378 | 518,5 | 103,8 | 7.515 | [20261125_175203_239_5bd6d0f6](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_175203_239_5bd6d0f6.json) |
| instancia-3 | 8 | Difícil | completed | 3 | 10 | 437 | 576,6 | 115,3 | 7.812 | [20261125_175347_022_b9a64f3f](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_175347_022_b9a64f3f.json) |
| instancia-3 | 9 | Difícil | completed | 3 | 10 | 396 | 572,5 | 114,5 | 7.735 | [20261125_175542_353_83a36edc](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_175542_353_83a36edc.json) |
| instancia-3 | 10 | Enjambre | defeat | 2 | 6 | 303 | 353,5 | 70,7 | 6.950 | [20261125_175736_879_ca70dbfa](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_175736_879_ca70dbfa.json) |
| instancia-3 | 11 | Enjambre | defeat | 3 | 9 | 563 | 527,1 | 105,5 | 11.690 | [20261125_175847_600_9c091e4d](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_175847_600_9c091e4d.json) |
| instancia-3 | 12 | Enjambre | defeat | 2 | 6 | 360 | 333,9 | 67,0 | 7.225 | [20261125_180033_604_6b75923a](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_180033_604_6b75923a.json) |
| instancia-3 | 13 | Normal | defeat | 3 | 10 | 266 | 425,0 | 85,1 | 4.492 | [20261125_180140_521_45503d14](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_180140_521_45503d14.json) |
| instancia-3 | 14 | Normal | interrupted | 1 | 0 | 0 | 10,2 | 2,1 | 900 | [20261125_180305_618_b11f2efb](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-3/run_20261125_180305_618_b11f2efb.json) |
| instancia-4 | 1 | Normal | completed | 3 | 11 | 248 | 437,1 | 88,2 | 4.568 | [20261125_174457_423_54883e9b](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_174457_423_54883e9b.json) |
| instancia-4 | 2 | Normal | completed | 3 | 11 | 238 | 451,4 | 90,3 | 3.798 | [20261125_174625_272_30bfdd71](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_174625_272_30bfdd71.json) |
| instancia-4 | 3 | Normal | defeat | 3 | 10 | 199 | 363,2 | 72,7 | 4.175 | [20261125_174755_527_0afdc867](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_174755_527_0afdc867.json) |
| instancia-4 | 4 | Fácil | completed | 3 | 10 | 125 | 267,6 | 53,6 | 2.690 | [20261125_174908_208_57ebae23](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_174908_208_57ebae23.json) |
| instancia-4 | 5 | Fácil | completed | 3 | 10 | 143 | 267,3 | 53,5 | 3.150 | [20261125_175001_858_afe1d29b](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175001_858_afe1d29b.json) |
| instancia-4 | 6 | Fácil | completed | 3 | 10 | 139 | 267,2 | 53,5 | 3.295 | [20261125_175055_371_40de803e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175055_371_40de803e.json) |
| instancia-4 | 7 | Difícil | defeat | 2 | 5 | 189 | 262,1 | 52,5 | 3.925 | [20261125_175148_856_70fd4b9d](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175148_856_70fd4b9d.json) |
| instancia-4 | 8 | Difícil | completed | 3 | 10 | 372 | 432,9 | 86,7 | 7.795 | [20261125_175241_345_764d0daf](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175241_345_764d0daf.json) |
| instancia-4 | 9 | Difícil | completed | 3 | 10 | 355 | 377,9 | 75,6 | 8.050 | [20261125_175408_081_4138c2b5](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175408_081_4138c2b5.json) |
| instancia-4 | 10 | Enjambre | completed | 3 | 13 | 839 | 785,5 | 157,1 | 19.385 | [20261125_175523_671_ab5ee935](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175523_671_ab5ee935.json) |
| instancia-4 | 11 | Enjambre | defeat | 2 | 6 | 320 | 340,5 | 68,1 | 6.878 | [20261125_175800_843_deb21c4f](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175800_843_deb21c4f.json) |
| instancia-4 | 12 | Enjambre | defeat | 2 | 5 | 337 | 290,2 | 58,2 | 7.320 | [20261125_175908_862_6b9dd110](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_175908_862_6b9dd110.json) |
| instancia-4 | 13 | Normal | completed | 3 | 11 | 287 | 401,9 | 80,4 | 5.390 | [20261125_180007_064_1e62cdfc](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_180007_064_1e62cdfc.json) |
| instancia-4 | 14 | Normal | defeat | 2 | 7 | 153 | 255,3 | 51,1 | 2.872 | [20261125_180127_564_428425f0](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_180127_564_428425f0.json) |
| instancia-4 | 15 | Normal | defeat | 2 | 7 | 160 | 248,0 | 49,7 | 3.365 | [20261125_180218_637_232b924b](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_180218_637_232b924b.json) |
| instancia-4 | 16 | Fácil | interrupted | 1 | 0 | 0 | 9,6 | 2,0 | 910 | [20261125_180308_352_c02cbbc1](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-4/run_20261125_180308_352_c02cbbc1.json) |
| instancia-5 | 1 | Normal | completed | 3 | 11 | 222 | 333,0 | 67,4 | 4.892 | [20261125_174457_429_9a1fdd54](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_174457_429_9a1fdd54.json) |
| instancia-5 | 2 | Normal | completed | 3 | 11 | 227 | 316,0 | 63,2 | 5.362 | [20261125_174604_431_40688d8a](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_174604_431_40688d8a.json) |
| instancia-5 | 3 | Normal | completed | 3 | 11 | 254 | 379,9 | 76,1 | 5.330 | [20261125_174707_666_56d5d8b2](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_174707_666_56d5d8b2.json) |
| instancia-5 | 4 | Fácil | completed | 3 | 10 | 174 | 292,4 | 58,5 | 3.935 | [20261125_174823_733_9181cc7e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_174823_733_9181cc7e.json) |
| instancia-5 | 5 | Fácil | completed | 3 | 10 | 151 | 274,1 | 54,9 | 3.530 | [20261125_174922_247_d81d0219](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_174922_247_d81d0219.json) |
| instancia-5 | 6 | Fácil | completed | 3 | 10 | 137 | 252,9 | 50,6 | 3.178 | [20261125_175017_133_e003a47f](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175017_133_e003a47f.json) |
| instancia-5 | 7 | Difícil | completed | 3 | 10 | 336 | 396,4 | 79,4 | 7.652 | [20261125_175107_739_d1421a56](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175107_739_d1421a56.json) |
| instancia-5 | 8 | Difícil | completed | 3 | 10 | 367 | 453,1 | 90,7 | 7.815 | [20261125_175227_137_0da6aaf1](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175227_137_0da6aaf1.json) |
| instancia-5 | 9 | Difícil | completed | 3 | 10 | 323 | 406,7 | 81,4 | 7.030 | [20261125_175357_792_48a52533](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175357_792_48a52533.json) |
| instancia-5 | 10 | Enjambre | completed | 3 | 13 | 602 | 457,5 | 91,5 | 14.968 | [20261125_175519_159_de9c098d](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175519_159_de9c098d.json) |
| instancia-5 | 11 | Enjambre | completed | 3 | 13 | 562 | 458,5 | 91,7 | 13.950 | [20261125_175650_715_7a657e99](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175650_715_7a657e99.json) |
| instancia-5 | 12 | Enjambre | completed | 3 | 13 | 598 | 456,3 | 91,3 | 13.718 | [20261125_175822_434_39f0b719](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175822_434_39f0b719.json) |
| instancia-5 | 13 | Normal | defeat | 1 | 2 | 47 | 133,3 | 26,7 | 1.000 | [20261125_175953_727_d1882ff3](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_175953_727_d1882ff3.json) |
| instancia-5 | 14 | Normal | defeat | 1 | 2 | 55 | 119,9 | 24,1 | 1.052 | [20261125_180020_342_b9d3e7bf](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_180020_342_b9d3e7bf.json) |
| instancia-5 | 15 | Normal | defeat | 1 | 2 | 48 | 118,8 | 23,8 | 1.000 | [20261125_180044_441_de7d70bc](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_180044_441_de7d70bc.json) |
| instancia-5 | 16 | Fácil | defeat | 2 | 5 | 61 | 218,2 | 43,7 | 490 | [20261125_180108_315_7b8a6b00](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_180108_315_7b8a6b00.json) |
| instancia-5 | 17 | Fácil | defeat | 1 | 4 | 33 | 153,6 | 30,8 | 430 | [20261125_180152_076_ef16e516](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_180152_076_ef16e516.json) |
| instancia-5 | 18 | Fácil | defeat | 1 | 4 | 44 | 160,0 | 32,1 | 415 | [20261125_180222_897_e9956048](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_180222_897_e9956048.json) |
| instancia-5 | 19 | Difícil | interrupted | 1 | 0 | 0 | 1,7 | 0,4 | 990 | [20261125_180255_008_be008c41](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-5/run_20261125_180255_008_be008c41.json) |
| instancia-6 | 1 | Normal | completed | 3 | 11 | 246 | 316,7 | 64,1 | 5.548 | [20261125_174457_427_84c07d4d](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_174457_427_84c07d4d.json) |
| instancia-6 | 2 | Normal | completed | 3 | 11 | 236 | 301,5 | 60,4 | 5.490 | [20261125_174601_166_5d376849](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_174601_166_5d376849.json) |
| instancia-6 | 3 | Normal | completed | 3 | 11 | 285 | 385,2 | 77,1 | 5.170 | [20261125_174701_504_ed017732](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_174701_504_ed017732.json) |
| instancia-6 | 4 | Fácil | completed | 3 | 10 | 238 | 472,0 | 94,5 | 3.835 | [20261125_174818_621_e583b388](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_174818_621_e583b388.json) |
| instancia-6 | 5 | Fácil | completed | 3 | 10 | 195 | 417,0 | 83,4 | 3.275 | [20261125_174953_084_e8331d12](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_174953_084_e8331d12.json) |
| instancia-6 | 6 | Fácil | completed | 3 | 10 | 201 | 402,5 | 80,5 | 3.202 | [20261125_175116_493_d1c2f345](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_175116_493_d1c2f345.json) |
| instancia-6 | 7 | Difícil | completed | 3 | 10 | 349 | 467,7 | 93,6 | 7.215 | [20261125_175237_035_badef73d](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_175237_035_badef73d.json) |
| instancia-6 | 8 | Difícil | completed | 3 | 10 | 310 | 462,5 | 92,5 | 6.922 | [20261125_175410_639_ead3c9e9](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_175410_639_ead3c9e9.json) |
| instancia-6 | 9 | Difícil | completed | 3 | 10 | 348 | 461,2 | 92,3 | 7.120 | [20261125_175543_145_a09b8389](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_175543_145_a09b8389.json) |
| instancia-6 | 10 | Enjambre | defeat | 2 | 6 | 323 | 330,7 | 66,2 | 7.522 | [20261125_175715_423_9abc14ed](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_175715_423_9abc14ed.json) |
| instancia-6 | 11 | Enjambre | completed | 3 | 13 | 545 | 493,9 | 98,8 | 14.065 | [20261125_175821_584_e15118a3](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_175821_584_e15118a3.json) |
| instancia-6 | 12 | Enjambre | defeat | 2 | 6 | 294 | 319,1 | 63,8 | 6.990 | [20261125_180000_538_1ea5fbb8](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_180000_538_1ea5fbb8.json) |
| instancia-6 | 13 | Normal | completed | 3 | 11 | 325 | 572,2 | 114,5 | 5.238 | [20261125_180104_317_9f695a25](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_180104_317_9f695a25.json) |
| instancia-6 | 14 | Normal | interrupted | 1 | 0 | 0 | 15,6 | 3,2 | 850 | [20261125_180258_889_a2dac4ce](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-6/run_20261125_180258_889_a2dac4ce.json) |
| instancia-7 | 1 | Normal | completed | 3 | 11 | 262 | 380,1 | 76,8 | 5.470 | [20261125_174457_432_2242a5df](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_174457_432_2242a5df.json) |
| instancia-7 | 2 | Normal | completed | 3 | 11 | 262 | 421,8 | 84,4 | 5.125 | [20261125_174613_825_e4ff1b1f](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_174613_825_e4ff1b1f.json) |
| instancia-7 | 3 | Normal | completed | 3 | 11 | 346 | 436,0 | 87,2 | 6.728 | [20261125_174738_208_ea117a45](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_174738_208_ea117a45.json) |
| instancia-7 | 4 | Fácil | completed | 3 | 10 | 208 | 491,5 | 98,3 | 3.352 | [20261125_174905_429_75634ae0](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_174905_429_75634ae0.json) |
| instancia-7 | 5 | Fácil | completed | 3 | 10 | 226 | 481,0 | 96,2 | 2.962 | [20261125_175043_759_bedbd19b](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_175043_759_bedbd19b.json) |
| instancia-7 | 6 | Fácil | completed | 3 | 10 | 204 | 479,5 | 95,9 | 2.855 | [20261125_175219_992_f53eb11a](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_175219_992_f53eb11a.json) |
| instancia-7 | 7 | Difícil | interrupted | 1 | 0 | 18 | 200,8 | 40,2 | -592 | [20261125_175355_917_3d9a1aba](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_175355_917_3d9a1aba.json) |
| instancia-7 | 8 | Difícil | defeat | 2 | 5 | 257 | 400,6 | 80,2 | 3.545 | [20261125_175436_099_235286f9](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_175436_099_235286f9.json) |
| instancia-7 | 9 | Difícil | defeat | 3 | 9 | 490 | 725,3 | 145,1 | 7.870 | [20261125_175556_292_efa4f90c](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_175556_292_efa4f90c.json) |
| instancia-7 | 10 | Enjambre | completed | 3 | 13 | 557 | 524,3 | 104,9 | 12.372 | [20261125_175821_485_d6f51b79](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_175821_485_d6f51b79.json) |
| instancia-7 | 11 | Enjambre | completed | 3 | 13 | 556 | 525,6 | 105,1 | 12.690 | [20261125_180006_456_1ea4b530](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_180006_456_1ea4b530.json) |
| instancia-7 | 12 | Enjambre | completed | 3 | 13 | 650 | 536,9 | 107,4 | 13.760 | [20261125_180151_599_c09ef72f](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_180151_599_c09ef72f.json) |
| instancia-7 | 13 | Normal | interrupted | 1 | 0 | 0 | 8,7 | 1,8 | 920 | [20261125_180338_970_be4cf74e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-7/run_20261125_180338_970_be4cf74e.json) |
| instancia-8 | 1 | Normal | completed | 3 | 11 | 298 | 364,5 | 73,7 | 6.035 | [20261125_174457_425_7a8daf22](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_174457_425_7a8daf22.json) |
| instancia-8 | 2 | Normal | completed | 3 | 11 | 220 | 337,6 | 67,6 | 5.418 | [20261125_174610_769_fedd9c44](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_174610_769_fedd9c44.json) |
| instancia-8 | 3 | Normal | completed | 3 | 11 | 245 | 345,8 | 69,4 | 5.442 | [20261125_174718_378_9d3edb4d](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_174718_378_9d3edb4d.json) |
| instancia-8 | 4 | Fácil | defeat | 3 | 9 | 166 | 340,9 | 68,2 | 2.422 | [20261125_174827_688_5c695e3e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_174827_688_5c695e3e.json) |
| instancia-8 | 5 | Fácil | completed | 3 | 10 | 166 | 323,8 | 64,9 | 2.958 | [20261125_174935_900_a6cc7099](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_174935_900_a6cc7099.json) |
| instancia-8 | 6 | Fácil | completed | 3 | 10 | 142 | 335,4 | 67,2 | 2.452 | [20261125_175040_796_cf1832ae](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175040_796_cf1832ae.json) |
| instancia-8 | 7 | Difícil | defeat | 1 | 2 | 66 | 119,3 | 23,9 | 1.540 | [20261125_175147_959_39b9c63e](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175147_959_39b9c63e.json) |
| instancia-8 | 8 | Difícil | defeat | 1 | 2 | 94 | 126,2 | 25,3 | 1.878 | [20261125_175211_886_1c6550b8](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175211_886_1c6550b8.json) |
| instancia-8 | 9 | Difícil | defeat | 1 | 2 | 79 | 134,6 | 27,0 | 1.510 | [20261125_175237_227_a861a6e0](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175237_227_a861a6e0.json) |
| instancia-8 | 10 | Enjambre | completed | 3 | 13 | 745 | 729,2 | 145,9 | 14.390 | [20261125_175304_265_6bcdeee2](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175304_265_6bcdeee2.json) |
| instancia-8 | 11 | Enjambre | completed | 3 | 13 | 703 | 780,5 | 156,1 | 14.205 | [20261125_175530_338_e18e8254](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175530_338_e18e8254.json) |
| instancia-8 | 12 | Enjambre | defeat | 2 | 6 | 383 | 461,9 | 92,3 | 7.202 | [20261125_175806_380_3384ce58](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175806_380_3384ce58.json) |
| instancia-8 | 13 | Normal | completed | 3 | 11 | 241 | 386,5 | 77,4 | 4.670 | [20261125_175938_696_9b28e622](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_175938_696_9b28e622.json) |
| instancia-8 | 14 | Normal | completed | 3 | 11 | 231 | 386,3 | 77,3 | 4.592 | [20261125_180056_127_03081fbf](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_180056_127_03081fbf.json) |
| instancia-8 | 15 | Normal | completed | 3 | 11 | 207 | 436,7 | 87,4 | 3.575 | [20261125_180213_425_2987488d](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_180213_425_2987488d.json) |
| instancia-8 | 16 | Fácil | interrupted | 1 | 0 | 0 | 11,1 | 2,2 | 890 | [20261125_180340_805_16b35f5f](BalanceRuns/AutoRun-seeded-2026-11-25-144429/instancia-8/run_20261125_180340_805_16b35f5f.json) |
