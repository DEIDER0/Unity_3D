"""
Cofre del tesoro con la tapa como objeto separado, para animarla desde Unity.

Jerarquía exportada (Assets/Models/Chest.fbx):
  Chest            (vacío, raíz, origen en la base)
  ├── Chest_Base   (malla: caja, bandas de hierro, cerradura y monedas)
  └── Chest_Lid    (malla: tapa curva; origen = bisagra trasera superior)
                    -> para abrirla basta rotarla sobre su eje X local.
Medidas: 1.0 m de ancho x 0.6 m de fondo x 0.8 m de alto (cerrado).
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common  # noqa: E402
from common import MeshBuilder  # noqa: E402

WIDTH, DEPTH, BASE_HEIGHT = 1.0, 0.6, 0.5
HINGE = (0.0, DEPTH / 2, BASE_HEIGHT)  # el frente mira a -Y, la bisagra queda atrás (+Y)


def build_base(root):
    b = MeshBuilder()
    b.box((WIDTH, DEPTH, BASE_HEIGHT), (0, 0, BASE_HEIGHT / 2), "Wood")
    b.box((WIDTH + 0.04, DEPTH + 0.04, 0.06), (0, 0, 0.03), "DarkWood")                # zócalo
    for x in (-0.36, 0.36):
        b.box((0.08, DEPTH + 0.02, BASE_HEIGHT + 0.01), (x, 0, BASE_HEIGHT / 2), "Iron")  # bandas
    b.box((0.16, 0.03, 0.14), (0, -DEPTH / 2 - 0.005, BASE_HEIGHT - 0.08), "Gold")     # cerradura
    b.box((0.04, 0.035, 0.05), (0, -DEPTH / 2 - 0.01, BASE_HEIGHT - 0.09), "Black")    # ojo de la llave
    # Monedas: se ven cuando la tapa se abre
    for (x, y, s) in ((-0.2, 0.0, 0.16), (0.1, -0.05, 0.2), (0.28, 0.1, 0.13), (-0.05, 0.12, 0.14)):
        b.sphere(s, (x, y, BASE_HEIGHT), "Gold", scale=(1, 1, 0.45), segments=7, rings=4)
    return b.build("Chest_Base", parent=root)


def lid_profile(radius, center_z, flatten=0.75, segments=6):
    """Semicírculo (y, z) aplastado, de atrás (+Y) hacia adelante (-Y)."""
    pts = []
    for i in range(segments + 1):
        a = math.pi * i / segments
        pts.append((radius * math.cos(a), center_z + radius * math.sin(a) * flatten))
    return pts


def build_lid(root):
    b = MeshBuilder()
    b.prism(lid_profile(DEPTH / 2, BASE_HEIGHT), WIDTH, "Wood", axis="X")
    for x in (-0.36, 0.36):
        b.prism(lid_profile(DEPTH / 2 + 0.012, BASE_HEIGHT - 0.001), 0.08, "Iron", axis="X", offset=x)
    b.box((0.12, 0.03, 0.1), (0, -DEPTH / 2 - 0.015, BASE_HEIGHT + 0.02), "Gold")      # pestillo
    return b.build("Chest_Lid", origin=HINGE, parent=root)


def create():
    common.reset_scene()
    root = common.make_empty("Chest")
    build_base(root)
    build_lid(root)
    common.export("Chest")


if __name__ == "__main__":
    create()
