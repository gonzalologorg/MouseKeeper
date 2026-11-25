# Informe de balance: 51 expediciones Auto-run

Fecha del análisis: 25 de noviembre de 2026. Se analizaron los 51 archivos JSON de las expediciones 1 a 51 entregadas para esta muestra. Este documento propone pruebas y cambios: **no se modificaron los valores del juego**.

## Lectura principal

El juego tiene una base interesante de decisiones: la formación, el daño de área, el dinero para curar y las especialidades producen resultados distintos. En esta muestra, sin embargo, destacan cuatro cuestiones antes de ajustar todo el arsenal:

1. **Enjambre es un salto de dificultad muy grande**: solo 1 de 6 expediciones finalizadas terminó en victoria, y 4 de las 6 terminaron en el primer piso.
2. **Veneno y explosiones concentran el daño**: juntos representan el 42,4 % del daño registrado a agentes. Conviene mejorar la respuesta de la IA al peligro antes de reducir a ciegas sus números.
3. **Las salas de jefe duran bastante más que los combates normales**, especialmente en Fácil. Hay que revisar la diferencia entre presión sostenible y desgaste prolongado.
4. **Las muestras todavía tienen problemas de navegación**: en 33 de las 51 expediciones hubo una recuperación durante una transición y cinco partidas finalizaron por falta de progreso. Esto importa tanto como el balance porque puede cambiar el tiempo, el daño y el botín.

Los resultados provienen de una IA a velocidad ×3. Permiten medir lo que hace esa IA; **no demuestran qué tan divertido resulta el juego para una persona** ni garantizan las mismas tasas de victoria con control manual.

## Alcance y calidad de la muestra

- 27 victorias, 17 derrotas, 6 archivos marcados como interrumpidos y 1 archivo principal en curso. Para tasas de victoria y promedios de balance se usan las **44 partidas terminadas**; las 7 restantes se estudian por separado para diagnosticar el sistema.
- De las 6 interrumpidas, 5 registran `auto_run_stalled`; la sexta duró solo 3,45 segundos de juego y no permite sacar conclusiones de balance.
- El archivo `.json.tmp` entregado corresponde al mismo `run_id` que el archivo principal en curso. Contiene un estado posterior marcado `interrupted`; se trata como una actualización de la misma expedición, **no como una muestra adicional**.
- Todas las muestras terminadas están marcadas como Auto-run y ninguna indica uso de comandos de depuración. El código, las configuraciones y el modo de juego pueden cambiar entre sesiones; los JSON no identifican una revisión de código concreta. Los valores de assets citados más adelante son los actuales del proyecto, no necesariamente una prueba de los que se usaron en cada archivo.
- Las composiciones, agentes, armas, dificultades y mapas cambian a la vez. Una relación observada no demuestra causalidad. Las muestras por combinación son pequeñas.
- Las recuperaciones y las partidas atascadas pueden sesgar tiempos, botín y supervivencia. No deben interpretarse como rendimiento humano.

## Resultados por dificultad

| Dificultad | Partidas terminadas | Victorias | Tiempo medio de juego | Ratas eliminadas por partida | Daño recibido por equipo | Dinero recogido por equipo |
|---|---:|---:|---:|---:|---:|---:|
| Fácil | 11 | 8/11 (73 %) | 493 s | 197 | 543 | $53 |
| Normal | 13 | 10/13 (77 %) | 442 s | 232 | 623 | $68 |
| Difícil | 14 | 8/14 (57 %) | 486 s | 335 | 776 | $97 |
| Enjambre | 6 | 1/6 (17 %) | 275 s | 243 | 636 | $65 |

El menor tiempo y daño acumulado de Enjambre **no implica que sea más fácil**: muchas partidas terminan antes. Las dificultades se eligieron al azar y cada grupo tiene entre 6 y 14 muestras, por lo que no conviene leer estas tasas como porcentajes definitivos.

Las victorias terminaron con un puntaje medio de 5.639 y las derrotas con 2.358. Las victorias también duraron más y mataron más enemigos, de modo que esa diferencia no mide por sí sola la calidad del equipo. En particular, comparar puntajes brutos entre dificultades no es justo todavía: la cantidad de ratas y el tiempo disponible cambian.

## Enemigos y daño recibido

Sobre las 44 partidas terminadas se registraron aproximadamente **28.754 puntos de daño efectivo** a los agentes:

