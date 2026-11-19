"""
Regenera TODOS los modelos del juego (FBX en Assets/Models y .blend en BlenderModels).

  blender --background --python BlenderScripts/generar_todo.py
  blender --background --python BlenderScripts/generar_todo.py -- --preview   (además, imágenes en docs/previews)
  python  BlenderScripts/generar_todo.py [--preview]                          (con el paquete bpy de PyPI)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import crear_cofre  # noqa: E402
import crear_enemigo  # noqa: E402
import crear_jefes  # noqa: E402
import crear_jugador  # noqa: E402
import crear_piezas_sala  # noqa: E402
import crear_puerta  # noqa: E402

if __name__ == "__main__":
    for module in (crear_jugador, crear_enemigo, crear_jefes, crear_cofre, crear_puerta, crear_piezas_sala):
        module.create()
    print("[OK] Todos los modelos generados.")
