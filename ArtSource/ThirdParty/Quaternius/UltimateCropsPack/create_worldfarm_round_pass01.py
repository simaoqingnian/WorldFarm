import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
PASS_SLUG = "round_pass_01"
PASS_FILE = "round_pass01"
PASS_LABEL = "RoundPass01"
OUTPUT_ROOT = ROOT / f"worldfarm_{PASS_SLUG}"
BLEND_DIR = OUTPUT_ROOT / "blends"
FBX_DIR = OUTPUT_ROOT / "fbx"
SCREENSHOT_DIR = OUTPUT_ROOT / "screenshots"
PREVIEW_PATH = SCREENSHOT_DIR / f"worldfarm_{PASS_FILE}_preview.png"

CROPS = (
    ("Carrot", -2.15, 0.86),
    ("Corn", -1.05, 1.30),
    ("Wheat", 0.05, 1.18),
    ("Rice", 1.08, 1.12),
    ("Lettuce", 2.05, 0.82),
)

MAX_PREVIEW_FOOTPRINT = 0.72


def ensure_dirs():
    BLEND_DIR.mkdir(parents=True, exist_ok=True)
    FBX_DIR.mkdir(parents=True, exist_ok=True)
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, roughness=0.72, specular=0.16):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = 1.0
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = 0.0
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = specular
    return material


def create_materials():
    return {
        "carrot_body": make_material("WF_Carrot_WarmBody", (0.98, 0.52, 0.13, 1.0), 0.76, 0.18),
        "carrot_ridge": make_material("WF_Carrot_SoftRidge", (0.77, 0.33, 0.07, 1.0), 0.82, 0.12),
        "stem": make_material("WF_Stem_Green", (0.34, 0.61, 0.25, 1.0), 0.78, 0.12),
        "leaf": make_material("WF_Leaf_Light", (0.52, 0.78, 0.35, 1.0), 0.76, 0.12),
        "leaf_dark": make_material("WF_Leaf_Dark", (0.22, 0.42, 0.19, 1.0), 0.82, 0.10),
        "corn": make_material("WF_Corn_Kernel", (0.96, 0.72, 0.22, 1.0), 0.72, 0.16),
        "grain": make_material("WF_Wheat_Gold", (0.82, 0.61, 0.24, 1.0), 0.78, 0.12),
        "grain_light": make_material("WF_Rice_LightGrain", (0.76, 0.78, 0.43, 1.0), 0.80, 0.10),
        "lettuce": make_material("WF_Lettuce_Main", (0.38, 0.58, 0.32, 1.0), 0.84, 0.08),
        "lettuce_light": make_material("WF_Lettuce_Light", (0.55, 0.74, 0.41, 1.0), 0.82, 0.08),
        "lettuce_dark": make_material("WF_Lettuce_Dark", (0.24, 0.37, 0.23, 1.0), 0.86, 0.06),
    }


def smooth_object(obj, bevel_width=0.0, bevel_segments=1):
    if obj.type != "MESH":
        return obj

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth()

    if bevel_width > 0.0:
        bevel = obj.modifiers.new("WF_soft_edge", "BEVEL")
        bevel.width = bevel_width
        bevel.segments = bevel_segments
        bevel.affect = "EDGES"

    normal = obj.modifiers.new("WF_weighted_normal", "WEIGHTED_NORMAL")
    normal.keep_sharp = True
    return obj


def align_z_to_vector(obj, direction):
    direction = Vector(direction)
    if direction.length < 0.0001:
        return
    obj.rotation_euler = direction.normalized().to_track_quat("Z", "Y").to_euler()


def add_cylinder_between(name, start, end, radius, material, vertices=18):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    length = max(direction.length, 0.001)
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=length, location=(start + end) * 0.5)
    obj = bpy.context.object
    obj.name = name
    align_z_to_vector(obj, direction)
    obj.data.materials.append(material)
    return smooth_object(obj, bevel_width=radius * 0.12, bevel_segments=2)


def add_curved_cylinder(name, start, end, radius, material, bend, segments=5, vertices=14):
    start = Vector(start)
    end = Vector(end)
    control = (start + end) * 0.5 + Vector(bend)
    objects = []
    previous = start

    for index in range(1, segments + 1):
        t = index / segments
        point = ((1.0 - t) ** 2) * start + 2.0 * (1.0 - t) * t * control + (t ** 2) * end
        objects.append(add_cylinder_between(f"{name}_{index}", previous, point, radius, material, vertices=vertices))
        previous = point

    return objects


