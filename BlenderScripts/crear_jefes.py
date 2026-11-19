"""
Tres jefes, uno por nivel. Son más grandes y distintos al goblin:
  Nivel 1 -> Boss_Ogre   : ogro gordo (~3.0 m) con un garrote enorme.
  Nivel 2 -> Boss_Golem  : gólem de piedra (~3.3 m) con cristales que brillan y un mazo de piedra.
  Nivel 3 -> Boss_Demon  : demonio rojo (~3.8 m) con cuernos, alas y un hacha.

Jerarquía de cada FBX (igual en los tres, para reutilizar el mismo prefab y controlador):
  Boss_X
  ├── Boss_Body
  └── Boss_Weapon   (origen = mano derecha)
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import common  # noqa: E402
from common import MeshBuilder  # noqa: E402


# ---------------------------------------------------------------------------
# Nivel 1: Ogro
# ---------------------------------------------------------------------------
def build_ogre():
    common.reset_scene()
    root = common.make_empty("Boss_Ogre")
    hand = (-0.95, -0.35, 1.05)

    b = MeshBuilder()
    for side in (-1, 1):
        b.box((0.45, 0.6, 0.25), (0.35 * side, -0.08, 0.125), "Leather")              # pies
        b.cylinder(0.22, 0.7, (0.35 * side, 0, 0.6), "OgreSkin", segments=8)          # piernas
        b.cylinder(0.2, 0.9, (0.85 * side, -0.15, 1.55), "OgreSkin", segments=8,
                   rot=(20, -12 * side, 0), radius_top=0.24)                          # brazos
        b.sphere(0.22, (0.95 * side, -0.35, 1.05), "OgreSkin", segments=8, rings=5)   # puños
        b.sphere(0.3, (0.75 * side, 0.05, 2.15), "OgreSkin", segments=8, rings=5)     # hombros
    b.box((1.0, 0.75, 0.35), (0, 0, 0.95), "Leather")                                  # taparrabo
    b.sphere(0.85, (0, 0, 1.6), "OgreSkin", scale=(1, 0.8, 0.95), segments=10, rings=7)  # cuerpo
    b.sphere(0.6, (0, -0.32, 1.4), "OgreBelly", scale=(1, 0.6, 0.9), segments=10, rings=6)  # barriga
    b.box((0.12, 0.9, 0.12), (0.2, 0, 1.9), "Leather", rot=(0, 35, 0))                 # correa

    # Cabeza pequeña con colmillos
    b.box((0.55, 0.5, 0.45), (0, -0.25, 2.5), "OgreSkin")
    b.box((0.5, 0.08, 0.1), (0, -0.5, 2.62), "OgreSkin")                               # ceja
    for side in (-1, 1):
        b.box((0.08, 0.02, 0.07), (0.13 * side, -0.505, 2.53), "GlowYellow")           # ojos
        b.cylinder(0.05, 0.16, (0.15 * side, -0.48, 2.42), "White", radius_top=0.0, segments=4)  # colmillos
        b.cylinder(0.08, 0.15, (0.3 * side, -0.25, 2.55), "OgreSkin", radius_top=0.03,
                   segments=5, rot=(0, -90 * side, 0))                                 # orejas
    b.build("Boss_Body", parent=root)

    hx, hy, hz = hand
    w = MeshBuilder()
    w.cylinder(0.08, 0.5, (hx, hy, hz), "DarkWood", segments=6)
    w.cylinder(0.12, 1.3, (hx, hy, hz + 0.9), "Wood", segments=7, radius_top=0.28)
    for i in range(6):
        a = i * math.pi / 3
        w.cylinder(0.05, 0.18, (hx + 0.27 * math.cos(a), hy + 0.27 * math.sin(a), hz + 1.3 + 0.1 * (i % 2)),
                   "Iron", radius_top=0.0, segments=4,
                   rot=(-math.degrees(math.sin(a)) * 1.4, math.degrees(math.cos(a)) * 1.4, 0))
    weapon = w.build("Boss_Weapon", origin=hand, parent=root)
    weapon.rotation_euler = (0.35, 0, 0)
    common.export("Boss_Ogre")


# ---------------------------------------------------------------------------
# Nivel 2: Gólem de piedra
# ---------------------------------------------------------------------------
def build_golem():
    common.reset_scene()
    root = common.make_empty("Boss_Golem")
    hand = (-1.05, -0.3, 1.0)

    b = MeshBuilder()
    for side in (-1, 1):
        b.box((0.55, 0.6, 0.3), (0.4 * side, -0.05, 0.15), "DarkStone")                     # pies
        b.box((0.45, 0.45, 0.8), (0.4 * side, 0, 0.7), "GolemStone", rot=(0, 0, 4 * side))  # piernas
        b.box((0.65, 0.65, 0.5), (1.0 * side, 0, 2.2), "DarkStone", rot=(0, 10 * side, 0))  # hombros
        b.box((0.42, 0.42, 0.75), (1.02 * side, -0.05, 1.65), "GolemStone", rot=(8, 0, 0))  # brazos
        b.box((0.55, 0.55, 0.5), (1.05 * side, -0.25, 1.0), "DarkStone", rot=(10, 0, 8))    # puños
        b.rock(0.18, (1.05 * side, 0.05, 2.55), "GlowCyan", scale=(0.7, 0.7, 1.6))          # cristales del hombro
    b.box((1.2, 0.8, 0.5), (0, 0, 1.25), "DarkStone")                                       # cadera
    b.box((1.6, 1.0, 1.0), (0, 0, 1.95), "GolemStone", rot=(0, 0, 2))                        # pecho
    b.rock(0.28, (0, -0.48, 2.0), "GlowCyan", scale=(1, 0.5, 1.2))                           # núcleo brillante
    for i, (x, z) in enumerate(((-0.5, 2.35), (0.55, 1.65), (-0.45, 1.6), (0.4, 2.4))):
        b.rock(0.2, (x, -0.5, z), "Stone", scale=(1, 0.5, 0.8))                              # rocas sueltas
    b.box((0.6, 0.55, 0.5), (0, -0.1, 2.75), "GolemStone")                                   # cabeza
    for side in (-1, 1):
        b.box((0.12, 0.02, 0.07), (0.13 * side, -0.38, 2.78), "GlowCyan")                    # ojos
    b.build("Boss_Body", parent=root)

    hx, hy, hz = hand
    w = MeshBuilder()
    w.cylinder(0.07, 1.6, (hx, hy, hz + 0.5), "DarkWood", segments=6)                        # mango
    w.box((0.9, 0.5, 0.5), (hx, hy, hz + 1.35), "Stone")                                     # cabeza del mazo
    w.box((0.3, 0.52, 0.52), (hx, hy, hz + 1.35), "Iron")                                    # aro de hierro
    weapon = w.build("Boss_Weapon", origin=hand, parent=root)
    weapon.rotation_euler = (0.3, 0, 0)
    common.export("Boss_Golem")


# ---------------------------------------------------------------------------
# Nivel 3: Demonio
# ---------------------------------------------------------------------------
def build_demon():
    common.reset_scene()
    root = common.make_empty("Boss_Demon")
    hand = (-0.85, -0.35, 1.45)

    b = MeshBuilder()
    for side in (-1, 1):
        b.cylinder(0.12, 0.3, (0.3 * side, -0.05, 0.15), "Horn", segments=6, radius_top=0.16)  # pezuñas
        b.box((0.26, 0.3, 0.6), (0.3 * side, 0.05, 0.6), "DemonSkin", rot=(-15, 0, 0))         # muslo bajo
        b.box((0.3, 0.32, 0.65), (0.3 * side, 0, 1.15), "DemonSkin", rot=(15, 0, 0))           # muslo
        b.box((0.22, 0.22, 0.75), (0.75 * side, -0.1, 2.0), "DemonSkin", rot=(15, -15 * side, 0))  # brazos
        b.box((0.24, 0.24, 0.24), (0.85 * side, -0.35, 1.45), "DemonDark")                     # manos
        b.sphere(0.24, (0.6 * side, 0, 2.55), "DemonDark", segments=6, rings=4)                # hombros
        # Alas: dos placas finas inclinadas hacia atrás
        b.box((1.2, 0.05, 1.0), (0.75 * side, 0.45, 2.7), "DemonDark", rot=(-20, 0, -25 * side))
        b.box((0.08, 0.08, 1.3), (0.4 * side, 0.42, 2.9), "Horn", rot=(-20, 50 * side, 0))      # hueso del ala
    b.box((0.75, 0.45, 0.35), (0, 0, 1.55), "DemonDark")                                     # cadera
    b.box((0.85, 0.5, 0.85), (0, 0, 2.15), "DemonSkin", rot=(-5, 0, 0))                      # torso
    b.box((0.6, 0.06, 0.5), (0, -0.25, 2.2), "DemonDark")                                    # pecho marcado
    b.cylinder(0.08, 1.0, (0, 0.55, 1.2), "DemonSkin", segments=5, radius_top=0.02, rot=(-60, 0, 0))  # cola

    b.box((0.45, 0.45, 0.45), (0, -0.05, 2.85), "DemonSkin")                                 # cabeza
    for side in (-1, 1):
        b.box((0.1, 0.02, 0.06), (0.11 * side, -0.28, 2.9), "GlowOrange")                    # ojos
        b.cylinder(0.08, 0.45, (0.25 * side, -0.05, 3.22), "Horn", segments=5, radius_top=0.0,
                   rot=(-15, 35 * side, 0))                                                  # cuernos
    b.box((0.25, 0.02, 0.05), (0, -0.28, 2.74), "Black")                                     # boca
    b.build("Boss_Body", parent=root)

    hx, hy, hz = hand
    w = MeshBuilder()
    w.cylinder(0.06, 2.0, (hx, hy, hz + 0.6), "Iron", segments=6)                            # asta
    # Hoja del hacha: perfil (x, z) relativo a la mano, extruido 6 cm en Y
    blade = [(0.0, 0.8), (-0.85, 0.5), (-1.0, 1.05), (-0.85, 1.6), (0.0, 1.3)]  # hacia afuera (-X)
    w.prism([(hx + dx, hz + dz) for dx, dz in blade], 0.08, "DarkSteel", axis="Y", offset=hy)
    w.box((0.05, 0.09, 0.95), (hx - 0.98, hy, hz + 1.05), "GlowOrange")                       # filo ardiente
    w.cylinder(0.1, 0.25, (hx, hy, hz + 1.2), "Gold", segments=6)                            # anillo
    w.cylinder(0.05, 0.2, (hx, hy, hz + 1.7), "Iron", radius_top=0.0, segments=4)            # punta
    weapon = w.build("Boss_Weapon", origin=hand, parent=root)
    weapon.rotation_euler = (0.25, 0, 0)
    common.export("Boss_Demon")


def create():
    build_ogre()
    build_golem()
    build_demon()


if __name__ == "__main__":
    create()
