using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorldFarm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ThirdPartyGrowthStagePreviewScene : MonoBehaviour
    {
        private const string ResourceRoot = "AssetPreview/ThirdParty/Quaternius/GrowthStagesOriginal/";
        private const float MaxItemFootprint = 0.36f;
        private const float RowSpacing = 0.88f;
        private const float TopRowZ = 1.9f;
        private const float VisibleRowCount = 5.3f;
        private const float VerticalDragSensitivity = 8.0f;
        private const float HorizontalDragSensitivity = 4.9f;
        private const float WheelScrollStep = 0.48f;
        private const float DefaultOrthoSize = 4.52f;
        private const float MinOrthoSize = 3.1f;
        private const float MaxOrthoSize = 6.2f;
        private const float PinchZoomSensitivity = 7.0f;
        private const float RowLabelX = -2.18f;
        private const float ContentHalfWidth = 2.32f;

        private static readonly Quaternion QuaterniusOriginalFbxRotation = Quaternion.Euler(-90f, 0f, 0f);
        private static readonly float[] ColumnX = { -1.45f, -0.82f, -0.19f, 0.44f, 1.07f, 1.70f };
        private static readonly string[] ColumnLabels = { "S1", "S2", "S3", "S4", "Crop", "After" };

        private readonly List<Transform> turntables = new List<Transform>(128);
        private Transform scrollRoot;
        private Camera previewCamera;
        private float autoYaw;
        private float targetVerticalScroll;
        private float currentVerticalScroll;
        private float targetHorizontalPan = 0.16f;
        private float currentHorizontalPan = 0.16f;
        private float targetOrthoSize = DefaultOrthoSize;
        private float currentOrthoSize = DefaultOrthoSize;
        private Vector2 lastMousePosition;
        private bool mouseDragging;

        private static readonly CropStageRow[] Crops =
        {
            new CropStageRow("Apple", "Apple", 0.92f, "Apple_Crop", "Apple_Harvested"),
            new CropStageRow("Bamboo", "Bamboo", 1.06f, "Bamboo_Crop", null),
            new CropStageRow("Beet", "Beet", 0.46f, "Beet_Crop", null),
            new CropStageRow("BushBerries", "Berries", 0.48f, "BushBerries_Crop", "BushBerries_Harvested"),
            new CropStageRow("Cactus", "Cactus", 0.86f, "Cactus_Crop", "Cactus_Harvested"),
            new CropStageRow("Carrot", "Carrot", 0.54f, "Carrot_Crop", null),
            new CropStageRow("Corn", "Corn", 0.98f, "Corn_Crop", "Corn_Harvested"),
            new CropStageRow("Flower", "Flower", 0.68f, "Flowers_Crop", "Flowers_Harvested"),
            new CropStageRow("Grass", "Grass", 0.42f, null, null),
            new CropStageRow("Lettuce", "Lettuce", 0.44f, "Lettuce_Crop", "Lettuce_Harvested"),
            new CropStageRow("Mushroom", "Mushroom", 0.48f, "Mushroom_Crop", "Mushroom_Harvested"),
            new CropStageRow("Orange", "Orange", 0.92f, "Orange_Crop", "Orange_Harvested"),
            new CropStageRow("PalmTree", "Palm", 1.14f, "PalmTree_Crop", "PalmTree_Harvested"),
            new CropStageRow("Pumpkin", "Pumpkin", 0.44f, "Pumpkin_Crop", "Pumpkin_Harvested"),
            new CropStageRow("Rice", "Rice", 0.68f, "Rice_Crop", null),
            new CropStageRow("Tomato", "Tomato", 0.70f, "Tomato_Crop", "Tomato_Harvested"),
            new CropStageRow("Watermelon", "Melon", 0.40f, "Watermelon_Crop", "Watermelon_Harvested"),
            new CropStageRow("Wheat", "Wheat", 0.70f, "Wheat_Crop", null),
        };

        private float MaxScroll
        {
            get
            {
                var ortho = previewCamera != null ? previewCamera.orthographicSize : DefaultOrthoSize;
                var visibleRows = Mathf.Clamp(VisibleRowCount * (ortho / DefaultOrthoSize), 3.4f, Crops.Length);
                return Mathf.Max(0f, (Crops.Length - visibleRows) * RowSpacing);
            }
        }

        private float MaxHorizontalPan
        {
            get
            {
                if (previewCamera == null || !previewCamera.orthographic)
                {
                    return 0.64f;
                }

                var visibleHalfWidth = previewCamera.orthographicSize * previewCamera.aspect;
                return Mathf.Max(0f, ContentHalfWidth - visibleHalfWidth + 0.16f);
            }
        }

        private void Awake()
        {
            ConfigureScene();
            BuildPreview();
            ApplyCameraZoom(true);
            ApplyScroll(true);
        }

        private void Update()
        {
            HandlePanAndZoomInput();
            ApplyCameraZoom(false);
            ApplyScroll(false);
            RotateSlots();
        }

        private void ConfigureScene()
        {
            gameObject.name = "WorldFarmThirdPartyGrowthStagePreview";
            RenderSettings.ambientLight = new Color(0.72f, 0.78f, 0.70f);

            previewCamera = Camera.main;
            if (previewCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                previewCamera = cameraObject.AddComponent<Camera>();
            }

            previewCamera.transform.SetPositionAndRotation(new Vector3(0f, 4.8f, -5.9f), Quaternion.Euler(54f, 0f, 0f));
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.84f, 0.89f, 0.78f);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = DefaultOrthoSize;
            currentOrthoSize = DefaultOrthoSize;
            targetOrthoSize = DefaultOrthoSize;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 60f;

            var lightObject = new GameObject("Key Light");
            lightObject.transform.SetPositionAndRotation(new Vector3(-2.4f, 5.6f, -3.2f), Quaternion.Euler(48f, -28f, 0f));
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;

            var fillObject = new GameObject("Fill Light");
            fillObject.transform.SetPositionAndRotation(new Vector3(2.8f, 3.4f, 2.8f), Quaternion.Euler(40f, 138f, 0f));
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.48f;
            fill.shadows = LightShadows.None;
        }

        private void BuildPreview()
        {
            var materials = new PreviewMaterials();

            scrollRoot = new GameObject("ScrollableCropCatalog").transform;
            scrollRoot.SetParent(transform, false);

            BuildColumnLabels(materials);

            for (var row = 0; row < Crops.Length; row++)
            {
                BuildCropRow(Crops[row], row, materials);
            }
        }

        private void BuildColumnLabels(PreviewMaterials materials)
        {
            var labelZ = TopRowZ + 0.48f;
            for (var i = 0; i < ColumnLabels.Length; i++)
            {
                AddText(
                    "ColumnLabel_" + ColumnLabels[i],
                    ColumnLabels[i],
                    new Vector3(ColumnX[i], 0.022f, labelZ),
                    0.034f,
                    TextAnchor.MiddleCenter,
                    materials.Text);
            }
        }

        private void BuildCropRow(CropStageRow crop, int rowIndex, PreviewMaterials materials)
        {
            var rowZ = TopRowZ - rowIndex * RowSpacing;
            var rowRoot = new GameObject(crop.ResourceName + "_Row").transform;
            rowRoot.SetParent(scrollRoot, false);

            AddStage(crop.ResourceName + "_Stage", rowRoot, rowZ, materials.Stage);
            AddText(
                crop.ResourceName + "_Label",
                crop.DisplayName,
                new Vector3(RowLabelX, 0.026f, rowZ),
                0.031f,
                TextAnchor.MiddleLeft,
                materials.Text);

            var models = new Transform[ColumnX.Length];
            for (var column = 0; column < ColumnX.Length; column++)
            {
                var slotRoot = new GameObject(crop.ResourceName + "_Slot_" + ColumnLabels[column]).transform;
                slotRoot.SetParent(rowRoot, false);
                slotRoot.localPosition = new Vector3(ColumnX[column], 0f, rowZ);

                AddSlotPad(slotRoot, materials.Pad);

                var resourceName = crop.GetResourceName(column);
                models[column] = BuildImportedItemModel(resourceName, slotRoot, materials);
                turntables.Add(slotRoot);
            }

            var rowScale = CalculateRowScale(models, crop.MatureTargetHeight);
            for (var i = 0; i < models.Length; i++)
            {
                models[i].localScale = Vector3.one * rowScale;
                CenterAndGround(models[i], i >= 4 ? 0.025f : 0.04f);
            }
        }

        private Transform BuildImportedItemModel(string resourceName, Transform parent, PreviewMaterials materials)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                return BuildMissingMarker(parent, materials);
            }

            var prefab = Resources.Load<GameObject>(ResourceRoot + resourceName);
            if (prefab == null)
            {
                return BuildMissingMarker(parent, materials);
            }

            var model = Instantiate(prefab, parent);
            model.name = resourceName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = QuaterniusOriginalFbxRotation;
            model.transform.localScale = Vector3.one;
            ConfigureRenderers(model);
            return model.transform;
        }

        private Transform BuildMissingMarker(Transform parent, PreviewMaterials materials)
        {
            var markerRoot = new GameObject("Missing").transform;
            markerRoot.SetParent(parent, false);

            AddCylinder("MissingDisc", markerRoot, new Vector3(0f, 0.012f, 0f), 0.075f, 0.014f, materials.Missing);
            return markerRoot;
        }

        private void AddStage(string name, Transform parent, float z, Material material)
        {
            var stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stage.name = name;
            stage.transform.SetParent(parent, false);
            stage.transform.localPosition = new Vector3(0f, -0.032f, z);
            stage.transform.localScale = new Vector3(4.62f, 0.035f, 0.56f);
            stage.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void AddSlotPad(Transform parent, Material material)
        {
            AddCylinder("SlotPad", parent, new Vector3(0f, 0.008f, 0f), 0.185f, 0.012f, material);
        }

        private GameObject AddCylinder(string name, Transform parent, Vector3 localPosition, float radius, float height, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            return cylinder;
        }

        private TextMesh AddText(string name, string text, Vector3 position, float size, TextAnchor anchor, Material material)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(scrollRoot, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 88;
            mesh.characterSize = size;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Center;
            mesh.color = material.color;

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return mesh;
        }

        private float CalculateRowScale(IReadOnlyList<Transform> models, float matureTargetHeight)
        {
            var matureIndex = Mathf.Min(3, models.Count - 1);
            var bounds = GetBounds(models[matureIndex].gameObject);
            var sourceHeight = Mathf.Max(0.001f, bounds.size.y);
            var scale = matureTargetHeight / sourceHeight;

            for (var i = 0; i < models.Count; i++)
            {
                var itemBounds = GetBounds(models[i].gameObject);
                var footprint = Mathf.Max(itemBounds.size.x, itemBounds.size.z);
                if (footprint <= 0.001f)
                {
                    continue;
                }

                scale = Mathf.Min(scale, MaxItemFootprint / footprint);
            }

            return Mathf.Clamp(scale, 0.04f, 2.2f);
        }

        private void CenterAndGround(Transform model, float groundOffset)
        {
            var bounds = GetBounds(model.gameObject);
            var centerOffset = new Vector3(bounds.center.x, bounds.min.y - groundOffset, bounds.center.z);
            model.localPosition -= centerOffset;
        }

        private static Bounds GetBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one * 0.001f);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private void HandlePanAndZoomInput()
        {
            if (Input.touchCount >= 2)
            {
                mouseDragging = false;
                HandleTwoFingerInput();
                return;
            }

            if (Input.touchCount == 1)
            {
                mouseDragging = false;
                AddScreenPan(Input.GetTouch(0).deltaPosition);
                return;
            }

            HandleMouseInput();
        }

        private void HandleTwoFingerInput()
        {
            var first = Input.GetTouch(0);
            var second = Input.GetTouch(1);

            var averageDelta = (first.deltaPosition + second.deltaPosition) * 0.5f;
            AddScreenPan(averageDelta);

            var previousFirst = first.position - first.deltaPosition;
            var previousSecond = second.position - second.deltaPosition;
            var previousDistance = Vector2.Distance(previousFirst, previousSecond);
            var currentDistance = Vector2.Distance(first.position, second.position);
            var distanceDelta = currentDistance - previousDistance;
            AddPinchZoom(distanceDelta);
        }

        private void HandleMouseInput()
        {
            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.001f)
            {
                targetVerticalScroll = Mathf.Clamp(targetVerticalScroll + wheel * WheelScrollStep, 0f, MaxScroll);
            }

            if (Input.GetMouseButtonDown(0))
            {
                mouseDragging = true;
                lastMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                mouseDragging = false;
            }

            if (!mouseDragging || !Input.GetMouseButton(0))
            {
                return;
            }

            var pointer = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            AddScreenPan(pointer - lastMousePosition);
            lastMousePosition = pointer;
        }

        private void AddScreenPan(Vector2 screenDelta)
        {
            var safeWidth = Mathf.Max(1f, Screen.width);
            var safeHeight = Mathf.Max(1f, Screen.height);

            targetHorizontalPan = Mathf.Clamp(
                targetHorizontalPan + (screenDelta.x / safeWidth) * HorizontalDragSensitivity,
                -MaxHorizontalPan,
                MaxHorizontalPan);
            targetVerticalScroll = Mathf.Clamp(
                targetVerticalScroll + (screenDelta.y / safeHeight) * VerticalDragSensitivity,
                0f,
                MaxScroll);
        }

        private void AddPinchZoom(float screenDistanceDelta)
        {
            var normalizedDelta = screenDistanceDelta / Mathf.Max(1f, Screen.height);
            targetOrthoSize = Mathf.Clamp(
                targetOrthoSize - normalizedDelta * PinchZoomSensitivity,
                MinOrthoSize,
                MaxOrthoSize);
        }

        private void ApplyCameraZoom(bool immediate)
        {
            currentOrthoSize = immediate
                ? targetOrthoSize
                : Mathf.Lerp(currentOrthoSize, targetOrthoSize, 1f - Mathf.Exp(-Time.deltaTime * 12f));

            previewCamera.orthographicSize = currentOrthoSize;
            targetVerticalScroll = Mathf.Clamp(targetVerticalScroll, 0f, MaxScroll);
            targetHorizontalPan = Mathf.Clamp(targetHorizontalPan, -MaxHorizontalPan, MaxHorizontalPan);
        }

        private void ApplyScroll(bool immediate)
        {
            currentVerticalScroll = immediate
                ? targetVerticalScroll
                : Mathf.Lerp(currentVerticalScroll, targetVerticalScroll, 1f - Mathf.Exp(-Time.deltaTime * 14f));
            currentHorizontalPan = immediate
                ? targetHorizontalPan
                : Mathf.Lerp(currentHorizontalPan, targetHorizontalPan, 1f - Mathf.Exp(-Time.deltaTime * 14f));

            scrollRoot.localPosition = new Vector3(currentHorizontalPan, 0f, currentVerticalScroll);
        }

        private void RotateSlots()
        {
            autoYaw += Time.deltaTime * 13f;
            var rotation = Quaternion.Euler(0f, autoYaw, 0f);

            for (var i = 0; i < turntables.Count; i++)
            {
                turntables[i].localRotation = rotation;
            }
        }

        private static void ConfigureRenderers(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
            }
        }

        private readonly struct CropStageRow
        {
            public readonly string ResourceName;
            public readonly string DisplayName;
            public readonly float MatureTargetHeight;
            private readonly string cropResourceName;
            private readonly string harvestedResourceName;

            public CropStageRow(string resourceName, string displayName, float matureTargetHeight, string cropResourceName, string harvestedResourceName)
            {
                ResourceName = resourceName;
                DisplayName = displayName;
                MatureTargetHeight = matureTargetHeight;
                this.cropResourceName = cropResourceName;
                this.harvestedResourceName = harvestedResourceName;
            }

            public string GetResourceName(int column)
            {
                if (column < 4)
                {
                    return ResourceName + "_" + (column + 1);
                }

                return column == 4 ? cropResourceName : harvestedResourceName;
            }
        }

        private sealed class PreviewMaterials
        {
            public readonly Material Stage = MakeMaterial("Stage", new Color(0.60f, 0.70f, 0.45f));
            public readonly Material Pad = MakeMaterial("Pad", new Color(0.44f, 0.56f, 0.32f));
            public readonly Material Text = MakeMaterial("Text", new Color(0.16f, 0.22f, 0.15f));
            public readonly Material Missing = MakeMaterial("Missing", new Color(0.48f, 0.48f, 0.43f));

            private static Material MakeMaterial(string name, Color color)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = "ThirdPartyGrowthStagePreview_" + name;
                material.color = color;
                return material;
            }
        }
    }
}
