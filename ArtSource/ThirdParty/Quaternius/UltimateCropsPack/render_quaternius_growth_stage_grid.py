import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source" / "Nature Crops Pack - Jan 2020" / "FBX"
SCREENSHOT_DIR = ROOT / "screenshots"
OUTPUT = SCREENSHOT_DIR / "quaternius_growth_stage_grid.png"

CROPS = (
    ("Carrot", 1.44, 0.66),
    ("Corn", 0.72, 0.92),
    ("Wheat", 0.00, 0.84),
    ("Rice", -0.72, 0.82),
    ("Lettuce", -1.44, 0.58),
)

STAGE_X = (-1.38, -0.46, 0.46, 1.38)
MAX_FOOTPRINT = 0.52


def ensure_dirs():
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, roughness=0.62, specular=0.14):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = 1.0
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = specular
    return material


def force_opaque_materials(objects):
    for obj in objects:
        if obj.type != "MESH":
            continue
        for material in obj.data.materials:
            if material is None:
                continue
            material.diffuse_color = (
                material.diffuse_color[0],
                material.diffuse_color[1],
                material.diffuse_color[2],
                1.0,
            )
            if hasattr(material, "surface_render_method"):
                material.surface_render_method = "DITHERED"
            elif hasattr(material, "blend_method"):
                material.blend_method = "OPAQUE"
            material.use_nodes = True
            bsdf = material.node_tree.nodes.get("Principled BSDF")
            if bsdf:
                if "Alpha" in bsdf.inputs:
                    bsdf.inputs["Alpha"].default_value = 1.0
                if "Roughness" in bsdf.inputs:
                    bsdf.inputs["Roughness"].default_value = 0.68


def smooth_objects(objects):
    for obj in objects:
        if obj.type != "MESH":
            continue
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_flat()
        obj.select_set(False)


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


def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported = [obj for obj in bpy.data.objects if obj not in before]
    force_opaque_materials(imported)
    smooth_objects(imported)
    return imported


def compute_row_scale(stage_objects, mature_target_height):
    bpy.context.view_layer.update()
    mature_bounds = get_bounds(stage_objects[-1])
    if mature_bounds is None:
        return 1.0

    mature_min, mature_max = mature_bounds
    mature_height = max(mature_max.z - mature_min.z, 0.001)
    max_footprint = 0.001
    for objects in stage_objects:
        bounds = get_bounds(objects)
        if bounds is None:
            continue
        min_point, max_point = bounds
        max_footprint = max(max_footprint, max_point.x - min_point.x, max_point.y - min_point.y)

    return min(mature_target_height / mature_height, MAX_FOOTPRINT / max_footprint)


def place_stage(objects, slot_x, slot_y, scale):
    for obj in objects:
        obj.location = obj.location * scale
        obj.scale = tuple(component * scale for component in obj.scale)
    bpy.context.view_layer.update()

    bounds = get_bounds(objects)
    if bounds is None:
        return

    min_point, max_point = bounds
    center = (min_point + max_point) * 0.5
    offset = Vector((slot_x - center.x, slot_y - center.y, -min_point.z + 0.04))
    for obj in objects:
        obj.location += offset
    bpy.context.view_layer.update()


def add_cylinder(name, radius, depth, location, material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=36, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return obj


def build_stage():
    stage_material = make_material("Mat_GrowthStage_Stage", (0.58, 0.68, 0.57, 1.0), 0.68, 0.10)
    platform_material = make_material("Mat_GrowthStage_Platform", (0.72, 0.59, 0.42, 1.0), 0.56, 0.14)
    platform_dark_material = make_material("Mat_GrowthStage_PlatformSide", (0.42, 0.31, 0.22, 1.0), 0.66, 0.10)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, -0.055))
    stage = bpy.context.object
    stage.name = "WorldFarm_QuaterniusGrowthStage_Stage"
    stage.scale = (1.95, 1.84, 0.035)
    stage.data.materials.append(stage_material)

    for crop_name, row_y, _target_height in CROPS:
        for stage_index, slot_x in enumerate(STAGE_X, start=1):
            add_cylinder(f"Slot_{crop_name}_{stage_index}_Top", 0.23, 0.045, (slot_x, row_y, 0.008), platform_material)
            add_cylinder(f"Slot_{crop_name}_{stage_index}_Side", 0.24, 0.035, (slot_x, row_y, -0.028), platform_dark_material)


def import_and_place_growth_stages():
    for crop_name, row_y, mature_target_height in CROPS:
        stage_objects = []
        for stage_index in range(1, 5):
            objects = import_fbx(SOURCE / f"{crop_name}_{stage_index}.fbx")
            for obj in objects:
                obj.name = f"{crop_name}_Stage{stage_index}_{obj.name}"
            stage_objects.append(objects)

        row_scale = compute_row_scale(stage_objects, mature_target_height)
        for stage_index, objects in enumerate(stage_objects):
            place_stage(objects, STAGE_X[stage_index], row_y, row_scale)


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def configure_scene():
    scene = bpy.context.scene
    available_engines = {item.identifier for item in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items}
    if "BLENDER_EEVEE_NEXT" in available_engines:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
    elif "BLENDER_EEVEE" in available_engines:
        scene.render.engine = "BLENDER_EEVEE"

    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1600
    scene.world = bpy.data.worlds.new("WorldFarm_QuaterniusGrowthStage_World")
    scene.world.color = (0.68, 0.82, 0.84)
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    bpy.ops.object.camera_add(location=(0.0, -5.95, 5.65))
    camera = bpy.context.object
    camera.name = "Camera_QuaterniusGrowthStageGrid"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 4.65
    look_at(camera, (0.0, 0.0, 0.40))
    scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-2.8, -3.8, 5.6))
    key = bpy.context.object
    key.name = "GrowthStage_Key_Light"
    key.data.energy = 620
    key.data.size = 5.5

    bpy.ops.object.light_add(type="POINT", location=(2.7, -1.5, 3.0))
    fill = bpy.context.object
    fill.name = "GrowthStage_Fill_Light"
    fill.data.energy = 150
    fill.data.shadow_soft_size = 1.0


def main():
    ensure_dirs()
    clear_scene()
    build_stage()
    import_and_place_growth_stages()
    configure_scene()
    bpy.context.scene.render.filepath = str(OUTPUT)
    bpy.ops.render.render(write_still=True)
    print(f"SCREENSHOT={OUTPUT}")


if __name__ == "__main__":
    main()
