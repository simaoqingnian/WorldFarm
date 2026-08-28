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
        private const string DebugLaunchScenePath = AssetPreviewScenePath;

        [MenuItem("WorldFarm/Bootstrap Project")]
        public static void Bootstrap()
        {
            ApplyPlayerSettings();
            EnsureMainScene();
            EnsureAssetPreviewScene();
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

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(AssetPreviewScenePath, true),
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
            RenderSettings.ambientSkyColor = new Color(0.80f, 0.89f, 0.91f);
            RenderSettings.ambientEquatorColor = new Color(0.53f, 0.58f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.20f, 0.15f);
            RenderSettings.ambientIntensity = 1.08f;

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
            camera.backgroundColor = new Color(0.62f, 0.76f, 0.80f);
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
            keyLight.intensity = 1.32f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.48f;
            keyLightObject.transform.rotation = Quaternion.Euler(48f, -32f, 19f);

            var fillLightObject = GameObject.Find("Preview Fill Light") ?? new GameObject("Preview Fill Light");
            var fillLight = fillLightObject.GetComponent<Light>();
            if (fillLight == null)
            {
                fillLight = fillLightObject.AddComponent<Light>();
            }

            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.59f, 0.80f, 1f);
            fillLight.intensity = 1.4f;
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
