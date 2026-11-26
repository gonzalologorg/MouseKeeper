# Mouse Keeper · Informe de balance de cuatro Auto-run ×5

Corte de lectura: **2026-11-25T14:17:38-03:00 (Buenos Aires)**. Lote: `AutoRun-x5-2026-11-25-134019`. Se leyó una instantánea de cada JSON publicado. Los procesos pueden seguir generando o actualizando expediciones después del corte.

**104 expediciones únicas:** 70 victorias, 21 derrotas, 9 interrumpidas y 4 en curso. Tasa entre terminadas: **76,9 %**. Se excluyeron de las tasas las partidas abiertas, interrumpidas o con comandos de depuración. Errores o duplicados: 0. Solo se analizaron archivos finales `run_*.json`.

## Cobertura por instancia

| Instancia | JSON | Victorias | Derrotas | Interrumpidas | En curso |
| --- | --- | --- | --- | --- | --- |
| instancia-1 | 31 | 17 | 8 | 5 | 1 |
| instancia-2 | 23 | 18 | 4 | 0 | 1 |
| instancia-3 | 25 | 17 | 3 | 4 | 1 |
| instancia-4 | 25 | 18 | 6 | 0 | 1 |

Configuraciones observadas (versión, esquema, ratas base, intervalo de spawn, tamaño base): `[('0.1.0', 2, 5, 0.35, 18.0, 14.0)]`. Mediana juego/tiempo real en terminadas: **5,00×**. El JSON no identifica commit, semilla o velocidad nominal. La comparación con el informe ×3 es descriptiva, con composición, mapas y cambios de código sin controlar.

## Resultados por dificultad

| Dificultad | Terminadas | Victorias | Juego medio s | Mediana s | Bajas/partida | Daño recibido/partida | $/partida | Puntaje medio | Derrotas P1/P2/P3 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fácil | 23 | 20/23 (87,0 %) | 337 | 321 | 175 | 341 | 48 | 3.251 | 0/2/1 |
| Normal | 26 | 18/26 (69,2 %) | 394 | 387 | 273 | 387 | 78 | 5.437 | 2/2/4 |
| Difícil | 25 | 20/25 (80,0 %) | 468 | 461 | 397 | 418 | 113 | 8.253 | 2/2/1 |
| Enjambre | 17 | 12/17 (70,6 %) | 514 | 517 | 511 | 524 | 142 | 10.921 | 2/2/1 |

| Dificultad | ×3 (informe anterior) | ×5 (este lote) |
| --- | --- | --- |
| Fácil | 8/11 (73 %) | 20/23 (87,0 %) |
| Normal | 10/13 (77 %) | 18/26 (69,2 %) |
| Difícil | 8/14 (57 %) | 20/25 (80,0 %) |
| Enjambre | 1/6 (17 %) | 12/17 (70,6 %) |

Puntaje medio: **7.520** en victoria y **3.890** en derrota. No comparar puntaje bruto entre grupos con oportunidades distintas de bajas, tiempo y botín.

## Enemigos y fuentes de daño

Daño efectivo por eventos: **37.263,9**; suma de fichas de agentes: **37.263,9**. La fuente usa `source`; el campo `detail=unknown` también puede aparecer cuando el enemigo sí está identificado.

| Fuente | Daño efectivo | % | Eventos | Apariciones del preset |
| --- | --- | --- | --- | --- |
| Normal | 9.365,4 | 25,1 % | 2211 | 12633 |
| Boss | 8.359,4 | 22,4 % | 1283 | 262 |
| Speedy | 4.174,7 | 11,2 % | 923 | 2692 |
| Explosive | 3.987,4 | 10,7 % | 550 | 1420 |
| unknown | 3.065,0 | 8,2 % | 218 | — |
| poison_cloud | 2.500,7 | 6,7 % | 504 | — |
| MussromRat | 2.281,9 | 6,1 % | 616 | 7825 |
| Jumping | 2.037,5 | 5,5 % | 317 | 2031 |
| Tanky | 960,1 | 2,6 % | 260 | 2166 |
| PoisonRat | 531,8 | 1,4 % | 148 | 1138 |

| Preset / variante de registro | Apariciones | Bajas | Daño efectivo a enemigos |
| --- | --- | --- | --- |
| Normal | 12633 | 12448 | 383.306 |
| MussromRat | 7825 | 7781 | 104.616 |
| Speedy | 2692 | 2654 | 84.502 |
| Tanky | 2166 | 2103 | 133.611 |
| Jumping | 2031 | 2025 | 95.423 |
| Explosive | 1420 | 1384 | 66.546 |
| PoisonRat | 1138 | 1107 | 69.259 |
| Boss | 262 | 236 | 348.559 |

Hongo por generación: **{0: 1192, 1: 2300, 2: 2201, 3: 2132}**. Ratios observados dos hijas por división: G1/G0 96,5 %, G2/G1 47,8 %, G3/G2 48,4 %. No equivalen a probabilidad configurada si hubo límites o enemigos pendientes.

## Ritmo, salas y jefes

| Dificultad | Combate | Visitas | Media s | P50 s | P90 s | Spawns/visita | Caídas | Marcadas limpias |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fácil | Normal | 241 | 7,3 | 6,8 | 12,1 | 10,6 | 1 | 239 |
| Fácil | Jefe | 72 | 22,8 | 18,1 | 40,0 | 21,3 | 16 | 64 |
| Normal | Normal | 263 | 10,0 | 9,2 | 15,2 | 19,3 | 4 | 259 |
| Normal | Jefe | 77 | 35,5 | 29,5 | 61,9 | 27,3 | 23 | 64 |
| Difícil | Normal | 271 | 12,9 | 11,8 | 19,4 | 29,3 | 11 | 261 |
| Difícil | Jefe | 69 | 44,9 | 34,5 | 73,9 | 30,6 | 8 | 64 |
| Enjambre | Normal | 193 | 16,5 | 15,3 | 24,2 | 38,4 | 15 | 181 |
| Enjambre | Jefe | 44 | 47,4 | 22,5 | 98,2 | 32,9 | 9 | 40 |