def add_ellipsoid(name, location, scale, material, direction=None, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    if direction is not None:
        align_z_to_vector(obj, direction)
    obj.data.materials.append(material)
    return smooth_object(obj, bevel_width=0.0, bevel_segments=1)


def add_revolved_mesh(name, profile, material, radial_segments=36):
    verts = []
    faces = []
    for ring_index, (z, radius_x, radius_y) in enumerate(profile):
        for segment in range(radial_segments):
            angle = math.tau * segment / radial_segments
            verts.append((math.cos(angle) * radius_x, math.sin(angle) * radius_y, z))

    for ring_index in range(len(profile) - 1):
        ring_start = ring_index * radial_segments
        next_start = (ring_index + 1) * radial_segments
        for segment in range(radial_segments):
            next_segment = (segment + 1) % radial_segments
            faces.append((
                ring_start + segment,
                ring_start + next_segment,
                next_start + next_segment,
                next_start + segment,
            ))

    bottom_center = len(verts)
    verts.append((0.0, 0.0, profile[0][0]))
    top_center = len(verts)
    verts.append((0.0, 0.0, profile[-1][0]))

    for segment in range(radial_segments):
        next_segment = (segment + 1) % radial_segments
        faces.append((bottom_center, next_segment, segment))
        top_ring = (len(profile) - 1) * radial_segments
        faces.append((top_center, top_ring + segment, top_ring + next_segment))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return smooth_object(obj, bevel_width=0.012, bevel_segments=2)


def add_leaf_ribbon(name, base, direction, length, width, thickness, material, curve=0.0, segments=9):
    verts = []
    faces = []
    for index in range(segments + 1):
        t = index / segments
        center_x = curve * math.sin(math.pi * t)
        half_width = width * (math.sin(math.pi * t) ** 0.62)
        half_width *= 1.0 - 0.10 * t
        z = length * t
        verts.extend((
            (center_x - half_width, -thickness * 0.5, z),
            (center_x + half_width, -thickness * 0.5, z),
            (center_x - half_width, thickness * 0.5, z),
            (center_x + half_width, thickness * 0.5, z),
        ))

    for index in range(segments):
        a = index * 4
        b = (index + 1) * 4
        faces.append((a, b, b + 1, a + 1))
        faces.append((a + 2, a + 3, b + 3, b + 2))
        faces.append((a, a + 2, b + 2, b))
        faces.append((a + 1, b + 1, b + 3, a + 3))

    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = base
    align_z_to_vector(obj, direction)
    obj.data.materials.append(material)
    return smooth_object(obj, bevel_width=thickness * 0.32, bevel_segments=2)


def add_tassel(name, center, material):
    objects = []
    for index, angle in enumerate((-0.72, -0.36, 0.0, 0.36, 0.72)):
        end = Vector(center) + Vector((math.sin(angle) * 0.12, math.cos(angle) * 0.07, 0.18 + 0.03 * math.cos(angle)))
        objects.append(add_cylinder_between(f"{name}_{index}", center, end, 0.007, material, vertices=10))
    return objects


def add_soft_carrot_ridges(material):
    objects = []
    for index, (z, radius, angle, length) in enumerate((
        (0.32, 0.135, 0.35, 0.085),
        (0.45, 0.170, 2.25, 0.105),
        (0.58, 0.187, 4.05, 0.095),
    )):
        location = (math.cos(angle) * radius, math.sin(angle) * radius, z)
        direction = (-math.sin(angle), math.cos(angle), 0.18)
        obj = add_cylinder_between(f"Carrot_soft_growth_mark_{index}", location, Vector(location) + Vector(direction).normalized() * length, 0.007, material, vertices=8)
        objects.append(obj)
    return objects


def build_carrot(materials):
    objects = []
    body = add_revolved_mesh(
        "WF_RoundPass01_Carrot_Body",
        (
            (0.00, 0.030, 0.030),
            (0.06, 0.070, 0.068),
            (0.22, 0.125, 0.120),
            (0.48, 0.185, 0.175),
            (0.70, 0.210, 0.198),
            (0.84, 0.175, 0.165),
            (0.92, 0.105, 0.100),
        ),
        materials["carrot_body"],
        radial_segments=40,
    )
    objects.append(body)
    objects.extend(add_soft_carrot_ridges(materials["carrot_ridge"]))
    objects.append(add_ellipsoid("WF_RoundPass01_Carrot_Crown", (0.0, 0.0, 0.905), (0.070, 0.065, 0.035), materials["stem"], segments=20, rings=10))

    leaf_specs = (
        ((0.00, 0.00, 0.90), (-0.20, 0.03, 0.66), 0.56, 0.060, 0.020, materials["leaf"], 0.035),
        ((0.01, 0.01, 0.91), (0.18, -0.07, 0.62), 0.52, 0.058, 0.020, materials["leaf"], -0.025),
        ((-0.01, 0.00, 0.90), (-0.06, -0.18, 0.68), 0.48, 0.052, 0.019, materials["leaf_dark"], 0.018),
        ((0.00, -0.01, 0.91), (0.06, 0.16, 0.70), 0.46, 0.050, 0.018, materials["stem"], -0.018),
    )
    for index, spec in enumerate(leaf_specs):
        base = Vector(spec[0])
        direction = Vector(spec[1]).normalized()
        objects.append(add_cylinder_between(
            f"WF_RoundPass01_Carrot_LeafStem_{index}",
            base,
            base + direction * 0.20,
            0.014,
            materials["stem"],
            vertices=12,
        ))
        objects.append(add_leaf_ribbon(f"WF_RoundPass01_Carrot_Leaf_{index}", *spec))

    return objects


def build_corn(materials):
    objects = []
    objects.append(add_cylinder_between("WF_RoundPass01_Corn_Stem", (0, 0, 0), (0, 0, 1.13), 0.036, materials["stem"], vertices=18))
    objects.append(add_ellipsoid("WF_RoundPass01_Corn_StemCap", (0, 0, 1.13), (0.038, 0.038, 0.022), materials["stem"], segments=18, rings=8))
    objects.extend(add_tassel("WF_RoundPass01_Corn_Tassel", (0, 0, 1.09), materials["grain"]))

    leaf_specs = (
        ((0, 0, 0.22), (-0.56, 0.04, 0.46), 0.56, 0.070, 0.018, materials["leaf"], 0.040),
        ((0, 0, 0.36), (0.55, -0.08, 0.42), 0.58, 0.078, 0.018, materials["leaf"], -0.045),
        ((0, 0, 0.52), (-0.38, -0.20, 0.50), 0.50, 0.066, 0.017, materials["leaf_dark"], 0.025),
        ((0, 0, 0.70), (0.38, 0.18, 0.54), 0.48, 0.065, 0.017, materials["leaf"], -0.022),
    )
    for index, spec in enumerate(leaf_specs):
        objects.append(add_leaf_ribbon(f"WF_RoundPass01_Corn_Leaf_{index}", *spec))

    objects.append(add_ellipsoid("WF_RoundPass01_Corn_Cob_A", (0.105, -0.035, 0.58), (0.070, 0.055, 0.180), materials["corn"], direction=(0.20, -0.08, 0.55), segments=24, rings=12))
    objects.append(add_ellipsoid("WF_RoundPass01_Corn_Cob_B", (-0.100, 0.032, 0.73), (0.060, 0.050, 0.150), materials["corn"], direction=(-0.16, 0.07, 0.50), segments=24, rings=12))
    objects.append(add_leaf_ribbon("WF_RoundPass01_Corn_Husk_A", (0.050, -0.025, 0.46), (0.25, -0.07, 0.38), 0.34, 0.040, 0.014, materials["leaf_dark"], curve=0.010))
    objects.append(add_leaf_ribbon("WF_RoundPass01_Corn_Husk_B", (-0.045, 0.022, 0.63), (-0.24, 0.08, 0.34), 0.30, 0.036, 0.014, materials["leaf_dark"], curve=-0.010))
    return objects


def add_grain_head(prefix, base, direction, material, grain_material, count=7):
    objects = []
    base = Vector(base)
    direction = Vector(direction).normalized()
    top = base + direction * 0.33
    objects.append(add_cylinder_between(prefix + "_axis", base, top, 0.010, material, vertices=10))
    side = direction.cross(Vector((0, 0, 1)))
    if side.length < 0.01:
        side = Vector((1, 0, 0))
    side.normalize()

    for index in range(count):
        t = (index + 0.35) / count
        center = base + direction * (0.05 + t * 0.25)
        offset = side * ((-1) ** index) * (0.030 + 0.010 * math.sin(t * math.pi))
        objects.append(add_ellipsoid(
            f"{prefix}_grain_{index}",
            center + offset,
            (0.026, 0.019, 0.055),
            grain_material,
            direction=direction + offset * 0.50,
            segments=16,
            rings=8,
        ))
    return objects


def build_wheat(materials):
    objects = []
    specs = (
        ((-0.055, 0.000, 0.00), (-0.070, 0.012, 0.75), (-0.030, 0.000, 0.020)),
        ((0.000, 0.020, 0.00), (0.015, 0.018, 0.92), (0.025, 0.010, 0.015)),
        ((0.050, -0.020, 0.00), (0.070, -0.035, 0.80), (0.035, -0.008, 0.010)),
        ((0.020, 0.055, 0.00), (0.000, 0.070, 0.70), (-0.018, 0.018, 0.014)),
    )
    for index, (start, end, bend) in enumerate(specs):
        objects.extend(add_curved_cylinder(f"WF_RoundPass01_Wheat_Stem_{index}", start, end, 0.015, materials["grain"], bend=bend, segments=4, vertices=12))
        direction = Vector(end) - Vector(start)
        head_base = Vector(end) - direction.normalized() * 0.03
        objects.extend(add_grain_head(f"WF_RoundPass01_Wheat_Head_{index}", head_base, direction + Vector((0, 0, 0.26)), materials["grain"], materials["grain"], count=5 if index != 1 else 7))

    return objects


def build_rice(materials):
    objects = []
    stem_specs = (
        ((-0.11, -0.02, 0), (-0.20, -0.03, 0.75), (-0.055, -0.010, 0.020)),
        ((-0.07, 0.03, 0), (-0.13, 0.06, 0.88), (-0.035, 0.020, 0.025)),
        ((0.00, -0.03, 0), (0.02, -0.08, 0.94), (0.015, -0.035, 0.018)),
        ((0.05, 0.02, 0), (0.15, 0.01, 0.82), (0.060, -0.010, 0.016)),
        ((0.10, -0.01, 0), (0.23, -0.05, 0.70), (0.075, -0.020, 0.012)),
        ((-0.02, 0.07, 0), (-0.02, 0.15, 0.78), (0.010, 0.055, 0.016)),
    )
    for index, (start, end, stem_bend) in enumerate(stem_specs):
        objects.extend(add_curved_cylinder(f"WF_RoundPass01_Rice_Stem_{index}", start, end, 0.014, materials["stem"], bend=stem_bend, segments=4, vertices=12))
        direction = Vector(end) - Vector(start)
        bend = Vector((0.12 if index % 2 == 0 else -0.10, 0.02, -0.05))
        objects.extend(add_grain_head(f"WF_RoundPass01_Rice_Panicle_{index}", Vector(end) - direction.normalized() * 0.04, direction + bend, materials["stem"], materials["grain_light"], count=4))

    leaf_specs = (
        ((-0.06, 0.00, 0.12), (-0.45, -0.04, 0.35), 0.48, 0.052, 0.014, materials["leaf_dark"], 0.025),
        ((0.02, 0.02, 0.18), (0.42, 0.03, 0.42), 0.50, 0.052, 0.014, materials["leaf"], -0.020),
        ((0.00, -0.02, 0.30), (-0.18, -0.24, 0.48), 0.44, 0.047, 0.013, materials["leaf"], 0.015),
    )
    for index, spec in enumerate(leaf_specs):
        objects.append(add_leaf_ribbon(f"WF_RoundPass01_Rice_Leaf_{index}", *spec))

    return objects


def build_lettuce(materials):
    objects = []
    layer_specs = (
        (8, 0.32, 0.090, 0.060, 0.12, materials["lettuce_dark"], 0.06),
        (7, 0.25, 0.085, 0.070, 0.22, materials["lettuce"], 0.12),
        (5, 0.17, 0.070, 0.080, 0.31, materials["lettuce_light"], 0.16),
    )
    for layer, (count, radius, width, thickness, z, material, lift) in enumerate(layer_specs):
        for index in range(count):
            angle = math.tau * index / count + layer * 0.23
            base = Vector((math.cos(angle) * radius * 0.30, math.sin(angle) * radius * 0.30, z * 0.28))
            direction = Vector((math.cos(angle) * radius, math.sin(angle) * radius, lift))
            objects.append(add_leaf_ribbon(
                f"WF_RoundPass01_Lettuce_Leaf_{layer}_{index}",
                base,
                direction,
                radius,
                width,
                thickness,
                material,
                curve=0.018 * math.sin(angle),
                segments=8,
            ))

    objects.append(add_ellipsoid("WF_RoundPass01_Lettuce_Core", (0, 0, 0.18), (0.13, 0.12, 0.10), materials["lettuce_light"], segments=24, rings=12))
    return objects


def get_bounds(objects):
    points = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        for corner in obj.bound_box:
            points.append(obj.matrix_world @ Vector(corner))
    if not points:
        return None
    min_point = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    max_point = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    return min_point, max_point


def normalize_objects(objects, slot_x=0.0, target_height=None):
    bpy.context.view_layer.update()
    bounds = get_bounds(objects)
    if bounds is None:
        return

    min_point, max_point = bounds
    height = max(max_point.z - min_point.z, 0.001)
    footprint = max(max_point.x - min_point.x, max_point.y - min_point.y, 0.001)
    if target_height is None:
        scale = 1.0
    else:
        scale = min(target_height / height, MAX_PREVIEW_FOOTPRINT / footprint)

    for obj in objects:
        obj.location = obj.location * scale
        obj.scale = tuple(component * scale for component in obj.scale)
    bpy.context.view_layer.update()

    bounds = get_bounds(objects)
    if bounds is None:
        return
    min_point, max_point = bounds
    center = (min_point + max_point) * 0.5
    offset = Vector((slot_x - center.x, -center.y, -min_point.z + 0.04))
    for obj in objects:
        obj.location += offset
    bpy.context.view_layer.update()


def build_crop(crop_name, materials):
    if crop_name == "Carrot":
        return build_carrot(materials)
    if crop_name == "Corn":
        return build_corn(materials)
    if crop_name == "Wheat":
        return build_wheat(materials)
    if crop_name == "Rice":
        return build_rice(materials)
    if crop_name == "Lettuce":
        return build_lettuce(materials)
    raise ValueError(f"Unsupported crop: {crop_name}")


def export_crop(crop_name):
    clear_scene()
    materials = create_materials()
    objects = build_crop(crop_name, materials)
    normalize_objects(objects)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]

    blend_path = BLEND_DIR / f"WF_Quaternius_{crop_name}_{PASS_LABEL}.blend"
    fbx_path = FBX_DIR / f"WF_Quaternius_{crop_name}_{PASS_LABEL}.fbx"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        bake_space_transform=False,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        use_mesh_modifiers=True,
    )
    print(f"EXPORTED={fbx_path}")


