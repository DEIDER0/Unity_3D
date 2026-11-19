"""
Puerta de madera con bandas de hierro que encaja en el hueco del arco (Room_Arch).

Assets/Models/Door.fbx -> un solo objeto "Door" (2 m de ancho, 2.7 m de alto en el centro, 12 cm de grosor).
Origen en la base, al centro. En Unity, DoorView la hunde bajo el piso para abrirla
(se baja 2.8 m en Y), así no atraviesa la pared.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arco  # noqa: E402
import common  # noqa: E402
from common import MeshBuilder  # noqa: E402

PLANKS = 8
THICKNESS = 0.12
GAP = 0.02  # holgura con el arco


def create():
    common.reset_scene()
    b = MeshBuilder()

    half = arco.OPENING_HALF_WIDTH - GAP
    plank_width = 2 * half / PLANKS
    for i in range(PLANKS):
        x0 = -half + i * plank_width
        x_center = x0 + plank_width / 2
        outer_edge = max(abs(x0), abs(x0 + plank_width))  # el borde más bajo del arco sobre esta tabla
        top = arco.arch_height(outer_edge) - GAP
        mat = "Wood" if i % 2 == 0 else "DarkWood"
        b.box((plank_width - 0.012, THICKNESS, top), (x_center, 0, top / 2), mat)

    for z in (0.45, 1.45):
        b.box((2 * half, THICKNESS + 0.03, 0.12), (0, 0, z), "Iron")             # bandas
    b.box((0.06, THICKNESS + 0.03, 1.2), (0, 0, 1.0), "Iron")                   # refuerzo vertical
    b.cylinder(0.09, 0.03, (0.55, -THICKNESS / 2 - 0.03, 1.1), "Iron", rot=(90, 0, 0), segments=8,
               radius_top=0.09)                                                 # aldaba (aro)
    b.cylinder(0.05, 0.05, (0.55, -THICKNESS / 2 - 0.02, 1.2), "Iron", rot=(90, 0, 0), segments=6)
    b.build("Door")
    common.export("Door")


if __name__ == "__main__":
    create()
