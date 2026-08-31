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
BLEND_PATH = SCRIPT_DIR / "carrot_normal_v001_model_r02.blend"
FBX_PATH = EXPORT_DIR / "Crop_Carrot_Normal_v001_model_r02.fbx"


def create_material(name, color, roughness=0.86, specular=0.08):
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

    return material


def body_surface_point(height_percent, angle_degrees, surface_offset=1.018):
    radius = blockout.MAX_RADIUS * blockout.radius_percent_at(height_percent) / 100.0
    angle = math.radians(angle_degrees)
    x = math.cos(angle) * radius * surface_offset
    y = math.sin(angle) * radius * blockout.SIDE_DEPTH_SCALE * surface_offset
    z = height_percent * blockout.BODY_HEIGHT
    return Vector((x, y, z))


def create_body_growth_mark(name, height_percent, center_angle, arc_degrees, vertical_drift, width, material):
    segment_count = 16
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


def create_body_skin_streak(name, start_height, end_height, start_angle, end_angle, angular_width, material):
    segment_count = 10
    vertices = []
    faces = []

    for segment in range(segment_count + 1):
        u = segment / segment_count
        height = start_height + (end_height - start_height) * u
        angle = start_angle + (end_angle - start_angle) * u
        left = body_surface_point(height, angle - angular_width * 0.5, 1.021)
        right = body_surface_point(height, angle + angular_width * 0.5, 1.021)
        vertices.extend([tuple(left), tuple(right)])

    for segment in range(segment_count):
        left_a = segment * 2
        right_a = left_a + 1
        left_b = left_a + 2
        right_b = left_a + 3
        faces.append((left_a, left_b, right_b, right_a))
        faces.append((right_a, right_b, left_b, left_a))

    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def create_body_warm_patch(name, height, angle, height_size, angular_width, material):
    vertices = []
    faces = []
    samples = [
        (-0.52, -0.35),
        (0.50, -0.22),
        (0.62, 0.24),
        (-0.46, 0.40),
    ]

    for height_offset, angle_offset in samples:
        point = body_surface_point(
            height + height_size * height_offset,
            angle + angular_width * angle_offset,
            1.020,
        )
        vertices.append(tuple(point))

    faces.append((0, 1, 2, 3))
    faces.append((3, 2, 1, 0))

    mesh = bpy.data.meshes.new(f"Mesh_{name}")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