| Fuente | Daño | Participación |
|---|---:|---:|
| Nube de veneno | 7.745 | 26,9 % |
| Rata normal | 5.992 | 20,8 % |
| Jefe | 5.362 | 18,6 % |
| Explosiva | 4.456 | 15,5 % |
| Rata hongo, daño directo | 1.108 | 3,9 % |
| Rata rápida | 1.096 | 3,8 % |
| Fuente desconocida | 1.046 | 3,6 % |
| Otras fuentes | 1.949 | 6,8 % |

La rata normal suma mucho por ser frecuente: se registraron 5.037 apariciones. En cambio, el daño total de las nubes es mayor con 451 apariciones de PoisonRat. No hay que interpretar esto como daño por rata: una nube puede afectar a varios agentes y el registro de daño de nube no identifica individualmente a su creadora.

**Veneno.** La prioridad es que los agentes reconozcan la zona y busquen salir. La señal roja que se agregó ayuda a una persona, pero no prueba que la IA la evite. Prueba inicial: aplicar un costo alto de navegación dentro de la nube y buscar una posición segura a 1–2 unidades fuera del radio. Si la única ruta es atravesarla, que pueda hacerlo; no debería quedarse bloqueado. Medir antes y después tiempo dentro de la nube y daño por exposición. Como segunda prueba, si sigue dominando, reducir la duración un 25 % o el daño por segundo un 20 %, **una variable por vez**. Los valores actuales del asset son 5 de daño por segundo, 4 segundos y radio 4; confirmar la configuración usada en la siguiente tanda.

**Explosivas.** Hay 510 apariciones y 4.456 de daño atribuido a explosiones. Probar que la IA se aleje del radio cuando una explosiva esté cerca de morir y, en la interfaz, reforzar el aviso de que explota al morir. Antes de cambiar daño o radio, medir daño por explosión y cuántos agentes alcanza cada una. Si sigue castigando sin posibilidad real de reacción, probar radio 4 → 3,5; el asset actual configura radio 4 y daño 8. Una explosión instantánea al morir puede seguir siendo difícil de evitar aunque exista un círculo visible.

**Hongo.** Las generaciones registradas fueron 440 originales, 839 de primera generación, 809 de segunda y 795 de tercera. La relación observada es aproximadamente 48 % y 49 % de divisiones por cría en las dos transiciones, teniendo en cuenta que los límites del jefe y enemigos pendientes también pueden reducir el número. Es compatible con la probabilidad nueva del 50 %. No propondría otra reducción sin medir saturación simultánea y duración de cada combate.

**Desconocido.** El 3,6 % del daño aún aparece como `unknown` en 17 partidas. Agregar fuente, tipo y responsable a esos eventos antes de sacar conclusiones sobre su origen.

## Ritmo y jefes

Los siguientes valores son **promedios por visita registrada a una sala de combate con enemigos**, no por expedición. Una sala puede visitarse más de una vez y una visita puede quedar incompleta.

| Dificultad | Combate normal: visitas / tiempo medio / enemigos | Jefe: visitas / tiempo medio / enemigos |
|---|---|---|
| Fácil | 114 / 7,6 s / 10,4 | 36 / 40,4 s / 28,4 |
| Normal | 116 / 10,5 s / 18,9 | 34 / 32,4 s / 24,9 |
| Difícil | 130 / 15,5 s / 29,3 | 37 / 37,2 s / 26,1 |
| Enjambre | 29 / 20,8 s / 47,9 | 7 / 24,9 s / 20,0 |

En Fácil, el combate de jefe registrado dura en promedio unas **5,3 veces** una sala de pelea normal. Entre esas visitas se registraron 13 caídas en jefes y 3 en salas normales. El tiempo incluye la presión de las ratas de la sala, no solo el tiempo de vida del jefe.

**Ideas para probar:**

- Apuntar inicialmente a jefes de **20–35 segundos de combate en Fácil y Normal**, y **30–45 en Difícil**, sin tratar estos rangos como objetivos demostrados de diversión. Revisar percentiles y no solo promedios.
- Antes de bajar vida, registrar el momento de muerte del jefe, la cantidad de ratas simultáneas y el tiempo necesario para limpiar las restantes. Esto separa «jefe demasiado resistente» de «la sala tarda demasiado en terminar».
- En Enjambre, probar reducir el multiplicador de cantidad de enemigos de **2,5 a 2,0** como experimento inicial, manteniendo los demás valores. Evaluar también una rampa por piso si se quiere que el primer cuarto sea accesible y el tercero siga siendo abrumador.
- Evitar recompensar una sala larga únicamente con más presión: telegráficos, fases y ventanas de respiro pueden producir tensión sin mantener el daño constante.

