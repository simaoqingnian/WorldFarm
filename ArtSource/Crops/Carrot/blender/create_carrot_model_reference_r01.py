import math
from pathlib import Path

import bpy
from mathutils import Vector


SCRIPT_DIR = Path(__file__).resolve().parent
CARROT_DIR = SCRIPT_DIR.parent
EXPORT_DIR = CARROT_DIR / "exports"
SCREENSHOT_DIR = CARROT_DIR / "screenshots"

BLEND_PATH = SCRIPT_DIR / "carrot_normal_v001_reference_r01.blend"
FBX_PATH = EXPORT_DIR / "Crop_Carrot_Normal_v001_reference_r01.fbx"

BODY_HEIGHT = 2.36
RADIAL_SEGMENTS = 72
HEIGHT_SEGMENTS = 86


def ensure_dirs():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def clamp01(value):
    return max(0.0, min(1.0, value))


def smoothstep(value):
    value = clamp01(value)
    return value * value * (3.0 - 2.0 * value)


def lerp(a, b, t):
    return a + (b - a) * t


def profile_lerp(points, t):
    for index in range(len(points) - 1):
        t0, v0 = points[index]
        t1, v1 = points[index + 1]
        if t0 <= t <= t1:
            local_t = smoothstep((t - t0) / (t1 - t0))
            return lerp(v0, v1, local_t)
    return points[-1][1]


def make_material(name, color, roughness=0.55, specular=0.40, metallic=0.0):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True

    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = specular
        if "Coat Weight" in bsdf.inputs:
            bsdf.inputs["Coat Weight"].default_value = 0.12

    return material


def body_radius(t):
    return profile_lerp(
        [
            (0.00, 0.040),
            (0.055, 0.145),
            (0.22, 0.300),
            (0.50, 0.475),
            (0.76, 0.595),
            (0.90, 0.525),
            (0.965, 0.250),
            (1.00, 0.120),
        ],
        t,
    )


def body_center(t):
    bend_x = -0.030 + math.sin(t * math.pi) * 0.035 + smoothstep((t - 0.58) / 0.42) * 0.045
    return Vector((bend_x, 0.0, BODY_HEIGHT * t))


def body_squash_x(t):
    return lerp(0.92, 1.07, smoothstep(t))


def body_squash_y(t):
    return lerp(0.82, 0.95, smoothstep(t))


def create_reference_body(material):
    vertices = []
    faces = []
    uvs = []

    for y_index in range(HEIGHT_SEGMENTS + 1):
        t = y_index / HEIGHT_SEGMENTS
        center = body_center(t)
        radius = body_radius(t)
        squash_x = body_squash_x(t)
        squash_y = body_squash_y(t)

        for radial_index in range(RADIAL_SEGMENTS):
            angle = math.tau * radial_index / RADIAL_SEGMENTS
            x = math.cos(angle) * radius * squash_x
            y = math.sin(angle) * radius * squash_y
            vertices.append(tuple(center + Vector((x, y, 0.0))))
            uvs.append((radial_index / RADIAL_SEGMENTS, t))

    for y_index in range(HEIGHT_SEGMENTS):
        row = y_index * RADIAL_SEGMENTS
        next_row = (y_index + 1) * RADIAL_SEGMENTS
        for radial_index in range(RADIAL_SEGMENTS):
            next_radial = (radial_index + 1) % RADIAL_SEGMENTS
            faces.append((row + radial_index, row + next_radial, next_row + next_radial, next_row + radial_index))

    bottom_center_index = len(vertices)
    vertices.append(tuple(body_center(0.0) + Vector((0.0, 0.0, -0.012))))
    uvs.append((0.5, 0.0))

    top_row = HEIGHT_SEGMENTS * RADIAL_SEGMENTS
    for radial_index in range(RADIAL_SEGMENTS):
        next_radial = (radial_index + 1) % RADIAL_SEGMENTS
        faces.append((bottom_center_index, radial_index, next_radial))

    mesh = bpy.data.meshes.new("Mesh_Carrot_Normal_v001_ReferenceBody_r01")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mesh.uv_layers.new(name="UVMap")
    for index, uv in enumerate(uvs):
        if index < len(mesh.uv_layers["UVMap"].data):
            mesh.uv_layers["UVMap"].data[index].uv = uv

    body = bpy.data.objects.new("Crop_Carrot_Normal_v001_Body_Reference_r01", mesh)
    bpy.context.collection.objects.link(body)
    body.data.materials.append(material)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    bpy.ops.object.shade_smooth()
    modifier = body.modifiers.new("IconSmoothSubdivision", "SUBSURF")
    modifier.levels = 1
    modifier.render_levels = 1
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    body.select_set(False)
    return body


