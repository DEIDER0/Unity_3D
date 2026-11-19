"""
Utilidades comunes para generar los modelos low-poly del juego con Blender (bpy).

Convenciones (todas las piezas las respetan):
  * 1 unidad = 1 metro.
  * El modelo mira hacia -Y en Blender (vista frontal). Al exportar con
    axis_forward='-Z', axis_up='Y' y bake_space_transform=True, en Unity queda
    mirando hacia +Z (adelante), con Y hacia arriba y sin rotaciones raras en la raíz.
  * El origen está en la base del objeto (los pies tocan z = 0). El piso tiene su
    origen en la cara superior, para que los personajes queden apoyados en y = 0.
  * Las partes que se animan desde Unity (arma, tapa del cofre) son objetos
    separados, con el origen en su pivote (mano, bisagra).

Se puede ejecutar de dos formas:
  blender --background --python BlenderScripts/crear_jugador.py
  python  BlenderScripts/crear_jugador.py        (con el paquete "bpy" de PyPI instalado)
Agrega "-- --preview" (Blender) o "--preview" (Python) para renderizar una imagen PNG de vista previa.
"""
import math
import os
import sys

import bpy  # bpy primero: en el módulo de PyPI, bmesh y mathutils se cargan al importar bpy
import bmesh
from mathutils import Euler, Matrix, Vector

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
FBX_DIR = os.path.join(REPO_ROOT, "Assets", "Models")
BLEND_DIR = os.path.join(REPO_ROOT, "BlenderModels")
PREVIEW_DIR = os.path.join(REPO_ROOT, "docs", "previews")