## Especialidades y agentes

Cada fila agrupa agentes de todas las dificultades y combinaciones. El DPS es daño efectivo total dividido por tiempo vivo de combate total; no aísla arma ni estadísticas individuales.

| Especialidad | Agentes | DPS efectivo agregado | Daño recibido medio | Curación dada media | Agentes con al menos una caída |
|---|---:|---:|---:|---:|---:|
| DPS | 32 | 29,0 | 169 | 1,6 | 12/32 |
| Médico | 25 | 17,4 | 157 | 190,4 | 12/25 |
| Bardo | 30 | 14,7 | 193 | 172,5 | 17/30 |
| Tanque | 25 | 18,5 | 428 | 0 | 8/25 |
| Cerrajero | 20 | 19,4 | 148 | 0 | 14/20 |

- **Tanque:** absorbe mucho más daño, como se espera de su función. Hubo tanque en 18/25 victorias de sus grupos frente a 9/19 sin tanque, pero los grupos sin tanque incluyeron 5 partidas Enjambre y los grupos con tanque solo 1. No se puede atribuir esa diferencia al tanque sin controlar dificultad.
- **Médico:** la curación a compañeros registró 3.799 puntos en 23 partidas y la regeneración propia otros 962 en 19. El dato relevante para el siguiente análisis es cuántos dólares quedan disponibles cuando alguien necesita curación y si el médico llega a gastar dinero en el tanque. Mantendría provisionalmente el **5 % de vida máxima por $1**, con el umbral de 70 %, hasta separar la falta de fondos de la falta de oportunidad.
- **Bardo:** sus eventos suman 5.126 de curación en 26 partidas. Es una contribución grande, pero depende de cuántas veces se activa y de los máximos de vida del equipo. Medir activaciones, curación efectiva y daño evitado por activación antes de reducir el 30 %.
- **Cerrajero:** registra la mayor proporción de agentes con caídas en esta muestra. Hay 25 eventos de escape en total. Probar su valor midiendo qué proporción de escapes salva al grupo, si luego vuelve a intentar la sala y si el costo de repetirla compensa su beneficio. No reducir ni aumentar su habilidad únicamente a partir de estas 20 observaciones.
- **Composición:** DPS + Médico + Tanque ganó 6/6 partidas observadas; Bardo + Cerrajero + Médico ganó 0/3. Son indicios para diseñar pruebas controladas, no una clasificación definitiva.

### Agentes individuales para investigar

Los personajes cambiaron de especialidad, arma y dificultad entre partidas. Los resultados siguientes sirven para seleccionar casos, no para afirmar que un personaje esté desbalanceado.

| Agente | Partidas | Victorias del grupo | DPS efectivo agregado | Caídas acumuladas | Observación |
|---|---:|---:|---:|---:|---|
| Milo | 14 | 7 | 16,4 | 11 | Es el caso con más caídas acumuladas; revisar vida inicial, rol, arma y fuentes de daño de cada partida. |
| Olivia | 11 | 7 | 25,4 | 4 | Buen aporte de daño, pero alternó entre todas las especialidades. |
| Kira | 9 | 8 | 23,7 | 4 | Buen resultado de grupo; revisar qué armas y dificultades acompañaron esas victorias. |
| Luna | 10 | 7 | 9,9 | 6 | Ocho de diez partidas fueron como tanque o bardo; su DPS no se puede comparar directamente con un DPS ofensivo. |
| Pipo | 6 | 3 | 11,3 | 5 | Cuatro partidas como tanque; recibió 446 de daño medio. Muestra pequeña. |
| Nico | 6 | 4 | 13,7 | 2 | Cuatro partidas como médico; promedió 181 de curación dada. |

Para cada uno sería útil registrar estadísticas reales al comenzar, tiempo con cada arma y daño recibido por tipo y por piso. Una comparación justa usaría el mismo rol, arma, dificultad y mapa.

## Armas: qué revisar primero

El DPS de esta tabla es **daño efectivo atribuido al arma / tiempo de combate equipado agregado**, no daño teórico contra un objetivo fijo. Las armas de área pueden golpear varios enemigos; los efectos prolongados pueden seguir causando daño tras un cambio de arma. Se incluyen equipamientos de más de tres segundos de combate.

