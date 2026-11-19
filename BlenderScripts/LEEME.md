# Scripts de Blender

Generan todos los modelos low-poly del juego desde cero:
* `Assets/Models/*.fbx`: los que usa Unity.
* `BlenderModels/*.blend`: para abrirlos y editarlos en Blender.
* `docs/previews/*.png`: vistas previas (opcional).

## Cómo regenerarlos

**Con Blender instalado** (4.x o 5.x), desde la raíz del repositorio:

```
blender --background --python BlenderScripts/generar_todo.py
blender --background --python BlenderScripts/generar_todo.py -- --preview
```

Para generar uno solo: `blender --background --python BlenderScripts/crear_cofre.py`

**Sin Blender**, con el paquete `bpy` de PyPI. Requiere la versión de Python que pida `bpy`; la 5.3 funciona con Python 3.13.

```
python -m venv .venv-blender
.venv-blender/bin/pip install bpy
.venv-blender/bin/python BlenderScripts/generar_todo.py --preview
```

## Archivos
| Script | Modelos |
|---|---|
| `common.py` | Paleta de colores, constructor de mallas, exportación FBX y render de vista previa |
| `arco.py` | Medidas compartidas entre el arco y la puerta |
| `crear_jugador.py` | `Player` (cuerpo y espada separada) |
| `crear_enemigo.py` | `Enemy_Goblin` (cuerpo y garrote separado) |
| `crear_jefes.py` | `Boss_Ogre` (nivel 1), `Boss_Golem` (nivel 2), `Boss_Demon` (nivel 3) |
| `crear_cofre.py` | `Chest` (base y tapa separada, con el pivote en la bisagra) |
| `crear_puerta.py` | `Door` (encaja en el hueco de `Room_Arch`) |
| `crear_piezas_sala.py` | `Room_Floor`, `Room_Wall`, `Room_Corner`, `Room_Arch` (rejilla de 4 m) |

## Convenciones
* 1 unidad = 1 metro. El origen está en la base del objeto; en el piso, en la cara superior.
* En Blender los modelos miran hacia **-Y**; en Unity quedan mirando hacia **+Z**.
* Las armas y la tapa del cofre son objetos separados, con el origen en su pivote (mano o bisagra). Se animan desde Unity por código; no hay esqueleto (rig).
* Los colores salen de la paleta `COLORS` de `common.py`. Si cambian un color ahí y regeneran, se actualiza en todos los modelos.
