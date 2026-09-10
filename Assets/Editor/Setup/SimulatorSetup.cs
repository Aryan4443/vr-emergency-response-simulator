using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace VRSim.EditorTools
{
    /// <summary>
    /// Sets the project up for development without a headset attached.
    ///
    /// macOS has no OpenXR runtime, so the XR Device Simulator drives the head and both
    /// controllers from the mouse and keyboard in play mode instead. Android keeps its
    /// OpenXR loader so Quest builds are unaffected.
    /// </summary>
    public static class SimulatorSetup
    {
        const string XriPackage = "com.unity.xr.interaction.toolkit";

        static readonly string[] WantedSamples =
        {
            "Starter Assets",
            "XR Device Simulator",
        };

        [MenuItem("Tools/VR Sim/Set Up Headless Development")]
        public static void Run()
        {
            ImportSamples();
            DisableDesktopXrAutoStart();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Simulator] Headless development setup finished.");
        }

        static void ImportSamples()
        {
            var samples = Sample.FindByPackage(XriPackage, string.Empty).ToList();
            if (samples.Count == 0)
            {
                Debug.LogError($"[Simulator] No samples found for {XriPackage}.");
                return;
            }

            foreach (var wanted in WantedSamples)
            {
                var sample = samples.FirstOrDefault(s => s.displayName == wanted);
                if (sample.displayName == null)
                {
                    Debug.LogWarning($"[Simulator] Sample '{wanted}' not found.");
                    continue;
                }

                if (sample.isImported)
                {
                    Debug.Log($"[Simulator] Sample '{wanted}' already imported.");
                    continue;
                }

                Debug.Log(sample.Import(Sample.ImportOptions.OverridePreviousImports)
                    ? $"[Simulator] Imported sample '{wanted}'."
                    : $"[Simulator] Failed to import sample '{wanted}'.");
            }
        }

        static void DisableDesktopXrAutoStart()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget perBuildTarget);

            if (perBuildTarget == null)
                return;

            var standalone = perBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (standalone == null)
                return;

            // Without this the editor logs an OpenXR initialisation failure on every play,
            // because macOS has no OpenXR runtime installed.
            standalone.InitManagerOnStart = false;
            EditorUtility.SetDirty(standalone);
            Debug.Log("[Simulator] Disabled XR auto-start for Standalone so play mode uses the simulator.");
        }
    }
}
