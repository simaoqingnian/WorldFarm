import math
import os
from pathlib import Path

import bpy
from mathutils import Vector


SCRIPT_DIR = Path(__file__).resolve().parent
CARROT_DIR = SCRIPT_DIR.parent
EXPORT_DIR = CARROT_DIR / "exports"
SCREENSHOT_DIR = CARROT_DIR / "screenshots"
BLEND_PATH = SCRIPT_DIR / "carrot_normal_v001_blockout.blend"
FBX_PATH = EXPORT_DIR / "Crop_Carrot_Normal_v001_blockout.fbx"

BODY_HEIGHT = 2.20
PROFILE_MAX_T = 1.075
MAX_RADIUS = 0.50
SIDE_DEPTH_SCALE = 1.05
RADIAL_SEGMENTS = 56
HEIGHT_SEGMENTS = 80


def ensure_dirs():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, roughness=0.72):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
    return material


def smoothstep(value):
    value = max(0.0, min(1.0, value))
    return value * value * (3.0 - 2.0 * value)


def lerp(a, b, t):
    return a + (b - a) * t


def radius_percent_at(height_percent):
    # Keep the approved rounded bottom, then use one continuous monotonic curve
    # up to the widest shoulder. This avoids segmented "wave" changes in profile.
    if height_percent <= 0.055:
        t = smoothstep(height_percent / 0.055)
        return lerp(8.0, 26.0, t)

    if height_percent <= 0.90:
        profile_points = [
            (0.055, 26.0, 140.0),
            (0.18, 42.0, 105.0),
            (0.42, 62.0, 80.0),
            (0.68, 82.0, 72.0),
            (0.90, 100.0, 0.0),
        ]

        for index in range(len(profile_points) - 1):
            h0, r0, m0 = profile_points[index]
            h1, r1, m1 = profile_points[index + 1]
            if h0 <= height_percent <= h1:
                t = (height_percent - h0) / (h1 - h0)
                h00 = 2.0 * (t ** 3) - 3.0 * (t ** 2) + 1.0
                h10 = (t ** 3) - 2.0 * (t ** 2) + t
                h01 = -2.0 * (t ** 3) + 3.0 * (t ** 2)
                h11 = (t ** 3) - (t ** 2)
                return h00 * r0 + h10 * (h1 - h0) * m0 + h01 * r1 + h11 * (h1 - h0) * m1

    t = (height_percent - 0.90) / (PROFILE_MAX_T - 0.90)
    t = max(0.0, min(1.0, t))
    return 16.0 + 84.0 * (1.0 - (t ** 2.2))


def create_carrot_body(material):
    vertices = []
    faces = []

    for y_index in range(HEIGHT_SEGMENTS + 1):
        t = (y_index / HEIGHT_SEGMENTS) * PROFILE_MAX_T
        z = t * BODY_HEIGHT
        radius = MAX_RADIUS * radius_percent_at(t) / 100.0

        for radial_index in range(RADIAL_SEGMENTS):
            angle = 2.0 * math.pi * radial_index / RADIAL_SEGMENTS
            x = math.cos(angle) * radius
            y = math.sin(angle) * radius * SIDE_DEPTH_SCALE
            vertices.append((x, y, z))

    for y_index in range(HEIGHT_SEGMENTS):
        row = y_index * RADIAL_SEGMENTS
        next_row = (y_index + 1) * RADIAL_SEGMENTS
        for radial_index in range(RADIAL_SEGMENTS):
            next_radial = (radial_index + 1) % RADIAL_SEGMENTS
            faces.append((
                row + radial_index,
                row + next_radial,
                next_row + next_radial,
                next_row + radial_index,
            ))

    bottom_center_index = len(vertices)
    vertices.append((0.0, 0.0, -0.006))
    top_center_index = len(vertices)
    vertices.append((0.0, 0.0, BODY_HEIGHT * PROFILE_MAX_T + 0.006))

    for radial_index in range(RADIAL_SEGMENTS):
        next_radial = (radial_index + 1) % RADIAL_SEGMENTS
        faces.append((bottom_center_index, radial_index, next_radial))

        top_row = HEIGHT_SEGMENTS * RADIAL_SEGMENTS
        faces.append((top_center_index, top_row + next_radial, top_row + radial_index))

    mesh = bpy.data.meshes.new("Mesh_Carrot_Normal_v001_Blockout_Body")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    body = bpy.data.objects.new("Crop_Carrot_Normal_v001_Body_Blockout", mesh)
    bpy.context.collection.objects.link(body)
    body.data.materials.append(material)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    bpy.ops.object.shade_smooth()
    body.select_set(False)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    modifier = body.modifiers.new("BlockoutSmoothSubdivision", "SUBSURF")
    modifier.levels = 1
    modifier.render_levels = 1
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    body.select_set(False)
    return body