def frame_from_tangent(tangent):
    tangent = tangent.normalized()
    reference = Vector((0.0, 0.0, 1.0))
    if abs(tangent.dot(reference)) > 0.92:
        reference = Vector((1.0, 0.0, 0.0))

    normal = reference.cross(tangent).normalized()
    binormal = tangent.cross(normal).normalized()
    return normal, binormal


def bezier_point(points, t):
    if len(points) == 3:
        return (1.0 - t) ** 2 * points[0] + 2.0 * (1.0 - t) * t * points[1] + t ** 2 * points[2]
    return (
        (1.0 - t) ** 3 * points[0]
        + 3.0 * (1.0 - t) ** 2 * t * points[1]
        + 3.0 * (1.0 - t) * t ** 2 * points[2]
        + t ** 3 * points[3]
    )


def bezier_tangent(points, t):
    if len(points) == 3:
        return 2.0 * (1.0 - t) * (points[1] - points[0]) + 2.0 * t * (points[2] - points[1])
    return (
        3.0 * (1.0 - t) ** 2 * (points[1] - points[0])
        + 6.0 * (1.0 - t) * t * (points[2] - points[1])
        + 3.0 * t ** 2 * (points[3] - points[2])
    )


def create_tapered_stem(name, points, radius, material, tip_scale=0.78):
    radial_segments = 22
    ring_count = 34
    vertices = []
    faces = []

    for ring_index in range(ring_count):
        t = ring_index / (ring_count - 1)
        center = bezier_point(points, t)
        tangent = bezier_tangent(points, t)
        normal, binormal = frame_from_tangent(tangent)
        root_ease = lerp(0.78, 1.0, smoothstep(t / 0.16))
        tip_ease = lerp(1.0, tip_scale, smoothstep((t - 0.55) / 0.45))
        ring_radius = radius * root_ease * tip_ease

        for radial_index in range(radial_segments):
            angle = math.tau * radial_index / radial_segments
            offset = normal * math.cos(angle) * ring_radius + binormal * math.sin(angle) * ring_radius
            vertices.append(tuple(center + offset))

    for ring_index in range(ring_count - 1):
        row = ring_index * radial_segments
        next_row = (ring_index + 1) * radial_segments
        for radial_index in range(radial_segments):
            next_radial = (radial_index + 1) % radial_segments
            faces.append((row + radial_index, row + next_radial, next_row + next_radial, next_row + radial_index))

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


