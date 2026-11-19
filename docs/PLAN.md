# Plan del proyecto — Dungeon Depths (nombre provisional)

Dungeon crawler 3D de acción, cuerpo a cuerpo, para PC. Unity **6000.3.21f1** + URP.
Asignatura: Tecnologías Inmersivas.

## Decisiones aprobadas
| Tema | Decisión |
|---|---|
| Cámara | Isométrica fija que sigue al jugador |
| Controles | WASD o flechas para moverse · el jugador mira hacia el mouse · clic izquierdo o Espacio para atacar · Shift para esquivar · E para abrir el cofre |
| Esquive | Dash corto con unos 0,3 s de invulnerabilidad y un tiempo de espera |
| Puertas | Sala 1: sin enemigos vivos · Sala 2: cofre abierto (y sin enemigos) · Sala 3: jefe derrotado |
| Entre niveles | El jugador conserva vida e ítems en memoria (no hay guardado en disco) |
| Victoria y derrota | Las dos se muestran en la escena GameOver, con un mensaje distinto |
| Jefe | Fase de furia al 50 % de vida (daño ×1,5) |

## Arquitectura MVC
Dependencias en un solo sentido, impuestas por las asmdef:
`Controller → View, Model, Data` · `View → Model` · `Data → Model` · `Model → nada`

* **Model** (`Assets/Scripts/Model`, `noEngineReferences = true`): C# puro, con todas las reglas. Se comunica solo con eventos `System.Action`.
* **Data** (`Assets/Scripts/Data`): ScriptableObjects de configuración. Los niveles 2 y 3 solo cambian estos datos.
* **View** (`Assets/Scripts/View`): solo muestran información. Se suscriben en `OnEnable` y se desuscriben en `OnDisable`.
* **Controller** (`Assets/Scripts/Controller`): leen la entrada, coordinan y crean e inyectan el Modelo.
* **Editor** (`Assets/Scripts/Editor`): construyen escenas y prefabs automáticamente.

## Fases
| Fase | Contenido | Estado |
|---|---|---|
| 0 | Base del proyecto: paquetes, carpetas, asmdefs y acciones de entrada | ✅ |
| a | Modelo y pruebas unitarias | ✅ |
| b | Modelos 3D en Blender (bpy), exportación a FBX e importación | ⏳ |
| c | Jugador: movimiento, ataque y esquive | |
| d | Enemigo básico con IA | |
| e | Salas, puertas y RoomController | |
| f | Cofre y recompensa | |
| g | Jefe | |
| h | HUD, menú principal y GameOver | |
| i | Niveles 2 y 3 (solo datos) | |

## Fuera de alcance
Multijugador, guardado de partida, generación aleatoria de salas, inventario complejo.
