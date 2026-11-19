"""
Enemigo básico: goblin low-poly (~1.3 m), encorvado y con un garrote.

Jerarquía exportada (Assets/Models/Enemy_Goblin.fbx):
  Enemy_Goblin          (vacío, raíz)
  ├── Enemy_Body        (malla)
  └── Enemy_Weapon      (garrote; origen = mano derecha, para animar el golpe)
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common  # noqa: E402
from common import MeshBuilder  # noqa: E402

HAND_RIGHT = (-0.3, -0.12, 0.5)


def build_body(root):
    b = MeshBuilder()
    for side in (-1, 1):
        x = 0.1 * side
        b.box((0.16, 0.24, 0.1), (x, -0.04, 0.05), "GoblinSkin")         # pies
        b.box((0.13, 0.13, 0.32), (x, 0, 0.26), "GoblinSkin")             # piernas
        b.box((0.11, 0.11, 0.42), (0.29 * side, -0.06, 0.72), "GoblinSkin", rot=(15, 0, 0))  # brazos largos
        b.box((0.12, 0.12, 0.1), (0.3 * side, -0.12, 0.5), "GoblinSkin")  # manos

    b.box((0.4, 0.28, 0.24), (0, 0, 0.5), "GoblinRag")                    # taparrabo
    b.box((0.42, 0.3, 0.38), (0, 0.02, 0.78), "GoblinSkin", rot=(12, 0, 0))  # torso encorvado
    b.box((0.44, 0.31, 0.12), (0, 0.0, 0.62), "Leather")                 # cinturón
    b.box((0.06, 0.32, 0.36), (0.12, 0.03, 0.8), "GoblinRag", rot=(12, 0, 25))  # correa cruzada

    # Cabeza grande, adelantada
    b.box((0.38, 0.34, 0.32), (0, -0.08, 1.12), "GoblinSkin")
    b.box((0.08, 0.1, 0.1), (0, -0.28, 1.08), "GoblinSkin")               # nariz
    for side in (-1, 1):
        b.box((0.07, 0.02, 0.06), (0.09 * side, -0.255, 1.17), "GlowYellow")  # ojos
        b.cylinder(0.08, 0.3, (0.3 * side, -0.06, 1.16), "GoblinSkin", radius_top=0.0,
                   segments=4, rot=(0, -90 * side, 0), scale=(1, 0.5, 1))  # orejas puntiagudas
    b.box((0.2, 0.02, 0.04), (0, -0.255, 1.01), "Black")                  # boca
    for side in (-1, 1):
        b.cylinder(0.02, 0.06, (0.06 * side, -0.26, 1.04), "White", radius_top=0.0, segments=4)  # colmillos
    return b.build("Enemy_Body", parent=root)


def build_weapon(root):
    hx, hy, hz = HAND_RIGHT
    b = MeshBuilder()
    b.cylinder(0.03, 0.2, (hx, hy, hz), "DarkWood", segments=6)                       # empuñadura
    b.cylinder(0.04, 0.5, (hx, hy, hz + 0.35), "Wood", segments=6, radius_top=0.08)   # garrote
    for i, (ox, oy) in enumerate(((0.08, 0), (-0.08, 0), (0, 0.08), (0, -0.08))):
        b.cylinder(0.02, 0.07, (hx + ox, hy + oy, hz + 0.5 + 0.03 * (i % 2)), "Iron",
                   radius_top=0.0, segments=4, rot=(-oy * 900, ox * 900, 0))       # púas
    weapon = b.build("Enemy_Weapon", origin=HAND_RIGHT, parent=root)
    weapon.rotation_euler = (0.5, 0, 0)
    return weapon


def create():
    common.reset_scene()
    root = common.make_empty("Enemy_Goblin")
    build_body(root)
    build_weapon(root)
    common.export("Enemy_Goblin")


if __name__ == "__main__":
    create()
