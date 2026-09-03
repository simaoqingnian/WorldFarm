import math
import random
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
TEXTURE_DIR = CARROT_DIR / "textures"
BLEND_PATH = SCRIPT_DIR / "carrot_normal_v001_model_r03.blend"
FBX_PATH = EXPORT_DIR / "Crop_Carrot_Normal_v001_model_r03.fbx"
BODY_TEXTURE_PATH = TEXTURE_DIR / "Carrot_Skin_Base_r03.png"
TOP_TEXTURE_PATH = TEXTURE_DIR / "Carrot_Top_Crown_r03.png"
LEAF_TEXTURE_PATH = TEXTURE_DIR / "Carrot_Leaf_Stem_r03.png"


def ensure_dirs():
    EXPORT_DIR.mkdir(parents=True, exist_ok=True)
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)
    TEXTURE_DIR.mkdir(parents=True, exist_ok=True)


def clamp01(value):
    return max(0.0, min(1.0, value))


def mix_color(a, b, factor):
    factor = clamp01(factor)
    return (
        a[0] + (b[0] - a[0]) * factor,
        a[1] + (b[1] - a[1]) * factor,
        a[2] + (b[2] - a[2]) * factor,
        a[3] + (b[3] - a[3]) * factor,
    )


def periodic_distance(a, b):
    distance = abs(a - b)
    return min(distance, 1.0 - distance)


def signed_periodic_delta(a, b):
    delta = a - b
    if delta > 0.5:
        delta -= 1.0
    elif delta < -0.5:
        delta += 1.0
    return delta


def write_image(path, name, width, height, pixels):
    image = bpy.data.images.new(name, width=width, height=height, alpha=True, float_buffer=False)
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    return image


def get_pixel(pixels, width, x, y):
    index = (y * width + x) * 4
    return (
        pixels[index],
        pixels[index + 1],
        pixels[index + 2],
        pixels[index + 3],
    )


def set_pixel(pixels, width, x, y, color):
    index = (y * width + x) * 4
    pixels[index] = clamp01(color[0])
    pixels[index + 1] = clamp01(color[1])
    pixels[index + 2] = clamp01(color[2])
    pixels[index + 3] = clamp01(color[3])


def add_soft_stroke(pixels, width, height, center_u, center_v, length_u, thickness_v, angle_tilt, color, strength, rng):
    center_x = int(center_u * width)
    center_y = int(center_v * height)
    radius_x = max(2, int(length_u * width * 0.55))
    radius_y = max(2, int(thickness_v * height * 4.0))
    wave_phase = rng.random() * math.tau
    wave_freq = rng.uniform(1.1, 2.7)

    for dx in range(-radius_x, radius_x + 1):
        u_delta = dx / width
        if abs(u_delta) > length_u * 0.5:
            continue

        along = abs(u_delta) / (length_u * 0.5)
        center_curve = center_v + angle_tilt * u_delta
        center_curve += math.sin(along * math.tau * wave_freq + wave_phase) * thickness_v * 0.25
        gap = 0.82 + 0.18 * math.sin((u_delta / max(length_u, 0.001)) * math.tau * 5.0 + wave_phase)

        x = (center_x + dx) % width
        y_mid = int(center_curve * height)
        for y in range(max(0, y_mid - radius_y), min(height, y_mid + radius_y + 1)):
            v_delta = abs((y + 0.5) / height - center_curve)
            edge = 1.0 - clamp01(v_delta / max(thickness_v, 0.0001))
            end_fade = 1.0 - clamp01((along - 0.70) / 0.30)
            alpha = (edge ** 1.7) * end_fade * gap * strength
            if alpha <= 0.0:
                continue

            current = get_pixel(pixels, width, x, y)
            set_pixel(pixels, width, x, y, mix_color(current, color, alpha))