Unidad: visita Combat con spawns positivos, incluso visitas incompletas o repetidas. Tiempo de combate de la visita, en segundos de juego. Un promedio bajo puede reflejar derrotas tempranas.

## Especialidades y agentes

| Grupo | Agentes | Victorias del grupo | DPS efectivo | Daño recibido medio | Curación dada media | Con caída | Caídas | Revivieron |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Bardo | 53 | 41 | 21,6 | 95,7 | 69,1 | 14 | 16 | 3 |
| Cerrajero | 53 | 41 | 19,7 | 97,7 | 0,0 | 18 | 21 | 3 |
| DPS | 54 | 40 | 27,6 | 108,7 | 1,9 | 16 | 20 | 6 |
| Médico | 56 | 41 | 19,3 | 73,7 | 113,8 | 16 | 18 | 3 |
| Tanque | 57 | 47 | 17,8 | 298,5 | 0,0 | 11 | 12 | 2 |

| Grupo | Agentes | Victorias del grupo | DPS efectivo | Daño recibido medio | Curación dada media | Con caída | Caídas | Revivieron |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Bruno | 13 | 11 | 30,8 | 73,6 | 13,3 | 3 | 3 | 0 |
| Cleo | 13 | 10 | 21,8 | 179,3 | 23,6 | 3 | 3 | 0 |
| Dante | 21 | 18 | 31,9 | 116,2 | 5,1 | 3 | 3 | 0 |
| Kira | 14 | 9 | 21,2 | 180,9 | 23,7 | 5 | 5 | 0 |
| Luna | 21 | 19 | 17,4 | 86,5 | 34,4 | 4 | 4 | 1 |
| Milo | 16 | 13 | 12,4 | 133,6 | 32,3 | 4 | 5 | 1 |
| Nico | 22 | 18 | 18,1 | 118,0 | 60,7 | 5 | 5 | 1 |
| Nora | 22 | 13 | 14,9 | 163,0 | 55,4 | 11 | 14 | 3 |
| Olivia | 25 | 20 | 24,2 | 88,2 | 27,7 | 5 | 6 | 1 |
| Pipo | 22 | 17 | 21,6 | 195,3 | 13,2 | 6 | 8 | 2 |
| Rocco | 13 | 10 | 30,6 | 102,7 | 108,5 | 4 | 5 | 2 |
| Sasha | 20 | 15 | 20,5 | 244,8 | 67,1 | 5 | 6 | 2 |
| Teo | 20 | 15 | 22,6 | 98,4 | 11,7 | 6 | 7 | 1 |
| Tito | 17 | 12 | 20,7 | 139,9 | 41,4 | 6 | 7 | 1 |
| Vera | 14 | 10 | 15,1 | 127,7 | 53,8 | 5 | 6 | 2 |

| Composición | Terminadas | Victorias |
| --- | --- | --- |
| Bardo + DPS + Médico | 15 | 11/15 |
| Bardo + Cerrajero + Tanque | 13 | 13/13 |
| Cerrajero + Médico + Tanque | 12 | 9/12 |
| DPS + Médico + Tanque | 9 | 9/9 |
| Cerrajero + DPS + Tanque | 9 | 7/9 |
| Cerrajero + DPS + Médico | 8 | 4/8 |
| Bardo + DPS + Tanque | 7 | 5/7 |
| Bardo + Médico + Tanque | 7 | 4/7 |
| Bardo + Cerrajero + DPS | 6 | 4/6 |
| Bardo + Cerrajero + Médico | 5 | 4/5 |

DPS agregado = daño efectivo / suma de segundos de combate vivo. Especialidad, arma, mapa y dificultad varían. «Revivieron» suma eventos de reanimación registrados por agente.

## Armas

| Arma | Equipamientos | Combate s | DPS efectivo | Activaciones | Impactos/activación | Críticos | Bajas | Sobredaño |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| RustyBlade | 38 | 4.869 | 42,1 | 5569 | 1,37 | 0 | 4039 | 46.952 |
| PulseBlaster | 40 | 7.359 | 38,5 | 17347 | 2,09 | 0 | 5598 | 24.350 |
| MouseSweeper | 41 | 6.141 | 26,1 | 8498 | 1,13 | 0 | 4193 | 48.233 |
| MouseBlaster | 43 | 7.096 | 25,6 | 27887 | 0,52 | 0 | 4401 | 32.559 |
| Blower | 35 | 6.768 | 21,3 | 6819 | 1,50 | 0 | 3577 | 36.118 |
| FoamLauncher | 33 | 5.312 | 15,8 | 12753 | 0,53 | 0 | 2423 | 18.959 |
| MouseFlamer | 38 | 6.088 | 15,8 | 21606 | 0,36 | 0 | 2534 | 18.343 |
| Shotgun | 24 | 2.218 | 12,5 | 2542 | 0,83 | 0 | 666 | 4.994 |
| GasGrenade | 26 | 3.418 | 9,8 | 1362 | 4,90 | 0 | 876 | 2.330 |
| Tazer | 20 | 1.835 | 9,0 | 898 | 3,34 | 0 | 259 | 1.425 |
| Whip | 28 | 4.519 | 5,8 | 2099 | 0,99 | 0 | 588 | 4.074 |
| Cheese | 16 | 4.373 | 0,0 | 1629 | 0,00 | 0 | 0 | 0 |

Misma inclusión que el informe anterior: más de 3 segundos de combate equipado por agente/arma. El DPS incluye daño atribuido; los impactos por activación pueden superar 1 y no representan puntería. El daño no mide utilidad de control, protección o señuelo.

