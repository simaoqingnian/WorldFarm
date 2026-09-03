# WorldFarm Work Log

## 2026-09-03

### Current Direction

WorldFarm is now planned as a Unity/Tuanjie Android farming game. The current priority is to validate gameplay systems before investing further in final art, polished UI, or custom 3D crop modeling.

Recommended prototype direction:

- Build a text/control driven gameplay prototype inside Unity.
- Use simple UI controls, tables, buttons, logs, and test time controls.
- Skip final terrain art, country map visuals, crop models, animation, audio, and polished interface for now.
- Verify whether planting, harvesting, orders, country reputation, mutation, processing, certification, and breeding create a useful gameplay loop.

### Environment

- Project root: `D:\03_work\09_android\WorldFarm`
- Unity/Tuanjie project: `UnityProject`
- Tuanjie editor path used locally: `F:\01_program_files\2022.3.62t14\Editor\Tuanjie.exe`
- Git remote: `git@github.com:simaoqingnian/WorldFarm.git`
- Repository git author: `simaoqingnian <simaoqingnian@163.com>`

### Design Documents

Current core design documents:

- `design/WorldFarm_GameDesign.md`
- `design/WorldFarm_BiomeMutationDesign.md`
- `design/WorldFarm_CountryBiomeDesign.md`
- `design/WorldFarm_EconomyProcessingBreedingDesign.md`
- `design/WorldFarm_ArtPrototypeDevelopmentRoute.md`
- `design/WorldFarm_3DAssetPipeline.md`
- `design/WorldFarm_ControlledModelingWorkflow.md`
- `design/WorldFarm_QuaterniusCropCatalog.md`
- `design/WorldFarm_ThirdPartyAssetLibraryEvaluation.md`
- `design/WorldFarm_BlenderMcpSetup.md`

Recent design decisions:

- Landforms are not hard crop locks. They are rule containers made from base biome type, environment attributes, terroir tags, and signature crop resonance.
- A named biome such as Bordeaux Vineyard can allow all crops, while grapes receive special resonance and other crops receive Bordeaux-style terroir mutation opportunities.
- Crop adaptation is split into species/category adaptation and concrete variety adaptation.
- Stable mutations do not mutate again through normal planting. Further evolution goes through the breeding system.
- Secondary breeding uses stable mutation varieties as parents and supports trait combinations, side branches, unstable seeds, rare breakthroughs, and failure compensation.
- Economy uses one unified order system with multiple channels: daily, country, specialty, processing, research, and event orders.
- Processing uses a small number of building types. Country flavor is expressed through skins, modules, and recipe packs instead of unlimited independent factories.
- Country reputation is per-country deep progression. World exploration points unlock new countries; country reputation unlocks deeper content inside an already unlocked country.
- Development route is gameplay-first: text/control prototype, then color-block map, then CC0 visualization, then style unification, then AI/Blender and outsourcing for selected assets.

### Art And Asset Status

- A custom carrot asset pipeline was tested and documented, but the current plan is not to continue polishing individual crop models before gameplay is proven.
- Quaternius Ultimate Crops Pack has been imported as temporary prototype crop art.
- A crop catalog document lists available growth-stage, harvested, and crop item FBX files.
- Prototype preview scenes exist for third-party crop inspection, but the latest growth-stage preview build was interrupted and should be rebuilt before relying on that APK.

### Next Engineering Target

Build `M0: Gameplay System Prototype`.

First implementation target:

- `PrototypeGameplay.unity`
- Text/control based prototype UI.
- Countries, biomes, plot slots, crops, inventory, time controls, planting, harvesting, adaptation-based yield, basic orders, country reputation, and event log.

Later prototype layers:

- Processing queue and recipes.
- Specialty certification.
- Mutation clues and stable varieties.
- Secondary breeding with environment recipes and result branches.

### M0 Gameplay Prototype Implementation

Implemented a first text/control driven Unity prototype for gameplay validation.

Added:

- `UnityProject/Assets/WorldFarm/Scenes/PrototypeGameplay.unity`
- `UnityProject/Assets/WorldFarm/Scripts/Prototype/PrototypeGameplayScene.cs`
- `WorldFarm/Build Prototype Gameplay APK` editor menu command.
- Unity built-in module dependencies for IMGUI and JSON serialization.

Current playable systems:

- Local-time crop growth and maturity calculation.
- Test time controls for fast validation: +1 minute, +10 minutes, and mature all.
- Country unlock flow using world exploration points.
- Country reputation as per-country progression.
- Country biomes and multiple plot slots per biome.
- Crop planting with biome adaptation coefficients.
- Adaptation-based maturity speed and yield range.
- Harvesting into inventory.
- Repeatable orders with coin, experience, exploration point, research point, and country reputation rewards.
- Basic inventory selling.
- Runtime event log.

Current prototype content:

- Countries: China, Japan, France.
- Biomes: North China Dry Plain, Jiangnan Paddy Field, Southwest Terrace, Vegetable Loam, Snow Country Paddy Field, Uji Tea Hill, Bordeaux Vineyard.
- Crops: Chinese wheat, Chinese rice, Chinese cabbage, Chinese corn, Chinese tea, Japanese rice, Japanese tea, Bordeaux grape.