def create_body_texture():
    width = 1024
    height = 1024
    rng = random.Random(3103)
    pixels = [0.0] * (width * height * 4)
    fibers = []

    for _ in range(22):
        fibers.append(
            (
                rng.random(),
                rng.uniform(0.0075, 0.0180),
                rng.uniform(-0.018, 0.014),
                rng.uniform(0.7, 2.2),
                rng.random() * math.tau,
            )
        )

    base = (0.94, 0.43, 0.085, 1.0)
    warm = (1.00, 0.60, 0.170, 1.0)
    dark = (0.57, 0.21, 0.045, 1.0)

    for y in range(height):
        v = y / (height - 1)
        top_warmth = 0.026 * (v ** 1.4)
        bottom_shadow = -0.024 * ((1.0 - v) ** 2.2)

        for x in range(width):
            u = x / width
            low_wave = 0.006 * math.sin(math.tau * (u * 3.0 + math.sin(v * math.tau * 1.15) * 0.025))
            low_wave += 0.004 * math.sin(math.tau * (u * 7.0 + v * 0.45))
            fine_noise = 0.0035 * math.sin(math.tau * (u * 23.0 + v * 8.0))
            fine_noise += 0.0020 * math.sin(math.tau * (u * 53.0 - v * 15.0))

            fiber_value = 0.0
            for center, width_u, amplitude, freq, phase in fibers:
                shifted_center = (center + math.sin(v * math.tau * freq + phase) * 0.003) % 1.0
                distance = periodic_distance(u, shifted_center)
                fiber_value += amplitude * math.exp(-0.5 * (distance / width_u) ** 2)

            tone = top_warmth + bottom_shadow + low_wave + fine_noise + fiber_value
            color = (
                base[0] + tone,
                base[1] + tone * 0.72,
                base[2] + tone * 0.36,
                1.0,
            )

            if fiber_value > 0:
                color = mix_color(color, warm, clamp01(fiber_value * 1.3))
            elif fiber_value < -0.004:
                color = mix_color(color, dark, clamp01(abs(fiber_value) * 1.8))

            set_pixel(pixels, width, x, y, color)

    for _ in range(38):
        center_u = rng.random()
        center_v = rng.uniform(0.12, 0.93)
        length_u = rng.uniform(0.030, 0.105) * (1.0 - 0.18 * center_v)
        thickness_v = rng.uniform(0.0048, 0.0092)
        angle_tilt = rng.uniform(-0.055, 0.055)
        scar_color = rng.choice(
            [
                (0.58, 0.23, 0.050, 1.0),
                (0.68, 0.29, 0.065, 1.0),
                (0.82, 0.39, 0.095, 1.0),
            ]
        )
        strength = rng.uniform(0.14, 0.30)
        add_soft_stroke(pixels, width, height, center_u, center_v, length_u, thickness_v, angle_tilt, scar_color, strength, rng)

    for _ in range(420):
        x = rng.randrange(width)
        y = rng.randrange(height)
        current = get_pixel(pixels, width, x, y)
        speckle = rng.choice([(0.57, 0.20, 0.040, 1.0), (1.00, 0.58, 0.170, 1.0)])
        set_pixel(pixels, width, x, y, mix_color(current, speckle, rng.uniform(0.025, 0.080)))

    return write_image(BODY_TEXTURE_PATH, "Carrot_Skin_Base_r03", width, height, pixels)


