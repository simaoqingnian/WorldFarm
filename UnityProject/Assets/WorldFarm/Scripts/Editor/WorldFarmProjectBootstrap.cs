using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WorldFarm.Editor
{
    public static class WorldFarmProjectBootstrap
    {
        private const string CompanyName = "simaoqingnian";
        private const string ProductName = "WorldFarm";
        private const string AndroidPackageName = "com.simaoqingnian.worldfarm";
        private const string MainScenePath = "Assets/WorldFarm/Scenes/Main.unity";

        [MenuItem("WorldFarm/Bootstrap Project")]
        public static void Bootstrap()
        {
            ApplyPlayerSettings();
            EnsureMainScene();
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
                scenes = new[] { MainScenePath },
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
            Directory.CreateDirectory(Path.GetDirectoryName(MainScenePath));

            if (!File.Exists(MainScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                ConfigureDefaultSceneObjects();
                EditorSceneManager.SaveScene(scene, MainScenePath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };
        }

        private static void ConfigureDefaultSceneObjects()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5.5f;
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.backgroundColor = new Color(0.56f, 0.72f, 0.78f);
            }

            var root = new GameObject("WorldFarmRoot");
            root.transform.position = Vector3.zero;

            new GameObject("MapLayer").transform.SetParent(root.transform);
            new GameObject("CropLayer").transform.SetParent(root.transform);
            new GameObject("UILayer").transform.SetParent(root.transform);
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
