using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace VRSim.EditorTools
{
    /// <summary>
    /// One-shot project configuration for the VR Emergency Response Training Simulator.
    /// Creates the folder layout from the specification, installs a Quest-oriented URP
    /// pipeline asset, enables OpenXR for Android and desktop, and applies the Android
    /// player settings required by a standalone Quest build.
    ///
    /// Safe to run more than once: every step checks for existing state first.
    /// Run from the menu, or in batch mode with
    ///   -executeMethod VRSim.EditorTools.ProjectBootstrap.RunAll
    /// </summary>
    public static class ProjectBootstrap
    {
        const string SettingsFolder = "Assets/Settings";
        const string XrFolder = "Assets/XR";
        const string ScenesFolder = "Assets/Scenes";

        static readonly string[] ProjectFolders =
        {
            "Assets/Scenes",
            "Assets/Scripts",
            "Assets/Scripts/Core",
            "Assets/Scripts/Interaction",
            "Assets/Scripts/Scenario",
            "Assets/Scripts/Evaluation",
            "Assets/Scripts/Accessibility",
            "Assets/Scripts/UI",
            "Assets/Prefabs",
            "Assets/Art",
            "Assets/Art/Models",
            "Assets/Art/Materials",
            "Assets/Art/Textures",
            "Assets/Audio",
            "Assets/Data",
            "Assets/Settings",
            "Assets/Tests",
            "Assets/Tests/EditMode",
            "Assets/Tests/PlayMode",
        };

        static readonly string[] ScenePaths =
        {
            ScenesFolder + "/MainMenu.unity",
            ScenesFolder + "/TrainingRoom.unity",
            ScenesFolder + "/FireEvacuation.unity",
            ScenesFolder + "/Earthquake.unity",
        };

        [MenuItem("Tools/VR Sim/Run Project Bootstrap")]
        public static void RunAll()
        {
            CreateFolders();
            CreateScenes();
            ConfigureUniversalRenderPipeline();
            ConfigureXr();
            ConfigurePlayerSettings();
            ConfigureQuality();
            ImportTextMeshProEssentials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Bootstrap] Project bootstrap finished.");
        }

        // ------------------------------------------------------------------ folders

        static void CreateFolders()
        {
            foreach (var folder in ProjectFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
                    Debug.Log($"[Bootstrap] Created folder {folder}");
                }

                // Git cannot store empty directories, so keep a marker until real content lands.
                var keep = folder + "/.gitkeep";
                if (!File.Exists(keep))
                    File.WriteAllText(keep, string.Empty);
            }

            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------- scenes

        static void CreateScenes()
        {
            var buildScenes = new List<EditorBuildSettingsScene>();

            foreach (var path in ScenePaths)
            {
                if (!File.Exists(path))
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(scene, path);
                    Debug.Log($"[Bootstrap] Created scene {path}");
                }

                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }

            Debug.Log($"[Bootstrap] Registered {buildScenes.Count} scenes in the build settings.");

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        // ---------------------------------------------------------------------- URP

        static void ConfigureUniversalRenderPipeline()
        {
            const string rendererPath = SettingsFolder + "/QuestRenderer.asset";
            const string pipelinePath = SettingsFolder + "/QuestRenderPipeline.asset";

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (rendererData == null)
                {
                    rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, rendererPath);
                }

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
                Debug.Log($"[Bootstrap] Created URP asset {pipelinePath}");
            }

            // Mobile-friendly defaults for a standalone headset.
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false;
            pipeline.shadowDistance = 25f;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;

            var current = QualitySettings.GetQualityLevel();
            for (var level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        // ----------------------------------------------------------------------- XR

        static void ConfigureXr()
        {
            if (!AssetDatabase.IsValidFolder(XrFolder))
                AssetDatabase.CreateFolder("Assets", "XR");

            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var settings = GetOrCreateXrSettings(group);
                if (settings == null)
                {
                    Debug.LogError($"[Bootstrap] Could not create XR settings for {group}.");
                    continue;
                }

                if (!XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group))
                    Debug.LogWarning($"[Bootstrap] Could not assign the OpenXR loader for {group}.");
                else
                    Debug.Log($"[Bootstrap] OpenXR loader enabled for {group}.");

                settings.InitManagerOnStart = true;
                EditorUtility.SetDirty(settings);

                EnableOpenXrFeatures(group);
            }
        }

        static XRGeneralSettings GetOrCreateXrSettings(BuildTargetGroup group)
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget perBuildTarget);

            if (perBuildTarget == null)
            {
                perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perBuildTarget, XrFolder + "/XRGeneralSettingsPerBuildTarget.asset");
                AssetDatabase.SaveAssets();
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
            }

            if (!perBuildTarget.HasManagerSettingsForBuildTarget(group))
                perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(group);

            return perBuildTarget.SettingsForBuildTarget(group);
        }

        static void EnableOpenXrFeatures(BuildTargetGroup group)
        {
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);

            var openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (openXr == null)
            {
                Debug.LogWarning($"[Bootstrap] No OpenXR settings for {group}.");
                return;
            }

            // Interaction profiles and the Quest feature group are identified by type name so
            // this script does not need a hard reference to the Meta support assembly.
            var wanted = group == BuildTargetGroup.Android
                ? new[] { "OculusTouchControllerProfile", "MetaQuestFeature", "MetaQuestTouchProControllerProfile" }
                : new[] { "OculusTouchControllerProfile", "ValveIndexControllerProfile", "MicrosoftMotionControllerProfile" };

            foreach (var feature in openXr.GetFeatures())
            {
                if (feature == null) continue;

                var name = feature.GetType().Name;
                if (!wanted.Contains(name)) continue;

                feature.enabled = true;
                EditorUtility.SetDirty(feature);
                Debug.Log($"[Bootstrap] Enabled OpenXR feature {name} for {group}.");
            }

            EditorUtility.SetDirty(openXr);
        }

        // ------------------------------------------------------------ player settings

        static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Aryan Lakhani";
            PlayerSettings.productName = "VR Emergency Response Training Simulator";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.aryanlakhani.vrertsim");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetMobileMTRendering(android, true);

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.optimizedFramePacing = false;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.startInFullscreen = true;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Vulkan first, GLES3 as the fallback, matching Meta's guidance for Quest 2 and 3.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3,
            });

            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // XR Interaction Toolkit 3.x requires the Input System package.
            PlayerSettings.SetPropertyInt("activeInputHandler", 1, BuildTargetGroup.Standalone);
        }

        static void ConfigureQuality()
        {
            var current = QualitySettings.GetQualityLevel();
            for (var level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, false);
                QualitySettings.vSyncCount = 0;   // the headset compositor drives the frame rate
                QualitySettings.antiAliasing = 4;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        // -------------------------------------------------------------------- TextMeshPro

        static void ImportTextMeshProEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                return;

            var package = Directory
                .GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (package == null)
            {
                Debug.LogWarning("[Bootstrap] TMP Essential Resources package not found; import it from the Package Manager.");
                return;
            }

            // Importing a package is asynchronous, so it cannot be completed inside a batch run
            // that quits straight afterwards. TextMeshProImporter handles that case.
            AssetDatabase.ImportPackage(package, false);
            Debug.Log("[Bootstrap] Requested the TextMeshPro essential resources import. " +
                      "Run TextMeshProImporter.ImportEssentials in batch mode if it does not appear.");
        }
    }
}
