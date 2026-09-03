import math
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / "source" / "Nature Crops Pack - Jan 2020" / "FBX"
SCREENSHOT_DIR = ROOT / "screenshots"
OUTPUT = SCREENSHOT_DIR / "quaternius_first5_worldfarm_preview.png"

CROPS = (
    ("Carrot_4", "Carrot_4.fbx", -2.15, 0.86),
    ("Corn_4", "Corn_4.fbx", -1.05, 1.30),
    ("Wheat_4", "Wheat_4.fbx", 0.05, 1.18),
    ("Rice_4", "Rice_4.fbx", 1.08, 1.12),
    ("Lettuce_4", "Lettuce_4.fbx", 2.05, 0.82),
)

MAX_PREVIEW_FOOTPRINT = 0.72


def ensure_dirs():
    SCREENSHOT_DIR.mkdir(parents=True, exist_ok=True)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, roughness=0.55, specular=0.30):
    material = bpy.data.materials.new(name)
    material.diffuse_color = color
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        if "Base Color" in bsdf.inputs:
            bsdf.inputs["Base Color"].default_value = color
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = specular
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = specular
    return material


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


def normalize_imported(objects, slot_x, target_height):
    bounds = get_bounds(objects)
    if bounds is None:
        return

    min_point, max_point = bounds
    height = max(max_point.z - min_point.z, 0.001)
    footprint = max(max_point.x - min_point.x, max_point.y - min_point.y, 0.001)
    scale = min(target_height / height, MAX_PREVIEW_FOOTPRINT / footprint)

    for obj in objects:
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


def import_crop(name, filename, slot_x, target_height):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(SOURCE / filename))
    imported = [obj for obj in bpy.data.objects if obj not in before]

    for obj in imported:
        obj.name = f"{name}_{obj.name}"
        if obj.type != "MESH":
            continue
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_flat()
        obj.select_set(False)
        for material in obj.data.materials:
            if material:
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

    normalize_imported(imported, slot_x, target_height)


def add_cylinder(name, radius, depth, location, material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()
    return obj


def build_stage():
    stage_material = make_material("Mat_QuaterniusPreview_Stage", (0.60, 0.70, 0.57, 1.0), 0.62, 0.12)
    platform_material = make_material("Mat_QuaterniusPreview_Platform", (0.72, 0.58, 0.39, 1.0), 0.55, 0.16)
    platform_dark_material = make_material("Mat_QuaterniusPreview_PlatformSide", (0.43, 0.31, 0.20, 1.0), 0.66, 0.10)

    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, -0.055))
    stage = bpy.context.object
    stage.name = "WorldFarm_QuaterniusCandidate_Stage"
    stage.scale = (2.85, 0.72, 0.035)
    stage.data.materials.append(stage_material)

    for name, _filename, slot_x, _height in CROPS:
        add_cylinder(f"Slot_{name}_Top", 0.31, 0.055, (slot_x, 0.0, 0.010), platform_material)
        add_cylinder(f"Slot_{name}_Side", 0.325, 0.045, (slot_x, 0.0, -0.033), platform_dark_material)


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

    scene.render.resolution_x = 1800
    scene.render.resolution_y = 1000
    scene.world = bpy.data.worlds.new("WorldFarm_QuaterniusCandidate_World")
    scene.world.color = (0.68, 0.82, 0.84)
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0

    bpy.ops.object.camera_add(location=(3.55, -5.60, 3.30))
    camera = bpy.context.object
    camera.name = "Camera_QuaterniusCandidatePreview"
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 5.05
    look_at(camera, (0.0, 0.0, 0.72))
    scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-2.6, -3.8, 4.8))
    key = bpy.context.object
    key.name = "QuaterniusPreview_Key_Light"
    key.data.energy = 520
    key.data.size = 5.2

    bpy.ops.object.light_add(type="POINT", location=(2.7, -2.0, 2.6))
    fill = bpy.context.object
    fill.name = "QuaterniusPreview_Fill_Light"
    fill.data.energy = 120
    fill.data.shadow_soft_size = 1.0


def main():
    ensure_dirs()
    clear_scene()
    build_stage()
    for crop in CROPS:
        import_crop(*crop)
    configure_scene()
    bpy.context.scene.render.filepath = str(OUTPUT)
    bpy.ops.render.render(write_still=True)
    print(f"SCREENSHOT={OUTPUT}")


if __name__ == "__main__":
    main()