## Economía, curación y puntuación

| purchase | Eventos | Monto efectivo |
| --- | --- | --- |
| Passive | 80 | 480,0 |
| Health | 65 | 195,0 |
| Weapon | 32 | 288,0 |

| heal | Eventos | Monto efectivo |
| --- | --- | --- |
| room_clear | 929 | 7.116,0 |
| medic | 677 | 5.667,7 |
| medic_regeneration | 629 | 656,7 |
| health_pickup | 89 | 1.712,9 |
| bard | 89 | 3.664,5 |
| passive_max_health | 16 | 400,0 |
| inhaler | 15 | 237,9 |
| health_balance | 10 | 257,7 |
| revive_passive | 3 | 150,0 |

| money_collected | Eventos | Monto efectivo |
| --- | --- | --- |
| — | 8382 | 8.382,0 |

| money_spent | Eventos | Monto efectivo |
| --- | --- | --- |
| — | 855 | 1.650,0 |

| money_inheritance | Eventos | Monto efectivo |
| --- | --- | --- |
| — | 90 | 990,0 |

En terminadas: **8.382** dólares recogidos, **1.650** gastados y **6.732** en saldos finales individuales. No inferir una tasa de recogida: el lote no registra toda la oferta de dinero ni un contrafactual sin espera.

## Navegación y calidad de telemetría

| Evento | Eventos (todas) | Expediciones afectadas | Eventos (terminadas) | Principales fuentes |
| --- | --- | --- | --- | --- |
| auto_run_stalled | 9 | 9 | 0 | no_progress: 9 |
| navigation_replan | 44 | 15 | 42 | door_stall: 44 |
| agent_unstuck | 20 | 14 | 19 | escape: 15, auto_navigation: 5 |
| room_escape | 37 | 24 | 37 | locksmith: 37 |
| auto_run_loot_unclaimed | 2 | 2 | 2 | no_eligible_agent: 2 |
| emergency_weapon_spawned | 6 | 6 | 6 | no_offensive_weapon: 6 |
| agent_down | 87 | 31 | 87 | —: 87 |
| agent_revived | 17 | 15 | 17 | squad_revival: 14, revive_passive: 3 |

| Instancia | Run | Dificultad | Piso | Sala | Tiempo del atasco s |
| --- | --- | --- | --- | --- | --- |
| instancia-1 | 9 | Fácil | 1 | 1:1,1 | 154,0 |
| instancia-1 | 15 | Fácil | 2 | 2:1,1 | 272,2 |
| instancia-1 | 18 | Enjambre | 1 | 1:2,1 | 153,9 |
| instancia-1 | 20 | Normal | 1 | 1:2,1 | 193,4 |
| instancia-1 | 30 | Difícil | 1 | 1:4,0 | 171,4 |
| instancia-3 | 4 | Difícil | 2 | 2:2,1 | 380,7 |
| instancia-3 | 18 | Difícil | 1 | 1:2,2 | 296,5 |
| instancia-3 | 19 | Enjambre | 2 | 2:1,1 | 468,4 |
| instancia-3 | 21 | Difícil | 3 | 3:3,1 | 487,0 |

Comparación de navegación con ×3: el informe anterior distinguía 62 recuperaciones de transición en 33/51 runs y 13 de escape. El lote nuevo registra también `navigation_replan`; un replanteo y una reubicación son acciones distintas. Las tasas no son métricas directamente equivalentes.

## Qué mejorar, balancear y rehacer

### P0 · Rehacer la recuperación de combate sin progreso

**9/104 expediciones quedaron interrumpidas por falta de progreso.** En los últimos 120 segundos de juego de esas nueve hubo entre **82 y 775 activaciones de arma**, entre **0 y 6 eventos de daño** y ninguna baja. En tres casos quedaba una rata según la diferencia entre apariciones y bajas de la última visita. Siete escuadrones terminaron con Queso equipado, frente a 10/91 expediciones terminadas. Es una señal para reproducir, con sesgo de selección por el arma final y sin demostrar que el Queso cause el bloqueo. Además, hubo **6** apariciones de un arma de emergencia en expediciones que sí terminaron.

- Reproducir primero **instancia-1, run 18** y **instancia-3, runs 19 y 21**. Probar si el último enemigo queda fuera de línea de tiro, fuera del NavMesh, en otra zona o detrás de una puerta. Las activaciones muestran que el equipo seguía intentando pelear.
- Si pasan **8–12 s de juego sin daño** mientras hay enemigos, probar adquirir otro objetivo alcanzable, buscar un punto de disparo con línea de visión y reservar esa posición. Para el último enemigo, verificar que la sala y el contador describan el mismo conjunto de enemigos vivos.
- Mantener el señuelo, pero probar que el auto-run conserve un arma ofensiva viable y que el portador de Queso deje de gastar acciones cuando el señuelo no aporta. Registrar motivo de ataque fallido, distancia, objetivo, posición y estado de ruta. Meta inicial: **menos de 2 interrupciones por 100 runs** y menos activaciones sin contacto.

### P1 · Balancear jefes y dificultad antes de recortar Enjambre

Los jefes explican **22,4 %** del daño efectivo y acumulan **56 de 87 caídas** en las visitas analizadas, frente a 31 en combates normales. La cola de duración es el problema más notable: el P90 de jefe llega a **61,9 s en Normal**, **73,9 s en Difícil** y **98,2 s en Enjambre**. En Enjambre la mediana es 22,5 s, así que un recorte global de vida también acortaría jefes que ya se resuelven rápido.

