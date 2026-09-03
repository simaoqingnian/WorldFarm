import bpy


bpy.ops.preferences.addon_enable(module="blender_mcp")
print("WF_BLENDER_MCP_SMOKE_ADDON_ENABLED", hasattr(bpy.types, "blendermcp_server"))
