import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import create_carrot_blockout_v001 as blockout


CARROT_DIR = SCRIPT_DIR.parent
EXPORT_DIR = CARROT_DIR / "exports"
SCREENSHOT_DIR = CARROT_DIR / "screenshots"
BLEND_PATH = SCRIPT_DIR / "carrot_normal_v001_model.blend"
FBX_PATH = EXPORT_DIR / "Crop_Carrot_Normal_v001_model.fbx"


def create_material(name, color, roughness=0.62, specular=0.35):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True

    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = specular

    return material


def body_surface_point(height_percent, angle_degrees, surface_offset=1.012):
    radius = blockout.MAX_RADIUS * blockout.radius_percent_at(height_percent) / 100.0
    angle = math.radians(angle_degrees)
    x = math.cos(angle) * radius * surface_offset
    y = math.sin(angle) * radius * blockout.SIDE_DEPTH_SCALE * surface_offset
    z = height_percent * blockout.BODY_HEIGHT
    return Vector((x, y, z))


def create_body_growth_mark(name, height_percent, center_angle, arc_degrees, vertical_drift, width, material):
    segment_count = 10
    vertices = []
    faces = []

    for segment in range(segment_count + 1):
        u = segment / segment_count
        angle = center_angle + (u - 0.5) * arc_degrees
        height = height_percent + (u - 0.5) * vertical_drift
        lower = body_surface_point(height - width, angle)
        upper = body_surface_point(height + width, angle)
        vertices.extend([tuple(lower), tuple(upper)])

    for segment in range(segment_count):
        lower_a = segment * 2
        upper_a = lower_a + 1
        lower_b = lower_a + 2
        upper_b = lower_a + 3
        faces.append((lower_a, lower_b, upper_b, upper_a))
        faces.append((upper_a, upper_b, lower_b, lower_a))

    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def create_subtle_growth_marks(material):
    marks = [
        ("CarrotGrowthMark_00", 0.29, -104.0, 18.0, 0.020, 0.0042),
        ("CarrotGrowthMark_01", 0.43, -77.0, 22.0, -0.018, 0.0038),
        ("CarrotGrowthMark_02", 0.58, -111.0, 16.0, 0.015, 0.0035),
        ("CarrotGrowthMark_03", 0.74, -86.0, 20.0, -0.012, 0.0032),
    ]

    for name, height, angle, arc, drift, width in marks:
        create_body_growth_mark(name, height, angle, arc, drift, width, material)


def create_leaf_stems(primary_material, secondary_material, root_material):
    stem_base_z = blockout.BODY_HEIGHT * blockout.PROFILE_MAX_T - 0.018
    stem_root_z = stem_base_z - 0.055

    blockout.add_uv_sphere(
        "LeafStem_RoundedRoot_Knot",
        (0.0, 0.0, stem_base_z + 0.005),
        (0.135, 0.125, 0.095),
        root_material,
        16,
        8,
    )

    stems = [
        ("LeafStem_Center", [(0.00, 0.00, stem_root_z), (0.00, 0.00, 2.66), (0.00, 0.00, 2.96)], 0.086, primary_material),
        ("LeafStem_Left", [(-0.03, 0.00, stem_root_z), (-0.06, 0.00, 2.61), (-0.24, 0.02, 2.83)], 0.083, secondary_material),
        ("LeafStem_Right", [(0.03, 0.00, stem_root_z), (0.06, 0.00, 2.61), (0.25, -0.02, 2.83)], 0.083, primary_material),
        ("LeafStem_Back", [(0.00, 0.03, stem_root_z), (0.02, 0.09, 2.58), (0.08, 0.24, 2.78)], 0.078, secondary_material),
    ]

    for name, points, radius, material in stems:
        blockout.create_tapered_stem(name, points, radius, material)


def configure_scene():
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_WORKBENCH"

    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("WorldFarm_Model_World")
    scene.world.color = (0.76, 0.78, 0.76)

    bpy.ops.object.camera_add(location=(0.0, -6.0, 1.62))
    camera = bpy.context.object
    camera.name = "Camera_Front"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 3.45
    blockout.look_at(camera, (0.0, 0.0, 1.55))
    scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(0.0, -3.6, 5.0))
    key = bpy.context.object
    key.name = "Key_Light_Model"
    key.data.energy = 520
    key.data.size = 4.4

    bpy.ops.object.light_add(type="POINT", location=(-2.4, 2.5, 2.9))
    fill = bpy.context.object
    fill.name = "Fill_Light_Model"
    fill.data.energy = 72


def render_view(name, location, target, ortho_scale=3.45):
    scene = bpy.context.scene
    camera = scene.camera
    camera.location = Vector(location)
    camera.data.ortho_scale = ortho_scale
    blockout.look_at(camera, target)
    scene.render.filepath = str(SCREENSHOT_DIR / name)
    bpy.ops.render.render(write_still=True)


def export_fbx():
    try:
        bpy.ops.preferences.addon_enable(module="io_scene_fbx")
    except Exception:
        pass

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        apply_unit_scale=True,
    )


def main():
    blockout.ensure_dirs()
    blockout.clear_scene()

    body_material = create_material("Mat_Carrot_Body_WarmOrange_v001", (0.93, 0.36, 0.08, 1.0), 0.68, 0.28)
    growth_material = create_material("Mat_Carrot_SubtleGrowthMarks_v001", (0.68, 0.22, 0.045, 1.0), 0.74, 0.18)
    leaf_primary = create_material("Mat_Carrot_LeafPrimary_v001", (0.15, 0.53, 0.22, 1.0), 0.62, 0.25)
    leaf_secondary = create_material("Mat_Carrot_LeafSecondary_v001", (0.09, 0.40, 0.18, 1.0), 0.68, 0.18)
    root_material = create_material("Mat_Carrot_LeafRoot_v001", (0.12, 0.45, 0.20, 1.0), 0.70, 0.18)

    blockout.create_carrot_body(body_material)
    create_subtle_growth_marks(growth_material)
    create_leaf_stems(leaf_primary, leaf_secondary, root_material)
    configure_scene()

    render_view("v001_model_front.png", (0.0, -6.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_model_side.png", (6.0, 0.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_model_three_quarter.png", (4.2, -5.2, 2.1), (0.0, 0.0, 1.55), 3.55)
    render_view("v001_model_top.png", (0.0, 0.0, 6.4), (0.0, 0.0, 1.55), 3.25)

    export_fbx()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"SCREENSHOTS={SCREENSHOT_DIR}")


if __name__ == "__main__":
    main()