- Separar **tiempo hasta la muerte del jefe** y **tiempo de limpieza posterior**. Probar relevos de refuerzos o un límite de enemigos simultáneos exclusivamente en la fase que resulte larga. Medir P90, caídas por visita y duración sin daño.
- **Mantener provisionalmente Enjambre en ×2,5.** Subió de 1/6 a 12/17 victorias entre tandas; el tiempo y las bajas por partida sí crecen con la dificultad, pero la tasa de victoria observada no es monotónica. Como prueba, usar las mismas semillas, equipos y armas en Normal, Difícil y Enjambre antes de decidir si alterar la densidad inicial o los jefes. La tasa anterior de 17 % ya no justifica por sí sola bajar a ×2,0.
- El puntaje medio pasó de **3.251 en Fácil** a **10.921 en Enjambre**. Para competir por puntaje, probar clasificación por dificultad y mostrar el aporte de tiempo, bajas, salas y botín por separado.

### P1 · Rehacer la respuesta a peligros y mejorar la atribución

Veneno de nube + explosión identificada aportan **6.117,1 de daño efectivo (16,4 %)**. El total de la fila Explosive también incluye daño directo. El informe anterior agrupaba estas amenazas de otra manera y citaba 42,4 %, así que la diferencia orienta la prioridad pero no mide el efecto causal de ningún ajuste. En este lote **Speedy** supera por sí sola a la nube (11,2 % frente a 6,7 %). Además, **218 eventos** sin fuente identificada suman **3.065,0 (8,2 %)**, repartidos en los tres pisos.

- Etiquetar en primer lugar el daño sin atacante con categoría y causa explícitas, incluidos los caminos de daño entre aliados. Registrar exposición a nubes y víctimas por explosión antes de tocar parámetros. Probar salida lateral de la nube y alejamiento de explosivas próximas a morir; usar daño por encuentro y tiempo expuesto como resultados.
- Para Speedy, medir cuánto tiempo permanece cerca o detrás del equipo y si rompe la distancia de retirada. Probar priorización al acercarse y revisar aceleración o alcance de ataque solo si la IA sigue recibiendo impactos difíciles de evitar.
- En hongos, **G0→G1 produjo 2.300 crías a partir de 1.192 originales**. Las siguientes transiciones equivalen a alrededor de 48 % de divisiones por cría bajo el supuesto de dos hijas. Medir densidad simultánea y bajas por generación antes de reducir toda la familia.

### P1 · Balancear armas por función y objetivo

| Elemento | Evidencia del lote | Próximo experimento |
| --- | --- | --- |
| Cuchilla oxidada | 42,1 DPS agregados. El sangrado suma **84.182,5** de daño; **77.197,3 (91,7 %)** cayó sobre jefes. | Probar un techo de sangrado contra jefes o un coeficiente específico, conservando el golpe y el comportamiento contra ratas. Comparar tiempo hasta matar jefe y daño directo/sangrado. |
| Pulse Blaster | 38,5 DPS, 2,09 impactos por activación y 5.598 bajas atribuidas. | Separar disparo primario/secundario y tamaño de grupo. Medir energía por baja y daño por activación antes de tocar cadencia o costo. |
| Escopeta | 12,5 DPS, por encima de los 9,6 de la muestra anterior. El código actual pasa el campo `damage` al impacto. | Retirar el hallazgo previo de los 10 fijos. Comparar distancias cortas y objetivos múltiples antes de subir daño. |
| Tazer | 9,0 DPS con 3,34 impactos por activación. | Comparar 1, 2, 4 y 8 objetivos. Si su rol es control, medir interrupciones y tiempo de control antes de considerar más daño. |
| Látigo, espuma, sopladora y Queso | 5,8 / 15,8 / 21,3 / 0 DPS; el impacto de control o señuelo queda fuera de ese indicador. | Medir desplazamiento, ralentización, ataques cancelados y tiempo durante el que el señuelo desvía enemigos. Revisar primero el comportamiento de la IA con Queso y luego sus números. |
| Suerte / críticos | Los **273 agentes** de partidas terminadas empezaron con **Suerte 1** y se registraron cero críticos. | Preparar tandas con Suerte 1, 5 y 10. Este lote no permite estimar el valor de Suerte ni el rendimiento de pasivas que dependen de críticos. |

### P2 · Validar especialidades y rehacer la lectura de la economía

- **Tanque**: 47/57 victorias con tanque frente a 23/34 sin tanque. El grupo con tanque también gana más por separado en las cuatro dificultades, con subgrupos pequeños. El tanque recibe **298,5 de daño medio** y cae al menos una vez en **11/57** observaciones. Probar las composiciones Bardo + Cerrajero + Tanque (13/13) y DPS + Médico + Tanque (9/9) contra controles con las mismas semillas y armas.
- **Médico, Bardo y Cerrajero**: Médico aporta 5.667,7 de curación a aliados y 656,7 de regeneración propia por fuente; Bardo, 3.664,5. El Cerrajero realizó 37 escapes en 24 runs y presentó 21 caídas en 53 observaciones. Medir curación efectiva por activación, fondos disponibles bajo el umbral y supervivencia después de escapar antes de retocar habilidades.
- **Agentes concretos**: Nora tuvo **14 caídas en 22** observaciones y Milo **12,4 DPS** agregados. Dante, Bruno y Rocco superaron **30 DPS**. Usarlos como casos para cruces controlados de especialidad, arma, dificultad y vida inicial; las tablas no aíslan efecto de personaje.
- **Dinero**: de $8.382 recogidos se gastaron $1.650 (**19,7 %**), con $6.732 al cierre entre las fichas individuales. Las transferencias por herencia no generan dinero nuevo. Probar un registro de efectivo generado, remanente por sala, oportunidades de compra y de curación no aprovechadas. Con ese registro, evaluar si el saldo indica falta de oportunidades, gasto demasiado bajo o ahorro intencional. Separar el dinero que llevaba un caído del efectivamente recuperado.
- **Navegación fuera del combate**: hubo 44 replanteos de puerta y 20 reubicaciones. Instrumentar entrada y salida de cada puerta y escape con plantilla, destino y estado del NavMesh, y después comparar recuperaciones por visita de transición. El informe anterior medía otra acción y no sirve como comparación directa.

