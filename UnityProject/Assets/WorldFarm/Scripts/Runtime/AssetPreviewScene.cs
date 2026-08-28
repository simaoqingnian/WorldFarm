using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorldFarm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AssetPreviewScene : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedAssetPreview";
        private const float CarrotTopY = 0.98f;
        private const float CarrotHeight = 2.72f;

        [SerializeField] private float autoRotateDegreesPerSecond = 10f;
        [SerializeField] private float dragDegreesPerPixel = 0.18f;

        private readonly List<PreviewTurntable> turntables = new List<PreviewTurntable>();
        private float yaw = -18f;
        private bool dragging;
        private int activeTouchId = -1;
        private Vector2 previousPointerPosition;

        private struct PreviewTurntable
        {
            public readonly Transform Pivot;
            public readonly float OffsetDegrees;
            public readonly bool Rotates;

            public PreviewTurntable(Transform pivot, float offsetDegrees, bool rotates)
            {
                Pivot = pivot;
                OffsetDegrees = offsetDegrees;
                Rotates = rotates;
            }
        }

        private sealed class PreviewMaterials
        {
            public readonly Material Stage = CreateMaterial("Preview Stage", new Color(0.63f, 0.70f, 0.58f), 0.18f);
            public readonly Material Platform = CreateMaterial("Preview Warm Platform", new Color(0.72f, 0.58f, 0.39f), 0.22f);
            public readonly Material PlatformDark = CreateMaterial("Preview Platform Side", new Color(0.44f, 0.32f, 0.21f), 0.18f);
            public readonly Material Soil = CreateMaterial("Temperate Plain Soil", new Color(0.47f, 0.31f, 0.18f), 0.2f);
            public readonly Material SoilLight = CreateMaterial("Temperate Plain Soil Highlight", new Color(0.62f, 0.44f, 0.27f), 0.16f);
            public readonly Material Ridge = CreateMaterial("Field Ridge", new Color(0.33f, 0.22f, 0.13f), 0.14f);
            public readonly Material Grass = CreateMaterial("Warm Grass Edge", new Color(0.32f, 0.56f, 0.25f), 0.25f);
            public readonly Material Water = CreateMaterial("Soft Paddy Water", new Color(0.42f, 0.68f, 0.76f), 0.55f);
            public readonly Material CarrotBody = CreateMaterial("Carrot Body", new Color(0.93f, 0.36f, 0.08f), 0.32f);
            public readonly Material CarrotRidge = CreateMaterial("Carrot Soft Growth Lines", new Color(0.70f, 0.25f, 0.06f), 0.24f);
            public readonly Material Leaf = CreateMaterial("Crop Leaf", new Color(0.18f, 0.55f, 0.22f), 0.36f, true);
            public readonly Material LeafDark = CreateMaterial("Deep Crop Leaf", new Color(0.07f, 0.34f, 0.16f), 0.32f, true);
            public readonly Material LeafLight = CreateMaterial("Fresh Leaf Highlight", new Color(0.48f, 0.72f, 0.34f), 0.38f, true);
            public readonly Material CabbageOuter = CreateMaterial("Cabbage Outer Leaf", new Color(0.33f, 0.61f, 0.28f), 0.34f, true);
            public readonly Material CabbageInner = CreateMaterial("Cabbage Inner Leaf", new Color(0.70f, 0.82f, 0.48f), 0.38f, true);
            public readonly Material RiceStem = CreateMaterial("Rice Stem", new Color(0.51f, 0.62f, 0.22f), 0.28f);
            public readonly Material RiceGrain = CreateMaterial("Rice Grain", new Color(0.88f, 0.69f, 0.28f), 0.3f);
            public readonly Material TeaWood = CreateMaterial("Tea Wood", new Color(0.29f, 0.19f, 0.11f), 0.18f);
            public readonly Material TeaLeaf = CreateMaterial("Tea Leaf", new Color(0.12f, 0.43f, 0.22f), 0.35f);
            public readonly Material TeaTip = CreateMaterial("Tea Leaf Tip", new Color(0.55f, 0.77f, 0.37f), 0.4f);
        }

        private void Awake()
        {
            ConfigureScene();
            BuildPreview();
            ApplyRotations();
        }

        private void Update()
        {
            HandlePointerInput();

            if (!dragging)
            {
                yaw += autoRotateDegreesPerSecond * Time.deltaTime;
            }

            ApplyRotations();
        }

        private void ConfigureScene()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.80f, 0.89f, 0.91f);
            RenderSettings.ambientEquatorColor = new Color(0.53f, 0.58f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.20f, 0.15f);
            RenderSettings.ambientIntensity = 1.08f;

            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 45f;

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.76f, 0.80f);
            camera.orthographic = true;
            camera.orthographicSize = 4.75f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.transform.position = new Vector3(4.7f, 5.9f, -8.3f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.35f, 0f) - camera.transform.position, Vector3.up);

            var keyLightObject = GameObject.Find("Preview Key Light") ?? GameObject.Find("Directional Light") ?? new GameObject("Preview Key Light");
            keyLightObject.name = "Preview Key Light";

            var keyLight = keyLightObject.GetComponent<Light>() ?? keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.93f, 0.79f);
            keyLight.intensity = 1.32f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.48f;
            keyLightObject.transform.rotation = Quaternion.Euler(48f, -32f, 19f);

            var fillLightObject = GameObject.Find("Preview Fill Light") ?? new GameObject("Preview Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>() ?? fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.59f, 0.80f, 1f);
            fillLight.intensity = 1.4f;
            fillLight.range = 8f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.position = new Vector3(-2.7f, 3.2f, -3.6f);
        }

        private void BuildPreview()
        {
            DestroyGeneratedRoot();
            turntables.Clear();

            var materials = new PreviewMaterials();
            var root = new GameObject(GeneratedRootName).transform;
            root.SetParent(transform, false);
            root.localPosition = Vector3.zero;

            AddStage(root, materials);

            var carrotPivot = CreatePreviewSlot(root, "Slot_Carrot", new Vector3(-1.48f, 0f, 1.18f), 0.52f, materials.Platform, materials.PlatformDark);
            BuildCarrot(carrotPivot, materials, 0.31f);
            turntables.Add(new PreviewTurntable(carrotPivot, 14f, true));

            var cabbagePivot = CreatePreviewSlot(root, "Slot_Cabbage", new Vector3(0f, 0f, 1.34f), 0.58f, materials.Platform, materials.PlatformDark);
            BuildCabbage(cabbagePivot, materials, 0.76f);
            turntables.Add(new PreviewTurntable(cabbagePivot, -16f, true));

            var ricePivot = CreatePreviewSlot(root, "Slot_Rice", new Vector3(1.35f, 0f, 1.04f), 0.54f, materials.Platform, materials.PlatformDark);
            BuildRice(ricePivot, materials, 0.76f);
            turntables.Add(new PreviewTurntable(ricePivot, 20f, true));

            var teaPivot = CreatePreviewSlot(root, "Slot_TeaBush", new Vector3(-0.92f, 0f, -1.22f), 0.64f, materials.Platform, materials.PlatformDark);
            BuildTeaBush(teaPivot, materials, 0.82f);
            turntables.Add(new PreviewTurntable(teaPivot, -8f, true));

            var plotPivot = CreatePreviewSlot(root, "Slot_ChinaTemperatePlainPlot", new Vector3(0.98f, 0f, -1.25f), 0.76f, materials.Platform, materials.PlatformDark);
            BuildTemperatePlainPlot(plotPivot, materials, 0.82f);
            turntables.Add(new PreviewTurntable(plotPivot, 8f, true));
        }

        private void DestroyGeneratedRoot()
        {
            var existing = transform.Find(GeneratedRootName);
            if (existing == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        private static void AddStage(Transform root, PreviewMaterials materials)
        {
            var stage = AddPrimitive(
                "PreviewStage",
                PrimitiveType.Cube,
                materials.Stage,
                root,
                new Vector3(0f, -0.065f, 0f),
                Quaternion.identity,
                new Vector3(4.9f, 0.08f, 4.3f));

            stage.GetComponent<MeshRenderer>().receiveShadows = true;

            AddPrimitive("StageBackEdge", PrimitiveType.Cube, materials.Grass, root, new Vector3(0f, 0.015f, 2.22f), Quaternion.identity, new Vector3(5.15f, 0.16f, 0.13f));
            AddPrimitive("StageFrontEdge", PrimitiveType.Cube, materials.Grass, root, new Vector3(0f, 0.015f, -2.22f), Quaternion.identity, new Vector3(5.15f, 0.16f, 0.13f));
            AddPrimitive("StageLeftEdge", PrimitiveType.Cube, materials.Grass, root, new Vector3(-2.55f, 0.015f, 0f), Quaternion.identity, new Vector3(0.13f, 0.16f, 4.45f));
            AddPrimitive("StageRightEdge", PrimitiveType.Cube, materials.Grass, root, new Vector3(2.55f, 0.015f, 0f), Quaternion.identity, new Vector3(0.13f, 0.16f, 4.45f));
        }

        private static Transform CreatePreviewSlot(Transform root, string name, Vector3 localPosition, float radius, Material topMaterial, Material sideMaterial)
        {
            var slot = new GameObject(name).transform;
            slot.SetParent(root, false);
            slot.localPosition = localPosition;

            AddCylinder("SlotTop", radius, 0.08f, topMaterial, slot, new Vector3(0f, 0.02f, 0f), Quaternion.identity);
            AddCylinder("SlotSide", radius * 1.03f, 0.05f, sideMaterial, slot, new Vector3(0f, -0.045f, 0f), Quaternion.identity);

            var pivot = new GameObject("Turntable").transform;
            pivot.SetParent(slot, false);
            pivot.localPosition = new Vector3(0f, 0.07f, 0f);
            return pivot;
        }

        private static void BuildCarrot(Transform parent, PreviewMaterials materials, float scale)
        {
            var model = new GameObject("Asset_Carrot_Baseline").transform;
            model.SetParent(parent, false);
            model.localPosition = new Vector3(0f, 0.54f, 0f);
            model.localRotation = Quaternion.Euler(0f, 0f, -3f);
            model.localScale = Vector3.one * scale;

            AddMeshObject("CarrotBody", CreateCarrotBodyMesh(72, 40), materials.CarrotBody, model, Vector3.zero, Quaternion.identity);
            AddCarrotGrowthRings(model, materials.CarrotRidge);
            AddCarrotLeafCluster(model, materials.Leaf, materials.LeafDark);
        }

        private static void BuildCabbage(Transform parent, PreviewMaterials materials, float scale)
        {
            var model = new GameObject("Asset_Cabbage_Baseline").transform;
            model.SetParent(parent, false);
            model.localPosition = new Vector3(0f, 0.38f, 0f);
            model.localScale = Vector3.one * scale;

            AddPrimitive("CabbageHeart", PrimitiveType.Sphere, materials.CabbageInner, model, new Vector3(0f, 0.2f, 0f), Quaternion.identity, new Vector3(0.72f, 0.54f, 0.72f));

            var outerAngles = new[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
            for (var index = 0; index < outerAngles.Length; index++)
            {
                var leaf = AddMeshObject(
                    $"CabbageOuterLeaf_{index:00}",
                    CreateLeafMesh(0.74f, 0.33f, 0.31f, index % 2 == 0 ? 0.06f : -0.06f, 8),
                    materials.CabbageOuter,
                    model,
                    new Vector3(0f, -0.03f, 0f),
                    Quaternion.Euler(-18f, outerAngles[index], 0f));

                leaf.transform.localScale = new Vector3(1.08f, 1f, 1f);
            }

            var innerAngles = new[] { 18f, 78f, 138f, 198f, 258f, 318f };
            for (var index = 0; index < innerAngles.Length; index++)
            {
                AddMeshObject(
                    $"CabbageInnerLeaf_{index:00}",
                    CreateLeafMesh(0.48f, 0.42f, 0.20f, index % 2 == 0 ? 0.04f : -0.04f, 7),
                    materials.CabbageInner,
                    model,
                    new Vector3(0f, 0.12f, 0f),
                    Quaternion.Euler(-6f, innerAngles[index], 0f));
            }
        }

        private static void BuildRice(Transform parent, PreviewMaterials materials, float scale)
        {
            var model = new GameObject("Asset_Rice_Baseline").transform;
            model.SetParent(parent, false);
            model.localPosition = new Vector3(0f, 0.05f, 0f);
            model.localScale = Vector3.one * scale;

            AddPrimitive("RiceWetBase", PrimitiveType.Cylinder, materials.Water, model, new Vector3(0f, 0.02f, 0f), Quaternion.identity, new Vector3(0.74f, 0.025f, 0.74f));

            for (var index = 0; index < 14; index++)
            {
                var angle = index * 25.714f;
                var radius = 0.08f + 0.18f * ((index % 4) / 3f);
                var basePosition = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0.03f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius);

                var height = 0.86f + 0.18f * (index % 5) / 4f;
                var tilt = 5f + (index % 3) * 4f;
                var stalkRoot = new GameObject($"RiceStalk_{index:00}").transform;
                stalkRoot.SetParent(model, false);
                stalkRoot.localPosition = basePosition;
                stalkRoot.localRotation = Quaternion.Euler(tilt, angle, -tilt * 0.45f);

                AddCylinder("Stem", 0.013f, height, materials.RiceStem, stalkRoot, new Vector3(0f, height * 0.5f, 0f), Quaternion.identity);

                var grainRoot = new GameObject("Panicle").transform;
                grainRoot.SetParent(stalkRoot, false);
                grainRoot.localPosition = new Vector3(0f, height, 0f);
                grainRoot.localRotation = Quaternion.Euler(24f, 0f, index % 2 == 0 ? 12f : -12f);

                for (var grain = 0; grain < 5; grain++)
                {
                    var side = grain % 2 == 0 ? -1f : 1f;
                    var grainObject = AddPrimitive(
                        $"Grain_{grain:00}",
                        PrimitiveType.Sphere,
                        materials.RiceGrain,
                        grainRoot,
                        new Vector3(side * (0.025f + grain * 0.01f), -0.04f * grain, 0.055f + grain * 0.04f),
                        Quaternion.Euler(18f, 0f, side * 18f),
                        new Vector3(0.055f, 0.086f, 0.035f));

                    grainObject.GetComponent<MeshRenderer>().receiveShadows = false;
                }
            }
        }

        private static void BuildTeaBush(Transform parent, PreviewMaterials materials, float scale)
        {
            var model = new GameObject("Asset_TeaBush_Baseline").transform;
            model.SetParent(parent, false);
            model.localPosition = new Vector3(0f, 0.05f, 0f);
            model.localScale = Vector3.one * scale;

            AddCylinder("TeaTrunk", 0.055f, 0.58f, materials.TeaWood, model, new Vector3(0f, 0.29f, 0f), Quaternion.identity);

            for (var branch = 0; branch < 5; branch++)
            {
                var angle = branch * 72f + 18f;
                var branchRoot = new GameObject($"TeaBranch_{branch:00}").transform;
                branchRoot.SetParent(model, false);
                branchRoot.localPosition = new Vector3(0f, 0.38f, 0f);
                branchRoot.localRotation = Quaternion.Euler(60f, angle, 0f);
                AddCylinder("Branch", 0.022f, 0.62f, materials.TeaWood, branchRoot, new Vector3(0f, 0.31f, 0f), Quaternion.identity);
            }

            AddPrimitive("TeaCrownCenter", PrimitiveType.Sphere, materials.TeaLeaf, model, new Vector3(0f, 0.82f, 0f), Quaternion.identity, new Vector3(0.84f, 0.56f, 0.76f));
            AddPrimitive("TeaCrownLeft", PrimitiveType.Sphere, materials.TeaLeaf, model, new Vector3(-0.32f, 0.66f, 0.02f), Quaternion.identity, new Vector3(0.62f, 0.42f, 0.58f));
            AddPrimitive("TeaCrownRight", PrimitiveType.Sphere, materials.TeaLeaf, model, new Vector3(0.34f, 0.68f, -0.04f), Quaternion.identity, new Vector3(0.64f, 0.44f, 0.60f));
            AddPrimitive("TeaCrownBack", PrimitiveType.Sphere, materials.TeaLeaf, model, new Vector3(0.03f, 0.68f, 0.32f), Quaternion.identity, new Vector3(0.58f, 0.40f, 0.52f));

            var tipPositions = new[]
            {
                new Vector3(-0.28f, 0.96f, -0.16f),
                new Vector3(0.14f, 1.08f, -0.10f),
                new Vector3(0.35f, 0.88f, 0.16f),
                new Vector3(-0.12f, 0.84f, 0.34f),
                new Vector3(-0.42f, 0.72f, 0.08f)
            };

            for (var index = 0; index < tipPositions.Length; index++)
            {
                AddPrimitive($"TeaFreshTip_{index:00}", PrimitiveType.Sphere, materials.TeaTip, model, tipPositions[index], Quaternion.identity, new Vector3(0.18f, 0.11f, 0.15f));
            }
        }

        private static void BuildTemperatePlainPlot(Transform parent, PreviewMaterials materials, float scale)
        {
            var model = new GameObject("Asset_ChinaTemperatePlainPlot_Baseline").transform;
            model.SetParent(parent, false);
            model.localPosition = new Vector3(0f, 0.06f, 0f);
            model.localScale = Vector3.one * scale;

            AddPrimitive("PlotSoilBase", PrimitiveType.Cube, materials.Soil, model, new Vector3(0f, 0.03f, 0f), Quaternion.identity, new Vector3(1.46f, 0.12f, 1.06f));
            AddPrimitive("PlotLeftRidge", PrimitiveType.Cube, materials.SoilLight, model, new Vector3(-0.79f, 0.12f, 0f), Quaternion.identity, new Vector3(0.14f, 0.14f, 1.12f));
            AddPrimitive("PlotRightRidge", PrimitiveType.Cube, materials.SoilLight, model, new Vector3(0.79f, 0.12f, 0f), Quaternion.identity, new Vector3(0.14f, 0.14f, 1.12f));
            AddPrimitive("PlotBackRidge", PrimitiveType.Cube, materials.SoilLight, model, new Vector3(0f, 0.12f, 0.59f), Quaternion.identity, new Vector3(1.58f, 0.14f, 0.14f));
            AddPrimitive("PlotFrontRidge", PrimitiveType.Cube, materials.SoilLight, model, new Vector3(0f, 0.12f, -0.59f), Quaternion.identity, new Vector3(1.58f, 0.14f, 0.14f));

            for (var row = 0; row < 3; row++)
            {
                var z = -0.28f + row * 0.28f;
                AddPrimitive($"Furrow_{row:00}", PrimitiveType.Cube, materials.Ridge, model, new Vector3(0f, 0.115f, z), Quaternion.identity, new Vector3(1.12f, 0.035f, 0.05f));
            }

            for (var sprout = 0; sprout < 6; sprout++)
            {
                var x = -0.46f + (sprout % 3) * 0.46f;
                var z = -0.18f + (sprout / 3) * 0.34f;
                var sproutRoot = new GameObject($"PlainSprout_{sprout:00}").transform;
                sproutRoot.SetParent(model, false);
                sproutRoot.localPosition = new Vector3(x, 0.16f, z);
                sproutRoot.localRotation = Quaternion.Euler(0f, sprout * 37f, 0f);

                AddMeshObject("SproutLeafLeft", CreateLeafMesh(0.18f, 0.16f, 0.055f, -0.02f, 4), materials.LeafLight, sproutRoot, Vector3.zero, Quaternion.Euler(-28f, -32f, 0f));
                AddMeshObject("SproutLeafRight", CreateLeafMesh(0.18f, 0.16f, 0.055f, 0.02f, 4), materials.Leaf, sproutRoot, Vector3.zero, Quaternion.Euler(-28f, 32f, 0f));
            }
        }

        private static void AddCarrotGrowthRings(Transform parent, Material material)
        {
            for (var index = 0; index < 6; index++)
            {
                var t = 0.24f + index * 0.095f;
                var radius = CarrotRadius(t) * 1.008f;
                var tubeRadius = Mathf.Lerp(0.007f, 0.003f, t);
                var ring = AddMeshObject(
                    $"GrowthRing_{index:00}",
                    CreateTorusMesh(radius, tubeRadius, 72, 8),
                    material,
                    parent,
                    CarrotCenter(t),
                    Quaternion.identity);

                ring.transform.localScale = new Vector3(0.92f, 1f, 1.08f);
            }
        }

        private static void AddCarrotLeafCluster(Transform parent, Material primaryMaterial, Material darkMaterial)
        {
            var crownPosition = CarrotCenter(0f) + new Vector3(0f, 0.17f, 0f);
            AddPrimitive("LeafCrown", PrimitiveType.Sphere, darkMaterial, parent, crownPosition, Quaternion.identity, new Vector3(0.34f, 0.15f, 0.34f));

            var angles = new[] { -8f, 44f, 96f, 148f, 204f, 260f, 316f };
            for (var index = 0; index < angles.Length; index++)
            {
                var length = Mathf.Lerp(0.68f, 0.96f, index / (float)(angles.Length - 1));
                var height = index % 2 == 0 ? 0.82f : 0.66f;
                var width = index % 3 == 0 ? 0.14f : 0.11f;
                var curve = index % 2 == 0 ? 0.16f : -0.13f;
                var material = index % 2 == 0 ? primaryMaterial : darkMaterial;

                var leaf = AddMeshObject(
                    $"Leaf_{index:00}",
                    CreateLeafMesh(length, height, width, curve, 8),
                    material,
                    parent,
                    crownPosition + new Vector3(0f, 0.03f, 0f),
                    Quaternion.Euler(4f, angles[index], 0f));

                leaf.GetComponent<MeshRenderer>().receiveShadows = false;
            }
        }

        private static GameObject AddPrimitive(string name, PrimitiveType type, Material material, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;

            var renderer = primitive.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            return primitive;
        }

        private static GameObject AddCylinder(string name, float radius, float height, Material material, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            return AddPrimitive(
                name,
                PrimitiveType.Cylinder,
                material,
                parent,
                localPosition,
                localRotation,
                new Vector3(radius * 2f, height * 0.5f, radius * 2f));
        }

        private static GameObject AddMeshObject(string name, Mesh mesh, Material material, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var meshObject = new GameObject(name);
            meshObject.transform.SetParent(parent, false);
            meshObject.transform.localPosition = localPosition;
            meshObject.transform.localRotation = localRotation;

            var filter = meshObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            return meshObject;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, bool doubleSided = false)
        {
            var shader = Shader.Find("Standard") ??
                         Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Diffuse");

            var material = new Material(shader)
            {
                name = name,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", smoothness);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (doubleSided && material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)CullMode.Off);
            }

            return material;
        }

        private static Mesh CreateCarrotBodyMesh(int radialSegments, int heightSegments)
        {
            var ringCount = heightSegments + 1;
            var vertices = new Vector3[ringCount * radialSegments + 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[(ringCount - 1) * radialSegments * 6 + radialSegments * 6];

            for (var y = 0; y < ringCount; y++)
            {
                var t = y / (float)heightSegments;
                var center = CarrotCenter(t);
                var radius = CarrotRadius(t);
                var squashX = Mathf.Lerp(1.02f, 0.94f, t);
                var squashZ = Mathf.Lerp(1.06f, 0.98f, t);
                var ringStartIndex = y * radialSegments;

                for (var r = 0; r < radialSegments; r++)
                {
                    var angle = Mathf.PI * 2f * r / radialSegments;
                    var ringVariation = 1f + 0.003f * Mathf.Sin(angle * 2f + t * 7f);
                    var x = Mathf.Cos(angle) * radius * squashX * ringVariation;
                    var z = Mathf.Sin(angle) * radius * squashZ * ringVariation;
                    var vertexIndex = ringStartIndex + r;
                    vertices[vertexIndex] = center + new Vector3(x, 0f, z);
                    uvs[vertexIndex] = new Vector2(r / (float)radialSegments, t);
                }
            }

            var topCapIndex = vertices.Length - 2;
            vertices[topCapIndex] = CarrotCenter(0f) + new Vector3(0f, -0.035f, 0f);
            uvs[topCapIndex] = new Vector2(0.5f, 0f);

            var bottomCapIndex = vertices.Length - 1;
            vertices[bottomCapIndex] = CarrotCenter(1f) + new Vector3(0f, 0.018f, 0f);
            uvs[bottomCapIndex] = new Vector2(0.5f, 1f);

            var triangleIndex = 0;
            for (var ring = 0; ring < ringCount - 1; ring++)
            {
                for (var r = 0; r < radialSegments; r++)
                {
                    var nextR = (r + 1) % radialSegments;
                    var a = ring * radialSegments + r;
                    var b = ring * radialSegments + nextR;
                    var c = (ring + 1) * radialSegments + r;
                    var d = (ring + 1) * radialSegments + nextR;

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;

                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                    triangles[triangleIndex++] = c;
                }
            }

            for (var r = 0; r < radialSegments; r++)
            {
                var nextR = (r + 1) % radialSegments;
                triangles[triangleIndex++] = topCapIndex;
                triangles[triangleIndex++] = nextR;
                triangles[triangleIndex++] = r;

                var bottomRingStart = (ringCount - 1) * radialSegments;
                triangles[triangleIndex++] = bottomCapIndex;
                triangles[triangleIndex++] = bottomRingStart + r;
                triangles[triangleIndex++] = bottomRingStart + nextR;
            }

            return FinalizeMesh("Preview Carrot Body", vertices, uvs, triangles);
        }

        private static Mesh CreateTorusMesh(float majorRadius, float tubeRadius, int majorSegments, int tubeSegments)
        {
            var vertices = new Vector3[majorSegments * tubeSegments];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[majorSegments * tubeSegments * 6];

            var vertexIndex = 0;
            for (var major = 0; major < majorSegments; major++)
            {
                var theta = Mathf.PI * 2f * major / majorSegments;
                var radial = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
                var center = radial * majorRadius;

                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var phi = Mathf.PI * 2f * tube / tubeSegments;
                    vertices[vertexIndex] = center + radial * (Mathf.Cos(phi) * tubeRadius) + Vector3.up * (Mathf.Sin(phi) * tubeRadius);
                    uvs[vertexIndex] = new Vector2(major / (float)majorSegments, tube / (float)tubeSegments);
                    vertexIndex++;
                }
            }

            var triangleIndex = 0;
            for (var major = 0; major < majorSegments; major++)
            {
                var nextMajor = (major + 1) % majorSegments;
                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var nextTube = (tube + 1) % tubeSegments;
                    var a = major * tubeSegments + tube;
                    var b = nextMajor * tubeSegments + tube;
                    var c = major * tubeSegments + nextTube;
                    var d = nextMajor * tubeSegments + nextTube;

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;

                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                }
            }

            return FinalizeMesh("Preview Growth Ring", vertices, uvs, triangles);
        }

        private static Mesh CreateLeafMesh(float length, float height, float width, float sideCurve, int segments)
        {
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 12];

            for (var segment = 0; segment <= segments; segment++)
            {
                var t = segment / (float)segments;
                var leafWidth = width * Mathf.Sin(t * Mathf.PI);
                var center = new Vector3(
                    sideCurve * Mathf.Sin(t * Mathf.PI),
                    height * Mathf.Sin(t * Mathf.PI * 0.62f) - 0.18f * t * t,
                    length * t);

                var leftIndex = segment * 2;
                vertices[leftIndex] = center + Vector3.left * leafWidth;
                vertices[leftIndex + 1] = center + Vector3.right * leafWidth;
                uvs[leftIndex] = new Vector2(0f, t);
                uvs[leftIndex + 1] = new Vector2(1f, t);
            }

            var triangleIndex = 0;
            for (var segment = 0; segment < segments; segment++)
            {
                var left0 = segment * 2;
                var right0 = left0 + 1;
                var left1 = (segment + 1) * 2;
                var right1 = left1 + 1;

                triangles[triangleIndex++] = left0;
                triangles[triangleIndex++] = left1;
                triangles[triangleIndex++] = right0;

                triangles[triangleIndex++] = right0;
                triangles[triangleIndex++] = left1;
                triangles[triangleIndex++] = right1;

                triangles[triangleIndex++] = left0;
                triangles[triangleIndex++] = right0;
                triangles[triangleIndex++] = left1;

                triangles[triangleIndex++] = right0;
                triangles[triangleIndex++] = right1;
                triangles[triangleIndex++] = left1;
            }

            return FinalizeMesh("Preview Leaf", vertices, uvs, triangles);
        }

        private static Mesh FinalizeMesh(string name, Vector3[] vertices, Vector2[] uvs, int[] triangles)
        {
            var mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 CarrotCenter(float t)
        {
            var bend = Mathf.Sin(t * Mathf.PI) * 0.08f + t * t * 0.04f;
            var bottomDrop = 0.035f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.86f) / 0.14f));
            return new Vector3(bend, CarrotTopY - t * CarrotHeight - bottomDrop, 0f);
        }

        private static float CarrotRadius(float t)
        {
            float radius;
            if (t < 0.18f)
            {
                radius = Mathf.Lerp(0.42f, 0.84f, Mathf.SmoothStep(0f, 1f, t / 0.18f));
            }
            else if (t < 0.50f)
            {
                radius = Mathf.Lerp(0.84f, 0.76f, Mathf.SmoothStep(0f, 1f, (t - 0.18f) / 0.32f));
            }
            else
            {
                var bottomToBelly = Mathf.Clamp01((1f - t) / 0.50f);
                radius = Mathf.Lerp(0.22f, 0.76f, Mathf.Pow(bottomToBelly, 0.55f));
            }

            var ridge = 1f + 0.002f * Mathf.Sin(t * 24f);
            return radius * ridge;
        }

        private void HandlePointerInput()
        {
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
                return;
            }

            activeTouchId = -1;

            if (Input.GetMouseButtonDown(0))
            {
                dragging = true;
                previousPointerPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) && dragging)
            {
                var currentPosition = (Vector2)Input.mousePosition;
                RotateFromDelta(currentPosition - previousPointerPosition);
                previousPointerPosition = currentPosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }
        }

        private void HandleTouchInput()
        {
            if (activeTouchId < 0)
            {
                var touch = Input.GetTouch(0);
                activeTouchId = touch.fingerId;
                previousPointerPosition = touch.position;
                dragging = true;
                return;
            }

            for (var index = 0; index < Input.touchCount; index++)
            {
                var touch = Input.GetTouch(index);
                if (touch.fingerId != activeTouchId)
                {
                    continue;
                }

                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    activeTouchId = -1;
                    dragging = false;
                    return;
                }

                RotateFromDelta(touch.position - previousPointerPosition);
                previousPointerPosition = touch.position;
                return;
            }

            activeTouchId = -1;
            dragging = false;
        }

        private void RotateFromDelta(Vector2 delta)
        {
            yaw -= delta.x * dragDegreesPerPixel;
        }

        private void ApplyRotations()
        {
            for (var index = 0; index < turntables.Count; index++)
            {
                var turntable = turntables[index];
                if (!turntable.Rotates || turntable.Pivot == null)
                {
                    continue;
                }

                turntable.Pivot.localRotation = Quaternion.Euler(0f, yaw + turntable.OffsetDegrees, 0f);
            }
        }
    }
}
