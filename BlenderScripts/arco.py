"""
Medidas compartidas entre el arco de entrada (crear_piezas_sala.py) y la puerta (crear_puerta.py),
para que la puerta encaje exactamente en el hueco del arco.
"""
import math

WALL_WIDTH = 4.0        # ancho de un módulo de pared (m)
WALL_HEIGHT = 3.0       # alto de la pared (m)
WALL_THICKNESS = 0.5    # grosor de la pared (m)

OPENING_HALF_WIDTH = 1.0  # el hueco mide 2 m de ancho
SPRING_HEIGHT = 1.7       # altura donde empieza la curva del arco
# Radio interior del arco = OPENING_HALF_WIDTH -> el hueco mide 2.7 m de alto en el centro.


def arch_height(x, radius=OPENING_HALF_WIDTH):
    """Altura del borde del arco (semicírculo) en la posición horizontal x."""
    x = min(abs(x), radius)
    return SPRING_HEIGHT + math.sqrt(radius * radius - x * x)


def arch_points(radius=OPENING_HALF_WIDTH, segments=8, from_left=True):
    """Puntos (x, z) del semicírculo, de izquierda a derecha (o al revés)."""
    pts = []
    for i in range(segments + 1):
        a = math.pi * i / segments
        pts.append((-radius * math.cos(a), SPRING_HEIGHT + radius * math.sin(a)))
    return pts if from_left else list(reversed(pts))