def create_top_texture():
    size = 512
    rng = random.Random(3104)
    pixels = [0.0] * (size * size * 4)
    base = (0.94, 0.43, 0.085, 1.0)
    warm = (1.00, 0.57, 0.160, 1.0)
    dark = (0.56, 0.21, 0.045, 1.0)
    crack_angles = [(rng.random() * math.tau, rng.uniform(0.16, 0.84), rng.uniform(0.32, 0.88)) for _ in range(14)]

    for y in range(size):
        v = (y + 0.5) / size
        dy = v * 2.0 - 1.0
        for x in range(size):
            u = (x + 0.5) / size
            dx = u * 2.0 - 1.0
            radius = math.sqrt(dx * dx + dy * dy)
            angle = math.atan2(dy, dx)
            ring = 0.010 * math.sin(radius * math.tau * 5.0 + math.sin(angle * 3.0) * 0.35)
            radial = 0.007 * math.sin(angle * 9.0 + radius * 6.0)
            center_darken = -0.038 * math.exp(-0.5 * (radius / 0.28) ** 2)
            edge_warm = 0.024 * clamp01(radius)
            color = (
                base[0] + ring + radial + edge_warm + center_darken,
                base[1] + ring * 0.75 + radial * 0.45 + edge_warm * 0.7 + center_darken * 0.55,
                base[2] + ring * 0.35 + radial * 0.24 + center_darken * 0.22,
                1.0,
            )

            for crack_angle, start_radius, end_radius in crack_angles:
                angle_delta = abs(math.atan2(math.sin(angle - crack_angle), math.cos(angle - crack_angle)))
                if start_radius < radius < end_radius and angle_delta < 0.018:
                    color = mix_color(color, dark, 0.18 * (1.0 - angle_delta / 0.018))

            if radius > 0.58 and radius < 0.95:
                color = mix_color(color, warm, 0.12)

            set_pixel(pixels, size, x, y, color)

    return write_image(TOP_TEXTURE_PATH, "Carrot_Top_Crown_r03", size, size, pixels)


def create_leaf_texture():
    width = 512
    height = 512
    rng = random.Random(3105)
    pixels = [0.0] * (width * height * 4)
    base = (0.12, 0.44, 0.18, 1.0)
    light = (0.35, 0.66, 0.25, 1.0)
    dark = (0.04, 0.23, 0.10, 1.0)

    stripe_centers = [rng.random() for _ in range(10)]
    for y in range(height):
        v = y / (height - 1)
        root_shadow = 0.24 * ((1.0 - v) ** 2.6)
        for x in range(width):
            u = x / width
            tone = 0.008 * math.sin(math.tau * (u * 5.0 + v * 0.2))
            for center in stripe_centers:
                distance = periodic_distance(u, center)
                tone += 0.014 * math.exp(-0.5 * (distance / 0.018) ** 2)

            color = (
                base[0] + tone,
                base[1] + tone * 1.15,
                base[2] + tone * 0.85,
                1.0,
            )
            color = mix_color(color, dark, root_shadow)
            if 0.38 < u < 0.62:
                color = mix_color(color, light, 0.12 * (1.0 - abs(u - 0.5) / 0.12))
            set_pixel(pixels, width, x, y, color)

    return write_image(LEAF_TEXTURE_PATH, "Carrot_Leaf_Stem_r03", width, height, pixels)


def create_material(name, color, roughness=0.96, specular=0.0, texture_image=None):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True

    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = 0.0
        if "Coat Weight" in bsdf.inputs:
            bsdf.inputs["Coat Weight"].default_value = 0.0
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = specular

        if texture_image is not None:
            uv_node = material.node_tree.nodes.new("ShaderNodeUVMap")
            uv_node.name = f"{name}_UVMap"
            uv_node.uv_map = "UVMap"
            texture_node = material.node_tree.nodes.new("ShaderNodeTexImage")
            texture_node.name = f"{name}_ImageTexture"
            texture_node.image = texture_image
            material.node_tree.links.new(uv_node.outputs["UV"], texture_node.inputs["Vector"])
            material.node_tree.links.new(texture_node.outputs["Color"], bsdf.inputs["Base Color"])

    return material


