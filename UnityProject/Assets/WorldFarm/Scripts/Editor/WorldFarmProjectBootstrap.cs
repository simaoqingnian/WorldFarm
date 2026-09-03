using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WorldFarm.Runtime;

namespace WorldFarm.Editor
{
    public static class WorldFarmProjectBootstrap
    {
        private const string CompanyName = "simaoqingnian";
        private const string ProductName = "WorldFarm";
        private const string AndroidPackageName = "com.simaoqingnian.worldfarm";
        private const string MainScenePath = "Assets/WorldFarm/Scenes/Main.unity";
        private const string AssetPreviewScenePath = "Assets/WorldFarm/Scenes/AssetPreview.unity";
        private const string ThirdPartyCropPreviewScenePath = "Assets/WorldFarm/Scenes/ThirdPartyCropPreview.unity";
        private const string ThirdPartyGrowthStagePreviewScenePath = "Assets/WorldFarm/Scenes/ThirdPartyGrowthStagePreview.unity";
        private const string PrototypeGameplayScenePath = "Assets/WorldFarm/Scenes/PrototypeGameplay.unity";
        private const string DebugLaunchScenePath = PrototypeGameplayScenePath;

        [MenuItem("WorldFarm/Bootstrap Project")]
        public static void Bootstrap()
        {
            ApplyPlayerSettings();
            EnsureMainScene();
            EnsureAssetPreviewScene();
            EnsureThirdPartyCropPreviewScene();
            EnsureThirdPartyGrowthStagePreviewScene();
            EnsurePrototypeGameplayScene();
            ConfigureBuildSettings();
            SwitchToAndroidTarget();

            AssetDatabase.SaveAssets();
        }

        [MenuItem("WorldFarm/Build Android Debug APK")]
        public static void BuildAndroidDebugApk()
        {
            Bootstrap();

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds", "Android", "WorldFarm-dev.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { DebugLaunchScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Android build failed: {report.summary.result}");
            }
        }

        [MenuItem("WorldFarm/Build Third Party Crop Preview APK")]
        public static void BuildThirdPartyCropPreviewApk()
        {
            Bootstrap();

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds", "Android", "WorldFarm-thirdparty-crops.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ThirdPartyCropPreviewScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Third-party crop preview build failed: {report.summary.result}");
            }
        }

