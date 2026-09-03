import math
from pathlib import Path

import bpy
from mathutils import Vector


SCRIPT_DIR = Path(__file__).resolve().parent
CARROT_DIR = SCRIPT_DIR.parent
EXPORT_DIR = CARROT_DIR / "exports"
SCREENSHOT_DIR = CARROT_DIR / "screenshots"

BLEND_PATH = SCRIPT_DIR / "carrot_normal_v001_reference_r08.blend"
FBX_PATH = EXPORT_DIR / "Crop_Carrot_Normal_v001_reference_r08.fbx"

BODY_HEIGHT = 2.02
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
            bsdf.inputs["Coat Weight"].default_value = 0.08

    return material


def body_radius(t):
    tip_radius = 0.026
    max_radius = 0.560
    lower_growth = t ** 0.64
    top_taper = 1.0 - 0.66 * smoothstep((t - 0.835) / 0.165)
    return tip_radius + max_radius * lower_growth * top_taper


def body_center(t):
    bend_x = -0.010 + math.sin(t * math.pi) * 0.008 + smoothstep((t - 0.66) / 0.34) * 0.014
    return Vector((bend_x, 0.0, BODY_HEIGHT * t))


def body_squash_x(t):
    return lerp(0.98, 1.03, smoothstep(t))


def body_squash_y(t):
    return lerp(0.88, 0.94, smoothstep(t))


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

    top_row = HEIGHT_SEGMENTS * RADIAL_SEGMENTS
    top_base = body_center(1.0)
    top_radius = body_radius(1.0)
    last_row = top_row
    for z_offset, radius_scale in ((0.010, 0.80), (0.023, 0.46), (0.033, 0.18)):
        row_start = len(vertices)
        center = top_base + Vector((0.0, 0.0, z_offset))
        radius = top_radius * radius_scale
        for radial_index in range(RADIAL_SEGMENTS):
            angle = math.tau * radial_index / RADIAL_SEGMENTS
            x = math.cos(angle) * radius * body_squash_x(1.0)
            y = math.sin(angle) * radius * body_squash_y(1.0)
            vertices.append(tuple(center + Vector((x, y, 0.0))))
            uvs.append((radial_index / RADIAL_SEGMENTS, 1.0))

        for radial_index in range(RADIAL_SEGMENTS):
            next_radial = (radial_index + 1) % RADIAL_SEGMENTS
            faces.append((last_row + radial_index, last_row + next_radial, row_start + next_radial, row_start + radial_index))
        last_row = row_start

    top_center_index = len(vertices)
    vertices.append(tuple(top_base + Vector((0.0, 0.0, 0.039))))
    uvs.append((0.5, 1.0))

    for radial_index in range(RADIAL_SEGMENTS):
        next_radial = (radial_index + 1) % RADIAL_SEGMENTS
        faces.append((top_center_index, last_row + next_radial, last_row + radial_index))

    mesh = bpy.data.meshes.new("Mesh_Carrot_Normal_v001_ReferenceBody_r08")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    mesh.uv_layers.new(name="UVMap")
    for index, uv in enumerate(uvs):
        if index < len(mesh.uv_layers["UVMap"].data):
            mesh.uv_layers["UVMap"].data[index].uv = uv

    body = bpy.data.objects.new("Crop_Carrot_Normal_v001_Body_Reference_r08", mesh)
    bpy.context.collection.objects.link(body)
    body.data.materials.append(material)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    bpy.ops.object.shade_smooth()
    modifier = body.modifiers.new("IconSmoothSubdivision", "SUBSURF")
    modifier.levels = 2
    modifier.render_levels = 2
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