def create_carrot_body_textured(side_material, top_material):
    vertices = []
    faces = []
    face_uvs = []
    face_materials = []

    for y_index in range(blockout.HEIGHT_SEGMENTS + 1):
        height_percent = (y_index / blockout.HEIGHT_SEGMENTS) * blockout.PROFILE_MAX_T
        z = height_percent * blockout.BODY_HEIGHT
        radius = blockout.MAX_RADIUS * blockout.radius_percent_at(height_percent) / 100.0

        for radial_index in range(blockout.RADIAL_SEGMENTS):
            angle = math.tau * radial_index / blockout.RADIAL_SEGMENTS
            x = math.cos(angle) * radius
            y = math.sin(angle) * radius * blockout.SIDE_DEPTH_SCALE
            vertices.append((x, y, z))

    for y_index in range(blockout.HEIGHT_SEGMENTS):
        row = y_index * blockout.RADIAL_SEGMENTS
        next_row = (y_index + 1) * blockout.RADIAL_SEGMENTS
        v0 = y_index / blockout.HEIGHT_SEGMENTS
        v1 = (y_index + 1) / blockout.HEIGHT_SEGMENTS

        for radial_index in range(blockout.RADIAL_SEGMENTS):
            next_radial = (radial_index + 1) % blockout.RADIAL_SEGMENTS
            u0 = radial_index / blockout.RADIAL_SEGMENTS
            u1 = (radial_index + 1) / blockout.RADIAL_SEGMENTS
            faces.append((row + radial_index, row + next_radial, next_row + next_radial, next_row + radial_index))
            face_uvs.append(((u0, v0), (u1, v0), (u1, v1), (u0, v1)))
            face_materials.append(0)

    bottom_center_index = len(vertices)
    vertices.append((0.0, 0.0, -0.006))
    top_center_index = len(vertices)
    vertices.append((0.0, 0.0, blockout.BODY_HEIGHT * blockout.PROFILE_MAX_T + 0.006))
    top_row = blockout.HEIGHT_SEGMENTS * blockout.RADIAL_SEGMENTS

    for radial_index in range(blockout.RADIAL_SEGMENTS):
        next_radial = (radial_index + 1) % blockout.RADIAL_SEGMENTS
        angle0 = math.tau * radial_index / blockout.RADIAL_SEGMENTS
        angle1 = math.tau * (radial_index + 1) / blockout.RADIAL_SEGMENTS

        faces.append((bottom_center_index, radial_index, next_radial))
        face_uvs.append(((0.5, 0.5), (0.5 + math.cos(angle0) * 0.45, 0.5 + math.sin(angle0) * 0.45), (0.5 + math.cos(angle1) * 0.45, 0.5 + math.sin(angle1) * 0.45)))
        face_materials.append(0)

        faces.append((top_center_index, top_row + next_radial, top_row + radial_index))
        face_uvs.append(((0.5, 0.5), (0.5 + math.cos(angle1) * 0.45, 0.5 + math.sin(angle1) * 0.45), (0.5 + math.cos(angle0) * 0.45, 0.5 + math.sin(angle0) * 0.45)))
        face_materials.append(1)

    mesh = bpy.data.meshes.new("Mesh_Carrot_Normal_v001_Textured_Body_r03")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(side_material)
    mesh.materials.append(top_material)
    uv_layer = mesh.uv_layers.new(name="UVMap")

    for polygon_index, polygon in enumerate(mesh.polygons):
        polygon.material_index = face_materials[polygon_index]
        for loop_offset, loop_index in enumerate(polygon.loop_indices):
            uv_layer.data[loop_index].uv = face_uvs[polygon_index][loop_offset]

    mesh.update()

    body = bpy.data.objects.new("Crop_Carrot_Normal_v001_Body_Textured_r03", mesh)
    bpy.context.collection.objects.link(body)
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    bpy.ops.object.shade_smooth()
    modifier = body.modifiers.new("BlockoutShapeLockedSmoothSubdivision", "SUBSURF")
    modifier.levels = 1
    modifier.render_levels = 1
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    body.select_set(False)
    return body


