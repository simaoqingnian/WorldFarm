# WorldFarm

WorldFarm 是一个准备开发为农场类游戏的 Android App 工程。当前工程复用 `D:/03_work/09_android/FilmWord` 下的本地 JDK、Android SDK 和 Gradle wrapper 配置，业务代码为独立包名 `com.worldfarm.app`。

## 当前内容

- 原生 Android Java 单 Activity 工程
- App 名称：WorldFarm
- 首页雏形：中国初始农田、世界地图解锁入口、重叠作物规则提示
- 游戏设计文档：[design/WorldFarm_GameDesign.md](design/WorldFarm_GameDesign.md)
- 地貌与变异设计：[design/WorldFarm_BiomeMutationDesign.md](design/WorldFarm_BiomeMutationDesign.md)
- 国家与地貌设计：[design/WorldFarm_CountryBiomeDesign.md](design/WorldFarm_CountryBiomeDesign.md)

## 构建

```powershell
.\gradlew.bat assembleDebug
```
