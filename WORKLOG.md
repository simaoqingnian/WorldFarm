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

### Notes For Continuing On Another Computer

- Pull the repository.
- Open `UnityProject` with Tuanjie/Unity compatible with the current project version.
- Do not rely on Unity `Library` because it is generated and ignored.
- If asset previews need testing, rebuild APKs from the editor bootstrap methods instead of using old local build outputs.