def create_tapered_stem_textured(name, points, radius, material):
    radial_segments = 18
    ring_count = 30
    vertices = []
    faces = []
    face_uvs = []

    for ring_index in range(ring_count):
        t = ring_index / (ring_count - 1)
        center = blockout.bezier_point(points, t)
        tangent = blockout.bezier_tangent(points, t)
        normal, binormal = blockout.stem_frame(tangent)
        ring_radius = radius * blockout.stem_radius_scale(t)

        for radial_index in range(radial_segments):
            angle = math.tau * radial_index / radial_segments
            offset = normal * math.cos(angle) * ring_radius + binormal * math.sin(angle) * ring_radius
            vertices.append(tuple(center + offset))

    for ring_index in range(ring_count - 1):
        row = ring_index * radial_segments
        next_row = (ring_index + 1) * radial_segments
        v0 = ring_index / (ring_count - 1)
        v1 = (ring_index + 1) / (ring_count - 1)

        for radial_index in range(radial_segments):
            next_radial = (radial_index + 1) % radial_segments
            u0 = radial_index / radial_segments
            u1 = (radial_index + 1) / radial_segments
            faces.append((row + radial_index, row + next_radial, next_row + next_radial, next_row + radial_index))
            face_uvs.append(((u0, v0), (u1, v0), (u1, v1), (u0, v1)))

    faces.append(tuple(reversed(range(radial_segments))))
    face_uvs.append(tuple((0.5 + math.cos(math.tau * radial_index / radial_segments) * 0.35, 0.5 + math.sin(math.tau * radial_index / radial_segments) * 0.35) for radial_index in reversed(range(radial_segments))))

    last_row = (ring_count - 1) * radial_segments
    faces.append(tuple(last_row + radial_index for radial_index in range(radial_segments)))
    face_uvs.append(tuple((0.5 + math.cos(math.tau * radial_index / radial_segments) * 0.35, 0.5 + math.sin(math.tau * radial_index / radial_segments) * 0.35) for radial_index in range(radial_segments)))

    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon_index, polygon in enumerate(mesh.polygons):
        for loop_offset, loop_index in enumerate(polygon.loop_indices):
            uv_layer.data[loop_index].uv = face_uvs[polygon_index][loop_offset]
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)
    return obj


def offset_points(points, offset):
    return [(point[0] + offset[0], point[1] + offset[1], point[2] + offset[2]) for point in points]


def trim_stem_points(points, start_t=0.06, end_t=0.78):
    return [
        tuple(blockout.bezier_point(points, start_t)),
        tuple(blockout.bezier_point(points, (start_t + end_t) * 0.5)),
        tuple(blockout.bezier_point(points, end_t)),
    ]


def create_leaf_stems(primary_material, secondary_material, root_material, highlight_material, shadow_material):
    stem_base_z = blockout.BODY_HEIGHT * blockout.PROFILE_MAX_T - 0.018
    stem_root_z = stem_base_z - 0.055

    blockout.add_uv_sphere(
        "LeafStem_RoundedRoot_Knot_Textured_r03",
        (0.0, 0.0, stem_base_z + 0.005),
        (0.135, 0.125, 0.095),
        root_material,
        16,
        8,
    )

    stems = [
        ("LeafStem_Primary_Center_Textured_r03", [(0.00, 0.00, stem_root_z), (0.00, 0.00, 2.66), (0.00, 0.00, 2.96)], 0.086, primary_material),
        ("LeafStem_Secondary_Left_Textured_r03", [(-0.03, 0.00, stem_root_z), (-0.06, 0.00, 2.61), (-0.24, 0.02, 2.83)], 0.083, secondary_material),
        ("LeafStem_Primary_Right_Textured_r03", [(0.03, 0.00, stem_root_z), (0.06, 0.00, 2.61), (0.25, -0.02, 2.83)], 0.083, primary_material),
        ("LeafStem_Shadow_Back_Textured_r03", [(0.00, 0.03, stem_root_z), (0.02, 0.09, 2.58), (0.08, 0.24, 2.78)], 0.078, shadow_material),
    ]

    for name, points, radius, material in stems:
        create_tapered_stem_textured(name, points, radius, material)

    highlights = [
        ("LeafStemHighlight_Center_r03", trim_stem_points(stems[0][1]), (0.018, -0.014, 0.010), 0.010),
        ("LeafStemHighlight_Left_r03", trim_stem_points(stems[1][1]), (0.016, -0.014, 0.004), 0.009),
        ("LeafStemHighlight_Right_r03", trim_stem_points(stems[2][1]), (0.016, -0.014, 0.004), 0.009),
    ]

    for name, points, offset, radius in highlights:
        create_tapered_stem_textured(name, offset_points(points, offset), radius, highlight_material)


