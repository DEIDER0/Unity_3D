"""
Piezas modulares para armar las salas (rejilla de 4 m):
  Room_Floor  : baldosa de piso 4 x 4 m (16 losas). Origen en la cara SUPERIOR (y = 0 en Unity).
  Room_Wall   : pared de 4 m de ancho x 3 m de alto x 0.5 m de grosor, de ladrillos. Origen abajo, al centro.
  Room_Corner : columna de esquina para tapar las uniones entre paredes. Origen abajo, al centro.
  Room_Arch   : pared de 4 m con un arco de entrada de 2 x 2.7 m (ahí va la puerta). Origen abajo, al centro.
Las paredes van a lo largo del eje X (en Unity también X); su grosor va en Y (en Unity, Z).
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import arco  # noqa: E402
import common  # noqa: E402
from common import MeshBuilder  # noqa: E402

W, H, T = arco.WALL_WIDTH, arco.WALL_HEIGHT, arco.WALL_THICKNESS
BRICK_H = 0.5
BRICK_L = 1.0
BRICK_DEPTH = 0.07  # cuánto sobresalen los ladrillos del núcleo


def add_bricks(b, x_min, x_max, z_min, z_max):
    """Ladrillos en ambas caras de un tramo de pared, con hiladas alternas."""
    rows = int(round((z_max - z_min) / BRICK_H))
    for r in range(rows):
        z = z_min + r * BRICK_H + BRICK_H / 2
        x = x_min - (BRICK_L / 2 if r % 2 else 0)
        while x < x_max - 1e-6:
            x0, x1 = max(x, x_min), min(x + BRICK_L, x_max)
            if x1 - x0 > 0.05:
                mat = "Stone" if (r + int(x * 2)) % 3 else "LightStone"
                for side in (-1, 1):
                    b.box((x1 - x0 - 0.04, BRICK_DEPTH, BRICK_H - 0.04),
                          ((x0 + x1) / 2, side * (T / 2 - BRICK_DEPTH / 2 + 0.01), z), mat)
            x += BRICK_L


def create_floor():
    common.reset_scene()
    b = MeshBuilder()
    b.box((W, W, 0.18), (0, 0, -0.11), "Mortar")                      # base (junta)
    tile = W / 4
    for i in range(4):
        for j in range(4):
            mat = "FloorStone" if (i + j) % 2 == 0 else "DarkStone"
            b.box((tile - 0.05, tile - 0.05, 0.2), (-W / 2 + tile * (i + 0.5), -W / 2 + tile * (j + 0.5), -0.1), mat)
    b.build("Room_Floor")
    common.export("Room_Floor")


def create_wall():
    common.reset_scene()
    b = MeshBuilder()
    b.box((W, T - 2 * BRICK_DEPTH + 0.02, H), (0, 0, H / 2), "Mortar")  # núcleo
    add_bricks(b, -W / 2, W / 2, 0, H)
    b.box((W, T + 0.06, 0.12), (0, 0, H + 0.06), "DarkStone")           # remate superior
    b.build("Room_Wall")
    common.export("Room_Wall")


def create_corner():
    common.reset_scene()
    b = MeshBuilder()
    b.box((0.9, 0.9, 0.3), (0, 0, 0.15), "DarkStone")                   # base
    b.box((0.75, 0.75, H - 0.3), (0, 0, 0.3 + (H - 0.3) / 2), "Stone")  # fuste
    for z in (0.9, 1.8, 2.6):
        b.box((0.78, 0.78, 0.06), (0, 0, z), "LightStone")              # anillos decorativos
    b.box((0.9, 0.9, 0.22), (0, 0, H + 0.05), "DarkStone")              # capitel
    b.build("Room_Corner")
    common.export("Room_Corner")


def create_arch():
    common.reset_scene()
    b = MeshBuilder()
    half = arco.OPENING_HALF_WIDTH
    core_t = T - 2 * BRICK_DEPTH + 0.02

    # Núcleo: dos mitades (polígonos cóncavos) con el hueco del arco, extruidas en Y.
    arc = arco.arch_points(segments=8)            # de (-1, 1.7) pasando por (0, 2.7) hasta (1, 1.7)
    left_arc = [p for p in arc if p[0] <= 1e-6]   # mitad izquierda: (-1,1.7) ... (0,2.7)
    left = [(-W / 2, 0), (-half, 0)] + left_arc + [(0, H), (-W / 2, H)]
    right = [(-x, z) for (x, z) in reversed(left)]
    b.prism(left, core_t, "Mortar", axis="Y")
    b.prism(right, core_t, "Mortar", axis="Y")

    # Ladrillos solo en los tramos laterales
    add_bricks(b, -W / 2, -half - 0.25, 0, H)
    add_bricks(b, half + 0.25, W / 2, 0, H)

    # Dovelas (piedras del arco) y jambas laterales
    ring_r = half + 0.125
    for i in range(9):
        a = math.pi * (i + 0.5) / 9
        x, z = -ring_r * math.cos(a), arco.SPRING_HEIGHT + ring_r * math.sin(a)
        b.box((0.36, T + 0.06, 0.25), (x, 0, z), "LightStone", rot=(0, math.degrees(a) - 90, 0))
    for side in (-1, 1):
        for k in range(3):
            b.box((0.25, T + 0.06, 0.55), (side * ring_r, 0, 0.29 + k * 0.57), "LightStone")
    # Enjutas: piedra entre el borde exterior del arco y el remate (sin tapar el hueco)
    outer = [p for p in arco.arch_points(radius=half + 0.25, segments=8) if p[0] <= 1e-6]
    spandrel_left = outer + [(0, H), (-(half + 0.25), H)]
    spandrel_right = [(-x, z) for (x, z) in reversed(spandrel_left)]
    b.prism(spandrel_left, T, "Stone", axis="Y")
    b.prism(spandrel_right, T, "Stone", axis="Y")
    b.box((W, T + 0.06, 0.12), (0, 0, H + 0.06), "DarkStone")           # remate superior
    b.build("Room_Arch")
    common.export("Room_Arch")


def create():
    create_floor()
    create_wall()
    create_corner()
    create_arch()


if __name__ == "__main__":
    create()