def create_tapered_stem(name, points, radius, material, tip_scale=0.34):
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
    crown_z = BODY_HEIGHT + 0.030

    add_uv_sphere(
        "LeafStem_CoreBase_Reference_r08",
        (top.x + 0.006, -0.012, crown_z - 0.004),
        (0.104, 0.082, 0.056),
        leaf_dark_material,
        20,
        8,
    )

    stems = [
        (
            "LeafStem_LeftBack_Reference_r08",
            [Vector((top.x - 0.018, -0.014, crown_z - 0.020)), Vector((top.x - 0.108, -0.040, 2.27)), Vector((top.x - 0.248, -0.058, 2.40))],
            0.052,
            leaf_material,
        ),
        (
            "LeafStem_CenterTall_Reference_r08",
            [Vector((top.x + 0.002, -0.014, crown_z - 0.020)), Vector((top.x + 0.046, -0.040, 2.35)), Vector((top.x + 0.110, -0.034, 2.55))],
            0.053,
            leaf_light_material,
        ),
        (
            "LeafStem_RightTall_Reference_r08",
            [Vector((top.x + 0.024, -0.006, crown_z - 0.020)), Vector((top.x + 0.170, -0.034, 2.28)), Vector((top.x + 0.330, -0.050, 2.41))],
            0.052,
            leaf_material,
        ),
        (
            "LeafStem_RightLow_Reference_r08",
            [Vector((top.x + 0.022, 0.028, crown_z - 0.020)), Vector((top.x + 0.158, 0.070, 2.19)), Vector((top.x + 0.340, 0.096, 2.25))],
            0.049,
            leaf_dark_material,
        ),
    ]

    for name, points, radius, material in stems:
        stem = create_tapered_stem(name, points, radius, material)
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
    create_surface_ribbon("CarrotSoftGroove_Upper_Reference_r08", 0.610, -0.185, 0.235, 0.014, -23.0, groove_material, -0.002, 0.002)
    create_surface_ribbon("CarrotSoftGroove_UpperLight_Reference_r08", 0.615, -0.192, 0.155, 0.003, -23.0, groove_light_material, -0.006, 0.001)
    create_surface_ribbon("CarrotSoftGroove_Lower_Reference_r08", 0.450, -0.225, 0.215, 0.013, -21.0, groove_material, -0.002, 0.002)
    create_surface_ribbon("CarrotSoftGroove_LowerLight_Reference_r08", 0.455, -0.232, 0.145, 0.003, -21.0, groove_light_material, -0.006, 0.001)


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
    scene.world = bpy.data.worlds.new("WorldFarm_ReferenceCarrot_World_r08")
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
    key.data.energy = 520
    key.data.size = 4.9

    bpy.ops.object.light_add(type="POINT", location=(2.4, -2.2, 2.8))
    fill = bpy.context.object
    fill.name = "Reference_Fill_Light"
    fill.data.energy = 145

    bpy.ops.object.light_add(type="POINT", location=(-1.8, -3.4, 2.7))
    glint = bpy.context.object
    glint.name = "Reference_Gloss_Glint"
    glint.data.energy = 120
    glint.data.shadow_soft_size = 0.75


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

    body_material = make_material("Mat_Carrot_Reference_GlossyBody_r08", (1.00, 0.41, 0.030, 1.0), 0.40, 0.50)
    groove_material = make_material("Mat_Carrot_Reference_SoftGroove_r08", (0.90, 0.27, 0.015, 1.0), 0.50, 0.16)
    groove_light_material = make_material("Mat_Carrot_Reference_GrooveHighlight_r08", (1.00, 0.67, 0.150, 1.0), 0.44, 0.26)
    highlight_material = make_material("Mat_Carrot_Reference_PaintedHighlight_r08", (1.00, 0.72, 0.22, 1.0), 0.34, 0.46)
    leaf_material = make_material("Mat_Carrot_Reference_LeafGloss_r08", (0.30, 0.72, 0.105, 1.0), 0.40, 0.44)
    leaf_light_material = make_material("Mat_Carrot_Reference_LeafLight_r08", (0.54, 0.90, 0.18, 1.0), 0.38, 0.48)
    leaf_dark_material = make_material("Mat_Carrot_Reference_LeafDark_r08", (0.11, 0.43, 0.08, 1.0), 0.46, 0.34)

    create_reference_body(body_material)
    create_reference_surface_marks(groove_material, groove_light_material, highlight_material)
    create_leaf_cluster(leaf_material, leaf_light_material, leaf_dark_material)
    configure_scene()

    render_view("v001_reference_r08_front.png", (0.0, -6.2, 1.34), (0.02, 0.0, 1.22), 2.72)
    render_view("v001_reference_r08_side.png", (6.0, 0.0, 1.36), (0.03, 0.0, 1.22), 2.72)
    render_view("v001_reference_r08_three_quarter.png", (4.5, -5.2, 1.72), (0.03, 0.0, 1.22), 2.82)
    render_view("v001_reference_r08_icon_angle.png", (-2.9, -6.4, 1.66), (0.02, 0.0, 1.22), 2.55, 31.0)

    export_fbx()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"SCREENSHOTS={SCREENSHOT_DIR}")


if __name__ == "__main__":
    main()