def configure_scene():
    scene = bpy.context.scene
    available_engines = {
        item.identifier
        for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items
    }
    if "BLENDER_EEVEE" in available_engines:
        scene.render.engine = "BLENDER_EEVEE"
    elif "BLENDER_EEVEE_NEXT" in available_engines:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    else:
        scene.render.engine = "BLENDER_WORKBENCH"

    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("WorldFarm_Model_World_r03")
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
    key.name = "Key_Light_Model_r03"
    key.data.energy = 410
    key.data.size = 4.8

    bpy.ops.object.light_add(type="POINT", location=(-2.4, 2.5, 2.9))
    fill = bpy.context.object
    fill.name = "Fill_Light_Model_r03"
    fill.data.energy = 96


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
        path_mode="RELATIVE",
    )


def main():
    ensure_dirs()
    blockout.clear_scene()

    body_texture = create_body_texture()
    top_texture = create_top_texture()
    leaf_texture = create_leaf_texture()

    body_material = create_material("Mat_Carrot_Body_Texture_r03", (0.94, 0.43, 0.085, 1.0), 0.98, 0.0, body_texture)
    top_material = create_material("Mat_Carrot_Top_Texture_r03", (0.93, 0.41, 0.085, 1.0), 0.98, 0.0, top_texture)
    leaf_primary = create_material("Mat_Carrot_LeafPrimary_Texture_r03", (0.13, 0.48, 0.20, 1.0), 0.88, 0.03, leaf_texture)
    leaf_secondary = create_material("Mat_Carrot_LeafSecondary_Texture_r03", (0.08, 0.35, 0.16, 1.0), 0.90, 0.02, leaf_texture)
    leaf_highlight = create_material("Mat_Carrot_LeafHighlight_r03", (0.34, 0.66, 0.25, 1.0), 0.84, 0.03)
    leaf_shadow = create_material("Mat_Carrot_LeafShadow_Texture_r03", (0.05, 0.25, 0.12, 1.0), 0.92, 0.01, leaf_texture)
    root_material = create_material("Mat_Carrot_LeafRoot_Texture_r03", (0.08, 0.34, 0.15, 1.0), 0.92, 0.01, leaf_texture)

    create_carrot_body_textured(body_material, top_material)
    create_leaf_stems(leaf_primary, leaf_secondary, root_material, leaf_highlight, leaf_shadow)
    configure_scene()

    render_view("v001_model_r03_front.png", (0.0, -6.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_model_r03_side.png", (6.0, 0.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_model_r03_three_quarter.png", (4.2, -5.2, 2.1), (0.0, 0.0, 1.55), 3.55)
    render_view("v001_model_r03_top.png", (0.0, 0.0, 6.4), (0.0, 0.0, 1.55), 3.25)

    export_fbx()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    print(f"BODY_TEXTURE={BODY_TEXTURE_PATH}")
    print(f"TOP_TEXTURE={TOP_TEXTURE_PATH}")
    print(f"LEAF_TEXTURE={LEAF_TEXTURE_PATH}")
    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"SCREENSHOTS={SCREENSHOT_DIR}")


if __name__ == "__main__":
    main()