        [MenuItem("WorldFarm/Build Third Party Growth Stage Preview APK")]
        public static void BuildThirdPartyGrowthStagePreviewApk()
        {
            Bootstrap();

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds", "Android", "WorldFarm-thirdparty-growth-stages.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ThirdPartyGrowthStagePreviewScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Third-party growth stage preview build failed: {report.summary.result}");
            }
        }

        [MenuItem("WorldFarm/Build Prototype Gameplay APK")]
        public static void BuildPrototypeGameplayApk()
        {
            Bootstrap();

            var outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds", "Android", "WorldFarm-prototype-gameplay.apk"));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { PrototypeGameplayScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Prototype gameplay build failed: {report.summary.result}");
            }
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidPackageName);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        }

        private static void EnsureMainScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToAbsoluteProjectPath(MainScenePath)));

            var scene = File.Exists(ToAbsoluteProjectPath(MainScenePath))
                ? EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigureDefaultSceneObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScenePath);
        }

        private static void EnsureAssetPreviewScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToAbsoluteProjectPath(AssetPreviewScenePath)));

            var scene = File.Exists(ToAbsoluteProjectPath(AssetPreviewScenePath))
                ? EditorSceneManager.OpenScene(AssetPreviewScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigureAssetPreviewSceneObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, AssetPreviewScenePath);
        }

        private static void EnsureThirdPartyCropPreviewScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToAbsoluteProjectPath(ThirdPartyCropPreviewScenePath)));

            var scene = File.Exists(ToAbsoluteProjectPath(ThirdPartyCropPreviewScenePath))
                ? EditorSceneManager.OpenScene(ThirdPartyCropPreviewScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigureThirdPartyCropPreviewSceneObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ThirdPartyCropPreviewScenePath);
        }

        private static void EnsureThirdPartyGrowthStagePreviewScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToAbsoluteProjectPath(ThirdPartyGrowthStagePreviewScenePath)));

            var scene = File.Exists(ToAbsoluteProjectPath(ThirdPartyGrowthStagePreviewScenePath))
                ? EditorSceneManager.OpenScene(ThirdPartyGrowthStagePreviewScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigureThirdPartyGrowthStagePreviewSceneObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ThirdPartyGrowthStagePreviewScenePath);
        }

        private static void EnsurePrototypeGameplayScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToAbsoluteProjectPath(PrototypeGameplayScenePath)));

            var scene = File.Exists(ToAbsoluteProjectPath(PrototypeGameplayScenePath))
                ? EditorSceneManager.OpenScene(PrototypeGameplayScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigurePrototypeGameplaySceneObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PrototypeGameplayScenePath);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(PrototypeGameplayScenePath, true),
                new EditorBuildSettingsScene(AssetPreviewScenePath, true),
                new EditorBuildSettingsScene(ThirdPartyCropPreviewScenePath, true),
                new EditorBuildSettingsScene(ThirdPartyGrowthStagePreviewScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }

        private static void ConfigureDefaultSceneObjects()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.88f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.56f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.22f, 0.18f);
            RenderSettings.ambientIntensity = 1.1f;

            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindObjectOfType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            camera.gameObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.orthographic = false;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.transform.position = new Vector3(0f, 0.35f, -8.5f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -0.1f, 0f) - camera.transform.position, Vector3.up);
            camera.backgroundColor = new Color(0.62f, 0.77f, 0.82f);

            var keyLightObject = GameObject.Find("Demo Key Light") ?? GameObject.Find("Directional Light") ?? new GameObject("Demo Key Light");
            keyLightObject.name = "Demo Key Light";

            var keyLight = keyLightObject.GetComponent<Light>();
            if (keyLight == null)
            {
                keyLight = keyLightObject.AddComponent<Light>();
            }

            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.94f, 0.82f);
            keyLight.intensity = 1.35f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.55f;
            keyLightObject.transform.rotation = Quaternion.Euler(48f, -34f, 18f);

            var fillLightObject = GameObject.Find("Demo Fill Light") ?? new GameObject("Demo Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillLightObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.58f, 0.82f, 1f);
            fillLight.intensity = 1.6f;
            fillLight.range = 8f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.position = new Vector3(-2.5f, 2.8f, -3.2f);

            var root = GameObject.Find("WorldFarmRoot") ?? new GameObject("WorldFarmRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            EnsureChild(root.transform, "MapLayer");
            EnsureChild(root.transform, "CropLayer");
            EnsureChild(root.transform, "UILayer");

            if (root.GetComponent<CarrotDemoScene>() == null)
            {
                root.AddComponent<CarrotDemoScene>();
            }

            var duplicateDemoScenes = root.GetComponents<CarrotDemoScene>();
            for (var index = 1; index < duplicateDemoScenes.Length; index++)
            {
                Object.DestroyImmediate(duplicateDemoScenes[index]);
            }
        }

        private static void EnsureChild(Transform parent, string childName)
        {
            if (parent.Find(childName) != null)
            {
                return;
            }

            new GameObject(childName).transform.SetParent(parent, false);
        }

        private static void ConfigureAssetPreviewSceneObjects()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.84f, 0.93f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.66f, 0.54f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.31f, 0.22f);
            RenderSettings.ambientIntensity = 1.20f;

            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindObjectOfType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            camera.gameObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.68f, 0.82f, 0.84f);
            camera.orthographic = true;
            camera.orthographicSize = 4.75f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.transform.position = new Vector3(4.7f, 5.9f, -8.3f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.35f, 0f) - camera.transform.position, Vector3.up);

            var keyLightObject = GameObject.Find("Preview Key Light") ?? GameObject.Find("Directional Light") ?? new GameObject("Preview Key Light");
            keyLightObject.name = "Preview Key Light";

            var keyLight = keyLightObject.GetComponent<Light>();
            if (keyLight == null)
            {
                keyLight = keyLightObject.AddComponent<Light>();
            }

            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.93f, 0.79f);
            keyLight.intensity = 1.26f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.32f;
            keyLightObject.transform.rotation = Quaternion.Euler(48f, -32f, 19f);

            var fillLightObject = GameObject.Find("Preview Fill Light") ?? new GameObject("Preview Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillLightObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.59f, 0.80f, 1f);
            fillLight.intensity = 2.0f;
            fillLight.range = 8f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.position = new Vector3(-2.7f, 3.2f, -3.6f);

            var root = GameObject.Find("WorldFarmAssetPreviewRoot") ?? new GameObject("WorldFarmAssetPreviewRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (root.GetComponent<AssetPreviewScene>() == null)
            {
                root.AddComponent<AssetPreviewScene>();
            }

            var duplicatePreviewScenes = root.GetComponents<AssetPreviewScene>();
            for (var index = 1; index < duplicatePreviewScenes.Length; index++)
            {
                Object.DestroyImmediate(duplicatePreviewScenes[index]);
            }
        }

        private static void ConfigureThirdPartyCropPreviewSceneObjects()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.84f, 0.93f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.64f, 0.68f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.28f, 0.20f);
            RenderSettings.ambientIntensity = 1.18f;

            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindObjectOfType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            camera.gameObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.68f, 0.82f, 0.84f);
            camera.orthographic = true;
            camera.orthographicSize = 4.40f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            camera.transform.position = new Vector3(4.8f, 5.7f, -8.4f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.30f, 0.75f) - camera.transform.position, Vector3.up);

            var keyLightObject = GameObject.Find("ThirdParty Preview Key Light") ?? GameObject.Find("Directional Light") ?? new GameObject("ThirdParty Preview Key Light");
            keyLightObject.name = "ThirdParty Preview Key Light";

            var keyLight = keyLightObject.GetComponent<Light>();
            if (keyLight == null)
            {
                keyLight = keyLightObject.AddComponent<Light>();
            }

            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.93f, 0.78f);
            keyLight.intensity = 1.25f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.28f;
            keyLightObject.transform.rotation = Quaternion.Euler(48f, -32f, 18f);

            var fillLightObject = GameObject.Find("ThirdParty Preview Fill Light") ?? new GameObject("ThirdParty Preview Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillLightObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.59f, 0.80f, 1f);
            fillLight.intensity = 1.8f;
            fillLight.range = 8f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.position = new Vector3(-2.8f, 3.0f, -3.6f);

            var root = GameObject.Find("WorldFarmThirdPartyCropPreviewRoot") ?? new GameObject("WorldFarmThirdPartyCropPreviewRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (root.GetComponent<ThirdPartyCropPreviewScene>() == null)
            {
                root.AddComponent<ThirdPartyCropPreviewScene>();
            }

            var duplicatePreviewScenes = root.GetComponents<ThirdPartyCropPreviewScene>();
            for (var index = 1; index < duplicatePreviewScenes.Length; index++)
            {
                Object.DestroyImmediate(duplicatePreviewScenes[index]);
            }
        }

        private static void ConfigureThirdPartyGrowthStagePreviewSceneObjects()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.84f, 0.93f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.64f, 0.68f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.28f, 0.20f);
            RenderSettings.ambientIntensity = 1.18f;

            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindObjectOfType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            camera.gameObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.68f, 0.82f, 0.84f);
            camera.orthographic = true;
            camera.orthographicSize = 4.60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 90f;
            camera.transform.position = new Vector3(0f, 6.7f, -7.4f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.38f, 0f) - camera.transform.position, Vector3.up);

            var keyLightObject = GameObject.Find("Growth Stage Preview Key Light") ?? GameObject.Find("Directional Light") ?? new GameObject("Growth Stage Preview Key Light");
            keyLightObject.name = "Growth Stage Preview Key Light";

            var keyLight = keyLightObject.GetComponent<Light>();
            if (keyLight == null)
            {
                keyLight = keyLightObject.AddComponent<Light>();
            }

            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.93f, 0.78f);
            keyLight.intensity = 1.23f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.26f;
            keyLightObject.transform.rotation = Quaternion.Euler(50f, -25f, 18f);

            var fillLightObject = GameObject.Find("Growth Stage Preview Fill Light") ?? new GameObject("Growth Stage Preview Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillLightObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.59f, 0.80f, 1f);
            fillLight.intensity = 1.8f;
            fillLight.range = 8f;
            fillLight.shadows = LightShadows.None;
            fillLightObject.transform.position = new Vector3(-2.6f, 3.2f, -3.8f);

            var root = GameObject.Find("WorldFarmThirdPartyGrowthStagePreviewRoot") ?? new GameObject("WorldFarmThirdPartyGrowthStagePreviewRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (root.GetComponent<ThirdPartyGrowthStagePreviewScene>() == null)
            {
                root.AddComponent<ThirdPartyGrowthStagePreviewScene>();
            }

            var duplicatePreviewScenes = root.GetComponents<ThirdPartyGrowthStagePreviewScene>();
            for (var index = 1; index < duplicatePreviewScenes.Length; index++)
            {
                Object.DestroyImmediate(duplicatePreviewScenes[index]);
            }
        }

        private static void ConfigurePrototypeGameplaySceneObjects()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.54f, 0.60f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.27f, 0.22f);
            RenderSettings.ambientIntensity = 1.0f;

            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindObjectOfType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            camera.gameObject.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.86f, 0.90f, 0.82f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;

            var root = GameObject.Find("WorldFarmPrototypeGameplayRoot") ?? new GameObject("WorldFarmPrototypeGameplayRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (root.GetComponent<PrototypeGameplayScene>() == null)
            {
                root.AddComponent<PrototypeGameplayScene>();
            }

            var duplicatePrototypeScenes = root.GetComponents<PrototypeGameplayScene>();
            for (var index = 1; index < duplicatePrototypeScenes.Length; index++)
            {
                Object.DestroyImmediate(duplicatePrototypeScenes[index]);
            }
        }

        private static string ToAbsoluteProjectPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void SwitchToAndroidTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
        }
    }
}