# ---------------------------------------------------------------------------
# Paleta de colores (sRGB 0..1). Los materiales se comparten por nombre.
# ---------------------------------------------------------------------------
COLORS = {
    "Skin": (0.93, 0.74, 0.60),
    "Steel": (0.62, 0.65, 0.70),
    "DarkSteel": (0.25, 0.27, 0.30),
    "Iron": (0.16, 0.16, 0.18),
    "Gold": (0.95, 0.75, 0.20),
    "BlueCloth": (0.18, 0.33, 0.75),
    "RedCloth": (0.75, 0.15, 0.15),
    "Leather": (0.36, 0.22, 0.12),
    "Pants": (0.22, 0.24, 0.32),
    "Wood": (0.55, 0.36, 0.20),
    "DarkWood": (0.35, 0.22, 0.12),
    "Black": (0.05, 0.05, 0.06),
    "White": (0.95, 0.95, 0.92),
    "GoblinSkin": (0.38, 0.62, 0.25),
    "GoblinRag": (0.45, 0.35, 0.22),
    "OgreSkin": (0.52, 0.55, 0.30),
    "OgreBelly": (0.66, 0.66, 0.42),
    "Stone": (0.45, 0.45, 0.48),
    "DarkStone": (0.30, 0.30, 0.33),
    "LightStone": (0.60, 0.59, 0.56),
    "FloorStone": (0.36, 0.34, 0.36),
    "Mortar": (0.20, 0.19, 0.20),
    "GolemStone": (0.50, 0.47, 0.44),
    "DemonSkin": (0.62, 0.10, 0.08),
    "DemonDark": (0.22, 0.05, 0.10),
    "Horn": (0.12, 0.10, 0.10),
}
# Materiales que brillan (ojos, cristales): color y fuerza de emisión.
EMISSIVE = {
    "GlowYellow": ((1.0, 0.85, 0.1), 3.0),
    "GlowCyan": ((0.2, 0.9, 1.0), 4.0),
    "GlowOrange": ((1.0, 0.45, 0.05), 4.0),
}
METALLIC = {"Steel": 0.8, "DarkSteel": 0.8, "Iron": 0.7, "Gold": 0.9}


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def get_material(name):
    """Devuelve (o crea) el material M_<name> con color liso."""
    mat_name = "M_" + name
    mat = bpy.data.materials.get(mat_name)
    if mat:
        return mat

    mat = bpy.data.materials.new(mat_name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")

    if name in EMISSIVE:
        rgb, strength = EMISSIVE[name]
    else:
        rgb, strength = COLORS[name], 0.0
    lin = tuple(srgb_to_linear(c) for c in rgb) + (1.0,)

    bsdf.inputs["Base Color"].default_value = lin
    bsdf.inputs["Roughness"].default_value = 0.35 if name in METALLIC else 0.85
    bsdf.inputs["Metallic"].default_value = METALLIC.get(name, 0.0)
    if strength > 0:
        bsdf.inputs["Emission Color"].default_value = lin
        bsdf.inputs["Emission Strength"].default_value = strength
    mat.diffuse_color = lin  # color en la vista del editor
    return mat


# ---------------------------------------------------------------------------
# Escena
# ---------------------------------------------------------------------------
def reset_scene():
    """Deja una escena vacía en metros."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0


def make_empty(name, location=(0, 0, 0), parent=None):
    """Objeto vacío que sirve de raíz o de pivote."""
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_size = 0.25
    bpy.context.scene.collection.objects.link(obj)
    _place(obj, location, parent)
    return obj


def _place(obj, world_location, parent):
    if parent is not None:
        obj.parent = parent
        obj.location = Vector(world_location) - parent.matrix_world.translation
    else:
        obj.location = Vector(world_location)
    bpy.context.view_layer.update()


def _matrix(loc, rot, scale):
    return (Matrix.Translation(Vector(loc))
            @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4()
            @ Matrix.Diagonal((scale[0], scale[1], scale[2], 1.0)))


# ---------------------------------------------------------------------------
# Constructor de mallas low-poly (bmesh: no depende del contexto de la interfaz)
# Todas las coordenadas son del mundo; build() mueve la malla a su origen.
# Las rotaciones se dan en grados (X, Y, Z).
# ---------------------------------------------------------------------------
class MeshBuilder:
    def __init__(self):
        self.bm = bmesh.new()
        self.materials = []

    def _assign(self, verts, mat_name):
        mat = get_material(mat_name)
        if mat not in self.materials:
            self.materials.append(mat)
        index = self.materials.index(mat)
        for face in {f for v in verts for f in v.link_faces}:
            face.material_index = index

    def box(self, size, loc, mat, rot=(0, 0, 0)):
        res = bmesh.ops.create_cube(self.bm, size=1.0, matrix=_matrix(loc, rot, size))
        self._assign(res["verts"], mat)

    def cylinder(self, radius, depth, loc, mat, rot=(0, 0, 0), segments=8, radius_top=None, scale=(1, 1, 1)):
        """Cilindro o cono (radius_top) a lo largo del eje Z local, centrado en loc."""
        res = bmesh.ops.create_cone(
            self.bm, cap_ends=True, cap_tris=False, segments=segments,
            radius1=radius, radius2=radius if radius_top is None else radius_top,
            depth=depth, matrix=_matrix(loc, rot, scale))
        self._assign(res["verts"], mat)

    def sphere(self, radius, loc, mat, scale=(1, 1, 1), rot=(0, 0, 0), segments=8, rings=6):
        res = bmesh.ops.create_uvsphere(
            self.bm, u_segments=segments, v_segments=rings, radius=radius,
            matrix=_matrix(loc, rot, scale))
        self._assign(res["verts"], mat)

    def rock(self, radius, loc, mat, scale=(1, 1, 1), rot=(0, 0, 0)):
        """Icosfera de 1 subdivisión: piedra o cristal facetado."""
        res = bmesh.ops.create_icosphere(
            self.bm, subdivisions=1, radius=radius, matrix=_matrix(loc, rot, scale))
        self._assign(res["verts"], mat)

    def prism(self, points, depth, mat, axis="Y", offset=0.0):
        """
        Extruye un polígono 2D (puede ser cóncavo) a lo largo de un eje.
        axis='Y': puntos (x, z), de offset-depth/2 a offset+depth/2 en Y.
        axis='X': puntos (y, z), extruido en X.
        """
        def to3d(p, d):
            if axis == "Y":
                return Vector((p[0], d, p[1]))
            return Vector((d, p[0], p[1]))

        half = depth / 2.0
        front = [self.bm.verts.new(to3d(p, offset - half)) for p in points]
        back = [self.bm.verts.new(to3d(p, offset + half)) for p in points]
        faces = [self.bm.faces.new(front), self.bm.faces.new(list(reversed(back)))]
        n = len(points)
        for i in range(n):
            j = (i + 1) % n
            faces.append(self.bm.faces.new((front[i], front[j], back[j], back[i])))
        bmesh.ops.recalc_face_normals(self.bm, faces=faces)
        self._assign(front + back, mat)

    def build(self, name, origin=(0, 0, 0), parent=None):
        """Crea el objeto con el origen (pivote) en 'origin' (coordenadas del mundo)."""
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        bmesh.ops.translate(self.bm, vec=-Vector(origin), verts=self.bm.verts)
        mesh = bpy.data.meshes.new(name)
        self.bm.to_mesh(mesh)
        self.bm.free()
        for mat in self.materials:
            mesh.materials.append(mat)
        for poly in mesh.polygons:
            poly.use_smooth = False  # sombreado plano (estilo low-poly)

        obj = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(obj)
        _place(obj, origin, parent)
        return obj


# ---------------------------------------------------------------------------
# Exportación
# ---------------------------------------------------------------------------
def wants_preview():
    return "--preview" in sys.argv


def export(name):
    """Exporta Assets/Models/<name>.fbx, guarda BlenderModels/<name>.blend y, si se pide, la vista previa."""
    os.makedirs(FBX_DIR, exist_ok=True)
    os.makedirs(BLEND_DIR, exist_ok=True)
    fbx_path = os.path.join(FBX_DIR, name + ".fbx")
    blend_path = os.path.join(BLEND_DIR, name + ".blend")

    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=False,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    bpy.ops.wm.save_as_mainfile(filepath=blend_path, compress=True)
    print(f"[OK] {name}: {os.path.relpath(fbx_path, REPO_ROOT)}  +  {os.path.relpath(blend_path, REPO_ROOT)}")

    if wants_preview():
        render_preview(name)


def render_preview(name, size=420):
    """Render rápido (Cycles en CPU) en vista 3/4 frontal, para revisar el modelo sin abrir Blender."""
    scene = bpy.context.scene
    meshes = [o for o in scene.objects if o.type == "MESH"]
    corners = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    lo = Vector((min(c.x for c in corners), min(c.y for c in corners), min(c.z for c in corners)))
    hi = Vector((max(c.x for c in corners), max(c.y for c in corners), max(c.z for c in corners)))
    center = (lo + hi) / 2
    extent = max((hi - lo).length, 0.5)

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = extent * 1.05
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    direction = Vector((0.9, -1.4, 0.8)).normalized()  # frente (-Y), un poco a la derecha y arriba
    cam.location = center + direction * extent * 3
    cam.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam

    sun_data = bpy.data.lights.new("PreviewSun", "SUN")
    sun_data.energy = 3.5
    sun = bpy.data.objects.new("PreviewSun", sun_data)
    sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(30))
    scene.collection.objects.link(sun)

    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.58, 0.62, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
    scene.world = world

    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 24
    scene.cycles.use_denoising = False
    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.film_transparent = False
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)
    print(f"[OK] vista previa: docs/previews/{name}.png")
