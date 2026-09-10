using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace VRSim.EditorTools
{
    /// <summary>
    /// Imports the TextMeshPro essential resources and waits for the import to finish.
    ///
    /// TMP will not render a single character until "Assets/TextMesh Pro/Resources/TMP Settings.asset"
    /// exists, and opening the project without it pops a modal TMP Importer dialog that also blocks
    /// automated runs. Importing a unitypackage is asynchronous, so a batch run has to stay alive
    /// until the completion callback arrives rather than passing -quit:
    ///
    ///   Unity -batchmode -nographics -projectPath . \
    ///     -executeMethod VRSim.EditorTools.TextMeshProImporter.ImportEssentials
    /// </summary>
    public static class TextMeshProImporter
    {
        const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        /// <summary>True when TMP has everything it needs to render text.</summary>
        public static bool IsImported =>
            AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath) != null;

        [MenuItem("Tools/VR Sim/Import TextMeshPro Essentials")]
        public static void ImportEssentials()
        {
            if (IsImported)
            {
                Report("TextMeshPro essential resources are already present.", exitCode: 0);
                return;
            }

            AssetDatabase.importPackageCompleted += OnCompleted;
            AssetDatabase.importPackageFailed += OnFailed;
            AssetDatabase.importPackageCancelled += OnCancelled;

            var package = FindEssentialsPackage();
            if (package == null)
            {
                Report("Could not find the TMP Essential Resources package.", exitCode: 1);
                return;
            }

            Debug.Log($"[TMP] Importing {package}");
            AssetDatabase.ImportPackage(package, false);
        }

        static string FindEssentialsPackage()
        {
            // Ships inside the com.unity.ugui package, whose cache folder name carries a hash.
            return Directory
                .GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage",
                    SearchOption.AllDirectories)
                .FirstOrDefault();
        }

        static void OnCompleted(string packageName)
        {
            AssetDatabase.Refresh();
            Report(IsImported
                ? $"Imported {packageName}. TMP Settings created."
                : $"Imported {packageName} but TMP Settings is still missing.", IsImported ? 0 : 1);
        }

        static void OnFailed(string packageName, string error) =>
            Report($"Importing {packageName} failed: {error}", exitCode: 1);

        static void OnCancelled(string packageName) =>
            Report($"Importing {packageName} was cancelled.", exitCode: 1);

        static void Report(string message, int exitCode)
        {
            Debug.Log($"[TMP] {message}");

            // Only a batch run owns the editor's lifetime; an interactive one must stay open.
            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }
    }
}