def create_texture_details(growth_material, grain_material, warm_patch_material):
    marks = [
        ("CarrotGrowthMark_00", 0.19, -104.0, 28.0, -0.020, 0.0075),
        ("CarrotGrowthMark_01", 0.27, -136.0, 24.0, 0.018, 0.0065),
        ("CarrotGrowthMark_02", 0.34, -62.0, 26.0, -0.022, 0.0068),
        ("CarrotGrowthMark_03", 0.42, -112.0, 38.0, 0.026, 0.0082),
        ("CarrotGrowthMark_04", 0.50, -150.0, 22.0, -0.016, 0.0064),
        ("CarrotGrowthMark_05", 0.57, -82.0, 34.0, 0.022, 0.0076),
        ("CarrotGrowthMark_06", 0.65, -123.0, 30.0, -0.020, 0.0070),
        ("CarrotGrowthMark_07", 0.72, -48.0, 22.0, 0.014, 0.0058),
        ("CarrotGrowthMark_08", 0.79, -95.0, 30.0, -0.016, 0.0062),
        ("CarrotGrowthMark_09", 0.85, -136.0, 20.0, 0.012, 0.0050),
        ("CarrotGrowthMark_10", 0.88, -76.0, 18.0, -0.010, 0.0048),
    ]

    for name, height, angle, arc, drift, width in marks:
        create_body_growth_mark(name, height, angle, arc, drift, width, growth_material)

    skin_streaks = [
        ("CarrotSkinGrain_00", 0.16, 0.42, -118.0, -111.0, 2.8),
        ("CarrotSkinGrain_01", 0.22, 0.58, -92.0, -98.0, 2.2),
        ("CarrotSkinGrain_02", 0.30, 0.68, -143.0, -134.0, 2.4),
        ("CarrotSkinGrain_03", 0.36, 0.77, -63.0, -70.0, 2.0),
        ("CarrotSkinGrain_04", 0.48, 0.86, -109.0, -103.0, 2.6),
        ("CarrotSkinGrain_05", 0.18, 0.32, -72.0, -78.0, 2.0),
        ("CarrotSkinGrain_06", 0.54, 0.82, -154.0, -146.0, 2.1),
        ("CarrotSkinGrain_07", 0.64, 0.91, -86.0, -91.0, 1.8),
        ("CarrotSkinGrain_08", 0.26, 0.49, -43.0, -50.0, 1.7),
        ("CarrotSkinGrain_09", 0.39, 0.63, -128.0, -123.0, 1.9),
        ("CarrotSkinGrain_10", 0.58, 0.74, -55.0, -62.0, 1.6),
        ("CarrotSkinGrain_11", 0.72, 0.92, -116.0, -110.0, 1.8),
        ("CarrotSkinGrain_12", 0.46, 0.58, -32.0, -39.0, 1.5),
        ("CarrotSkinGrain_13", 0.73, 0.86, -148.0, -142.0, 1.7),
    ]

    for name, start_height, end_height, start_angle, end_angle, angular_width in skin_streaks:
        create_body_skin_streak(name, start_height, end_height, start_angle, end_angle, angular_width, grain_material)

    warm_patches = [
        ("CarrotSkinWarmPatch_00", 0.31, -104.0, 0.060, 10.0),
        ("CarrotSkinWarmPatch_01", 0.55, -69.0, 0.075, 11.5),
        ("CarrotSkinWarmPatch_02", 0.69, -132.0, 0.065, 9.5),
        ("CarrotSkinWarmPatch_03", 0.82, -92.0, 0.055, 8.0),
    ]

    for name, height, angle, height_size, angular_width in warm_patches:
        create_body_warm_patch(name, height, angle, height_size, angular_width, warm_patch_material)


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
        "LeafStem_RoundedRoot_Knot",
        (0.0, 0.0, stem_base_z + 0.005),
        (0.135, 0.125, 0.095),
        root_material,
        16,
        8,
    )

    stems = [
        ("LeafStem_Primary_Center", [(0.00, 0.00, stem_root_z), (0.00, 0.00, 2.66), (0.00, 0.00, 2.96)], 0.086, primary_material),
        ("LeafStem_Secondary_Left", [(-0.03, 0.00, stem_root_z), (-0.06, 0.00, 2.61), (-0.24, 0.02, 2.83)], 0.083, secondary_material),
        ("LeafStem_Primary_Right", [(0.03, 0.00, stem_root_z), (0.06, 0.00, 2.61), (0.25, -0.02, 2.83)], 0.083, primary_material),
        ("LeafStem_Shadow_Back", [(0.00, 0.03, stem_root_z), (0.02, 0.09, 2.58), (0.08, 0.24, 2.78)], 0.078, shadow_material),
    ]

    for name, points, radius, material in stems:
        blockout.create_tapered_stem(name, points, radius, material)

    highlights = [
        ("LeafStemHighlight_Center", trim_stem_points(stems[0][1]), (0.018, -0.014, 0.010), 0.010),
        ("LeafStemHighlight_Left", trim_stem_points(stems[1][1]), (0.016, -0.014, 0.004), 0.009),
        ("LeafStemHighlight_Right", trim_stem_points(stems[2][1]), (0.016, -0.014, 0.004), 0.009),
    ]

    for name, points, offset, radius in highlights:
        blockout.create_tapered_stem(name, offset_points(points, offset), radius, highlight_material)


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
    key.data.energy = 430
    key.data.size = 4.4

    bpy.ops.object.light_add(type="POINT", location=(-2.4, 2.5, 2.9))
    fill = bpy.context.object
    fill.name = "Fill_Light_Model"
    fill.data.energy = 88


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

    body_material = create_material("Mat_Carrot_Body_MatteOrange_r02", (0.93, 0.36, 0.075, 1.0), 0.98, 0.0)
    growth_material = create_material("Mat_Carrot_GrowthScars_r02", (0.52, 0.14, 0.030, 1.0), 0.98, 0.0)
    grain_material = create_material("Mat_Carrot_SkinGrain_r02", (0.66, 0.20, 0.050, 1.0), 0.98, 0.0)
    warm_patch_material = create_material("Mat_Carrot_WarmSkinMottle_r02", (0.99, 0.48, 0.14, 1.0), 0.98, 0.0)
    leaf_primary = create_material("Mat_Carrot_LeafPrimary_r02", (0.13, 0.48, 0.20, 1.0), 0.82, 0.06)
    leaf_secondary = create_material("Mat_Carrot_LeafSecondary_r02", (0.08, 0.35, 0.16, 1.0), 0.86, 0.04)
    leaf_highlight = create_material("Mat_Carrot_LeafHighlight_r02", (0.34, 0.66, 0.25, 1.0), 0.80, 0.05)
    leaf_shadow = create_material("Mat_Carrot_LeafShadow_r02", (0.05, 0.25, 0.12, 1.0), 0.90, 0.03)
    root_material = create_material("Mat_Carrot_LeafRoot_r02", (0.08, 0.34, 0.15, 1.0), 0.88, 0.04)

    blockout.create_carrot_body(body_material)
    create_texture_details(growth_material, grain_material, warm_patch_material)
    create_leaf_stems(leaf_primary, leaf_secondary, root_material, leaf_highlight, leaf_shadow)
    configure_scene()

    render_view("v001_model_r02_front.png", (0.0, -6.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_model_r02_side.png", (6.0, 0.0, 1.62), (0.0, 0.0, 1.55))
    render_view("v001_model_r02_three_quarter.png", (4.2, -5.2, 2.1), (0.0, 0.0, 1.55), 3.55)
    render_view("v001_model_r02_top.png", (0.0, 0.0, 6.4), (0.0, 0.0, 1.55), 3.25)

    export_fbx()
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    print(f"BLEND={BLEND_PATH}")
    print(f"FBX={FBX_PATH}")
    print(f"SCREENSHOTS={SCREENSHOT_DIR}")


if __name__ == "__main__":
    main()
