# WorldFarm 第三方作物模型库评估

记录日期：2026-09-02

## 目标

为 WorldFarm 寻找可免费商用、可修改、适合 Unity/Tuanjie 导入的 3D 作物或农场相关资产库。

这些资产当前只作为候选库、占位库和风格对比库，不直接等同于 WorldFarm 最终美术资产。正式资产仍以项目自己的“轻写实、圆润、卡通农场 3D”规范为准。

## 当前优先候选

### Quaternius Ultimate Crops Pack

来源：

- 官方页：https://quaternius.com/packs/ultimatecrops.html
- OpenGameArt 镜像：https://opengameart.org/content/lowpoly-crops-pack

公开信息：

- 内容：100+ 作物模型，包含 5 阶段生长。
- 格式：FBX、OBJ、Blend。
- 授权：CC0 / Public Domain。

适合用途：

- 早期玩法 Demo 的作物占位。
- 快速补齐多国家、多作物、多阶段的原型资源。
- 作为 Blender 批量改造的基础网格。
- 作为“低多边形库”和 WorldFarm 自制资产之间的风格对比样本。

不适合直接作为最终美术的原因：

- 低多边形棱角明显，和当前胡萝卜流程确定的圆润方向有差距。
- 部分模型比例偏细、偏尖，手机小屏识别度需要复测。
- 导入 Blender 时部分 FBX 材质 Alpha 为 0，需要在预览脚本中强制不透明。
- 资产整体更像通用低模素材包，不具备 WorldFarm 后续需要的地貌变异特色。

本轮已导入测试资产：

- `Carrot_4.fbx`
- `Corn_4.fbx`
- `Wheat_4.fbx`
- `Rice_4.fbx`
- `Lettuce_4.fbx`

完整作物清单见：`design/WorldFarm_QuaterniusCropCatalog.md`

本地路径：

- 原始包：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/`
- 授权记录：`ArtSource/ThirdParty/_licenses/quaternius_nature_crops_pack.md`
- Unity 测试资源：`UnityProject/Assets/WorldFarm/Resources/AssetPreview/ThirdParty/Quaternius/UltimateCrops/`
- Unity 预览场景：`UnityProject/Assets/WorldFarm/Scenes/ThirdPartyCropPreview.unity`
- Android 测试 APK：`Builds/Android/WorldFarm-thirdparty-crops.apk`
- Blender 候选图：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/screenshots/quaternius_first5_worldfarm_preview.png`

## 其他候选

### Kenney Food Kit

来源：https://kenney.nl/assets/food-kit

公开信息：

- 内容：200 个 3D 食物相关模型。
- 授权：Creative Commons CC0。

适合用途：

- 仓库、背包、订单、餐食、加工品、市场摊位等系统。
- 收获物图标或简化 3D 道具。

不作为第一批田间作物库的原因：

- 主要是食物和厨房相关模型，不是完整的田间生长阶段。
- 对“作物种植、成熟、变异”的帮助低于 Quaternius 作物包。

### Quaternius Nature / Stylized Nature 系列

来源：

- https://quaternius.com/
- https://quaternius.com/packs/ultimatestylizednature.html

适合用途：

- 地貌装饰物、树木、草丛、石头、地图边界、国家版图环境。
- 早期国家地块的视觉填充。

不作为第一批作物库的原因：

- 重点是自然环境，不是作物生长阶段。

## 当前结论

第一批实际接入 Quaternius Ultimate Crops Pack 是合理的：它的授权风险低、格式齐全、作物数量多、带生长阶段，最适合快速验证 WorldFarm 的多作物预览和手机端表现。

但它不能直接替代正式资产流程。WorldFarm 最终资产建议继续采用以下路线：

1. 第三方库用于原型和对比。
2. 通过 Unity 预览 APK 在手机上筛选“轮廓可读性”和“尺寸适配”。
3. 对通过筛选的第三方基础模型进入 Blender 批量风格改造。
4. 改造标准统一为：更圆润、更少硬折角、更清晰的大形、更温和的颜色、更适合手机小屏。
5. 对核心作物仍使用正式可控建模流程，从轮廓确认、灰模、材质、Unity 预览逐步锁定。

