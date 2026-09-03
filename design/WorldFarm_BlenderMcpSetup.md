# WorldFarm Blender MCP 接入记录

记录日期：2026-09-01

本文记录当前开发机上 Codex 与 Blender MCP 的接入状态，用于后续参考图驱动建模流程。

## 1. 当前结论

Blender MCP 已完成最小可用接入验证：

- `uv/uvx` 已安装到 `C:\Users\Administrator\.local\bin`。
- Codex 已添加全局 MCP server：`blender`。
- Blender MCP addon 已安装到 Blender 5.2 用户插件目录。
- Blender addon 已启用，并保存到 Blender 用户配置。
- MCP server 已验证能连接到 Blender addon，并完成 `get_addon_info` 握手。
- Blender MCP 遥测已通过 Codex MCP 环境变量关闭，同时 Blender addon 偏好中也已关闭。

当前限制：

- 当前 Codex 会话启动时还没有加载这个 MCP server，所以本轮安装完成后通常需要重启 Codex/ChatGPT Desktop，新的 Blender MCP 工具才会出现在可调用工具列表中。
- Blender MCP addon 不能在 `blender --background` 模式启动 server。它需要普通 Blender GUI 进程，因为命令执行依赖 Blender 主循环。

## 2. 本机路径

```text
Blender:
F:\01_program_files\Blender\blender.exe

Blender MCP addon:
C:\Users\Administrator\AppData\Roaming\Blender Foundation\Blender\5.2\scripts\addons\blender_mcp.py

uv / uvx:
C:\Users\Administrator\.local\bin\uv.exe
C:\Users\Administrator\.local\bin\uvx.exe

Codex config:
C:\Users\Administrator\.codex\config.toml

WorldFarm smoke script:
tools\BlenderMcp\start_blender_mcp_smoke.py
```

## 3. Codex MCP 配置

通过命令添加：

```powershell
codex mcp add blender `
  --env DISABLE_TELEMETRY=true `
  --env BLENDER_HOST=localhost `
  --env BLENDER_PORT=9876 `
  --env UV_PYTHON_PREFERENCE=only-managed `
  -- C:\Users\Administrator\.local\bin\uvx.exe --python 3.11 blender-mcp
```

写入后的关键配置：

```toml
[mcp_servers.blender]
command = 'C:\Users\Administrator\.local\bin\uvx.exe'
args = ["--python", "3.11", "blender-mcp"]

[mcp_servers.blender.env]
BLENDER_HOST = "localhost"
BLENDER_PORT = "9876"
DISABLE_TELEMETRY = "true"
UV_PYTHON_PREFERENCE = "only-managed"
```

验证命令：

```powershell
codex mcp list
codex mcp get blender
```

## 4. Blender 侧验证

Blender MCP 官方 addon 支持自动启动 server，但不能在后台模式启动。后台模式会输出：

```text
BlenderMCP: cannot start server in background mode (blender -b) - commands would never execute
```

可用方式是启动普通 Blender GUI 进程，并加载插件：

```powershell
F:\01_program_files\Blender\blender.exe --python tools\BlenderMcp\start_blender_mcp_smoke.py
```

smoke 脚本内容只做一件事：

```python
import bpy

bpy.ops.preferences.addon_enable(module="blender_mcp")
print("WF_BLENDER_MCP_SMOKE_ADDON_ENABLED", hasattr(bpy.types, "blendermcp_server"))
```

验证结果：

```text
BlenderMCP server started on localhost:9876
WF_BLENDER_MCP_SMOKE_ADDON_ENABLED True
```

## 5. MCP server 握手验证

在 Blender GUI 进程已启动并监听 `9876` 后，启动 Codex 配置中的 MCP server 命令：

```powershell
C:\Users\Administrator\.local\bin\uvx.exe --python 3.11 blender-mcp
```

验证日志中出现：

```text
Connected to Blender at localhost:9876
Created new persistent connection to Blender
Sending command: get_addon_info
Response parsed, status: success
Blender addon up to date (protocol 5, addon [1, 6], Blender 5.2.1 LTS)
Successfully connected to Blender on startup
```

这说明：

- Codex MCP server 可以启动。
- MCP server 能连接 Blender addon。
- Blender addon 协议版本匹配。
- 本机接入链路是有效的。

## 6. 使用流程

后续使用 Blender MCP 建模时，建议按以下步骤：

1. 启动普通 Blender，不使用 `--background`。
2. 确认 Blender MCP addon 已启用。
3. 确认 `BlenderMCP` 面板显示 server 正在 `localhost:9876` 运行。
4. 重启 Codex/ChatGPT Desktop，让新的 `blender` MCP server 被加载。
5. 在 Codex 中检查是否出现 Blender MCP 工具。
6. 用最小命令测试：获取场景信息、创建 cube、设置材质、保存 blend。
7. 测试通过后，再进入 WorldFarm 作物建模流程。

## 7. 对 WorldFarm 建模流程的影响

Blender MCP 接入后，参考图建模流程应调整为：

```text
参考图
-> 结构拆解
-> 轮廓控制点
-> Blender MCP 直接创建/修改场景
-> 固定四视角渲染
-> 参考图并排对比
-> 自检报告
-> 用户反馈
-> FBX/GLB 导出
-> Unity/Tuanjie AssetPreview
-> Android 实机预览
```

收益：

- 可以减少反复手写 Blender Python 脚本的成本。
- 可以更直接地检查和修改 Blender 场景里的对象。
- 更适合做局部修正，例如顶部连接、叶簇角度、材质和相机。

不能解决的问题：

- MCP 不会自动保证模型和参考图一致。
- 仍然需要轮廓检查、视角对比、用户反馈和版本锁定。
- 最终 Unity 手机效果仍需在 AssetPreview 和 APK 中验证。

## 8. 安全约束

Blender MCP 提供执行 Blender Python 代码的能力，权限较高。WorldFarm 项目中使用时遵守以下约束：

- 只连接本机 `localhost:9876`。
- 默认关闭遥测。
- 默认不启用 Poly Haven、Sketchfab、Poly Pizza、Hyper3D、Hunyuan3D 等外部资源下载能力，除非明确需要。
- 导入第三方模型前确认授权，优先使用自制资产或 CC0 资产。
- 正式资产进入 Unity 前保留源文件、导出文件、预览图和反馈记录。

## 9. 后续待办

下一步不是继续直接改胡萝卜，而是先完成 MCP 工具在 Codex 会话中的可调用验证：

```text
重启 Codex/ChatGPT Desktop
-> 确认 blender MCP 工具出现
-> 通过 MCP 获取 Blender 场景
-> 通过 MCP 创建简单模型
-> 通过 MCP 保存 .blend
-> 再开始参考图胡萝卜重建 v002
```