| Arma | Equipamientos observados | Tiempo de combate agregado | DPS efectivo observado |
|---|---:|---:|---:|
| Cuchilla oxidada | 10 | 589 s | 50,6 |
| Pulse Blaster | 23 | 3.555 s | 36,5 |
| Mouse Sweeper | 26 | 3.307 s | 28,6 |
| Mouse Blaster | 19 | 2.377 s | 27,8 |
| Sopladora | 15 | 2.585 s | 19,7 |
| Lanzador de espuma | 27 | 3.579 s | 18,0 |
| Mouse Flamer | 17 | 2.329 s | 11,7 |
| Granada de gas | 15 | 1.306 s | 10,8 |
| Escopeta | 17 | 1.418 s | 9,6 |
| Tazer | 7 | 806 s | 6,7 |
| Látigo | 16 | 1.509 s | 4,0 |
| Queso | 8 | 1.338 s | 0 |

- **Cuchilla:** observar contra jefes y grupos por separado. El sangrado basado en vida máxima puede dar mucho valor contra enemigos resistentes. Antes de reducirlo, medir cuánto daño proviene del golpe y cuánto del sangrado; probar un límite de daño de sangrado al jefe o reducir su porcentaje solo contra jefes si confirma el problema.
- **Pulse Blaster:** revisar su daño por activación y energía. Fue arma inicial en 14 agentes de los cuales 12 pertenecieron a equipos ganadores, pero eso incluye diferencias de composición y dificultad.
- **Tazer:** sigue apareciendo bajo en daño. El asset actual tiene 20 de daño y 2 segundos de intervalo; el daño se divide entre objetivos. Prueba aislada: comparar 1, 2, 4 y 8 objetivos. Si se busca un arma eficaz contra grupos, probar **25–30 de daño total** o reducir intervalo a **1,5–1,7 s**, una de las dos cosas por experimento. Si debe controlar, añadir una interrupción breve de **0,25–0,4 s** y medirla.
- **Escopeta:** el asset muestra daño 15, pero su implementación aplica actualmente **10 fijos por objetivo**. Conviene unificar comportamiento y campo configurable antes de balancearla. La distancia y la dispersión también afectan su resultado en manos de la IA.
- **Látigo:** su daño no refleja el valor de empujar. Registrar distancia desplazada, interrupciones y daño evitado antes de subir daño. El asset actual indica intervalo 3 s y rango 5; se podría probar **2,0–2,5 s** si el control se siente demasiado esporádico.
- **Queso:** no causa daño por diseño. Si los tres agentes reciben armas con capacidad ofensiva insuficiente, Auto-run puede quedar sin resolver el combate. Para obtener muestras jugables, probar una regla de planificación que garantice **al menos un arma con daño**, y registrar esa restricción; mantener otra serie completamente aleatoria para encontrar configuraciones imposibles.
- **Espuma y sopladora:** no juzgarlas solo por DPS. Añadir métricas de segundos de ralentización, ratas afectadas y distancia empujada. Sus valores actuales de asset son respectivamente daño 15 / intervalo 0,35 s y daño 25 / intervalo 0,75 s; confirmar cómo cada implementación interpreta esos campos antes de modificarlos.

## Economía y puntuación

Las 44 partidas terminadas registraron **3.206 recogidas de dinero**, repartidas en 41 partidas. Las tres sin dinero terminaron sin salas despejadas. En una partida hubo dos avisos de dinero sin recoger por falta de un agente elegible; esa evidencia no alcanza para concluir que todo el dinero se recoge correctamente, pero sí muestra que la recogida ocurre.

Se registraron 33 compras de pasivas, 15 de armas y 32 de salud. La simple cantidad de recogidas no representa riqueza: el contador suma eventos, mientras que `money_collected` suma dólares. Para la siguiente tanda conviene registrar por sala el dinero que apareció, se recogió y quedó sin recoger; también quién tenía dinero cuando un aliado quedó bajo el umbral de curación.

El reloj penaliza **10 puntos por segundo de juego**, por lo que esperar botín o curación puede hacer caer la puntuación aunque mejore la supervivencia. Es una tensión interesante si el jugador puede decidir, pero Auto-run siempre busca recoger. Como experimentos separados:

1. Mostrar al terminar un desglose de puntos por bajas, salas, botín y tiempo.
2. Comparar puntaje dentro de cada dificultad, o probar una tabla independiente por dificultad, antes de cambiar multiplicadores.
3. Medir cuánto tiempo extra se dedica al dinero y cuánto beneficio efectivo genera. Si la recogida domina el ritmo, probar agrupar dinero cercano en una sola recogida visual, manteniendo el valor total y las reglas de reparto.

## Problemas técnicos y telemetría

