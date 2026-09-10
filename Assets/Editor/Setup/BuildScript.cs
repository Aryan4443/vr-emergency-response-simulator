using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VRSim.EditorTools
{
    /// <summary>
    /// Command-line entry points for producing builds, so the same steps run locally and in CI.
    ///   Unity -batchmode -quit -projectPath . -executeMethod VRSim.EditorTools.BuildScript.BuildQuest
    /// </summary>
    public static class BuildScript
    {
        const string OutputFolder = "Build/Android";

        [MenuItem("Tools/VR Sim/Build Quest APK")]
        public static void BuildQuest()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new Exception("No scenes are enabled in the build settings.");

            Directory.CreateDirectory(OutputFolder);
            var apkPath = Path.Combine(OutputFolder, "VRERTSim.apk");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log($"[Build] Result {summary.result}, {summary.totalSize} bytes, {summary.totalTime} elapsed, output {apkPath}");

            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"Android build failed: {summary.result}");
        }
    }
}
