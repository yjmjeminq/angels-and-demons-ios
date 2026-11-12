#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AngelsAndDemons.EditorTools
{
    public static class DefenseBuilder
    {
        public const string BuildVersion = "1.0.0";
        public const string ScenePath = "Assets/AngelsAndDemons/Scenes/SanctuaryWar.unity";

        [Serializable]
        sealed class BuildProof
        {
            public string version;
            public string target;
            public string result;
            public string output;
            public int errors;
            public int warnings;
            public ulong bytes;
            public double seconds;
        }

        [MenuItem("AngelsAndDemons/Create Playable Scene")]
        public static void CreateScene()
        {
            ConfigureSettings();
            ImportAssets();
            Directory.CreateDirectory("Assets/AngelsAndDemons/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("AngelsAndDemonsGame").AddComponent<DefenseGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        static void ConfigureSettings()
        {
            PlayerSettings.companyName = "Jemin";
            PlayerSettings.productName = "Angels And Demons";
            PlayerSettings.bundleVersion = BuildVersion;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.yjmjemin.angelsanddemons");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.yjmjemin.angelsanddemons");
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });

            var objects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (objects.Length > 0)
            {
                var settings = new SerializedObject(objects[0]);
                var input = settings.FindProperty("activeInputHandler");
                if (input != null)
                {
                    input.intValue = 0;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = ShadowQuality.Disable;

            var haptics = AssetImporter.GetAtPath("Assets/Plugins/iOS/AngelsHaptics.mm") as PluginImporter;
            if (haptics != null && (haptics.GetCompatibleWithAnyPlatform() || !haptics.GetCompatibleWithPlatform(BuildTarget.iOS) || haptics.GetCompatibleWithEditor()))
            {
                haptics.SetCompatibleWithAnyPlatform(false);
                haptics.SetCompatibleWithEditor(false);
                haptics.SetCompatibleWithPlatform(BuildTarget.iOS, true);
                haptics.SaveAndReimport();
            }
        }

        public static void ImportAssets()
        {
            if (!Directory.Exists("Assets/Resources/AngelsAndDemons")) return;
            foreach (string path in Directory.GetFiles("Assets/Resources/AngelsAndDemons", "*.png", SearchOption.AllDirectories))
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                bool changed = false;
                if (importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize != 4096 || importer.textureCompression != TextureImporterCompression.Uncompressed || !importer.isReadable)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.isReadable = true;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.maxTextureSize = 4096;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    changed = true;
                }
                var ios = importer.GetPlatformTextureSettings("iPhone");
                if (!ios.overridden || ios.format != TextureImporterFormat.ASTC_6x6 || ios.maxTextureSize != 4096)
                {
                    ios.name = "iPhone";
                    ios.overridden = true;
                    ios.maxTextureSize = 4096;
                    ios.format = TextureImporterFormat.ASTC_6x6;
                    importer.SetPlatformTextureSettings(ios);
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        }

        [MenuItem("AngelsAndDemons/Verify and Build Windows (No Launch)")]
        public static void VerifyAndBuildWindows()
        {
            AssetDatabase.Refresh();
            CreateScene();
            DefenseTests.Run();
            CreateScene();
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/AngelsAndDemons.exe");
        }

        [MenuItem("AngelsAndDemons/Export iOS Xcode Project")]
        public static void ExportIOS()
        {
            AssetDatabase.Refresh();
            CreateScene();
            DefenseTests.Run();
            CreateScene();
            Build(BuildTarget.iOS, "Builds/iOS-Xcode");
        }

        static void Build(BuildTarget target, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = target,
                locationPathName = outputPath,
                options = BuildOptions.CompressWithLz4HC
            });
            Directory.CreateDirectory("BuildLogs");
            File.WriteAllText("BuildLogs/build-" + target + ".json", JsonUtility.ToJson(new BuildProof
            {
                version = PlayerSettings.bundleVersion,
                target = target.ToString(),
                result = report.summary.result.ToString(),
                errors = report.summary.totalErrors,
                warnings = report.summary.totalWarnings,
                bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds,
                output = Path.GetFullPath(outputPath)
            }, true));

            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
            {
                throw new BuildFailedException(target + " build failed or logged errors");
            }
            Debug.Log("ANGELS_AND_DEMONS_BUILD_OK " + target + " " + outputPath);
        }
    }
}
#endif