### Tanda siguiente

1. Guardar versión o commit, semilla, velocidad, configuración de presets y reglas de selección del escuadrón por JSON.
2. Corregir los bloqueos de último enemigo y la atribución desconocida. Repetir los casos identificados y comparar con el mismo mapa y equipo.
3. Ejecutar un control y una variante por vez, con **30–50 expediciones terminadas por dificultad y variante** como punto de partida. Pasar cambios de jefes, Cuchilla y Enjambre por este control antes de ajustar cifras en conjunto.
4. Complementar con sesiones humanas para evaluar claridad de peligros, justicia de las derrotas, ritmo de recuperación y si las decisiones de gasto se entienden.

Fuentes de contraste en el proyecto: [informe ×3](Reporte_balance_AutoRun_2026-11-25.md), [definición de métricas](BALANCE.md), [telemetría](Assets/Scripts/Gameplay/GameRun.BalanceTelemetry.cs), [rutina de sangrado](Assets/Scripts/NPC/MouseEnemy.cs) y [escopeta](Assets/Scripts/Weapons/ShotgunWeaponDefinition.cs). Los valores anteriores son hipótesis de prueba; este análisis no cambia parámetros del juego.

## Apéndice · inventario de runs

| Instancia | N.º | Dificultad | Resultado | Piso | Salas | Bajas | Juego s | Real s | Puntaje | Archivo |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| instancia-1 | 1 | Normal | completed | 3 | 14 | 310 | 370,7 | 74,6 | 6.715 | [20261125_164028_471_6db1413c](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164028_471_6db1413c.json) |
| instancia-1 | 2 | Enjambre | defeat | 3 | 18 | 836 | 583,6 | 116,7 | 18.300 | [20261125_164142_831_4d1657b2](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164142_831_4d1657b2.json) |
| instancia-1 | 3 | Fácil | completed | 3 | 12 | 194 | 320,5 | 64,2 | 3.328 | [20261125_164339_599_7b43f8ca](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164339_599_7b43f8ca.json) |
| instancia-1 | 4 | Difícil | defeat | 1 | 3 | 102 | 173,0 | 34,6 | 1.745 | [20261125_164443_772_da3e9c58](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164443_772_da3e9c58.json) |
| instancia-1 | 5 | Normal | completed | 3 | 13 | 358 | 498,8 | 99,8 | 6.945 | [20261125_164518_399_69575e01](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164518_399_69575e01.json) |
| instancia-1 | 6 | Enjambre | defeat | 2 | 10 | 344 | 327,3 | 65,5 | 6.892 | [20261125_164658_241_a4fcf936](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164658_241_a4fcf936.json) |
| instancia-1 | 7 | Enjambre | defeat | 1 | 0 | 9 | 42,8 | 8,6 | 850 | [20261125_164803_701_bdc93240](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164803_701_bdc93240.json) |
| instancia-1 | 8 | Normal | defeat | 2 | 8 | 198 | 322,7 | 64,6 | 3.062 | [20261125_164812_316_3cf240ca](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164812_316_3cf240ca.json) |
| instancia-1 | 9 | Fácil | interrupted | 1 | 0 | 3 | 154,0 | 30,8 | -440 | [20261125_164916_926_f363e05a](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164916_926_f363e05a.json) |
| instancia-1 | 10 | Normal | completed | 3 | 12 | 227 | 324,9 | 65,0 | 5.535 | [20261125_164947_769_70a02c58](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_164947_769_70a02c58.json) |
| instancia-1 | 11 | Normal | completed | 3 | 16 | 285 | 368,4 | 73,7 | 6.805 | [20261125_165052_821_5a474df6](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_165052_821_5a474df6.json) |
| instancia-1 | 12 | Normal | defeat | 3 | 9 | 227 | 393,7 | 78,8 | 3.888 | [20261125_165206_520_bec1e132](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_165206_520_bec1e132.json) |
| instancia-1 | 13 | Difícil | completed | 3 | 15 | 419 | 461,0 | 92,2 | 9.990 | [20261125_165325_273_6350b564](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_165325_273_6350b564.json) |
| instancia-1 | 14 | Enjambre | completed | 3 | 14 | 482 | 428,3 | 85,7 | 11.265 | [20261125_165457_562_aa75f759](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_165457_562_aa75f759.json) |
| instancia-1 | 15 | Fácil | interrupted | 2 | 5 | 92 | 272,2 | 54,5 | 495 | [20261125_165623_224_c2d889c6](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_165623_224_c2d889c6.json) |
| instancia-1 | 16 | Normal | completed | 3 | 10 | 265 | 319,7 | 64,0 | 5.318 | [20261125_165717_657_5f188294](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_165717_657_5f188294.json) |
| instancia-1 | 17 | Enjambre | completed | 3 | 17 | 705 | 804,7 | 160,9 | 14.210 | [20261125_165821_637_924b560c](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_165821_637_924b560c.json) |
| instancia-1 | 18 | Enjambre | interrupted | 1 | 0 | 31 | 153,9 | 30,8 | 208 | [20261125_170102_591_a430a997](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170102_591_a430a997.json) |
| instancia-1 | 19 | Enjambre | completed | 3 | 12 | 398 | 457,1 | 91,4 | 9.250 | [20261125_170133_352_ede10be6](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170133_352_ede10be6.json) |
| instancia-1 | 20 | Normal | interrupted | 1 | 2 | 45 | 193,4 | 38,7 | 230 | [20261125_170304_820_e3065bd6](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170304_820_e3065bd6.json) |
| instancia-1 | 21 | Normal | completed | 3 | 16 | 424 | 608,6 | 121,7 | 7.490 | [20261125_170343_489_f909850a](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170343_489_f909850a.json) |
| instancia-1 | 22 | Fácil | completed | 3 | 14 | 226 | 324,4 | 64,9 | 4.170 | [20261125_170545_266_2c5edb29](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170545_266_2c5edb29.json) |
| instancia-1 | 23 | Difícil | completed | 3 | 16 | 437 | 438,7 | 87,8 | 10.232 | [20261125_170650_128_bc7a5d3e](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170650_128_bc7a5d3e.json) |
| instancia-1 | 24 | Difícil | defeat | 2 | 5 | 134 | 206,2 | 41,3 | 2.230 | [20261125_170817_906_e3eafa27](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170817_906_e3eafa27.json) |
| instancia-1 | 25 | Normal | defeat | 1 | 5 | 91 | 184,6 | 37,0 | 1.630 | [20261125_170859_147_fbe0c206](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170859_147_fbe0c206.json) |
| instancia-1 | 26 | Normal | completed | 3 | 13 | 267 | 374,5 | 74,9 | 5.480 | [20261125_170936_144_a40cc852](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_170936_144_a40cc852.json) |
| instancia-1 | 27 | Difícil | completed | 3 | 16 | 444 | 454,6 | 90,9 | 10.222 | [20261125_171051_122_392907fc](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_171051_122_392907fc.json) |
| instancia-1 | 28 | Normal | completed | 3 | 12 | 238 | 344,2 | 68,8 | 5.505 | [20261125_171222_075_d6b87f63](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_171222_075_d6b87f63.json) |
| instancia-1 | 29 | Fácil | completed | 3 | 10 | 149 | 244,9 | 49,0 | 3.310 | [20261125_171330_904_e31bda85](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_171330_904_e31bda85.json) |
| instancia-1 | 30 | Difícil | interrupted | 1 | 1 | 49 | 171,4 | 34,3 | 365 | [20261125_171419_884_c18691a5](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_171419_884_c18691a5.json) |
| instancia-1 | 31 | Normal | in_progress | 1 | 0 | 0 | 0,1 | 0,0 | 1.000 | [20261125_171454_187_fa67ad78](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-1/run_20261125_171454_187_fa67ad78.json) |
| instancia-2 | 1 | Normal | defeat | 2 | 8 | 201 | 340,4 | 68,6 | 3.388 | [20261125_164028_472_dcd40224](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164028_472_dcd40224.json) |
| instancia-2 | 2 | Difícil | completed | 3 | 15 | 400 | 633,0 | 126,7 | 7.425 | [20261125_164136_769_428382f8](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164136_769_428382f8.json) |
| instancia-2 | 3 | Difícil | completed | 3 | 15 | 438 | 392,4 | 78,5 | 10.292 | [20261125_164343_459_907b5463](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164343_459_907b5463.json) |
| instancia-2 | 4 | Fácil | defeat | 2 | 9 | 124 | 389,8 | 78,0 | 582 | [20261125_164501_949_af77f409](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164501_949_af77f409.json) |
| instancia-2 | 5 | Normal | defeat | 1 | 4 | 74 | 160,8 | 32,2 | 1.400 | [20261125_164619_910_33c9c3fc](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164619_910_33c9c3fc.json) |
| instancia-2 | 6 | Enjambre | completed | 3 | 19 | 691 | 608,2 | 121,7 | 15.190 | [20261125_164652_131_6ce907ce](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164652_131_6ce907ce.json) |
| instancia-2 | 7 | Fácil | completed | 3 | 12 | 150 | 275,5 | 55,1 | 3.502 | [20261125_164853_868_f4e98ff9](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164853_868_f4e98ff9.json) |
| instancia-2 | 8 | Fácil | completed | 3 | 13 | 221 | 478,4 | 95,7 | 3.130 | [20261125_164948_928_9595e5f8](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_164948_928_9595e5f8.json) |
| instancia-2 | 9 | Fácil | completed | 3 | 13 | 168 | 280,3 | 56,1 | 3.398 | [20261125_165124_660_c6adcae0](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_165124_660_c6adcae0.json) |
| instancia-2 | 10 | Enjambre | completed | 3 | 16 | 600 | 461,2 | 92,3 | 12.830 | [20261125_165220_716_6d66b557](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_165220_716_6d66b557.json) |
| instancia-2 | 11 | Normal | completed | 3 | 14 | 332 | 421,2 | 84,2 | 6.880 | [20261125_165353_005_f52f7f4f](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_165353_005_f52f7f4f.json) |
| instancia-2 | 12 | Difícil | completed | 3 | 14 | 453 | 598,7 | 119,8 | 8.445 | [20261125_165517_248_d4d21aaf](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_165517_248_d4d21aaf.json) |
| instancia-2 | 13 | Fácil | completed | 3 | 14 | 188 | 345,0 | 69,0 | 3.022 | [20261125_165717_011_a4b32f75](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_165717_011_a4b32f75.json) |
| instancia-2 | 14 | Fácil | completed | 3 | 16 | 183 | 320,2 | 64,1 | 4.038 | [20261125_165826_003_72d96f5f](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_165826_003_72d96f5f.json) |
| instancia-2 | 15 | Fácil | defeat | 2 | 10 | 126 | 285,6 | 57,1 | 2.400 | [20261125_165930_078_84bee3f5](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_165930_078_84bee3f5.json) |
| instancia-2 | 16 | Normal | completed | 3 | 12 | 226 | 341,4 | 68,3 | 5.032 | [20261125_170027_217_59d92e5d](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_170027_217_59d92e5d.json) |
| instancia-2 | 17 | Difícil | completed | 3 | 12 | 295 | 359,2 | 71,9 | 7.010 | [20261125_170135_581_7e536e61](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_170135_581_7e536e61.json) |
| instancia-2 | 18 | Difícil | completed | 3 | 18 | 634 | 751,7 | 150,4 | 12.480 | [20261125_170247_448_c095e159](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_170247_448_c095e159.json) |
| instancia-2 | 19 | Enjambre | completed | 3 | 16 | 566 | 557,8 | 111,5 | 13.210 | [20261125_170517_825_19f75bf9](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_170517_825_19f75bf9.json) |
| instancia-2 | 20 | Fácil | completed | 3 | 15 | 235 | 449,8 | 90,0 | 3.445 | [20261125_170709_365_15363086](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_170709_365_15363086.json) |
| instancia-2 | 21 | Enjambre | completed | 3 | 15 | 852 | 1.288,0 | 257,6 | 12.242 | [20261125_170839_307_fc448b60](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_170839_307_fc448b60.json) |
| instancia-2 | 22 | Enjambre | completed | 3 | 16 | 604 | 500,4 | 100,0 | 13.458 | [20261125_171256_979_3e8a2845](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_171256_979_3e8a2845.json) |
| instancia-2 | 23 | Fácil | in_progress | 2 | 6 | 71 | 150,2 | 30,0 | 1.585 | [20261125_171436_973_362a444a](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-2/run_20261125_171436_973_362a444a.json) |
| instancia-3 | 1 | Fácil | completed | 3 | 11 | 146 | 299,9 | 60,5 | 3.360 | [20261125_164028_471_96e857eb](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_164028_471_96e857eb.json) |
| instancia-3 | 2 | Fácil | completed | 3 | 15 | 196 | 372,5 | 74,5 | 4.125 | [20261125_164128_651_8127bc7a](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_164128_651_8127bc7a.json) |
| instancia-3 | 3 | Fácil | completed | 3 | 15 | 158 | 279,4 | 55,9 | 3.540 | [20261125_164243_170_5ca8613d](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_164243_170_5ca8613d.json) |
| instancia-3 | 4 | Difícil | interrupted | 2 | 8 | 215 | 380,7 | 76,2 | 3.358 | [20261125_164339_089_9e083b97](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_164339_089_9e083b97.json) |
| instancia-3 | 5 | Difícil | defeat | 3 | 11 | 513 | 817,9 | 163,6 | 6.292 | [20261125_164455_261_5b641963](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_164455_261_5b641963.json) |
| instancia-3 | 6 | Enjambre | completed | 3 | 15 | 646 | 688,7 | 137,8 | 14.635 | [20261125_164738_923_c81dce23](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_164738_923_c81dce23.json) |
| instancia-3 | 7 | Normal | completed | 3 | 14 | 269 | 432,4 | 86,5 | 6.075 | [20261125_164956_762_86a0c4f0](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_164956_762_86a0c4f0.json) |
| instancia-3 | 8 | Normal | completed | 3 | 16 | 359 | 477,2 | 95,4 | 7.020 | [20261125_165123_225_f3ad8bfd](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_165123_225_f3ad8bfd.json) |
| instancia-3 | 9 | Fácil | completed | 3 | 14 | 188 | 314,4 | 62,9 | 3.738 | [20261125_165258_678_be45500f](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_165258_678_be45500f.json) |
| instancia-3 | 10 | Enjambre | completed | 3 | 14 | 501 | 569,4 | 113,9 | 10.165 | [20261125_165401_558_adaeb752](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_165401_558_adaeb752.json) |
| instancia-3 | 11 | Enjambre | defeat | 1 | 1 | 55 | 97,6 | 19,5 | 1.455 | [20261125_165555_493_042c2d2e](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_165555_493_042c2d2e.json) |
| instancia-3 | 12 | Difícil | completed | 3 | 16 | 435 | 473,3 | 94,7 | 9.430 | [20261125_165614_979_0106765c](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_165614_979_0106765c.json) |
| instancia-3 | 13 | Difícil | completed | 3 | 14 | 391 | 347,8 | 69,6 | 9.750 | [20261125_165749_725_e748adca](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_165749_725_e748adca.json) |
| instancia-3 | 14 | Difícil | completed | 3 | 13 | 369 | 439,8 | 88,0 | 8.168 | [20261125_165859_293_b84a1213](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_165859_293_b84a1213.json) |
| instancia-3 | 15 | Fácil | completed | 3 | 15 | 189 | 314,8 | 63,0 | 3.755 | [20261125_170027_263_b44f27a9](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_170027_263_b44f27a9.json) |
| instancia-3 | 16 | Fácil | completed | 3 | 15 | 181 | 507,7 | 101,6 | 2.142 | [20261125_170130_212_003798e9](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_170130_212_003798e9.json) |
| instancia-3 | 17 | Fácil | completed | 3 | 15 | 175 | 361,8 | 72,4 | 3.500 | [20261125_170311_787_3c144b57](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_170311_787_3c144b57.json) |
| instancia-3 | 18 | Difícil | interrupted | 1 | 3 | 82 | 296,5 | 59,3 | 275 | [20261125_170424_171_da34b95e](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_170424_171_da34b95e.json) |
| instancia-3 | 19 | Enjambre | interrupted | 2 | 10 | 412 | 468,4 | 93,7 | 7.645 | [20261125_170523_471_57e33715](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_170523_471_57e33715.json) |
| instancia-3 | 20 | Normal | defeat | 3 | 11 | 256 | 468,8 | 93,8 | 4.475 | [20261125_170657_204_c84e4d64](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_170657_204_c84e4d64.json) |
| instancia-3 | 21 | Difícil | interrupted | 3 | 12 | 440 | 487,0 | 97,4 | 7.855 | [20261125_170830_961_c075fd8b](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_170830_961_c075fd8b.json) |
| instancia-3 | 22 | Difícil | completed | 3 | 10 | 311 | 509,5 | 101,9 | 5.035 | [20261125_171008_448_cec4b1ea](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_171008_448_cec4b1ea.json) |
| instancia-3 | 23 | Enjambre | completed | 3 | 16 | 598 | 557,5 | 111,5 | 14.672 | [20261125_171150_353_83eca989](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_171150_353_83eca989.json) |
| instancia-3 | 24 | Difícil | completed | 3 | 15 | 450 | 429,9 | 86,0 | 10.250 | [20261125_171341_885_3d462053](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_171341_885_3d462053.json) |
| instancia-3 | 25 | Difícil | in_progress | 1 | 0 | 0 | 0,2 | 0,0 | 1.000 | [20261125_171507_865_b02400f2](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-3/run_20261125_171507_865_b02400f2.json) |
| instancia-4 | 1 | Difícil | defeat | 1 | 2 | 69 | 158,4 | 32,2 | 1.345 | [20261125_164028_471_21651524](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164028_471_21651524.json) |
| instancia-4 | 2 | Normal | completed | 3 | 17 | 315 | 404,5 | 81,0 | 6.585 | [20261125_164100_356_bcf6b65a](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164100_356_bcf6b65a.json) |
| instancia-4 | 3 | Fácil | completed | 3 | 15 | 188 | 368,0 | 73,6 | 3.352 | [20261125_164221_351_cf685178](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164221_351_cf685178.json) |
| instancia-4 | 4 | Fácil | completed | 3 | 13 | 174 | 342,8 | 68,6 | 3.615 | [20261125_164334_951_07fd8c06](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164334_951_07fd8c06.json) |
| instancia-4 | 5 | Enjambre | completed | 3 | 16 | 599 | 517,0 | 103,4 | 13.210 | [20261125_164443_541_3aec611e](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164443_541_3aec611e.json) |
| instancia-4 | 6 | Normal | completed | 3 | 16 | 351 | 419,9 | 84,0 | 7.225 | [20261125_164626_986_5e62cbb4](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164626_986_5e62cbb4.json) |
| instancia-4 | 7 | Normal | completed | 3 | 13 | 255 | 297,1 | 59,4 | 5.792 | [20261125_164750_968_38be9fd0](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164750_968_38be9fd0.json) |
| instancia-4 | 8 | Normal | defeat | 3 | 15 | 320 | 515,9 | 103,2 | 4.942 | [20261125_164850_390_364b3193](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_164850_390_364b3193.json) |
| instancia-4 | 9 | Fácil | completed | 3 | 10 | 152 | 227,4 | 45,5 | 3.208 | [20261125_165033_609_e275ecf7](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_165033_609_e275ecf7.json) |
| instancia-4 | 10 | Normal | completed | 3 | 13 | 340 | 616,4 | 123,3 | 5.348 | [20261125_165119_147_27bb4e83](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_165119_147_27bb4e83.json) |
| instancia-4 | 11 | Difícil | completed | 3 | 16 | 359 | 436,6 | 87,3 | 8.560 | [20261125_165322_462_b9c075dc](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_165322_462_b9c075dc.json) |
| instancia-4 | 12 | Normal | completed | 3 | 16 | 308 | 418,8 | 83,8 | 6.715 | [20261125_165449_795_8ea92edd](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_165449_795_8ea92edd.json) |
| instancia-4 | 13 | Fácil | completed | 3 | 16 | 185 | 318,6 | 63,7 | 3.728 | [20261125_165613_556_359ea22a](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_165613_556_359ea22a.json) |
| instancia-4 | 14 | Difícil | defeat | 2 | 8 | 322 | 382,3 | 76,5 | 5.748 | [20261125_165717_288_5b7e6cf7](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_165717_288_5b7e6cf7.json) |
| instancia-4 | 15 | Difícil | completed | 3 | 16 | 490 | 475,7 | 95,2 | 11.425 | [20261125_165833_794_5cf72b51](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_165833_794_5cf72b51.json) |
| instancia-4 | 16 | Normal | defeat | 3 | 10 | 265 | 380,2 | 76,0 | 4.860 | [20261125_170009_035_8f843771](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_170009_035_8f843771.json) |
| instancia-4 | 17 | Difícil | completed | 3 | 13 | 509 | 618,6 | 123,8 | 10.175 | [20261125_170125_074_c2670ae9](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_170125_074_c2670ae9.json) |
| instancia-4 | 18 | Difícil | completed | 3 | 16 | 491 | 540,8 | 108,2 | 10.805 | [20261125_170328_902_f77a0648](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_170328_902_f77a0648.json) |
| instancia-4 | 19 | Difícil | completed | 3 | 18 | 570 | 596,8 | 119,4 | 10.972 | [20261125_170517_051_7de8ef8c](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_170517_051_7de8ef8c.json) |
| instancia-4 | 20 | Fácil | defeat | 3 | 11 | 135 | 323,4 | 64,7 | 2.375 | [20261125_170716_429_0502c36c](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_170716_429_0502c36c.json) |
| instancia-4 | 21 | Normal | completed | 3 | 16 | 340 | 437,1 | 87,5 | 7.255 | [20261125_170821_086_895eaf2a](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_170821_086_895eaf2a.json) |
| instancia-4 | 22 | Enjambre | defeat | 2 | 6 | 198 | 241,0 | 48,2 | 3.825 | [20261125_170948_594_e859f3a9](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_170948_594_e859f3a9.json) |
| instancia-4 | 23 | Difícil | completed | 3 | 13 | 383 | 462,1 | 92,5 | 8.270 | [20261125_171036_806_3534a049](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_171036_806_3534a049.json) |
| instancia-4 | 24 | Difícil | completed | 3 | 15 | 504 | 532,8 | 106,6 | 10.035 | [20261125_171209_319_f0371a84](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_171209_319_f0371a84.json) |
| instancia-4 | 25 | Difícil | in_progress | 3 | 7 | 166 | 300,2 | 60,0 | 3.185 | [20261125_171355_884_2dc231c8](BalanceRuns/AutoRun-x5-2026-11-25-134019/instancia-4/run_20261125_171355_884_2dc231c8.json) |