## 后续建议流程

下一步不要一次性导入所有 102 个模型。更稳妥的方式是：

1. 先从 Quaternius 中选 15 到 20 个作物成熟期模型。
2. 生成同一视角的 Blender 候选图。
3. 做一个 Unity 手机预览 APK。
4. 按“可直接用 / 可改造 / 只做参考 / 淘汰”四类标记。
5. 只对“可直接用”和“可改造”的模型继续投入处理时间。

筛选标准：

- 第一眼是否能认出作物。
- 手机屏幕上是否过细、过尖、过暗。
- 与 WorldFarm 胡萝卜 v001 的圆润程度是否冲突。
- 是否能较低成本改造成项目统一风格。
- 是否能为国家地貌、变异品种提供足够辨识点。

## 圆润化改造流程

第三方低模作物不能直接整模型一键平滑。WorldFarm 的目标不是把模型变得“模糊”，而是保留作物第一眼识别度，同时减少硬折角、尖刺感和手机端的杂乱细线。

推荐流程如下：

1. 方向和原点归一化

先统一模型朝向、缩放和底部原点。Quaternius FBX 在 Unity/Tuanjie 中需要从资源 Z-up 修正到 Unity Y-up，否则会横向躺在地块上。

输出要求：

- 作物竖直生长方向为 Unity `Y+`。
- 地面接触点在局部坐标 `Y=0`。
- 模型中心对齐地块中心。
- 成熟期作物高度进入统一范围，矮胖作物还要限制最大横向占地。

2. 按部件分类，不同部件不同圆润化策略

不能对根茎、叶片、穗粒、果实使用同一种处理。

根茎类主体，例如胡萝卜、甜菜、萝卜：

- 适合增加截面段数。
- 适合轻度 subdivision。
- 适合 bevel 边缘和 weighted normal。
- 重点修轮廓，让大形从多边形变成圆润锥体或圆润块体。

果实和球体类，例如南瓜、西瓜、卷心菜、生菜：

- 适合 smooth normal、bevel、局部膨胀。
- 需要保留大块结构，不要把叶片层次全部磨平。
- 手机端优先保证大轮廓清楚，而不是保留所有原始折面。

细杆和叶茎类，例如玉米秆、水稻秆、小麦秆：

- 不建议只做平滑，因为原始低模杆太细，平滑后仍然像线。
- 更适合替换为少量更粗、更圆的卡通茎杆。
- 茎杆数量要减少，轮廓要更稳定。

叶片类：

- 不建议全局 subdivision，容易让叶片变成软塌形状。
- 适合重建为少量弧形叶片，叶尖圆一点，边缘有厚度。
- 每株作物只保留最有识别度的几片主叶。

穗粒类，例如水稻、小麦：

- 不建议保留大量尖细粒。
- 适合合并成更大的粒组或穗块。
- 粒组边缘圆润，颜色更亮，避免手机端变成杂乱线条。

3. 统一 WorldFarm 材质

第三方材质只保留颜色参考，不保留最终参数。

统一规则：

- 非金属。
- 中高粗糙度。
- 低高光。
- 暖色更柔和，绿色分 2 到 3 层。
- 不使用过多真实纹理细节，优先使用大色块、轻微色带和少量可读纹理。

4. 每批只改 5 到 8 个模型

先做一轮“自动圆润化 pass”，生成前后对比图和 APK。只要某个模型自动改造后仍然不好看，就不要继续花时间自动修，直接进入正式可控建模流程。

每个模型评审结论分四类：

- 可直接用：只需要统一材质和比例。
- 可改造：轮廓还行，适合继续圆润化。
- 只做参考：题材有用，但模型结构不适合改。
- 淘汰：手机端不可读或风格偏差太大。