def add_uv_sphere(name, location, scale, material, segments=24, rings=12, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=1.0,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return obj


def create_leaf_cluster(leaf_material, leaf_light_material, leaf_dark_material):
    top = body_center(1.0)
    crown_z = BODY_HEIGHT + 0.045

    add_uv_sphere(
        "LeafStem_GlossyRootKnot_Reference_r01",
        (top.x + 0.01, -0.005, crown_z - 0.030),
        (0.275, 0.225, 0.122),
        leaf_dark_material,
        24,
        10,
    )

    stems = [
        (
            "LeafStem_CenterTall_Reference_r01",
            [Vector((top.x, -0.010, crown_z - 0.035)), Vector((top.x + 0.02, -0.030, 2.78)), Vector((top.x + 0.08, -0.020, 3.02))],
            0.075,
            leaf_material,
        ),
        (
            "LeafStem_LeftTall_Reference_r01",
            [Vector((top.x - 0.040, -0.010, crown_z - 0.030)), Vector((top.x - 0.18, -0.060, 2.72)), Vector((top.x - 0.36, -0.095, 2.88))],
            0.072,
            leaf_light_material,
        ),
        (
            "LeafStem_RightWide_Reference_r01",
            [Vector((top.x + 0.050, -0.006, crown_z - 0.025)), Vector((top.x + 0.23, -0.045, 2.65)), Vector((top.x + 0.47, -0.070, 2.72))],
            0.073,
            leaf_material,
        ),
        (
            "LeafStem_BackRight_Reference_r01",
            [Vector((top.x + 0.015, 0.035, crown_z - 0.030)), Vector((top.x + 0.18, 0.070, 2.60)), Vector((top.x + 0.36, 0.110, 2.63))],
            0.069,
            leaf_dark_material,
        ),
        (
            "LeafStem_BackTall_Reference_r01",
            [Vector((top.x - 0.015, 0.025, crown_z - 0.030)), Vector((top.x - 0.07, 0.080, 2.63)), Vector((top.x - 0.12, 0.120, 2.85))],
            0.068,
            leaf_material,
        ),
    ]

    for name, points, radius, material in stems:
        stem = create_tapered_stem(name, points, radius, material)
        end = points[-1]
        add_uv_sphere(f"{name}_RoundedEnd", tuple(end), (radius * 0.76, radius * 0.76, radius * 0.76), material, 18, 8)
        stem.select_set(False)


def front_surface_y(t, x):
    center = body_center(t)
    radius_x = max(0.001, body_radius(t) * body_squash_x(t))
    radius_y = body_radius(t) * body_squash_y(t)
    normalized_x = clamp01(abs((x - center.x) / radius_x))
    return -math.sqrt(max(0.0, 1.0 - normalized_x * normalized_x)) * radius_y - 0.012


def create_surface_ribbon(name, center_t, center_x, length, width, angle_degrees, material, y_offset=0.0, curve=0.020):
    segments = 12
    vertices = []
    faces = []
    angle = math.radians(angle_degrees)
    tangent = Vector((math.cos(angle), math.sin(angle)))
    normal = Vector((-tangent.y, tangent.x))

    for index in range(segments + 1):
        s = index / segments - 0.5
        wave = math.sin((index / segments) * math.pi) * curve
        point_2d = Vector((center_x, center_t * BODY_HEIGHT)) + tangent * (s * length) + normal * wave
        side = normal * (width * math.sin((index / segments) * math.pi) * 0.5 + width * 0.20)
        for sign in (-1.0, 1.0):
            x = point_2d.x + side.x * sign
            z = point_2d.y + side.y * sign
            t = clamp01(z / BODY_HEIGHT)
            y = front_surface_y(t, x) + y_offset
            vertices.append((x, y, z))

    for index in range(segments):
        left0 = index * 2
        right0 = left0 + 1
        left1 = (index + 1) * 2
        right1 = left1 + 1
        faces.append((left0, left1, right0))
        faces.append((right0, left1, right1))

    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def create_reference_surface_marks(groove_material, groove_light_material, highlight_material):
    create_surface_ribbon("CarrotSoftGroove_Upper_Reference_r01", 0.650, -0.225, 0.390, 0.030, -24.0, groove_material, -0.002, 0.010)
    create_surface_ribbon("CarrotSoftGroove_UpperLight_Reference_r01", 0.654, -0.230, 0.260, 0.007, -24.0, groove_light_material, -0.006, 0.004)
    create_surface_ribbon("CarrotSoftGroove_Lower_Reference_r01", 0.485, -0.292, 0.330, 0.027, -22.0, groove_material, -0.002, 0.008)
    create_surface_ribbon("CarrotSoftGroove_LowerLight_Reference_r01", 0.489, -0.296, 0.220, 0.007, -22.0, groove_light_material, -0.006, 0.003)


def configure_scene():
    scene = bpy.context.scene
    available_engines = {item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items}
    if "BLENDER_EEVEE_NEXT" in available_engines:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    elif "BLENDER_EEVEE" in available_engines:
        scene.render.engine = "BLENDER_EEVEE"
    else:
        scene.render.engine = "BLENDER_WORKBENCH"

    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("WorldFarm_ReferenceCarrot_World_r01")
    scene.world.color = (0.74, 0.76, 0.76)
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    bpy.ops.object.camera_add(location=(0.0, -6.2, 1.58))
    camera = bpy.context.object
    camera.name = "Camera_ReferencePreview"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 3.35
    scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-2.0, -4.0, 4.8))
    key = bpy.context.object
    key.name = "Reference_Key_Light"
    key.data.energy = 560
    key.data.size = 4.6

    bpy.ops.object.light_add(type="POINT", location=(2.4, -2.2, 2.8))
    fill = bpy.context.object
    fill.name = "Reference_Fill_Light"
    fill.data.energy = 120

    bpy.ops.object.light_add(type="POINT", location=(-1.8, -3.4, 2.7))
    glint = bpy.context.object
    glint.name = "Reference_Gloss_Glint"
    glint.data.energy = 175
    glint.data.shadow_soft_size = 0.55


