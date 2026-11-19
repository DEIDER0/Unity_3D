"""
Jugador: caballero low-poly (~1.75 m) con escudo y espada.

Jerarquía exportada (Assets/Models/Player.fbx):
  Player            (vacío, raíz, origen entre los pies)
  ├── Player_Body   (malla: cuerpo, casco y escudo)
  └── Player_Sword  (malla separada; origen = empuñadura en la mano derecha,
                     para animar el golpe rotándola desde Unity)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common  # noqa: E402
from common import MeshBuilder  # noqa: E402

HAND_RIGHT = (-0.33, -0.05, 0.68)  # la derecha del personaje es -X porque mira hacia -Y


def build_body(root):
    b = MeshBuilder()
    for side in (-1, 1):
        x = 0.12 * side
        b.box((0.2, 0.28, 0.15), (x, -0.03, 0.075), "Leather")  # botas
        b.box((0.17, 0.17, 0.56), (x, 0, 0.43), "Pants")        # piernas
        b.box((0.14, 0.14, 0.46), (0.33 * side, 0, 0.95), "Steel")  # brazos
        b.box((0.12, 0.12, 0.12), (0.33 * side, -0.02, 0.68), "Leather")  # guantes
        b.sphere(0.14, (0.31 * side, 0, 1.19), "Steel", scale=(1, 1, 0.8), segments=8, rings=4)  # hombreras

    b.box((0.5, 0.3, 0.55), (0, 0, 0.98), "Steel")             # torso (armadura)
    b.box((0.36, 0.02, 0.44), (0, -0.155, 0.96), "BlueCloth")  # tabardo
    b.box((0.08, 0.021, 0.3), (0, -0.165, 1.0), "Gold")         # emblema
    b.box((0.52, 0.32, 0.08), (0, 0, 0.74), "Leather")         # cinturón
    b.box((0.09, 0.02, 0.07), (0, -0.165, 0.74), "Gold")        # hebilla

    b.box((0.16, 0.16, 0.06), (0, 0, 1.28), "Skin")            # cuello
    b.box((0.3, 0.3, 0.3), (0, 0, 1.44), "Skin")               # cabeza
    for side in (-1, 1):
        b.box((0.05, 0.01, 0.06), (0.07 * side, -0.151, 1.46), "Black")  # ojos
    b.box((0.34, 0.34, 0.13), (0, 0, 1.6), "Steel")            # casco (copa)
    b.box((0.34, 0.07, 0.26), (0, 0.145, 1.47), "Steel")       # casco (nuca)
    for side in (-1, 1):
        b.box((0.04, 0.3, 0.2), (0.17 * side, 0.02, 1.47), "Steel")  # protectores laterales
    b.cylinder(0.05, 0.22, (0, 0.04, 1.76), "RedCloth", radius_top=0.0, segments=6)  # penacho

    # Escudo en el brazo izquierdo (+X)
    b.box((0.04, 0.46, 0.56), (0.415, -0.02, 0.92), "Steel")
    b.box((0.05, 0.4, 0.5), (0.43, -0.02, 0.92), "BlueCloth")
    b.box((0.02, 0.1, 0.24), (0.46, -0.02, 0.94), "Gold")
    b.box((0.02, 0.24, 0.08), (0.46, -0.02, 0.98), "Gold")
    return b.build("Player_Body", origin=(0, 0, 0), parent=root)


def build_sword(root):
    """Espada construida en vertical (eje +Z) desde la empuñadura; luego se inclina hacia adelante."""
    hx, hy, hz = HAND_RIGHT
    b = MeshBuilder()
    b.sphere(0.035, (hx, hy, hz - 0.1), "Gold", segments=6, rings=4)               # pomo
    b.cylinder(0.025, 0.18, (hx, hy, hz), "Leather", segments=6)                  # mango
    b.box((0.26, 0.05, 0.04), (hx, hy, hz + 0.11), "Gold")                         # guarda
    b.box((0.1, 0.03, 0.72), (hx, hy, hz + 0.49), "Steel")                        # hoja
    b.cylinder(0.05, 0.12, (hx, hy, hz + 0.91), "Steel", radius_top=0.0, segments=4,
               rot=(0, 0, 45), scale=(1.4, 0.45, 1))                                  # punta
    sword = b.build("Player_Sword", origin=HAND_RIGHT, parent=root)
    sword.rotation_euler = (0.6, 0, 0)  # ~35°: la hoja apunta hacia adelante y arriba
    return sword


def create():
    common.reset_scene()
    root = common.make_empty("Player")
    build_body(root)
    build_sword(root)
    common.export("Player")


if __name__ == "__main__":
    create()
