# 普通胡萝卜 v001 正式模型验收

## 文件

- Blender 源文件：`carrot_normal_v001_model.blend`
- 生成脚本：`create_carrot_model_v001.py`
- FBX 导出：`../exports/Crop_Carrot_Normal_v001_model.fbx`
- Unity 预览资源：`../../../UnityProject/Assets/WorldFarm/Resources/AssetPreview/Carrot/Crop_Carrot_Normal_v001_model.fbx`
- 截图：
  - `../screenshots/v001_model_front.png`
  - `../screenshots/v001_model_side.png`
  - `../screenshots/v001_model_three_quarter.png`
  - `../screenshots/v001_model_top.png`

## 已锁定基础体块

- 主体：平滑倒圆锥，P4 附近最宽。
- 顶部：叶茎从橙色顶部中心区域长出。
- 叶簇：4 根卡通直立粗叶茎。
- 叶簇末端：无球盖、无小突起，使用钝端。
- 底部：窄而圆润的钝头。
- 侧面：加厚后通过。

## 本阶段只看

- 基础材质是否符合“轻写实圆润农场 3D”。
- 胡萝卜主体颜色是否温暖、明亮、不过脏。
- 短生长痕是否增加质感，同时不形成环形凸带。
- 叶茎颜色层次是否自然。
- Unity 预览里 model 版是否比 blockout 版更适合作为正式资产基础。

## 本阶段不看

- 多成长阶段。
- 变异品种。
- 图标。
- 收获动画。
- 复杂贴图。
- 高精雕刻。

## 反馈格式

```text
主体材质：通过 / 太亮 / 太暗 / 太塑料 / 太脏 / 不够温暖
生长痕：通过 / 太多 / 太少 / 太深 / 太浅 / 仍像环带 / 位置不自然
叶茎材质：通过 / 太亮 / 太暗 / 层次太少 / 风格不一致
Unity 预览：通过 / 太大 / 太小 / 太亮 / 太暗 / 旋转不好看
是否锁定普通胡萝卜 v001：是 / 否
```
