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

### Notes For Continuing On Another Computer

- Pull the repository.
- Open `UnityProject` with Tuanjie/Unity compatible with the current project version.
- Do not rely on Unity `Library` because it is generated and ignored.
- If APKs need testing, rebuild them from the editor bootstrap methods instead of relying on old local build outputs.
