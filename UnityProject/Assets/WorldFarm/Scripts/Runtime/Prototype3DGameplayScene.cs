using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorldFarm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class Prototype3DGameplayScene : MonoBehaviour
    {
        private const string SaveKey = "WorldFarm.Prototype3DGameplay.v1";
        private const int SaveVersion = 1;
        private const string GeneratedRootName = "GeneratedPrototype3DGameplay";

        private readonly Prototype3DGame game = new Prototype3DGame();
        private readonly List<Transform> labels = new List<Transform>();

        private Camera sceneCamera;
        private Transform generatedRoot;
        private Prototype3DLayout layout;
        private Prototype3DMaterials materials;
        private bool pointerDown;
        private bool needsRebuild;
        private bool pinchZooming;
        private int selectedOrderIndex;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private float nextRefreshTime;
        private float pinchStartDistance;
        private float pinchStartSize;
        private Vector2 pointerDownPosition;
        private Vector2 previousPointerPosition;

        private void Awake()
        {
            game.LoadOrCreate();
            EnsureCameraAndLights();
            BuildScene();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + 0.5f;
                if (game.RefreshPlotMaturity())
                {
                    needsRebuild = true;
                }
            }

            HandleInput();

            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                ConfigureCameraForScreen();
                needsRebuild = true;
            }

            if (needsRebuild)
            {
                BuildScene();
            }
        }

        private void LateUpdate()
        {
            FaceLabelsToCamera();
        }

        private void EnsureCameraAndLights()
        {
            sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                sceneCamera = FindObjectOfType<Camera>();
            }

            if (sceneCamera == null)
            {
                sceneCamera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            sceneCamera.gameObject.tag = "MainCamera";
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.70f, 0.86f, 0.92f);
            sceneCamera.orthographic = true;
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 120f;
            ConfigureCameraForScreen();

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.92f, 0.96f, 0.98f);
            RenderSettings.ambientEquatorColor = new Color(0.66f, 0.72f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.35f, 0.24f);
            RenderSettings.ambientIntensity = 1.18f;

            var keyLightObject = GameObject.Find("Prototype 3D Key Light") ?? GameObject.Find("Directional Light") ?? new GameObject("Prototype 3D Key Light");
            keyLightObject.name = "Prototype 3D Key Light";
            var keyLight = keyLightObject.GetComponent<Light>();
            if (keyLight == null)
            {
                keyLight = keyLightObject.AddComponent<Light>();
            }

            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.94f, 0.80f);
            keyLight.intensity = 1.28f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.34f;
            keyLightObject.transform.rotation = Quaternion.Euler(50f, -32f, 18f);

            var fillLightObject = GameObject.Find("Prototype 3D Fill Light") ?? new GameObject("Prototype 3D Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillLightObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.62f, 0.82f, 1f);
            fillLight.intensity = 1.55f;
            fillLight.range = 9f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.position = new Vector3(-3.4f, 3.8f, -3.4f);
        }

        private void ConfigureCameraForScreen()
        {
            if (sceneCamera == null)
            {
                return;
            }

            var width = Mathf.Max(1, Screen.width);
            var height = Mathf.Max(1, Screen.height);
            var aspect = width / (float)height;
            var narrowPortrait = aspect < 0.72f;

            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = narrowPortrait ? 7.15f : 6.25f;
            sceneCamera.transform.position = new Vector3(0f, 9.2f, -7.3f);
            sceneCamera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.05f, 0f) - sceneCamera.transform.position, Vector3.up);

            lastScreenWidth = width;
            lastScreenHeight = height;
        }

        private void BuildScene()
        {
            needsRebuild = false;
            labels.Clear();
            materials = new Prototype3DMaterials();
            layout = new Prototype3DLayout(sceneCamera);

            var oldRoot = GameObject.Find(GeneratedRootName);
            if (oldRoot != null)
            {
                Destroy(oldRoot);
            }

            generatedRoot = new GameObject(GeneratedRootName).transform;
            generatedRoot.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            game.EnsureValidSelection();
            BuildGround();
            BuildStatusBar();
            BuildCountryRail();
            BuildBiomeMap();
            BuildSeedRack();
            BuildSystemBuildings();
            BuildTimeControls();
            BuildLogBoard();
            FaceLabelsToCamera();
        }

        private void BuildGround()
        {
            AddBox("World Grass Base", layout.Point(0.50f, 0.49f, -0.08f), layout.Size(0.96f, 0.12f, 0.90f), materials.Grass, null, null);
            AddBox("Back Hill Band", layout.Point(0.50f, 0.88f, -0.02f), layout.Size(0.96f, 0.10f, 0.07f), materials.Hill, null, null);
            AddCylinder("Sun Token", layout.Point(0.09f, 0.90f, 0.30f), layout.SmallRadius(0.030f), 0.06f, materials.Sun, null, null);
        }

        private void BuildStatusBar()
        {
            AddBox("Top HUD Board", layout.Point(0.50f, 0.955f, 0.18f), layout.Size(0.90f, 0.12f, 0.060f), materials.Panel, null, null);
            AddLabel(
                "WorldFarm 3D占位版\n" +
                "金币 " + game.State.coins +
                " 探索 " + game.State.worldExplorationPoints +
                " 研究 " + game.State.researchPoints +
                "  " + game.GetSelectedCountry().ShortName +
                "Lv." + game.GetCountryLevel(game.State.selectedCountryId),
                layout.Point(0.50f, 0.955f, 0.56f),
                layout.Text(0.044f),
                materials.TextDark.color,
                18,
                2);
        }

        private void BuildCountryRail()
        {
            AddLabel("国家", layout.Point(0.13f, 0.855f, 0.38f), layout.Text(0.046f), materials.TextDark.color);

            var countries = Prototype3DCatalog.Countries;
            for (var index = 0; index < countries.Length; index++)
            {
                var country = countries[index];
                var y = 0.795f - index * 0.075f;
                var unlocked = game.IsCountryUnlocked(country.Id);
                var selected = country.Id == game.State.selectedCountryId;
                var canUnlock = !unlocked && game.State.worldExplorationPoints >= country.RequiredExplorationPoints;
                var material = selected ? materials.Selected : unlocked ? materials.CountryOpen : canUnlock ? materials.Warning : materials.Locked;
                var center = layout.Point(0.13f, y, 0.16f);

                AddBox("Country " + country.Id, center, layout.Size(0.205f, 0.18f, 0.055f), material, "country", country.Id);
                AddLabel(
                    (selected ? ">" : string.Empty) + country.ShortName + "\n" + (unlocked ? "Lv." + game.GetCountryLevel(country.Id) : "需" + country.RequiredExplorationPoints),
                    layout.Point(0.13f, y, 0.50f),
                    layout.Text(0.034f),
                    unlocked || canUnlock ? materials.TextDark.color : materials.TextMuted.color,
                    8,
                    2);
            }
        }

        private void BuildBiomeMap()
        {
            var country = game.GetSelectedCountry();
            AddLabel(country.ShortName + "地貌", layout.Point(0.50f, 0.845f, 0.38f), layout.Text(0.046f), materials.TextDark.color);

            var biomes = game.GetSelectedCountryBiomes();
            for (var i = 0; i < biomes.Count; i++)
            {
                var biome = biomes[i];
                var gridX = i % 2;
                var gridZ = i / 2;
                var viewportX = 0.405f + gridX * 0.205f;
                var viewportY = 0.735f - gridZ * 0.145f;
                var center = layout.Point(viewportX, viewportY, 0.03f);
                var unlocked = game.IsBiomeUnlocked(biome.Id);
                var material = unlocked ? materials.GetBiomeMaterial(biome.Id) : materials.Locked;

                AddBox("Biome " + biome.Id, center, layout.Size(0.185f, 0.12f, 0.110f), material, unlocked ? "biome" : null, biome.Id);
                AddLabel(
                    biome.ShortName + "\n" + (unlocked ? "槽位" + biome.SlotCount : "声望Lv." + biome.RequiredReputationLevel),
                    layout.Point(viewportX, viewportY + 0.040f, 0.45f),
                    layout.Text(0.031f),
                    unlocked ? materials.TextDark.color : materials.TextMuted.color,
                    7,
                    2);

                if (biome.Id.Contains("paddy"))
                {
                    AddBox("Paddy Water " + biome.Id, layout.Point(viewportX, viewportY - 0.012f, 0.10f), layout.Size(0.145f, 0.035f, 0.050f), materials.Water, null, null);
                }

                BuildPlotsForBiome(biome, viewportX, viewportY, unlocked);
            }
        }

        private void BuildPlotsForBiome(Prototype3DBiomeDef biome, float viewportX, float viewportY, bool unlocked)
        {
            var plots = game.GetPlotsForBiome(biome.Id);
            for (var i = 0; i < plots.Count; i++)
            {
                var local = GetSlotOffset(i, plots.Count);
                var position = layout.Point(viewportX + local.x, viewportY + local.z, 0.16f);
                var plot = plots[i];
                var padMaterial = !unlocked ? materials.Locked : plot.status == Prototype3DPlotStatus.Mature ? materials.MaturePad : plot.status == Prototype3DPlotStatus.Growing ? materials.GrowingPad : materials.EmptyPad;

                AddCylinder("Plot " + plot.plotId, position, layout.SmallRadius(0.028f), 0.06f, padMaterial, unlocked ? "plot" : null, plot.plotId);

                if (plot.status != Prototype3DPlotStatus.Empty)
                {
                    AddCropModel(plot.cropId, position + new Vector3(0f, 0.12f, 0f), layout.CropScale(0.34f), "plot", plot.plotId);
                    AddLabel(
                        plot.status == Prototype3DPlotStatus.Mature ? "成熟" : FormatShortDuration(Math.Max(0L, plot.matureAtSeconds - game.NowSeconds)),
                        position + new Vector3(0f, 0.42f, 0f),
                        layout.Text(0.022f),
                        plot.status == Prototype3DPlotStatus.Mature ? materials.TextStrong.color : materials.TextDark.color);
                }
            }
        }

        private static Vector3 GetSlotOffset(int index, int count)
        {
            if (count <= 1)
            {
                return new Vector3(0f, 0f, -0.020f);
            }

            if (count == 2)
            {
                return new Vector3(index == 0 ? -0.040f : 0.040f, 0f, -0.020f);
            }

            if (count == 3)
            {
                return new Vector3((index - 1) * 0.040f, 0f, -0.020f);
            }

            return new Vector3(index % 2 == 0 ? -0.040f : 0.040f, 0f, index < 2 ? 0.000f : -0.045f);
        }

        private void BuildSeedRack()
        {
            var crop = game.GetSelectedCrop();
            AddBox("Seed Rack", layout.Point(0.86f, 0.735f, 0.18f), layout.Size(0.210f, 0.20f, 0.235f), materials.Wood, null, null);
            AddLabel("种子", layout.Point(0.86f, 0.845f, 0.50f), layout.Text(0.046f), materials.TextDark.color);

            AddCylinder("Selected Seed Pedestal", layout.Point(0.86f, 0.750f, 0.33f), layout.SmallRadius(0.045f), 0.12f, materials.Panel, "cycle_crop", string.Empty);
            if (crop != null)
            {
                AddCropModel(crop.Id, layout.Point(0.86f, 0.750f, 0.44f), layout.CropScale(0.54f), "cycle_crop", string.Empty);
                AddLabel((crop.IsMutation ? "变异\n" : string.Empty) + crop.ShortName, layout.Point(0.86f, 0.655f, 0.50f), layout.Text(0.033f), materials.TextDark.color, 6, 2);
            }

            AddBox("Prev Crop Button", layout.Point(0.805f, 0.590f, 0.28f), layout.Size(0.090f, 0.18f, 0.055f), materials.Button, "prev_crop", string.Empty);
            AddLabel("上", layout.Point(0.805f, 0.590f, 0.58f), layout.Text(0.032f), materials.TextDark.color);
            AddBox("Next Crop Button", layout.Point(0.915f, 0.590f, 0.28f), layout.Size(0.090f, 0.18f, 0.055f), materials.Button, "next_crop", string.Empty);
            AddLabel("下", layout.Point(0.915f, 0.590f, 0.58f), layout.Text(0.032f), materials.TextDark.color);

            var biome = game.GetSelectedBiomeForCropPreview();
            if (biome != null && crop != null)
            {
                var balance = Prototype3DBalance.Calculate(biome, crop);
                AddLabel("适应" + balance.adaptation.ToString("0.00") + "\n变异" + (balance.mutationChance * 100f).ToString("0") + "%", layout.Point(0.86f, 0.525f, 0.50f), layout.Text(0.030f), materials.TextMuted.color, 8, 2);
            }
        }

        private void BuildSystemBuildings()
        {
            BuildWarehouse(0.20f, 0.225f);
            BuildOrderBoard(0.50f, 0.225f);
            BuildMutationLab(0.80f, 0.225f);
        }

        private void BuildWarehouse(float viewportX, float viewportY)
        {
            var origin = layout.Point(viewportX, viewportY, 0f);
            AddBox("Warehouse Tap Pad", layout.Point(viewportX, viewportY, 0.05f), layout.Size(0.245f, 0.08f, 0.125f), materials.ClearTap, "warehouse", string.Empty);
            AddBox("Warehouse Body", origin + new Vector3(0f, 0.42f, 0f), layout.Scale(new Vector3(0.72f, 0.66f, 0.56f)), materials.Warehouse, "warehouse", string.Empty);
            AddBox("Warehouse Roof", origin + new Vector3(0f, 0.80f, 0f), layout.Scale(new Vector3(0.88f, 0.18f, 0.66f)), materials.RoofRed, "warehouse", string.Empty);
            AddBox("Warehouse Door", origin + new Vector3(0f, 0.25f, -0.26f), layout.Scale(new Vector3(0.30f, 0.32f, 0.05f)), materials.Door, "warehouse", string.Empty);
            AddLabel("仓库", layout.Point(viewportX, viewportY + 0.070f, 0.78f), layout.Text(0.044f), materials.TextDark.color);
            AddLabel(game.GetInventorySummary(), layout.Point(viewportX, viewportY - 0.070f, 0.48f), layout.Text(0.026f), materials.TextDark.color, 9, 2);
        }

        private void BuildOrderBoard(float viewportX, float viewportY)
        {
            var origin = layout.Point(viewportX, viewportY, 0f);
            AddBox("Order Tap Pad", layout.Point(viewportX, viewportY, 0.05f), layout.Size(0.260f, 0.08f, 0.125f), materials.ClearTap, "order", string.Empty);
            AddCylinder("Order Post L", origin + new Vector3(-layout.WorldWidth(0.055f), 0.40f, 0f), layout.SmallRadius(0.007f), 0.60f, materials.Door, "order", string.Empty);
            AddCylinder("Order Post R", origin + new Vector3(layout.WorldWidth(0.055f), 0.40f, 0f), layout.SmallRadius(0.007f), 0.60f, materials.Door, "order", string.Empty);
            AddBox("Order Board", origin + new Vector3(0f, 0.76f, -0.02f), layout.Scale(new Vector3(0.88f, 0.50f, 0.10f)), materials.OrderBoard, "order", string.Empty);
            AddLabel("订单牌", layout.Point(viewportX, viewportY + 0.070f, 0.78f), layout.Text(0.042f), materials.TextDark.color);

            var orders = game.GetVisibleOrders();
            if (orders.Count == 0)
            {
                AddLabel("暂无订单", layout.Point(viewportX, viewportY, 0.70f), layout.Text(0.028f), materials.TextDark.color);
                return;
            }

            selectedOrderIndex = Mathf.Clamp(selectedOrderIndex, 0, orders.Count - 1);
            var order = orders[selectedOrderIndex];
            var canSubmit = game.CanSubmitOrder(order);
            AddLabel(
                order.ShortName + "\n需 " + game.FormatRequirements(order),
                layout.Point(viewportX, viewportY - 0.005f, 0.66f),
                layout.Text(0.025f),
                canSubmit ? materials.TextStrong.color : materials.TextDark.color,
                9,
                2);
            AddBox("Order Submit Button", layout.Point(viewportX, viewportY - 0.082f, 0.18f), layout.Size(0.185f, 0.18f, 0.052f), canSubmit ? materials.Button : materials.Locked, "order", string.Empty);
            AddLabel(canSubmit ? "提交" : "缺货", layout.Point(viewportX, viewportY - 0.082f, 0.50f), layout.Text(0.032f), canSubmit ? materials.TextDark.color : materials.TextMuted.color);
        }

        private void BuildMutationLab(float viewportX, float viewportY)
        {
            var origin = layout.Point(viewportX, viewportY, 0f);
            AddBox("Mutation Tap Pad", layout.Point(viewportX, viewportY, 0.05f), layout.Size(0.245f, 0.08f, 0.125f), materials.ClearTap, "mutation", string.Empty);
            AddBox("Mutation Lab Base", origin + new Vector3(0f, 0.36f, 0f), layout.Scale(new Vector3(0.70f, 0.52f, 0.56f)), materials.MutationLab, "mutation", string.Empty);
            AddCylinder("Mutation Lab Dome", origin + new Vector3(0f, 0.72f, 0f), layout.SmallRadius(0.047f), 0.18f, materials.Glass, "mutation", string.Empty);
            AddCylinder("Mutation Crystal", origin + new Vector3(0f, 0.96f, -0.06f), layout.SmallRadius(0.014f), 0.28f, materials.MutationCrystal, "mutation", string.Empty);
            AddLabel("变异棚", layout.Point(viewportX, viewportY + 0.070f, 0.82f), layout.Text(0.042f), materials.TextDark.color);

            var rule = game.GetFocusedMutationRule();
            if (rule == null)
            {
                AddLabel("暂无规则", layout.Point(viewportX, viewportY - 0.060f, 0.48f), layout.Text(0.028f), materials.TextDark.color);
                return;
            }

            var stable = game.IsMutationCropUnlocked(rule.ResultCropId);
            AddLabel(
                rule.ShortName + "\n线索" + game.GetInventoryCount(rule.ClueItemId) + "/" + rule.RequiredClueCount + " 研" + game.State.researchPoints + "/" + rule.RequiredResearchPoints,
                layout.Point(viewportX, viewportY - 0.015f, 0.58f),
                layout.Text(0.025f),
                stable ? materials.TextStrong.color : materials.TextDark.color,
                9,
                2);

            AddBox("Mutation Main Button", layout.Point(viewportX, viewportY - 0.082f, 0.18f), layout.Size(0.185f, 0.18f, 0.052f), stable ? materials.Selected : materials.Button, "mutation", string.Empty);
            AddLabel(stable ? "已稳定" : game.CanStabilizeMutation(rule) ? "稳定" : "线索", layout.Point(viewportX, viewportY - 0.082f, 0.50f), layout.Text(0.032f), materials.TextDark.color);
        }

        private void BuildTimeControls()
        {
            AddBox("Time Button 10m", layout.Point(0.27f, 0.380f, 0.18f), layout.Size(0.170f, 0.18f, 0.055f), materials.Button, "time10", string.Empty);
            AddLabel("+10分", layout.Point(0.27f, 0.380f, 0.50f), layout.Text(0.034f), materials.TextDark.color);

            AddBox("Mature All Button", layout.Point(0.50f, 0.380f, 0.18f), layout.Size(0.180f, 0.18f, 0.055f), materials.Button, "mature_all", string.Empty);
            AddLabel("全成熟", layout.Point(0.50f, 0.380f, 0.50f), layout.Text(0.034f), materials.TextDark.color);

            AddBox("Reset Button", layout.Point(0.73f, 0.380f, 0.18f), layout.Size(0.170f, 0.18f, 0.055f), materials.Warning, "reset", string.Empty);
            AddLabel("重置", layout.Point(0.73f, 0.380f, 0.50f), layout.Text(0.034f), materials.TextDark.color);
        }

        private void BuildLogBoard()
        {
            AddBox("Log Board", layout.Point(0.50f, 0.055f, 0.10f), layout.Size(0.90f, 0.10f, 0.055f), materials.Panel, null, null);
            AddLabel(game.GetLatestLog(), layout.Point(0.50f, 0.055f, 0.38f), layout.Text(0.027f), materials.TextDark.color, 20, 2);
        }

        private void AddCropModel(string cropId, Vector3 position, float scale, string clickKind, string clickId)
        {
            var crop = Prototype3DCatalog.GetCrop(cropId);
            var speciesId = crop != null ? crop.SpeciesId : cropId;
            var isMutation = crop != null && crop.IsMutation;

            if (speciesId == "rice")
            {
                for (var i = 0; i < 5; i++)
                {
                    var angle = -28f + i * 14f;
                    var offset = new Vector3((i - 2) * 0.035f * scale, 0f, Mathf.Abs(i - 2) * 0.018f * scale);
                    var stem = AddCylinder("Rice Stem", position + offset + new Vector3(0f, 0.19f * scale, 0f), 0.018f * scale, 0.36f * scale, isMutation ? materials.MutationLeaf : materials.RiceStem, clickKind, clickId);
                    stem.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    AddBox("Rice Grain", position + offset + new Vector3(0.05f * Mathf.Sign(angle), 0.40f * scale, 0.02f), new Vector3(0.05f * scale, 0.13f * scale, 0.035f * scale), materials.RiceGrain, clickKind, clickId);
                }
            }
            else if (speciesId == "wheat")
            {
                for (var i = 0; i < 4; i++)
                {
                    var offset = new Vector3((i - 1.5f) * 0.055f * scale, 0f, (i % 2) * 0.045f * scale);
                    var stem = AddCylinder("Wheat Stem", position + offset + new Vector3(0f, 0.18f * scale, 0f), 0.014f * scale, 0.34f * scale, materials.WheatStem, clickKind, clickId);
                    stem.transform.rotation = Quaternion.Euler(0f, 0f, -8f + i * 5f);
                    AddCylinder("Wheat Ear", position + offset + new Vector3(0f, 0.39f * scale, 0f), 0.030f * scale, 0.16f * scale, isMutation ? materials.WaterTrait : materials.WheatEar, clickKind, clickId);
                }
            }
            else if (speciesId == "cabbage")
            {
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * 60f;
                    var radius = 0.13f * scale;
                    var offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 0.09f * scale, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
                    var leaf = AddSphere("Cabbage Leaf", position + offset, new Vector3(0.18f * scale, 0.10f * scale, 0.13f * scale), isMutation ? materials.MutationLeaf : materials.CabbageOuter, clickKind, clickId);
                    leaf.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                }

                AddSphere("Cabbage Core", position + new Vector3(0f, 0.16f * scale, 0f), new Vector3(0.18f * scale, 0.15f * scale, 0.18f * scale), materials.CabbageInner, clickKind, clickId);
            }
            else if (speciesId == "corn")
            {
                AddCylinder("Corn Stem", position + new Vector3(0f, 0.25f * scale, 0f), 0.035f * scale, 0.50f * scale, materials.CornStem, clickKind, clickId);
                AddCylinder("Corn Cob", position + new Vector3(0.08f * scale, 0.38f * scale, -0.02f), 0.070f * scale, 0.22f * scale, isMutation ? materials.WaterTrait : materials.CornCob, clickKind, clickId);
                AddSphere("Corn Leaf L", position + new Vector3(-0.11f * scale, 0.30f * scale, 0f), new Vector3(0.15f * scale, 0.045f * scale, 0.08f * scale), materials.CornLeaf, clickKind, clickId);
                AddSphere("Corn Leaf R", position + new Vector3(0.12f * scale, 0.25f * scale, 0.02f), new Vector3(0.15f * scale, 0.045f * scale, 0.08f * scale), materials.CornLeaf, clickKind, clickId);
            }
            else if (speciesId == "tea")
            {
                AddCylinder("Tea Trunk", position + new Vector3(0f, 0.13f * scale, 0f), 0.040f * scale, 0.24f * scale, materials.TeaWood, clickKind, clickId);
                AddSphere("Tea Crown", position + new Vector3(0f, 0.28f * scale, 0f), new Vector3(0.28f * scale, 0.18f * scale, 0.25f * scale), isMutation ? materials.MutationLeaf : materials.TeaLeaf, clickKind, clickId);
                AddSphere("Tea Tip", position + new Vector3(0.08f * scale, 0.43f * scale, -0.02f), new Vector3(0.10f * scale, 0.08f * scale, 0.08f * scale), materials.TeaTip, clickKind, clickId);
            }
            else if (speciesId == "grape")
            {
                AddCylinder("Grape Post", position + new Vector3(-0.10f * scale, 0.25f * scale, 0f), 0.025f * scale, 0.50f * scale, materials.Door, clickKind, clickId);
                AddCylinder("Grape Vine", position + new Vector3(0.05f * scale, 0.44f * scale, 0f), 0.022f * scale, 0.36f * scale, materials.TeaWood, clickKind, clickId);
                for (var i = 0; i < 5; i++)
                {
                    var offset = new Vector3((i % 2 == 0 ? -0.04f : 0.04f) * scale, (0.28f - i * 0.032f) * scale, -0.05f * scale);
                    AddSphere("Grape Berry", position + offset, new Vector3(0.055f * scale, 0.055f * scale, 0.055f * scale), isMutation ? materials.MutationGrape : materials.Grape, clickKind, clickId);
                }
            }
            else
            {
                AddSphere("Fallback Crop", position + new Vector3(0f, 0.22f * scale, 0f), new Vector3(0.18f * scale, 0.26f * scale, 0.18f * scale), materials.CabbageOuter, clickKind, clickId);
            }

            if (isMutation)
            {
                AddCylinder("Mutation Ring", position + new Vector3(0f, 0.06f * scale, 0f), 0.23f * scale, 0.02f * scale, materials.MutationCrystal, clickKind, clickId);
            }
        }

        private GameObject AddBox(string name, Vector3 position, Vector3 scale, Material material, string clickKind, string clickId)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(generatedRoot, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            AddClickTarget(box, clickKind, clickId);
            return box;
        }

        private GameObject AddCylinder(string name, Vector3 position, float radius, float height, Material material, string clickKind, string clickId)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(generatedRoot, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            AddClickTarget(cylinder, clickKind, clickId);
            return cylinder;
        }

        private GameObject AddSphere(string name, Vector3 position, Vector3 scale, Material material, string clickKind, string clickId)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(generatedRoot, false);
            sphere.transform.localPosition = position;
            sphere.transform.localScale = scale;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            AddClickTarget(sphere, clickKind, clickId);
            return sphere;
        }

        private void AddClickTarget(GameObject targetObject, string clickKind, string clickId)
        {
            if (string.IsNullOrEmpty(clickKind))
            {
                return;
            }

            var target = targetObject.AddComponent<Prototype3DClickTarget>();
            target.Kind = clickKind;
            target.Id = clickId;
        }

        private TextMesh AddLabel(string text, Vector3 position, float characterSize, Color color, int maxCharsPerLine = 0, int maxLines = 0)
        {
            var labelObject = new GameObject("Label " + text.Replace("\n", " "));
            labelObject.transform.SetParent(generatedRoot, false);
            labelObject.transform.localPosition = position;

            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = CompactLabelText(text, maxCharsPerLine, maxLines);
            textMesh.fontSize = 88;
            textMesh.characterSize = characterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.lineSpacing = 0.86f;
            textMesh.color = color;

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            labels.Add(labelObject.transform);
            return textMesh;
        }

        private void HandleInput()
        {
            if (Input.touchCount >= 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                var distance = Vector2.Distance(a.position, b.position);
                if (!pinchZooming)
                {
                    pinchZooming = true;
                    pinchStartDistance = distance;
                    pinchStartSize = sceneCamera.orthographicSize;
                }
                else if (pinchStartDistance > 1f)
                {
                    sceneCamera.orthographicSize = Mathf.Clamp(pinchStartSize * pinchStartDistance / Mathf.Max(1f, distance), 4.2f, 7.4f);
                }

                pointerDown = false;
                return;
            }

            pinchZooming = false;

            if (Input.touchCount == 1)
            {
                HandleSingleTouch(Input.GetTouch(0));
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                pointerDownPosition = Input.mousePosition;
                previousPointerPosition = pointerDownPosition;
            }

            if (pointerDown && Input.GetMouseButton(0))
            {
                var current = (Vector2)Input.mousePosition;
                var delta = current - previousPointerPosition;
                previousPointerPosition = current;
                if (Vector2.Distance(current, pointerDownPosition) > GetTapThresholdPixels())
                {
                    PanCamera(delta);
                }
            }

            if (pointerDown && Input.GetMouseButtonUp(0))
            {
                var upPosition = (Vector2)Input.mousePosition;
                var moved = Vector2.Distance(upPosition, pointerDownPosition);
                pointerDown = false;
                if (moved <= GetTapThresholdPixels())
                {
                    TryClick(upPosition);
                }
            }
        }

        private void HandleSingleTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                pointerDown = true;
                pointerDownPosition = touch.position;
                previousPointerPosition = touch.position;
                return;
            }

            if (!pointerDown)
            {
                return;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                var current = touch.position;
                var delta = current - previousPointerPosition;
                previousPointerPosition = current;
                if (Vector2.Distance(current, pointerDownPosition) > GetTapThresholdPixels())
                {
                    PanCamera(delta);
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                var moved = Vector2.Distance(touch.position, pointerDownPosition);
                pointerDown = false;
                if (moved <= GetTapThresholdPixels())
                {
                    TryClick(touch.position);
                }
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                pointerDown = false;
            }
        }

        private static float GetTapThresholdPixels()
        {
            return Screen.dpi > 0f ? Mathf.Clamp(Screen.dpi * 0.10f, 24f, 52f) : 34f;
        }

        private void PanCamera(Vector2 delta)
        {
            var right = sceneCamera.transform.right;
            right.y = 0f;
            right.Normalize();

            var forward = Vector3.Cross(right, Vector3.up);
            forward.y = 0f;
            forward.Normalize();

            var movement = (-right * delta.x + forward * delta.y) * (sceneCamera.orthographicSize * 0.0012f);
            sceneCamera.transform.position += movement;
            sceneCamera.transform.position = new Vector3(
                Mathf.Clamp(sceneCamera.transform.position.x, -0.9f, 0.9f),
                sceneCamera.transform.position.y,
                Mathf.Clamp(sceneCamera.transform.position.z, -8.7f, -6.5f));
        }

        private void TryClick(Vector2 screenPosition)
        {
            var ray = sceneCamera.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, 120f);
            if (hits.Length == 0)
            {
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var target = hits[i].collider.GetComponent<Prototype3DClickTarget>();
                if (target == null)
                {
                    target = hits[i].collider.GetComponentInParent<Prototype3DClickTarget>();
                }

                if (target == null)
                {
                    continue;
                }

                HandleTargetClick(target.Kind, target.Id);
                return;
            }
        }

        private void HandleTargetClick(string kind, string id)
        {
            if (kind == "country")
            {
                if (game.SelectOrUnlockCountry(id))
                {
                    selectedOrderIndex = 0;
                }
            }
            else if (kind == "plot")
            {
                game.ActivatePlot(id);
            }
            else if (kind == "biome")
            {
                var biome = Prototype3DCatalog.GetBiome(id);
                if (biome != null)
                {
                    game.AddLog("地貌：" + biome.ShortName + "，点击圆形地块种植或收获");
                }
            }
            else if (kind == "cycle_crop" || kind == "next_crop")
            {
                game.CycleCrop(1);
            }
            else if (kind == "prev_crop")
            {
                game.CycleCrop(-1);
            }
            else if (kind == "time10")
            {
                game.AdvanceTestTime(600);
            }
            else if (kind == "mature_all")
            {
                game.MatureAllGrowingPlots();
            }
            else if (kind == "reset")
            {
                game.ResetState();
                selectedOrderIndex = 0;
            }
            else if (kind == "order")
            {
                var orders = game.GetVisibleOrders();
                if (orders.Count > 0)
                {
                    selectedOrderIndex = Mathf.Clamp(selectedOrderIndex, 0, orders.Count - 1);
                    if (!game.SubmitOrder(orders[selectedOrderIndex]))
                    {
                        selectedOrderIndex = (selectedOrderIndex + 1) % orders.Count;
                        game.AddLog("切换订单：" + orders[selectedOrderIndex].ShortName);
                    }
                }
            }
            else if (kind == "warehouse")
            {
                game.AddLog("仓库：" + game.GetInventorySummary());
            }
            else if (kind == "mutation")
            {
                game.ActivateFocusedMutationRule();
            }

            needsRebuild = true;
        }

        private void FaceLabelsToCamera()
        {
            if (sceneCamera == null)
            {
                return;
            }

            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (label == null)
                {
                    continue;
                }

                label.rotation = Quaternion.LookRotation(label.position - sceneCamera.transform.position, Vector3.up);
            }
        }

        private static string FormatShortDuration(long seconds)
        {
            if (seconds <= 0)
            {
                return "0分";
            }

            if (seconds < 60)
            {
                return seconds + "秒";
            }

            return Mathf.CeilToInt(seconds / 60f) + "分";
        }

        private static string CompactLabelText(string text, int maxCharsPerLine, int maxLines)
        {
            if (string.IsNullOrEmpty(text) || maxCharsPerLine <= 0)
            {
                return text;
            }

            var output = new List<string>();
            var sourceLines = text.Split('\n');
            for (var lineIndex = 0; lineIndex < sourceLines.Length; lineIndex++)
            {
                var source = sourceLines[lineIndex].Trim();
                if (source.Length == 0)
                {
                    output.Add(string.Empty);
                    continue;
                }

                for (var start = 0; start < source.Length; start += maxCharsPerLine)
                {
                    var length = Mathf.Min(maxCharsPerLine, source.Length - start);
                    output.Add(source.Substring(start, length));
                }
            }

            if (maxLines > 0 && output.Count > maxLines)
            {
                output.RemoveRange(maxLines, output.Count - maxLines);
                var lastIndex = output.Count - 1;
                if (lastIndex >= 0 && output[lastIndex].Length > 3)
                {
                    output[lastIndex] = output[lastIndex].Substring(0, output[lastIndex].Length - 3) + "...";
                }
            }

            return string.Join("\n", output.ToArray());
        }

        private sealed class Prototype3DClickTarget : MonoBehaviour
        {
            public string Kind;
            public string Id;
        }

        private sealed class Prototype3DLayout
        {
            private readonly Camera camera;
            private readonly Rect safeViewport;
            private readonly float scale;
            private readonly float textScale;

            public Prototype3DLayout(Camera camera)
            {
                this.camera = camera;

                var screenWidth = Mathf.Max(1f, Screen.width);
                var screenHeight = Mathf.Max(1f, Screen.height);
                var safeArea = Screen.safeArea;
                if (safeArea.width <= 1f || safeArea.height <= 1f)
                {
                    safeArea = new Rect(0f, 0f, screenWidth, screenHeight);
                }

                safeViewport = new Rect(
                    Mathf.Clamp01(safeArea.xMin / screenWidth),
                    Mathf.Clamp01(safeArea.yMin / screenHeight),
                    Mathf.Clamp01(safeArea.width / screenWidth),
                    Mathf.Clamp01(safeArea.height / screenHeight));

                var aspect = screenWidth / screenHeight;
                scale = Mathf.Clamp(aspect / 0.56f, 0.84f, 1.18f);
                textScale = Mathf.Clamp(0.56f / Mathf.Max(0.42f, aspect), 0.82f, 1.04f);
            }

            public Vector3 Point(float viewportX, float viewportY, float planeY)
            {
                var x = Mathf.Lerp(safeViewport.xMin + 0.02f, safeViewport.xMax - 0.02f, Mathf.Clamp01(viewportX));
                var y = Mathf.Lerp(safeViewport.yMin + 0.02f, safeViewport.yMax - 0.02f, Mathf.Clamp01(viewportY));
                var ray = camera.ViewportPointToRay(new Vector3(x, y, 0f));
                var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
                float enter;
                if (plane.Raycast(ray, out enter))
                {
                    return ray.GetPoint(enter);
                }

                return new Vector3(0f, planeY, 0f);
            }

            public Vector3 Size(float viewportWidth, float height, float viewportHeight)
            {
                return new Vector3(
                    Mathf.Max(0.10f, WorldWidth(viewportWidth)),
                    height,
                    Mathf.Max(0.08f, WorldDepth(viewportHeight)));
            }

            public Vector3 Scale(Vector3 value)
            {
                return new Vector3(value.x * scale, value.y, value.z * scale);
            }

            public float Text(float value)
            {
                return value * textScale;
            }

            public float CropScale(float value)
            {
                return value * Mathf.Clamp(scale, 0.88f, 1.10f);
            }

            public float SmallRadius(float viewportRadius)
            {
                return Mathf.Max(0.045f, WorldWidth(viewportRadius));
            }

            public float WorldWidth(float viewportWidth)
            {
                var left = Point(0.50f - viewportWidth * 0.5f, 0.50f, 0f);
                var right = Point(0.50f + viewportWidth * 0.5f, 0.50f, 0f);
                return Mathf.Abs(right.x - left.x);
            }

            private float WorldDepth(float viewportHeight)
            {
                var bottom = Point(0.50f, 0.50f - viewportHeight * 0.5f, 0f);
                var top = Point(0.50f, 0.50f + viewportHeight * 0.5f, 0f);
                var bottomFlat = new Vector3(bottom.x, 0f, bottom.z);
                var topFlat = new Vector3(top.x, 0f, top.z);
                return Vector3.Distance(bottomFlat, topFlat);
            }
        }

        private sealed class Prototype3DMaterials
        {
            public readonly Material Grass = CreateMaterial("3D Grass", new Color(0.46f, 0.67f, 0.37f), 0.28f);
            public readonly Material Hill = CreateMaterial("3D Hill", new Color(0.34f, 0.56f, 0.31f), 0.25f);
            public readonly Material Sun = CreateMaterial("3D Sun", new Color(1.0f, 0.80f, 0.26f), 0.40f);
            public readonly Material Panel = CreateMaterial("3D Panel", new Color(0.86f, 0.78f, 0.58f), 0.28f);
            public readonly Material Wood = CreateMaterial("3D Wood", new Color(0.58f, 0.38f, 0.21f), 0.18f);
            public readonly Material Door = CreateMaterial("3D Door", new Color(0.32f, 0.22f, 0.16f), 0.14f);
            public readonly Material RoofRed = CreateMaterial("3D Roof Red", new Color(0.74f, 0.24f, 0.18f), 0.20f);
            public readonly Material Warehouse = CreateMaterial("3D Warehouse", new Color(0.86f, 0.66f, 0.34f), 0.24f);
            public readonly Material OrderBoard = CreateMaterial("3D Order Board", new Color(0.79f, 0.52f, 0.26f), 0.20f);
            public readonly Material MutationLab = CreateMaterial("3D Mutation Lab", new Color(0.56f, 0.66f, 0.78f), 0.32f);
            public readonly Material Glass = CreateMaterial("3D Soft Glass", new Color(0.60f, 0.86f, 0.92f), 0.55f);
            public readonly Material MutationCrystal = CreateMaterial("3D Mutation Crystal", new Color(0.46f, 0.82f, 0.86f), 0.68f);
            public readonly Material Button = CreateMaterial("3D Button", new Color(0.96f, 0.74f, 0.34f), 0.26f);
            public readonly Material ClearTap = CreateMaterial("3D Clear Tap", new Color(1.0f, 0.94f, 0.54f, 0.18f), 0.05f);
            public readonly Material Selected = CreateMaterial("3D Selected", new Color(0.98f, 0.86f, 0.36f), 0.36f);
            public readonly Material Warning = CreateMaterial("3D Warning", new Color(0.92f, 0.48f, 0.28f), 0.22f);
            public readonly Material Locked = CreateMaterial("3D Locked", new Color(0.48f, 0.50f, 0.48f), 0.12f);
            public readonly Material CountryOpen = CreateMaterial("3D Country Open", new Color(0.58f, 0.78f, 0.50f), 0.28f);
            public readonly Material EmptyPad = CreateMaterial("3D Empty Pad", new Color(0.55f, 0.36f, 0.20f), 0.18f);
            public readonly Material GrowingPad = CreateMaterial("3D Growing Pad", new Color(0.34f, 0.55f, 0.24f), 0.20f);
            public readonly Material MaturePad = CreateMaterial("3D Mature Pad", new Color(0.94f, 0.72f, 0.26f), 0.32f);
            public readonly Material DryField = CreateMaterial("3D Dry Field", new Color(0.64f, 0.45f, 0.25f), 0.18f);
            public readonly Material PaddyField = CreateMaterial("3D Paddy Field", new Color(0.40f, 0.68f, 0.72f), 0.38f);
            public readonly Material VegetableField = CreateMaterial("3D Vegetable Field", new Color(0.42f, 0.64f, 0.32f), 0.26f);
            public readonly Material HillField = CreateMaterial("3D Terrace Field", new Color(0.48f, 0.57f, 0.33f), 0.22f);
            public readonly Material Vineyard = CreateMaterial("3D Vineyard", new Color(0.52f, 0.47f, 0.36f), 0.20f);
            public readonly Material Water = CreateMaterial("3D Water", new Color(0.35f, 0.65f, 0.78f), 0.62f);
            public readonly Material RiceStem = CreateMaterial("3D Rice Stem", new Color(0.48f, 0.62f, 0.22f), 0.30f);
            public readonly Material RiceGrain = CreateMaterial("3D Rice Grain", new Color(0.92f, 0.72f, 0.30f), 0.36f);
            public readonly Material WheatStem = CreateMaterial("3D Wheat Stem", new Color(0.66f, 0.58f, 0.22f), 0.25f);
            public readonly Material WheatEar = CreateMaterial("3D Wheat Ear", new Color(0.95f, 0.72f, 0.30f), 0.34f);
            public readonly Material CabbageOuter = CreateMaterial("3D Cabbage Outer", new Color(0.33f, 0.62f, 0.27f), 0.35f);
            public readonly Material CabbageInner = CreateMaterial("3D Cabbage Inner", new Color(0.75f, 0.86f, 0.48f), 0.38f);
            public readonly Material CornStem = CreateMaterial("3D Corn Stem", new Color(0.28f, 0.56f, 0.22f), 0.26f);
            public readonly Material CornLeaf = CreateMaterial("3D Corn Leaf", new Color(0.22f, 0.58f, 0.25f), 0.30f);
            public readonly Material CornCob = CreateMaterial("3D Corn Cob", new Color(0.98f, 0.72f, 0.22f), 0.35f);
            public readonly Material TeaWood = CreateMaterial("3D Tea Wood", new Color(0.27f, 0.18f, 0.10f), 0.20f);
            public readonly Material TeaLeaf = CreateMaterial("3D Tea Leaf", new Color(0.12f, 0.42f, 0.22f), 0.36f);
            public readonly Material TeaTip = CreateMaterial("3D Tea Tip", new Color(0.55f, 0.77f, 0.37f), 0.42f);
            public readonly Material Grape = CreateMaterial("3D Grape", new Color(0.43f, 0.20f, 0.56f), 0.32f);
            public readonly Material MutationGrape = CreateMaterial("3D Mutation Grape", new Color(0.36f, 0.36f, 0.72f), 0.44f);
            public readonly Material MutationLeaf = CreateMaterial("3D Mutation Leaf", new Color(0.42f, 0.72f, 0.62f), 0.42f);
            public readonly Material WaterTrait = CreateMaterial("3D Water Trait", new Color(0.58f, 0.82f, 0.82f), 0.45f);
            public readonly Material TextDark = CreateMaterial("3D Text Dark", new Color(0.13f, 0.15f, 0.10f), 0.0f);
            public readonly Material TextMuted = CreateMaterial("3D Text Muted", new Color(0.42f, 0.45f, 0.38f), 0.0f);
            public readonly Material TextStrong = CreateMaterial("3D Text Strong", new Color(0.04f, 0.36f, 0.18f), 0.0f);

            public Material GetBiomeMaterial(string biomeId)
            {
                if (biomeId.Contains("paddy"))
                {
                    return PaddyField;
                }

                if (biomeId.Contains("vegetable"))
                {
                    return VegetableField;
                }

                if (biomeId.Contains("hill") || biomeId.Contains("tea"))
                {
                    return HillField;
                }

                if (biomeId.Contains("vineyard"))
                {
                    return Vineyard;
                }

                return DryField;
            }

            private static Material CreateMaterial(string name, Color color, float smoothness)
            {
                var shader = Shader.Find("Standard");
                var material = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
                material.name = name;
                material.color = color;
                material.SetFloat("_Glossiness", smoothness);
                material.SetFloat("_Metallic", 0f);

                if (color.a < 0.99f)
                {
                    material.SetFloat("_Mode", 3f);
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                }

                return material;
            }
        }

        [Serializable]
        private sealed class Prototype3DGameState
        {
            public int version = SaveVersion;
            public int coins = 120;
            public int worldExplorationPoints;
            public int researchPoints;
            public int experience;
            public long testTimeOffsetSeconds;
            public string selectedCountryId = "cn";
            public string selectedCropId = "cn_wheat";
            public List<Prototype3DCountryProgressState> countries = new List<Prototype3DCountryProgressState>();
            public List<Prototype3DPlotStateData> plots = new List<Prototype3DPlotStateData>();
            public List<Prototype3DInventoryStack> inventory = new List<Prototype3DInventoryStack>();
            public List<Prototype3DOrderProgressState> orders = new List<Prototype3DOrderProgressState>();
            public List<string> stableMutationCropIds = new List<string>();
            public List<Prototype3DLogEntry> logs = new List<Prototype3DLogEntry>();
        }

        [Serializable]
        private sealed class Prototype3DCountryProgressState
        {
            public string countryId;
            public bool unlocked;
            public int reputation;
        }

        [Serializable]
        private sealed class Prototype3DPlotStateData
        {
            public string plotId;
            public string biomeId;
            public Prototype3DPlotStatus status;
            public string cropId;
            public long plantedAtSeconds;
            public long matureAtSeconds;
            public float adaptation;
            public int expectedMinYield;
            public int expectedMaxYield;
        }

        [Serializable]
        private sealed class Prototype3DInventoryStack
        {
            public string itemId;
            public int count;
        }

        [Serializable]
        private sealed class Prototype3DOrderProgressState
        {
            public string orderId;
            public int completedCount;
        }

        [Serializable]
        private sealed class Prototype3DLogEntry
        {
            public long timeSeconds;
            public string message;
        }

        private enum Prototype3DPlotStatus
        {
            Empty,
            Growing,
            Mature
        }

        private sealed class Prototype3DGame
        {
            private static readonly int[] ReputationThresholds = { 0, 30, 80, 150, 260, 400, 580, 800, 1060, 1360, 1700 };

            public Prototype3DGameState State { get; private set; }

            public long NowSeconds
            {
                get { return DateTimeOffset.Now.ToUnixTimeSeconds() + State.testTimeOffsetSeconds; }
            }

            public void LoadOrCreate()
            {
                var json = PlayerPrefs.GetString(SaveKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        State = JsonUtility.FromJson<Prototype3DGameState>(json);
                    }
                    catch (Exception)
                    {
                        State = null;
                    }
                }

                if (State == null || State.version != SaveVersion)
                {
                    ResetState();
                    return;
                }

                NormalizeState();
                AddLog("读取3D原型存档", false);
                Save();
            }

            public void ResetState()
            {
                State = new Prototype3DGameState();
                NormalizeState();
                AddLog("3D原型已重置", false);
                Save();
            }

            public void Save()
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State));
                PlayerPrefs.Save();
            }

            public void EnsureValidSelection()
            {
                if (!IsCountryUnlocked(State.selectedCountryId))
                {
                    State.selectedCountryId = "cn";
                }

                if (!IsCropUnlocked(State.selectedCropId) || GetSelectedCrop().CountryId != State.selectedCountryId)
                {
                    State.selectedCropId = GetFirstUnlockedCropId(State.selectedCountryId);
                }
            }

            public bool RefreshPlotMaturity()
            {
                var changed = false;
                var now = NowSeconds;
                for (var i = 0; i < State.plots.Count; i++)
                {
                    var plot = State.plots[i];
                    if (plot.status == Prototype3DPlotStatus.Growing && now >= plot.matureAtSeconds)
                    {
                        plot.status = Prototype3DPlotStatus.Mature;
                        var crop = Prototype3DCatalog.GetCrop(plot.cropId);
                        AddLog((crop != null ? crop.ShortName : "作物") + "成熟", false);
                        changed = true;
                    }
                }

                if (changed)
                {
                    Save();
                }

                return changed;
            }

            public void ActivatePlot(string plotId)
            {
                var plot = GetPlot(plotId);
                if (plot == null || !IsBiomeUnlocked(plot.biomeId))
                {
                    return;
                }

                if (plot.status == Prototype3DPlotStatus.Empty)
                {
                    Plant(plot);
                    return;
                }

                if (plot.status == Prototype3DPlotStatus.Mature)
                {
                    Harvest(plot);
                    return;
                }

                AddLog("作物还在生长");
            }

            public void CycleCrop(int direction)
            {
                var crops = GetUnlockedCrops(State.selectedCountryId);
                if (crops.Count == 0)
                {
                    return;
                }

                var currentIndex = 0;
                for (var i = 0; i < crops.Count; i++)
                {
                    if (crops[i].Id == State.selectedCropId)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                currentIndex = (currentIndex + direction + crops.Count) % crops.Count;
                State.selectedCropId = crops[currentIndex].Id;
                AddLog("选择作物：" + crops[currentIndex].ShortName);
                Save();
            }

            public void AdvanceTestTime(int seconds)
            {
                State.testTimeOffsetSeconds += seconds;
                RefreshPlotMaturity();
                AddLog("时间前进 " + FormatShortDuration(seconds));
                Save();
            }

            public void MatureAllGrowingPlots()
            {
                var changed = false;
                for (var i = 0; i < State.plots.Count; i++)
                {
                    var plot = State.plots[i];
                    if (plot.status != Prototype3DPlotStatus.Growing)
                    {
                        continue;
                    }

                    plot.matureAtSeconds = NowSeconds - 1;
                    plot.status = Prototype3DPlotStatus.Mature;
                    changed = true;
                }

                AddLog(changed ? "全部作物已成熟" : "没有生长中的作物");
                Save();
            }

            public bool SelectOrUnlockCountry(string countryId)
            {
                var country = Prototype3DCatalog.GetCountry(countryId);
                if (country == null)
                {
                    return false;
                }

                var progress = GetCountryProgress(countryId);
                if (!progress.unlocked)
                {
                    if (State.worldExplorationPoints < country.RequiredExplorationPoints)
                    {
                        AddLog("探索点不足，无法解锁" + country.ShortName);
                        return false;
                    }

                    State.worldExplorationPoints -= country.RequiredExplorationPoints;
                    progress.unlocked = true;
                    AddLog("解锁国家：" + country.ShortName);
                }

                State.selectedCountryId = countryId;
                State.selectedCropId = GetFirstUnlockedCropId(countryId);
                Save();
                return true;
            }

            public bool SubmitOrder(Prototype3DOrderDef order)
            {
                if (!CanSubmitOrder(order))
                {
                    AddLog("订单材料不足，已切换订单");
                    return false;
                }

                for (var i = 0; i < order.Requirements.Length; i++)
                {
                    RemoveInventory(order.Requirements[i].ItemId, order.Requirements[i].Count);
                }

                State.coins += order.RewardCoins;
                State.experience += order.RewardExperience;
                State.worldExplorationPoints += order.RewardExplorationPoints;
                State.researchPoints += order.RewardResearchPoints;
                GetCountryProgress(order.CountryId ?? State.selectedCountryId).reputation += order.RewardCountryReputation;
                GetOrderProgress(order.Id).completedCount++;
                AddLog("提交订单：" + order.ShortName);
                Save();
                return true;
            }

            public bool CanSubmitOrder(Prototype3DOrderDef order)
            {
                for (var i = 0; i < order.Requirements.Length; i++)
                {
                    if (GetInventoryCount(order.Requirements[i].ItemId) < order.Requirements[i].Count)
                    {
                        return false;
                    }
                }

                return true;
            }

            public void ActivateFocusedMutationRule()
            {
                var rule = GetFocusedMutationRule();
                if (rule == null)
                {
                    AddLog("当前国家暂无变异规则");
                    return;
                }

                if (IsMutationCropUnlocked(rule.ResultCropId))
                {
                    var crop = Prototype3DCatalog.GetCrop(rule.ResultCropId);
                    State.selectedCropId = rule.ResultCropId;
                    AddLog("已选择稳定品种：" + (crop != null ? crop.ShortName : "变异品种"));
                    Save();
                    return;
                }

                if (CanStabilizeMutation(rule))
                {
                    RemoveInventory(rule.ClueItemId, rule.RequiredClueCount);
                    State.researchPoints -= rule.RequiredResearchPoints;
                    State.stableMutationCropIds.Add(rule.ResultCropId);
                    State.selectedCropId = rule.ResultCropId;
                    AddLog("稳定变异：" + rule.ShortName);
                    Save();
                    return;
                }

                AddInventory(rule.ClueItemId, 1);
                AddLog("测试获得线索：" + rule.ClueDisplayName);
                Save();
            }

            public bool CanStabilizeMutation(Prototype3DMutationRuleDef rule)
            {
                return rule != null
                    && !IsMutationCropUnlocked(rule.ResultCropId)
                    && GetInventoryCount(rule.ClueItemId) >= rule.RequiredClueCount
                    && State.researchPoints >= rule.RequiredResearchPoints;
            }

            public bool IsMutationCropUnlocked(string cropId)
            {
                var crop = Prototype3DCatalog.GetCrop(cropId);
                return crop != null && crop.IsMutation && State.stableMutationCropIds.Contains(cropId);
            }

            public bool IsCountryUnlocked(string countryId)
            {
                return GetCountryProgress(countryId).unlocked;
            }

            public bool IsBiomeUnlocked(string biomeId)
            {
                var biome = Prototype3DCatalog.GetBiome(biomeId);
                return biome != null && IsCountryUnlocked(biome.CountryId) && GetCountryLevel(biome.CountryId) >= biome.RequiredReputationLevel;
            }

            public bool IsCropUnlocked(string cropId)
            {
                var crop = Prototype3DCatalog.GetCrop(cropId);
                if (crop == null || !IsCountryUnlocked(crop.CountryId))
                {
                    return false;
                }

                if (crop.IsMutation)
                {
                    return IsMutationCropUnlocked(crop.Id);
                }

                return GetCountryLevel(crop.CountryId) >= crop.RequiredReputationLevel;
            }

            public int GetCountryLevel(string countryId)
            {
                var reputation = GetCountryProgress(countryId).reputation;
                var level = 0;
                for (var i = 0; i < ReputationThresholds.Length; i++)
                {
                    if (reputation >= ReputationThresholds[i])
                    {
                        level = i;
                    }
                }

                return level;
            }

            public int GetInventoryCount(string itemId)
            {
                for (var i = 0; i < State.inventory.Count; i++)
                {
                    if (State.inventory[i].itemId == itemId)
                    {
                        return State.inventory[i].count;
                    }
                }

                return 0;
            }

            public Prototype3DCountryDef GetSelectedCountry()
            {
                return Prototype3DCatalog.GetCountry(State.selectedCountryId) ?? Prototype3DCatalog.Countries[0];
            }

            public Prototype3DCropDef GetSelectedCrop()
            {
                return Prototype3DCatalog.GetCrop(State.selectedCropId);
            }

            public Prototype3DBiomeDef GetSelectedBiomeForCropPreview()
            {
                var biomes = GetSelectedCountryBiomes();
                for (var i = 0; i < biomes.Count; i++)
                {
                    if (IsBiomeUnlocked(biomes[i].Id))
                    {
                        return biomes[i];
                    }
                }

                return null;
            }

            public List<Prototype3DBiomeDef> GetSelectedCountryBiomes()
            {
                var result = new List<Prototype3DBiomeDef>();
                for (var i = 0; i < Prototype3DCatalog.Biomes.Length; i++)
                {
                    var biome = Prototype3DCatalog.Biomes[i];
                    if (biome.CountryId == State.selectedCountryId)
                    {
                        result.Add(biome);
                    }
                }

                return result;
            }

            public List<Prototype3DPlotStateData> GetPlotsForBiome(string biomeId)
            {
                var result = new List<Prototype3DPlotStateData>();
                for (var i = 0; i < State.plots.Count; i++)
                {
                    if (State.plots[i].biomeId == biomeId)
                    {
                        result.Add(State.plots[i]);
                    }
                }

                return result;
            }

            public List<Prototype3DOrderDef> GetVisibleOrders()
            {
                var result = new List<Prototype3DOrderDef>();
                for (var i = 0; i < Prototype3DCatalog.Orders.Length; i++)
                {
                    var order = Prototype3DCatalog.Orders[i];
                    if (!string.IsNullOrEmpty(order.CountryId) && order.CountryId != State.selectedCountryId)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(order.CountryId) && !IsCountryUnlocked(order.CountryId))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(order.CountryId) && GetCountryLevel(order.CountryId) < order.RequiredReputationLevel)
                    {
                        continue;
                    }

                    result.Add(order);
                }

                return result;
            }

            public Prototype3DMutationRuleDef GetFocusedMutationRule()
            {
                var firstRule = (Prototype3DMutationRuleDef)null;
                for (var i = 0; i < Prototype3DCatalog.MutationRules.Length; i++)
                {
                    var rule = Prototype3DCatalog.MutationRules[i];
                    var crop = Prototype3DCatalog.GetCrop(rule.BaseCropId);
                    if (crop == null || crop.CountryId != State.selectedCountryId)
                    {
                        continue;
                    }

                    if (firstRule == null)
                    {
                        firstRule = rule;
                    }

                    if (!IsMutationCropUnlocked(rule.ResultCropId))
                    {
                        return rule;
                    }
                }

                return firstRule;
            }

            public string FormatRequirements(Prototype3DOrderDef order)
            {
                var result = string.Empty;
                for (var i = 0; i < order.Requirements.Length; i++)
                {
                    var requirement = order.Requirements[i];
                    if (i > 0)
                    {
                        result += " ";
                    }

                    result += GetItemShortName(requirement.ItemId) + requirement.Count + "/" + GetInventoryCount(requirement.ItemId);
                }

                return result;
            }

            public string GetInventorySummary()
            {
                if (State.inventory.Count == 0)
                {
                    return "空仓";
                }

                var result = string.Empty;
                var shown = 0;
                for (var i = 0; i < State.inventory.Count && shown < 4; i++)
                {
                    var stack = State.inventory[i];
                    if (stack.count <= 0)
                    {
                        continue;
                    }

                    if (shown > 0)
                    {
                        result += "\n";
                    }

                    result += GetItemShortName(stack.itemId) + " x" + stack.count;
                    shown++;
                }

                return result;
            }

            public string GetLatestLog()
            {
                return State.logs.Count > 0 ? State.logs[0].message : "点击地块播种，成熟后再点地块收获";
            }

            public void AddLog(string message, bool save = true)
            {
                State.logs.Insert(0, new Prototype3DLogEntry
                {
                    timeSeconds = NowSeconds,
                    message = message
                });

                while (State.logs.Count > 40)
                {
                    State.logs.RemoveAt(State.logs.Count - 1);
                }

                if (save)
                {
                    Save();
                }
            }

            private void Plant(Prototype3DPlotStateData plot)
            {
                var crop = GetSelectedCrop();
                var biome = Prototype3DCatalog.GetBiome(plot.biomeId);
                if (crop == null || biome == null || crop.CountryId != biome.CountryId || !IsCropUnlocked(crop.Id))
                {
                    AddLog("当前作物不能种在这里");
                    return;
                }

                var balance = Prototype3DBalance.Calculate(biome, crop);
                var now = NowSeconds;
                plot.cropId = crop.Id;
                plot.status = Prototype3DPlotStatus.Growing;
                plot.plantedAtSeconds = now;
                plot.matureAtSeconds = now + balance.growthSeconds;
                plot.adaptation = balance.adaptation;
                plot.expectedMinYield = balance.minYield;
                plot.expectedMaxYield = balance.maxYield;
                AddLog("播种：" + crop.ShortName + " 适应" + balance.adaptation.ToString("0.00"));
                Save();
            }

            private void Harvest(Prototype3DPlotStateData plot)
            {
                var crop = Prototype3DCatalog.GetCrop(plot.cropId);
                var biome = Prototype3DCatalog.GetBiome(plot.biomeId);
                var yield = UnityEngine.Random.Range(Mathf.Max(1, plot.expectedMinYield), Mathf.Max(1, plot.expectedMaxYield) + 1);
                AddInventory(plot.cropId, yield);

                var message = "收获：" + (crop != null ? crop.ShortName : "作物") + " x" + yield;
                var rule = crop != null && biome != null ? Prototype3DCatalog.GetMutationRuleForPlanting(crop.Id, biome.Id) : null;
                if (rule != null && crop.NaturalMutationEnabled && !IsMutationCropUnlocked(rule.ResultCropId))
                {
                    var balance = Prototype3DBalance.Calculate(biome, crop);
                    if (UnityEngine.Random.value < balance.mutationChance)
                    {
                        AddInventory(rule.ClueItemId, 1);
                        message += " + " + rule.ClueDisplayName;
                    }
                }

                plot.status = Prototype3DPlotStatus.Empty;
                plot.cropId = string.Empty;
                plot.plantedAtSeconds = 0;
                plot.matureAtSeconds = 0;
                plot.adaptation = 0f;
                plot.expectedMinYield = 0;
                plot.expectedMaxYield = 0;

                AddLog(message, false);
                Save();
            }

            private void NormalizeState()
            {
                if (State.countries == null)
                {
                    State.countries = new List<Prototype3DCountryProgressState>();
                }

                if (State.plots == null)
                {
                    State.plots = new List<Prototype3DPlotStateData>();
                }

                if (State.inventory == null)
                {
                    State.inventory = new List<Prototype3DInventoryStack>();
                }

                if (State.orders == null)
                {
                    State.orders = new List<Prototype3DOrderProgressState>();
                }

                if (State.stableMutationCropIds == null)
                {
                    State.stableMutationCropIds = new List<string>();
                }

                if (State.logs == null)
                {
                    State.logs = new List<Prototype3DLogEntry>();
                }

                for (var i = 0; i < Prototype3DCatalog.Countries.Length; i++)
                {
                    var country = Prototype3DCatalog.Countries[i];
                    var progress = GetCountryProgress(country.Id);
                    if (country.StartsUnlocked)
                    {
                        progress.unlocked = true;
                    }
                }

                for (var i = 0; i < Prototype3DCatalog.Biomes.Length; i++)
                {
                    var biome = Prototype3DCatalog.Biomes[i];
                    for (var slotIndex = 1; slotIndex <= biome.SlotCount; slotIndex++)
                    {
                        var plotId = biome.Id + "_" + slotIndex.ToString("00");
                        if (GetPlot(plotId) != null)
                        {
                            continue;
                        }

                        State.plots.Add(new Prototype3DPlotStateData
                        {
                            plotId = plotId,
                            biomeId = biome.Id,
                            cropId = string.Empty,
                            status = Prototype3DPlotStatus.Empty
                        });
                    }
                }

                for (var i = State.inventory.Count - 1; i >= 0; i--)
                {
                    if (State.inventory[i].count <= 0)
                    {
                        State.inventory.RemoveAt(i);
                    }
                }

                for (var i = State.stableMutationCropIds.Count - 1; i >= 0; i--)
                {
                    var crop = Prototype3DCatalog.GetCrop(State.stableMutationCropIds[i]);
                    if (crop == null || !crop.IsMutation || State.stableMutationCropIds.IndexOf(State.stableMutationCropIds[i]) != i)
                    {
                        State.stableMutationCropIds.RemoveAt(i);
                    }
                }

                for (var i = 0; i < Prototype3DCatalog.Orders.Length; i++)
                {
                    GetOrderProgress(Prototype3DCatalog.Orders[i].Id);
                }
            }

            private string GetFirstUnlockedCropId(string countryId)
            {
                for (var i = 0; i < Prototype3DCatalog.Crops.Length; i++)
                {
                    var crop = Prototype3DCatalog.Crops[i];
                    if (crop.CountryId == countryId && IsCropUnlocked(crop.Id))
                    {
                        return crop.Id;
                    }
                }

                return string.Empty;
            }

            private List<Prototype3DCropDef> GetUnlockedCrops(string countryId)
            {
                var result = new List<Prototype3DCropDef>();
                for (var i = 0; i < Prototype3DCatalog.Crops.Length; i++)
                {
                    var crop = Prototype3DCatalog.Crops[i];
                    if (crop.CountryId == countryId && IsCropUnlocked(crop.Id))
                    {
                        result.Add(crop);
                    }
                }

                return result;
            }

            private string GetItemShortName(string itemId)
            {
                var crop = Prototype3DCatalog.GetCrop(itemId);
                if (crop != null)
                {
                    return crop.ShortName;
                }

                var rule = Prototype3DCatalog.GetMutationRuleByClueItem(itemId);
                if (rule != null)
                {
                    return rule.ClueShortName;
                }

                return itemId;
            }

            private Prototype3DPlotStateData GetPlot(string plotId)
            {
                for (var i = 0; i < State.plots.Count; i++)
                {
                    if (State.plots[i].plotId == plotId)
                    {
                        return State.plots[i];
                    }
                }

                return null;
            }

            private Prototype3DCountryProgressState GetCountryProgress(string countryId)
            {
                for (var i = 0; i < State.countries.Count; i++)
                {
                    if (State.countries[i].countryId == countryId)
                    {
                        return State.countries[i];
                    }
                }

                var progress = new Prototype3DCountryProgressState
                {
                    countryId = countryId,
                    unlocked = false,
                    reputation = 0
                };
                State.countries.Add(progress);
                return progress;
            }

            private Prototype3DOrderProgressState GetOrderProgress(string orderId)
            {
                for (var i = 0; i < State.orders.Count; i++)
                {
                    if (State.orders[i].orderId == orderId)
                    {
                        return State.orders[i];
                    }
                }

                var progress = new Prototype3DOrderProgressState
                {
                    orderId = orderId,
                    completedCount = 0
                };
                State.orders.Add(progress);
                return progress;
            }

            private void AddInventory(string itemId, int count)
            {
                if (count <= 0)
                {
                    return;
                }

                for (var i = 0; i < State.inventory.Count; i++)
                {
                    if (State.inventory[i].itemId == itemId)
                    {
                        State.inventory[i].count += count;
                        return;
                    }
                }

                State.inventory.Add(new Prototype3DInventoryStack
                {
                    itemId = itemId,
                    count = count
                });
            }

            private void RemoveInventory(string itemId, int count)
            {
                for (var i = 0; i < State.inventory.Count; i++)
                {
                    var stack = State.inventory[i];
                    if (stack.itemId != itemId)
                    {
                        continue;
                    }

                    stack.count -= count;
                    if (stack.count <= 0)
                    {
                        State.inventory.RemoveAt(i);
                    }

                    return;
                }
            }
        }

        private static class Prototype3DCatalog
        {
            public static readonly Prototype3DCountryDef[] Countries =
            {
                new Prototype3DCountryDef("cn", "中国 China", "中国", 0, true),
                new Prototype3DCountryDef("jp", "日本 Japan", "日本", 100, false),
                new Prototype3DCountryDef("fr", "法国 France", "法国", 160, false)
            };

            public static readonly Prototype3DBiomeDef[] Biomes =
            {
                new Prototype3DBiomeDef("cn_dry_plain", "cn", "华北旱田", 2, 0),
                new Prototype3DBiomeDef("cn_paddy", "cn", "江南水田", 2, 0),
                new Prototype3DBiomeDef("cn_vegetable_bed", "cn", "城郊菜畦", 2, 0),
                new Prototype3DBiomeDef("cn_terrace_hill", "cn", "丘陵梯田", 1, 3),
                new Prototype3DBiomeDef("jp_snow_paddy", "jp", "雪融水田", 2, 0),
                new Prototype3DBiomeDef("jp_tea_hill", "jp", "山麓茶园", 1, 2),
                new Prototype3DBiomeDef("fr_bordeaux_vineyard", "fr", "波尔多园", 2, 0)
            };

            public static readonly Prototype3DCropDef[] Crops =
            {
                new Prototype3DCropDef("cn_wheat", "cn", "中国小麦", "小麦", "wheat", 3, 5, 120, 0, new[]
                {
                    new Prototype3DAffinity("cn_dry_plain", 1.15f),
                    new Prototype3DAffinity("cn_paddy", 0.45f),
                    new Prototype3DAffinity("cn_vegetable_bed", 0.75f),
                    new Prototype3DAffinity("cn_terrace_hill", 0.65f)
                }),
                new Prototype3DCropDef("cn_rice", "cn", "中国水稻", "水稻", "rice", 4, 8, 300, 0, new[]
                {
                    new Prototype3DAffinity("cn_dry_plain", 0.35f),
                    new Prototype3DAffinity("cn_paddy", 1.20f),
                    new Prototype3DAffinity("cn_vegetable_bed", 0.65f),
                    new Prototype3DAffinity("cn_terrace_hill", 0.55f)
                }),
                new Prototype3DCropDef("cn_cabbage", "cn", "中国白菜", "白菜", "cabbage", 3, 6, 90, 0, new[]
                {
                    new Prototype3DAffinity("cn_dry_plain", 0.75f),
                    new Prototype3DAffinity("cn_paddy", 0.60f),
                    new Prototype3DAffinity("cn_vegetable_bed", 1.15f),
                    new Prototype3DAffinity("cn_terrace_hill", 0.70f)
                }),
                new Prototype3DCropDef("cn_corn", "cn", "中国玉米", "玉米", "corn", 2, 12, 480, 1, new[]
                {
                    new Prototype3DAffinity("cn_dry_plain", 1.10f),
                    new Prototype3DAffinity("cn_paddy", 0.45f),
                    new Prototype3DAffinity("cn_vegetable_bed", 0.65f),
                    new Prototype3DAffinity("cn_terrace_hill", 0.80f)
                }),
                new Prototype3DCropDef("cn_tea", "cn", "中国茶叶", "茶叶", "tea", 2, 20, 1800, 3, new[]
                {
                    new Prototype3DAffinity("cn_dry_plain", 0.40f),
                    new Prototype3DAffinity("cn_paddy", 0.55f),
                    new Prototype3DAffinity("cn_vegetable_bed", 0.50f),
                    new Prototype3DAffinity("cn_terrace_hill", 1.20f)
                }),
                new Prototype3DCropDef("mut_cn_drought_rice", "cn", "耐旱稻", "耐旱稻", "rice", 3, 11, 360, 0, new[]
                {
                    new Prototype3DAffinity("cn_dry_plain", 1.18f),
                    new Prototype3DAffinity("cn_paddy", 0.68f),
                    new Prototype3DAffinity("cn_vegetable_bed", 0.55f),
                    new Prototype3DAffinity("cn_terrace_hill", 0.75f)
                }, true),
                new Prototype3DCropDef("mut_cn_water_wheat", "cn", "水麦芽", "水麦芽", "wheat", 3, 8, 150, 0, new[]
                {
                    new Prototype3DAffinity("cn_dry_plain", 0.70f),
                    new Prototype3DAffinity("cn_paddy", 1.08f),
                    new Prototype3DAffinity("cn_vegetable_bed", 0.80f),
                    new Prototype3DAffinity("cn_terrace_hill", 0.68f)
                }, true),
                new Prototype3DCropDef("jp_rice", "jp", "日本粳米", "粳米", "rice", 4, 10, 360, 0, new[]
                {
                    new Prototype3DAffinity("jp_snow_paddy", 1.20f),
                    new Prototype3DAffinity("jp_tea_hill", 0.50f)
                }),
                new Prototype3DCropDef("jp_tea", "jp", "日本茶叶", "抹茶", "tea", 2, 24, 1500, 2, new[]
                {
                    new Prototype3DAffinity("jp_snow_paddy", 0.55f),
                    new Prototype3DAffinity("jp_tea_hill", 1.18f)
                }),
                new Prototype3DCropDef("mut_jp_snow_rice", "jp", "雪泉稻", "雪泉稻", "rice", 4, 14, 420, 0, new[]
                {
                    new Prototype3DAffinity("jp_snow_paddy", 1.25f),
                    new Prototype3DAffinity("jp_tea_hill", 0.55f)
                }, true),
                new Prototype3DCropDef("fr_grape", "fr", "法国葡萄", "葡萄", "grape", 3, 18, 900, 0, new[]
                {
                    new Prototype3DAffinity("fr_bordeaux_vineyard", 1.22f)
                }),
                new Prototype3DCropDef("mut_fr_seamist_grape", "fr", "海雾葡萄", "海雾葡萄", "grape", 3, 26, 1020, 0, new[]
                {
                    new Prototype3DAffinity("fr_bordeaux_vineyard", 1.28f)
                }, true)
            };

            public static readonly Prototype3DMutationRuleDef[] MutationRules =
            {
                new Prototype3DMutationRuleDef("rule_cn_drought_rice", "cn_rice", "mut_cn_drought_rice", "cn_dry_plain", "耐旱稻", "耐旱线索", "clue_cn_drought_rice", 2, 2, 0.07f),
                new Prototype3DMutationRuleDef("rule_cn_water_wheat", "cn_wheat", "mut_cn_water_wheat", "cn_paddy", "水麦芽", "水麦线索", "clue_cn_water_wheat", 2, 2, 0.07f),
                new Prototype3DMutationRuleDef("rule_jp_snow_rice", "jp_rice", "mut_jp_snow_rice", "jp_snow_paddy", "雪泉稻", "雪泉线索", "clue_jp_snow_rice", 2, 3, 0.07f),
                new Prototype3DMutationRuleDef("rule_fr_seamist_grape", "fr_grape", "mut_fr_seamist_grape", "fr_bordeaux_vineyard", "海雾葡萄", "海雾线索", "clue_fr_seamist_grape", 2, 4, 0.07f)
            };

            public static readonly Prototype3DOrderDef[] Orders =
            {
                new Prototype3DOrderDef("daily_wheat", null, 0, "小麦补给", 85, 8, 0, 0, 0, new[] { new Prototype3DRequirement("cn_wheat", 3) }),
                new Prototype3DOrderDef("daily_cabbage", null, 0, "菜篮订单", 90, 8, 0, 0, 0, new[] { new Prototype3DRequirement("cn_cabbage", 4) }),
                new Prototype3DOrderDef("cn_starter", "cn", 0, "饭馆订单", 180, 18, 10, 1, 14, new[] { new Prototype3DRequirement("cn_rice", 2), new Prototype3DRequirement("cn_cabbage", 2) }),
                new Prototype3DOrderDef("cn_drought", "cn", 0, "耐旱稻订单", 360, 36, 24, 4, 26, new[] { new Prototype3DRequirement("mut_cn_drought_rice", 2) }),
                new Prototype3DOrderDef("jp_rice", "jp", 0, "便当米饭", 210, 22, 12, 1, 18, new[] { new Prototype3DRequirement("jp_rice", 3) }),
                new Prototype3DOrderDef("fr_grape", "fr", 0, "葡萄采买", 260, 25, 14, 1, 20, new[] { new Prototype3DRequirement("fr_grape", 3) })
            };

            public static Prototype3DCountryDef GetCountry(string id)
            {
                for (var i = 0; i < Countries.Length; i++)
                {
                    if (Countries[i].Id == id)
                    {
                        return Countries[i];
                    }
                }

                return null;
            }

            public static Prototype3DBiomeDef GetBiome(string id)
            {
                for (var i = 0; i < Biomes.Length; i++)
                {
                    if (Biomes[i].Id == id)
                    {
                        return Biomes[i];
                    }
                }

                return null;
            }

            public static Prototype3DCropDef GetCrop(string id)
            {
                for (var i = 0; i < Crops.Length; i++)
                {
                    if (Crops[i].Id == id)
                    {
                        return Crops[i];
                    }
                }

                return null;
            }

            public static Prototype3DMutationRuleDef GetMutationRuleForPlanting(string cropId, string biomeId)
            {
                for (var i = 0; i < MutationRules.Length; i++)
                {
                    if (MutationRules[i].BaseCropId == cropId && MutationRules[i].TriggerBiomeId == biomeId)
                    {
                        return MutationRules[i];
                    }
                }

                return null;
            }

            public static Prototype3DMutationRuleDef GetMutationRuleByClueItem(string clueItemId)
            {
                for (var i = 0; i < MutationRules.Length; i++)
                {
                    if (MutationRules[i].ClueItemId == clueItemId)
                    {
                        return MutationRules[i];
                    }
                }

                return null;
            }
        }

        private static class Prototype3DBalance
        {
            public static Prototype3DBalanceResult Calculate(Prototype3DBiomeDef biome, Prototype3DCropDef crop)
            {
                if (biome == null || crop == null)
                {
                    return new Prototype3DBalanceResult(0.05f, 60, 1, 1, 0f);
                }

                var adaptation = Mathf.Clamp(crop.GetAffinity(biome.Id), 0.05f, 1.28f);
                var stress = Mathf.Max(0f, 1f - adaptation);
                var minYield = Mathf.Max(1, Mathf.RoundToInt(crop.BaseYield * Mathf.Clamp(adaptation, 0.18f, 1.35f) * 0.85f));
                var maxYield = Mathf.Max(1, Mathf.RoundToInt(crop.BaseYield * Mathf.Clamp(adaptation, 0.18f, 1.35f) * 1.15f));
                var rule = Prototype3DCatalog.GetMutationRuleForPlanting(crop.Id, biome.Id);
                var mutationChance = 0f;
                if (crop.NaturalMutationEnabled && rule != null)
                {
                    mutationChance = Mathf.Clamp(0.02f + stress * stress * 0.22f + rule.ChanceBonus, 0f, 0.35f);
                }

                return new Prototype3DBalanceResult(
                    adaptation,
                    Mathf.Max(5, Mathf.RoundToInt(crop.GrowthSeconds * (1f + stress * 0.55f))),
                    minYield,
                    maxYield,
                    mutationChance);
            }
        }

        private readonly struct Prototype3DBalanceResult
        {
            public readonly float adaptation;
            public readonly int growthSeconds;
            public readonly int minYield;
            public readonly int maxYield;
            public readonly float mutationChance;

            public Prototype3DBalanceResult(float adaptation, int growthSeconds, int minYield, int maxYield, float mutationChance)
            {
                this.adaptation = adaptation;
                this.growthSeconds = growthSeconds;
                this.minYield = minYield;
                this.maxYield = maxYield;
                this.mutationChance = mutationChance;
            }
        }

        private sealed class Prototype3DCountryDef
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string ShortName;
            public readonly int RequiredExplorationPoints;
            public readonly bool StartsUnlocked;

            public Prototype3DCountryDef(string id, string displayName, string shortName, int requiredExplorationPoints, bool startsUnlocked)
            {
                Id = id;
                DisplayName = displayName;
                ShortName = shortName;
                RequiredExplorationPoints = requiredExplorationPoints;
                StartsUnlocked = startsUnlocked;
            }
        }

        private sealed class Prototype3DBiomeDef
        {
            public readonly string Id;
            public readonly string CountryId;
            public readonly string ShortName;
            public readonly int SlotCount;
            public readonly int RequiredReputationLevel;

            public Prototype3DBiomeDef(string id, string countryId, string shortName, int slotCount, int requiredReputationLevel)
            {
                Id = id;
                CountryId = countryId;
                ShortName = shortName;
                SlotCount = slotCount;
                RequiredReputationLevel = requiredReputationLevel;
            }
        }

        private sealed class Prototype3DCropDef
        {
            public readonly string Id;
            public readonly string CountryId;
            public readonly string DisplayName;
            public readonly string ShortName;
            public readonly string SpeciesId;
            public readonly int BaseYield;
            public readonly int BasePrice;
            public readonly int GrowthSeconds;
            public readonly int RequiredReputationLevel;
            public readonly bool IsMutation;
            public readonly bool NaturalMutationEnabled;
            private readonly Prototype3DAffinity[] affinities;

            public Prototype3DCropDef(
                string id,
                string countryId,
                string displayName,
                string shortName,
                string speciesId,
                int baseYield,
                int basePrice,
                int growthSeconds,
                int requiredReputationLevel,
                Prototype3DAffinity[] affinities,
                bool isMutation = false)
            {
                Id = id;
                CountryId = countryId;
                DisplayName = displayName;
                ShortName = shortName;
                SpeciesId = speciesId;
                BaseYield = baseYield;
                BasePrice = basePrice;
                GrowthSeconds = growthSeconds;
                RequiredReputationLevel = requiredReputationLevel;
                IsMutation = isMutation;
                NaturalMutationEnabled = !isMutation;
                this.affinities = affinities;
            }

            public float GetAffinity(string biomeId)
            {
                for (var i = 0; i < affinities.Length; i++)
                {
                    if (affinities[i].BiomeId == biomeId)
                    {
                        return affinities[i].Value;
                    }
                }

                return 0.25f;
            }
        }

        private readonly struct Prototype3DAffinity
        {
            public readonly string BiomeId;
            public readonly float Value;

            public Prototype3DAffinity(string biomeId, float value)
            {
                BiomeId = biomeId;
                Value = value;
            }
        }

        private sealed class Prototype3DMutationRuleDef
        {
            public readonly string Id;
            public readonly string BaseCropId;
            public readonly string ResultCropId;
            public readonly string TriggerBiomeId;
            public readonly string ShortName;
            public readonly string ClueDisplayName;
            public readonly string ClueItemId;
            public readonly int RequiredClueCount;
            public readonly int RequiredResearchPoints;
            public readonly float ChanceBonus;

            public string ClueShortName
            {
                get { return ClueDisplayName; }
            }

            public Prototype3DMutationRuleDef(string id, string baseCropId, string resultCropId, string triggerBiomeId, string shortName, string clueDisplayName, string clueItemId, int requiredClueCount, int requiredResearchPoints, float chanceBonus)
            {
                Id = id;
                BaseCropId = baseCropId;
                ResultCropId = resultCropId;
                TriggerBiomeId = triggerBiomeId;
                ShortName = shortName;
                ClueDisplayName = clueDisplayName;
                ClueItemId = clueItemId;
                RequiredClueCount = requiredClueCount;
                RequiredResearchPoints = requiredResearchPoints;
                ChanceBonus = chanceBonus;
            }
        }

        private sealed class Prototype3DOrderDef
        {
            public readonly string Id;
            public readonly string CountryId;
            public readonly int RequiredReputationLevel;
            public readonly string ShortName;
            public readonly int RewardCoins;
            public readonly int RewardExperience;
            public readonly int RewardExplorationPoints;
            public readonly int RewardResearchPoints;
            public readonly int RewardCountryReputation;
            public readonly Prototype3DRequirement[] Requirements;

            public Prototype3DOrderDef(string id, string countryId, int requiredReputationLevel, string shortName, int rewardCoins, int rewardExperience, int rewardExplorationPoints, int rewardResearchPoints, int rewardCountryReputation, Prototype3DRequirement[] requirements)
            {
                Id = id;
                CountryId = countryId;
                RequiredReputationLevel = requiredReputationLevel;
                ShortName = shortName;
                RewardCoins = rewardCoins;
                RewardExperience = rewardExperience;
                RewardExplorationPoints = rewardExplorationPoints;
                RewardResearchPoints = rewardResearchPoints;
                RewardCountryReputation = rewardCountryReputation;
                Requirements = requirements;
            }
        }

        private readonly struct Prototype3DRequirement
        {
            public readonly string ItemId;
            public readonly int Count;

            public Prototype3DRequirement(string itemId, int count)
            {
                ItemId = itemId;
                Count = count;
            }
        }
    }
}
