using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorldFarm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ThirdPartyCropPreviewScene : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedThirdPartyCropPreview";
        private const string ResourceRoot = "AssetPreview/ThirdParty/Quaternius/WorldFarmRoundPass02/";
        private const float MaxPreviewFootprint = 0.86f;
        private static readonly Quaternion PreviewModelRotation = Quaternion.identity;

        [SerializeField] private float autoRotateDegreesPerSecond = 8f;

        private readonly List<Transform> turntables = new List<Transform>();
        private float yaw = -18f;

        private static readonly CropPreviewItem[] Crops =
        {
            new CropPreviewItem("WF_Quaternius_Carrot_RoundPass02", ResourceRoot + "WF_Quaternius_Carrot_RoundPass02", new Vector3(-1.82f, 0f, 1.08f), 1.02f),
            new CropPreviewItem("WF_Quaternius_Corn_RoundPass02", ResourceRoot + "WF_Quaternius_Corn_RoundPass02", new Vector3(-0.91f, 0f, 1.06f), 1.48f),
            new CropPreviewItem("WF_Quaternius_Wheat_RoundPass02", ResourceRoot + "WF_Quaternius_Wheat_RoundPass02", new Vector3(0.10f, 0f, 1.04f), 1.34f),
            new CropPreviewItem("WF_Quaternius_Rice_RoundPass02", ResourceRoot + "WF_Quaternius_Rice_RoundPass02", new Vector3(1.07f, 0f, 1.02f), 1.30f),
            new CropPreviewItem("WF_Quaternius_Lettuce_RoundPass02", ResourceRoot + "WF_Quaternius_Lettuce_RoundPass02", new Vector3(1.86f, 0f, 1.06f), 0.96f),
        };

        private struct CropPreviewItem
        {
            public readonly string Name;
            public readonly string ResourcePath;
            public readonly Vector3 Position;
            public readonly float TargetHeight;

            public CropPreviewItem(string name, string resourcePath, Vector3 position, float targetHeight)
            {
                Name = name;
                ResourcePath = resourcePath;
                Position = position;
                TargetHeight = targetHeight;
            }
        }

        private sealed class PreviewMaterials
        {
            public readonly Material Stage = CreateMaterial("ThirdParty Preview Stage", new Color(0.60f, 0.70f, 0.57f), 0.16f);
            public readonly Material Platform = CreateMaterial("ThirdParty Warm Platform", new Color(0.71f, 0.57f, 0.38f), 0.18f);
            public readonly Material PlatformDark = CreateMaterial("ThirdParty Platform Side", new Color(0.42f, 0.30f, 0.20f), 0.12f);
            public readonly Material Fallback = CreateMaterial("ThirdParty Missing Material", new Color(0.84f, 0.58f, 0.34f), 0.18f);
        }

        private void Awake()
        {
            ConfigureScene();
            BuildPreview();
        }

        private void Update()
        {
            yaw += autoRotateDegreesPerSecond * Time.deltaTime;

            for (var index = 0; index < turntables.Count; index++)
            {
                var turntable = turntables[index];
                if (turntable == null)
                {
                    continue;
                }

                turntable.localRotation = Quaternion.Euler(0f, yaw + index * 16f, 0f);
            }
        }

        private void ConfigureScene()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.84f, 0.93f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.64f, 0.68f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.28f, 0.20f);
            RenderSettings.ambientIntensity = 1.18f;

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
            camera.backgroundColor = new Color(0.68f, 0.82f, 0.84f);
            camera.orthographic = true;
            camera.orthographicSize = 4.05f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.transform.position = new Vector3(4.8f, 5.7f, -8.4f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.30f, 0.75f) - camera.transform.position, Vector3.up);

            var keyLightObject = GameObject.Find("ThirdParty Preview Key Light") ?? GameObject.Find("Directional Light") ?? new GameObject("ThirdParty Preview Key Light");
            keyLightObject.name = "ThirdParty Preview Key Light";

            var keyLight = keyLightObject.GetComponent<Light>() ?? keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.93f, 0.78f);
            keyLight.intensity = 1.25f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.28f;
            keyLightObject.transform.rotation = Quaternion.Euler(48f, -32f, 18f);

            var fillLightObject = GameObject.Find("ThirdParty Preview Fill Light") ?? new GameObject("ThirdParty Preview Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>() ?? fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.59f, 0.80f, 1f);
            fillLight.intensity = 1.8f;
            fillLight.range = 8f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.position = new Vector3(-2.8f, 3.0f, -3.6f);
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

            for (var index = 0; index < Crops.Length; index++)
            {
                var crop = Crops[index];
                var turntable = CreatePreviewSlot(root, "Slot_" + crop.Name, crop.Position, 0.38f, materials.Platform, materials.PlatformDark);
                BuildImportedCrop(crop, turntable, materials);
                turntables.Add(turntable);
            }
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
                "ThirdPartyPreviewStage",
                PrimitiveType.Cube,
                materials.Stage,
                root,
                new Vector3(0f, -0.065f, 0.92f),
                Quaternion.identity,
                new Vector3(4.65f, 0.08f, 1.55f));

            stage.GetComponent<MeshRenderer>().receiveShadows = true;
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

        private static void BuildImportedCrop(CropPreviewItem crop, Transform parent, PreviewMaterials materials)
        {
            var prefab = Resources.Load<GameObject>(crop.ResourcePath);
            if (prefab == null)
            {
                AddPrimitive("Missing_" + crop.Name, PrimitiveType.Cube, materials.Fallback, parent, new Vector3(0f, 0.25f, 0f), Quaternion.identity, Vector3.one * 0.28f);
                return;
            }

            var model = Instantiate(prefab, parent, false);
            model.name = crop.Name;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = PreviewModelRotation;
            model.transform.localScale = Vector3.one;

            PrepareRenderers(model.transform, materials.Fallback);
            FitImportedModelToSlot(model.transform, crop.TargetHeight);
        }

        private static void PrepareRenderers(Transform model, Material fallbackMaterial)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                var materials = renderer.sharedMaterials;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials[materialIndex] == null)
                    {
                        materials[materialIndex] = fallbackMaterial;
                    }
                }

                renderer.sharedMaterials = materials;

                var runtimeMaterials = renderer.materials;
                for (var materialIndex = 0; materialIndex < runtimeMaterials.Length; materialIndex++)
                {
                    ForceOpaqueMaterial(runtimeMaterials[materialIndex]);
                }

                renderer.materials = runtimeMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void ForceOpaqueMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Color"))
            {
                var color = material.GetColor("_Color");
                color.a = 1f;
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                var color = material.GetColor("_BaseColor");
                color.a = 1f;
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 0f);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (material.HasProperty("_AlphaClip"))
            {
                material.SetFloat("_AlphaClip", 0f);
            }

            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        private static void FitImportedModelToSlot(Transform model, float targetHeight)
        {
            if (!TryGetWorldRenderBounds(model, out var bounds))
            {
                return;
            }

            var height = Mathf.Max(bounds.size.y, 0.001f);
            var footprint = Mathf.Max(bounds.size.x, bounds.size.z, 0.001f);
            var scale = Mathf.Min(targetHeight / height, MaxPreviewFootprint / footprint);
            model.localScale = Vector3.one * scale;

            if (!TryGetWorldRenderBounds(model, out bounds))
            {
                return;
            }

            var centerLocal = model.parent.InverseTransformPoint(bounds.center);
            var bottomLocal = model.parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            model.localPosition += new Vector3(-centerLocal.x, -bottomLocal.y, -centerLocal.z);
        }

        private static bool TryGetWorldRenderBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
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

        private static Material CreateMaterial(string name, Color color, float smoothness)
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

            return material;
        }
    }
}
