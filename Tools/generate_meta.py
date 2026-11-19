"""
Genera archivos .meta (con GUID aleatorio y estable) para carpetas, scripts .cs y .asmdef
dentro de Assets/ que todavía no lo tengan. Así los 3 integrantes del equipo comparten los
mismos GUID desde el primer commit y se evitan conflictos de referencias en Unity.

Uso (desde la raíz del repositorio):  python3 Tools/generate_meta.py
Los demás tipos de archivo (fbx, inputactions, escenas...) los genera Unity al abrir el proyecto.
"""
import os
import uuid

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets")

FOLDER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

SCRIPT = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

ASMDEF = """fileFormatVersion: 2
guid: {guid}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_meta(path, template):
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    with open(meta, "w", newline="\n") as f:
        f.write(template.format(guid=uuid.uuid4().hex))
    print("creado", os.path.relpath(meta, os.path.join(ROOT, "..")))


def main():
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if not d.startswith(".")]
        for d in dirnames:
            write_meta(os.path.join(dirpath, d), FOLDER)
        for name in filenames:
            full = os.path.join(dirpath, name)
            if name.endswith(".cs"):
                write_meta(full, SCRIPT)
            elif name.endswith(".asmdef"):
                write_meta(full, ASMDEF)


if __name__ == "__main__":
    main()