def add_stage():
    stage_material = make_material("WF_RoundPass01_Stage", (0.60, 0.70, 0.57, 1.0), 0.62, 0.12)
    platform_material = make_material("WF_RoundPass01_Platform", (0.72, 0.58, 0.39, 1.0), 0.55, 0.16)
    platform_dark_material = make_material("WF_RoundPass01_PlatformSide", (0.43, 0.31, 0.20, 1.0), 0.66, 0.10)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, -0.055))
    stage = bpy.context.object
    stage.name = "WorldFarm_RoundPass01_Stage"
    stage.scale = (2.85, 0.72, 0.035)
    stage.data.materials.append(stage_material)

    for crop_name, slot_x, _height in CROPS:
        bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.31, depth=0.055, location=(slot_x, 0.0, 0.010))
        top = bpy.context.object
        top.name = f"Slot_{crop_name}_Top"
        top.data.materials.append(platform_material)
        smooth_object(top)

        bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=0.325, depth=0.045, location=(slot_x, 0.0, -0.033))
        side = bpy.context.object
        side.name = f"Slot_{crop_name}_Side"
        side.data.materials.append(platform_dark_material)
        smooth_object(side)


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def configure_render_scene():
    scene = bpy.context.scene
    available_engines = {item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items}
    if "BLENDER_EEVEE_NEXT" in available_engines:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    elif "BLENDER_EEVEE" in available_engines:
        scene.render.engine = "BLENDER_EEVEE"

    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1000
    scene.world = bpy.data.worlds.new("WorldFarm_RoundPass01_World")
    scene.world.color = (0.68, 0.82, 0.84)
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    bpy.ops.object.camera_add(location=(3.65, -5.80, 3.45))
    camera = bpy.context.object
    camera.name = "Camera_WorldFarm_RoundPass01"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 5.05
    look_at(camera, (0.0, 0.0, 0.72))
    scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-2.6, -3.8, 4.8))
    key = bpy.context.object
    key.name = "RoundPass01_Key_Light"
    key.data.energy = 520
    key.data.size = 5.2

    bpy.ops.object.light_add(type="POINT", location=(2.7, -2.0, 2.6))
    fill = bpy.context.object
    fill.name = "RoundPass01_Fill_Light"
    fill.data.energy = 120
    fill.data.shadow_soft_size = 1.0


def render_preview():
    clear_scene()
    add_stage()
    materials = create_materials()
    for crop_name, slot_x, target_height in CROPS:
        objects = build_crop(crop_name, materials)
        normalize_objects(objects, slot_x=slot_x, target_height=target_height)
    configure_render_scene()
    bpy.context.scene.render.filepath = str(PREVIEW_PATH)
    bpy.ops.render.render(write_still=True)
    print(f"SCREENSHOT={PREVIEW_PATH}")


def main():
    ensure_dirs()
    for crop_name, _slot_x, _height in CROPS:
        export_crop(crop_name)
    render_preview()


if __name__ == "__main__":
    main()