5. 第一轮建议

先从 Quaternius 已导入的 5 个成熟期模型开始：

- 胡萝卜：只作为对比，不替代已通过的 WorldFarm 自制胡萝卜。
- 玉米：测试细杆重建和叶片简化。
- 小麦：测试穗粒合并。
- 水稻：测试细线减少和稻穗块状化。
- 生菜：测试矮胖作物的圆润大形。

这轮目标不是做最终资产，而是验证“第三方低模能不能通过批处理变成 WorldFarm 可用底稿”。如果这 5 个里只有 1 到 2 个能改好，后续第三方库只做占位和参考；如果 3 个以上效果接近目标，再扩大到 15 到 20 个作物。

## Round Pass 01 结果

生成日期：2026-09-02

本轮不是直接平滑 Quaternius 原始网格，而是采用“第三方题材参考 + WorldFarm 轻量重塑”的方式，优先验证低成本批量改造方向。

输出路径：

- Blender 批处理脚本：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/create_worldfarm_round_pass01.py`
- Blend 源文件：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/worldfarm_round_pass_01/blends/`
- FBX 输出：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/worldfarm_round_pass_01/fbx/`
- Blender 预览图：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/worldfarm_round_pass_01/screenshots/worldfarm_round_pass01_preview.png`
- Unity 导入目录：`UnityProject/Assets/WorldFarm/Resources/AssetPreview/ThirdParty/Quaternius/WorldFarmRoundPass01/`
- Android 预览 APK：`Builds/Android/WorldFarm-thirdparty-crops.apk`

本轮处理策略：

- 胡萝卜：保留根茎大形，重建圆润主体、少量生长痕和叶簇连接。
- 玉米：减少碎叶，改为粗茎、少量大叶、两个可读玉米棒和顶部穗簇。
- 小麦：把原始尖细小麦改成较粗的茎杆和更大的穗粒组。
- 水稻：减少密集细线，改成竖向秆组和较大的浅色稻粒组。
- 生菜：从低模球状叶团改成圆润层叠叶座。

当前自评：

- 胡萝卜：只能作为第三方改造对比，仍不替代已通过的 WorldFarm 自制胡萝卜。
- 玉米：比原始低模更统一，但顶部穗和主茎还偏简单，后续可继续做 pass02。
- 小麦：手机端可读性应该比原始模型强，但目前更像“谷穗束”，自然感不足。
- 水稻：减少了碎线问题，但粒组偏圆，可能会显得像装饰植物，需要手机端确认。
- 生菜：圆润方向较接近 WorldFarm，可作为第三方改造可行性的重点观察对象。

下一步验收重点：

- 先在手机上确认五个模型是否仍然全部直立。
- 检查是否比 Quaternius 原始版更圆润、更卡通、更容易识别。
- 重点看玉米、水稻、小麦是否从“细线”变成了可读作物，而不是变成抽象装饰。
- 如果 5 个里至少 3 个方向通过，再进入 pass02；否则第三方库只保留为占位和结构参考。

## Round Pass 02 结果

生成日期：2026-09-02

基于用户对 pass01 的反馈：

- 整体方向通过。
- 胡萝卜、玉米最满意。
- 小麦、水稻最不满意。
- 圆润度合适，卡通程度合适。
- Unity 手机预览里模型再次平躺。
- 整体显示略小。
- 小麦、水稻茎杆过直，需要一两根轻微弧度。

本轮修正：

