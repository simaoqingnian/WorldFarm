import math
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


def bezier_point(points, t):
    vectors = [Vector(point) for point in points]
    while len(vectors) > 1:
        vectors = [vectors[index].lerp(vectors[index + 1], t) for index in range(len(vectors) - 1)]
    return vectors[0]


def bezier_tangent(points, t):
    delta = 0.001
    t0 = max(0.0, t - delta)
    t1 = min(1.0, t + delta)
    tangent = bezier_point(points, t1) - bezier_point(points, t0)
    if tangent.length == 0.0:
        return Vector((0.0, 0.0, 1.0))
    return tangent.normalized()


def stem_frame(tangent):
    return Vector((1.0, 0.0, 0.0)), Vector((0.0, 1.0, 0.0))


def stem_radius_scale(t):
    if t < 0.14:
        return lerp(0.72, 1.0, smoothstep(t / 0.14))
    return 1.0


def create_tapered_stem(name, points, radius, material):
    radial_segments = 18
    ring_count = 30
    vertices = []
    faces = []

    for ring_index in range(ring_count):
        # The stem ends with a plain blunt face. Avoid separate ball caps,
        # tapered nipples, and single-point tips.
        t = ring_index / (ring_count - 1)
        center = bezier_point(points, t)
        tangent = bezier_tangent(points, t)
        normal, binormal = stem_frame(tangent)
        ring_radius = radius * stem_radius_scale(t)

        for radial_index in range(radial_segments):
            angle = 2.0 * math.pi * radial_index / radial_segments
            offset = normal * math.cos(angle) * ring_radius + binormal * math.sin(angle) * ring_radius
            vertices.append(tuple(center + offset))

    for ring_index in range(ring_count - 1):
        row = ring_index * radial_segments
        next_row = (ring_index + 1) * radial_segments
        for radial_index in range(radial_segments):
            next_radial = (radial_index + 1) % radial_segments
            faces.append((
                row + radial_index,
                row + next_radial,
                next_row + next_radial,
                next_row + radial_index,
            ))

    faces.append(tuple(reversed(range(radial_segments))))

    last_row = (ring_count - 1) * radial_segments
    faces.append(tuple(last_row + radial_index for radial_index in range(radial_segments)))

    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)
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


def create_leaf_stems(primary_material):
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
        created.append(create_tapered_stem(name, points, radius, primary_material))

    return created


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

    create_carrot_body(body_material)
    create_leaf_stems(leaf_material)
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