def look_at(obj, target, roll_degrees=0.0):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    if roll_degrees:
        obj.rotation_euler.rotate_axis("Z", math.radians(roll_degrees))


def render_view(name, location, target, ortho_scale=3.35, roll_degrees=0.0):
    scene = bpy.context.scene
    camera = scene.camera
    camera.location = Vector(location)
    camera.data.ortho_scale = ortho_scale
    look_at(camera, target, roll_degrees)
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
        path_mode="RELATIVE",
    )


def main():
    ensure_dirs()
    clear_scene()

    body_material = make_material("Mat_Carrot_Reference_GlossyBody_r01", (1.00, 0.39, 0.030, 1.0), 0.30, 0.66)
    groove_material = make_material("Mat_Carrot_Reference_SoftGroove_r01", (0.86, 0.23, 0.012, 1.0), 0.42, 0.24)
    groove_light_material = make_material("Mat_Carrot_Reference_GrooveHighlight_r01", (1.00, 0.67, 0.160, 1.0), 0.36, 0.44)
    highlight_material = make_material("Mat_Carrot_Reference_PaintedHighlight_r01", (1.00, 0.76, 0.26, 1.0), 0.26, 0.72)
    leaf_material = make_material("Mat_Carrot_Reference_LeafGloss_r01", (0.25, 0.70, 0.095, 1.0), 0.34, 0.56)
    leaf_light_material = make_material("Mat_Carrot_Reference_LeafLight_r01", (0.43, 0.86, 0.14, 1.0), 0.32, 0.62)
    leaf_dark_material = make_material("Mat_Carrot_Reference_LeafDark_r01", (0.10, 0.42, 0.08, 1.0), 0.40, 0.44)

    create_reference_body(body_material)
    create_reference_surface_marks(groove_material, groove_light_material, highlight_material)
    create_leaf_cluster(leaf_material, leaf_light_material, leaf_dark_material)
    configure_scene()

    render_view("v001_reference_r01_front.png", (0.0, -6.2, 1.58), (0.02, 0.0, 1.50), 3.35)
    render_view("v001_reference_r01_side.png", (6.0, 0.0, 1.60), (0.03, 0.0, 1.48), 3.35)
    render_view("v001_reference_r01_three_quarter.png", (4.5, -5.2, 2.05), (0.03, 0.0, 1.48), 3.45)
    render_view("v001_reference_r01_icon_angle.png", (2.9, -6.4, 1.95), (0.02, 0.0, 1.48), 3.18, -24.0)

    export_fbx()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"SCREENSHOTS={SCREENSHOT_DIR}")


if __name__ == "__main__":
    main()