- 新增 pass02 入口脚本：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/create_worldfarm_round_pass02.py`
- pass02 输出目录：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/worldfarm_round_pass_02/`
- pass02 预览图：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/worldfarm_round_pass_02/screenshots/worldfarm_round_pass02_preview.png`
- Unity 导入目录：`UnityProject/Assets/WorldFarm/Resources/AssetPreview/ThirdParty/Quaternius/WorldFarmRoundPass02/`
- Android 预览 APK：`Builds/Android/WorldFarm-thirdparty-crops.apk`

技术调整：

- Blender FBX 导出轴向改为 Unity/Tuanjie 更标准的 `axis_forward="-Z"`、`axis_up="Y"`。
- Unity 预览脚本切换到 pass02 资源。
- Unity 预览脚本取消针对原始 Quaternius FBX 的 `-90` 度额外旋转。
- Unity 预览目标高度和最大横向占地放大，镜头略微拉近。
- 小麦、水稻的部分茎杆由直线圆柱改为轻微弧线分段圆柱。

pass02 验收重点：

- 五个模型在手机中是否恢复直立。
- 放大后是否仍然不拥挤、不裁切。
- 小麦、水稻的弧度是否更自然。
- 胡萝卜、玉米是否保持 pass01 的满意方向。
- 生菜是否仍然像地块上直立生长的叶菜，而不是横向摆件。

用户对 pass02 的补充反馈：

- 圆润化整体方向通过，粗细和卡通程度合适。
- 胡萝卜、玉米方向较满意。
- 小麦、水稻需要继续改善自然弧度。
- 玉米顶部穗与主杆衔接生硬，pass02 自评时遗漏了这个问题。
- 多模型展示必须检查屏幕边框，右侧模型不能出屏；一行展示不下时优先多行布局。

后续所有作物预览必须增加两个检查项：

- 部件连接检查：叶簇、穗、果实、茎杆之间不能悬浮、硬插或突然断开。
- 安全边框检查：桌面图和手机 APK 都必须留出屏幕边距，宁可多行展示，也不把模型挤出画面。

## 原始生长阶段预览

生成日期：2026-09-02

为查看 Quaternius 素材库自带的生长阶段，已导入 5 个作物的 `1-4` 阶段原始 FBX：

- 胡萝卜：`Carrot_1.fbx` 到 `Carrot_4.fbx`
- 玉米：`Corn_1.fbx` 到 `Corn_4.fbx`
- 小麦：`Wheat_1.fbx` 到 `Wheat_4.fbx`
- 水稻：`Rice_1.fbx` 到 `Rice_4.fbx`
- 生菜：`Lettuce_1.fbx` 到 `Lettuce_4.fbx`

展示规则：

- 4 列：从左到右是阶段 1、阶段 2、阶段 3、阶段 4。
- 5 行：从上到下是胡萝卜、玉米、小麦、水稻、生菜。
- 使用多行布局，不再把所有作物挤在一行。
- 原始 Quaternius FBX 在 Unity/Tuanjie 中仍需 `-90` 度轴向修正。
- 阶段预览按每个作物的成熟期比例统一缩放，保留同一作物 1-4 阶段的相对大小。

输出路径：

- Blender 阶段总览脚本：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/render_quaternius_growth_stage_grid.py`
- Blender 阶段总览图：`ArtSource/ThirdParty/Quaternius/UltimateCropsPack/screenshots/quaternius_growth_stage_grid.png`
- Unity 阶段资源目录：`UnityProject/Assets/WorldFarm/Resources/AssetPreview/ThirdParty/Quaternius/GrowthStagesOriginal/`
- Unity 阶段预览脚本：`UnityProject/Assets/WorldFarm/Scripts/Runtime/ThirdPartyGrowthStagePreviewScene.cs`
- Unity 阶段预览场景：`UnityProject/Assets/WorldFarm/Scenes/ThirdPartyGrowthStagePreview.unity`
- Android 阶段预览 APK：`Builds/Android/WorldFarm-thirdparty-growth-stages.apk`

2026-09-02 更新：

- 阶段预览已从 5 个作物扩展到全部 18 个作物。
- Unity 资源目录中包含全部 `作物名_1.fbx` 到 `作物名_4.fbx`，共 72 个阶段模型。
- APK 场景改为纵向可滑动浏览器，避免全部模型挤在一屏导致边缘裁切。
- 当前作物顺序以 `design/WorldFarm_QuaterniusCropCatalog.md` 为准。