Verification:

- Tuanjie batch Bootstrap completed without C# compiler errors after enabling `com.unity.modules.imgui` and `com.unity.modules.jsonserialize`.
- Android development APK built successfully at `Builds\Android\WorldFarm-prototype-gameplay.apk`.
- Build artifact size: about 24.5 MB.

Known prototype limits:

- UI is intentionally plain IMGUI, not final production interface.
- Mutation, processing, certification, and secondary breeding are still design-documented only and should be added in the next prototype layer.
- Current values are first-pass test values and should be tuned after several play loops.

### M0.1 First Mutation Prototype

Implemented the first playable mutation loop in the text/control prototype.

Added:

- Larger touch buttons and larger IMGUI button padding for mobile testing.
- A new `变异 / Mutations` tab.
- First-level mutation rules driven by base crop plus trigger biome.
- Mutation clue inventory items.
- Stable mutation crop unlock state.
- Stable mutation crops as independent plantable crops and independent inventory stacks.
- Debug `+1 clue` button for fast prototype validation.

Current first-level mutation rules:

- 中国水稻 + 华北旱田 -> 耐旱稻
- 中国小麦 + 江南水田 -> 水麦芽
- 中国玉米 + 江南水田 -> 湿地玉米
- 中国白菜 + 丘陵梯田 -> 雾叶白菜
- 日本粳米 + 雪融水田 -> 雪泉稻
- 日本茶叶 + 山麓茶园 -> 影香茶
- 法国葡萄 + 波尔多葡萄园 -> 海雾葡萄

Current behavior:

- Harvesting a base crop in its mutation trigger biome rolls for a mutation clue.
- Normal crop yield is still granted even when a clue drops.
- Mutation clues are listed separately in inventory and cannot be sold as crops.
- The Mutations tab shows clue count, research point cost, trigger biome, and current chance.
- Stabilizing a mutation consumes clues and research points, then unlocks the stable crop.
- Stable mutation crops do not naturally mutate again.
- Stable mutation crops have their own biome affinities, including penalties when returning to some original high-adaptation landforms.
- Early country orders now grant a small amount of research points so mutation stabilization can be tested before the full research system exists.

Verification:

- Tuanjie batch Bootstrap completed without C# compiler errors.
- Android development APK rebuilt successfully at `Builds\Android\WorldFarm-prototype-gameplay.apk`.

### Art And Prototype Development Route

Documented the long-term low-barrier art and prototype route in `design/WorldFarm_ArtPrototypeDevelopmentRoute.md`.

Fixed direction:

- Continue gameplay-first development before final UI and final 3D assets.
- Use text/control prototypes for systems that are still changing.
- Use color blocks and simple geometry for the first map and landform interaction pass.
- Use CC0 libraries such as Quaternius and Kenney as prototype and early visualization assets, with license records.
- Use Unity/Tuanjie lighting, materials, scale, and camera rules to unify mixed-source assets.
- Use AI 3D tools only for selected custom assets after checking commercial rights.
- Use Blender to clean AI or third-party models before importing into Unity/Tuanjie.
- Delay outsourcing until gameplay scope and style rules are stable.
- Delay Substance-style heavy texture production until the project has enough stable content to justify it.

### M0.2 3D Placeholder Gameplay Prototype

Implemented a 3D placeholder gameplay scene to replace the pure text/control feel for the next prototype pass.

Added:

- New startup/debug scene `Assets/WorldFarm/Scenes/Prototype3DGameplay.unity`.
- New runtime scene driver `Assets/WorldFarm/Scripts/Runtime/Prototype3DGameplayScene.cs`.
- Simple 3D placeholder world layout with farm plots, country unlock pads, biome tiles, seed rack, warehouse, order board, mutation shed, and time controls.
- TextMesh labels attached to major 3D placeholders so temporary models are readable on device, such as `仓库`, `订单牌`, `变异棚`, crop names, biome names, and action buttons.
- Touch/click interaction through 3D raycast targets instead of only IMGUI buttons.
- Camera drag panning and two-finger pinch zoom for mobile testing.
- Primitive crop placeholders for rice, wheat, cabbage, corn, tea, grape, and several stable mutation variants.
- The M0.1 crop loop remains available conceptually: planting, local-time maturity, harvesting, orders, research points, mutation clues, and stable first-level mutation unlocks.

Build/runtime notes:

- The 3D placeholder scene is now first in Editor Build Settings and is the default bootstrap/debug launch scene.
- Added the built-in Physics module because the prototype uses colliders and raycasts.
- Built Android development APK at `Builds/Android/WorldFarm-prototype-3d.apk`.
- This APK is intentionally committed for this handoff even though build outputs are normally ignored.

### Notes For Continuing On Another Computer

- Pull the repository.
- Open `UnityProject` with Tuanjie/Unity compatible with the current project version.
- Do not rely on Unity `Library` because it is generated and ignored.
- If APKs need testing, rebuild them from the editor bootstrap methods instead of relying on old local build outputs.
