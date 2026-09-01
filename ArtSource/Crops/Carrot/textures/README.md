# Carrot Textures

本目录保存胡萝卜正式模型的程序化贴图源结果。

当前验收版本：`r03`

文件：

- `Carrot_Skin_Base_r03.png`：主体环绕表皮贴图，使用完整圆柱 UV，避免只有单面有纹理。
- `Carrot_Top_Crown_r03.png`：顶部冠部贴图，用于解决顶部纯色光滑问题。
- `Carrot_Leaf_Stem_r03.png`：叶茎贴图，用于保留叶茎纵向层次。

生成方式：

- 由 `../blender/create_carrot_model_r03.py` 程序化生成。
- 纹理参数固定随机种子，结果可复现。
- 后续如果反馈“太重、太淡、太密、太脏、像南瓜沟槽”，优先修改脚本参数再重新生成，不手工涂改 PNG。