def create_bezier_stem(name, points, radius, material):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 18
    curve.bevel_depth = radius
    curve.bevel_resolution = 5
    curve.use_fill_caps = True

    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, coordinate in zip(spline.bezier_points, points):
        point.co = Vector(coordinate)
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"

    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def add_uv_sphere(name, location, scale, material, segments=16, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=1.0,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return obj


def create_leaf_stems(primary_material, highlight_material):
    stem_base_z = BODY_HEIGHT * PROFILE_MAX_T - 0.018
    stem_root_z = stem_base_z - 0.055

    add_uv_sphere(
        "LeafStem_RoundedRoot_Knot",
        (0.0, 0.0, stem_base_z + 0.005),
        (0.135, 0.125, 0.095),
        primary_material,
        16,
        8,
    )

    stems = [
        ("LeafStem_Center", [(0.00, 0.00, stem_root_z), (0.00, 0.00, 2.66), (0.00, 0.00, 2.96)], 0.086),
        ("LeafStem_Left", [(-0.03, 0.00, stem_root_z), (-0.06, 0.00, 2.61), (-0.24, 0.02, 2.83)], 0.083),
        ("LeafStem_Right", [(0.03, 0.00, stem_root_z), (0.06, 0.00, 2.61), (0.25, -0.02, 2.83)], 0.083),
        ("LeafStem_Back", [(0.00, 0.03, stem_root_z), (0.02, 0.09, 2.58), (0.08, 0.24, 2.78)], 0.078),
    ]

    created = []
    for name, points, radius in stems:
        created.append(create_bezier_stem(name, points, radius, primary_material))
        tip = Vector(points[-1])
        add_uv_sphere(f"{name}_RoundedTip", tip, (radius * 1.18, radius * 1.18, radius * 1.18), primary_material, 16, 8)

    return created


def convert_curves_to_mesh():
    bpy.ops.object.select_all(action="DESELECT")
    curve_objects = [obj for obj in bpy.context.scene.objects if obj.type == "CURVE"]
    if not curve_objects:
        return

    for obj in curve_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = curve_objects[0]
    bpy.ops.object.convert(target="MESH")

    for obj in bpy.context.selected_objects:
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_smooth()


def create_lights():
    bpy.ops.object.light_add(type="AREA", location=(0.0, -3.6, 5.0))
    key = bpy.context.object
    key.name = "Key_Light_Blockout"
    key.data.energy = 460
    key.data.size = 4.2

    bpy.ops.object.light_add(type="POINT", location=(-2.5, 2.6, 2.8))
    fill = bpy.context.object
    fill.name = "Fill_Light_Blockout"
    fill.data.energy = 70


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def configure_scene():
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scene.render.engine = "BLENDER_WORKBENCH"

    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("WorldFarm_Blockout_World")
    scene.world.color = (0.78, 0.78, 0.78)

    bpy.ops.object.camera_add(location=(0.0, -6.0, 1.62))
    camera = bpy.context.object
    camera.name = "Camera_Front"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 3.45
    look_at(camera, (0.0, 0.0, 1.55))
    scene.camera = camera

    create_lights()


def render_view(name, location, target, ortho_scale=3.45):
    scene = bpy.context.scene
    camera = scene.camera
    camera.location = Vector(location)
    camera.data.ortho_scale = ortho_scale
    look_at(camera, target)
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
    ensure_dirs()
    clear_scene()

    body_material = make_material("Mat_Blockout_Carrot_Body_ReviewOnly", (0.93, 0.40, 0.10, 1.0), 0.8)
    leaf_material = make_material("Mat_Blockout_Cartoon_LeafStems_ReviewOnly", (0.16, 0.58, 0.23, 1.0), 0.78)
    leaf_highlight_material = make_material("Mat_Blockout_LeafStem_Tips_ReviewOnly", (0.47, 0.78, 0.34, 1.0), 0.75)

    create_carrot_body(body_material)
    create_leaf_stems(leaf_material, leaf_highlight_material)
    convert_curves_to_mesh()
    configure_scene()

    render_view("v001_blockout_front.png", (0.0, -6.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_blockout_side.png", (6.0, 0.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_blockout_three_quarter.png", (4.2, -5.2, 2.1), (0.0, 0.0, 1.55), 3.55)
    render_view("v001_blockout_top.png", (0.0, 0.0, 6.4), (0.0, 0.0, 1.55), 3.25)

    export_fbx()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"SCREENSHOTS={SCREENSHOT_DIR}")


if __name__ == "__main__":
    main()