1. **Recuperaciones de transición:** 62 eventos en 33 de las 51 partidas, más 13 recuperaciones durante escapes en 8 partidas. No son necesariamente 75 fallos independientes: una misma sala puede recuperar a varios agentes. Investigar distribución por puerta, plantilla, posición de entrada y dirección. Una primera meta operativa sería menos de **una recuperación de transición cada diez expediciones**, sin introducir teletransportes ocultos; es una meta de prueba, no un resultado medido.
2. **Partidas estancadas:** 5 eventos `auto_run_stalled`, todos con el límite de 120 segundos de juego. Registrar posición de cada agente y enemigo, destino, estado del NavMesh, objetivo actual, sala, puertas y armas al dispararse el límite. Diferenciar «sin capacidad de hacer daño», «enemigo inaccesible», «agente atrapado» y «esperando transición».
3. **Registro de build:** agregar número de versión o commit, semilla de generación, versión de reglas, velocidad y configuración de presets al inicio del JSON. `game_version` por sí solo no permite separar cambios locales.
4. **Economía:** añadir dinero generado y abandonado, dinero por agente al entrar/salir de sala, intentos de curación y razón de fallo.
5. **Peligros:** registrar entrada y salida de nubes/radios, duración de exposición, muertes evitables y proximidad a explosivas. Los círculos visuales no son una medición de evasión.
6. **Armas:** registrar daño directo y por estado, daño a jefe y ratas, objetivos afectados por disparo, tiempo de recarga y control aplicado. Guardar los valores de arma usados en cada muestra.
7. **Distribuciones:** además de promedios, recoger mediana y percentiles 10/90 de tiempo de combate, daño recibido, dinero y puntaje. Separar visitas repetidas de salas únicas.
8. **Experiencia humana:** registrar opcionalmente pausas, decisiones de formación, cambios de objetivo y abandono; acompañar con una encuesta corta sobre claridad de amenazas, tensión, justicia y momentos favoritos. La diversión no se deduce directamente de la tasa de victoria.

## Plan de iteración recomendado

1. **Instrumentación y navegación:** identificar recuperaciones, daño desconocido y causas de estancamiento. Mantener los resultados afectados identificables; no mezclar partidas interrumpidas con derrotas.
2. **Amenazas:** probar evasión de veneno y explosiones con los valores existentes. Solo después ensayar cambios de radio o daño uno por uno.
3. **Enjambre:** hacer una tanda con multiplicador 2,0 frente al control 2,5. Mantener las mismas composiciones y semillas cuando sea posible.
4. **Armas:** corregir la escopeta y comparar Tazer, Pulse, cuchilla y armas de control en escenarios controlados antes de aplicar cambios globales.
5. **Validación:** por dificultad y variante, apuntar a **al menos 30–50 partidas terminadas**, complementadas por sesiones humanas. Esto reduce ruido respecto de las 6–14 actuales, aunque no garantiza precisión suficiente para diferencias pequeñas. Guardar los grupos de control y experimento por separado.

## Valores propuestos para la próxima prueba

Son puntos de partida para experimentos, **no cambios ya aplicados**:

| Elemento | Control actual observado en proyecto | Variante inicial | Qué medir |
|---|---|---|---|
| Enjambre | Multiplicador 2,5 | 2,0 | Victoria, piso de derrota, enemigos máximos simultáneos y duración. |
| Veneno | 5 DPS, 4 s, radio 4 | Primero evasión de IA; si no basta, 3 s **o** 4 DPS | Tiempo expuesto y daño por agente; cambiar una variable. |
| Explosiva | Radio 4, daño 8 | Primero evasión; después radio 3,5 si sigue siendo inevitable | Víctimas por explosión y daño por encuentro. |
| Tazer | 20 de daño total, intervalo 2 s | 25–30 de daño **o** intervalo 1,5–1,7 s | Daño total y control frente a 1, 2, 4 y 8 ratas. |
| Látigo | Intervalo 3 s, rango 5 | Intervalo 2,0–2,5 s | Ratas alejadas, tiempo de control y daño recibido por el equipo. |
| Médico | 5 % de vida máxima/$, umbral 70 % | Mantener | Curación efectiva, fondos disponibles y distribución entre aliados. |
| Hongo | 50 % de división por cría | Mantener | Máximo simultáneo, duración de sala y variación por semilla. |

Los tiempos citados son segundos de juego. En Auto-run ×3, 30 segundos de juego equivalen aproximadamente a 10 segundos reales, salvo cargas y caídas de rendimiento.
