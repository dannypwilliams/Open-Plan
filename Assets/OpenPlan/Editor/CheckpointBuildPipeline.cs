using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OpenPlan.Editor
{
    /// <summary>Non-interactive Windows player entry point used by the reusable checkpoint packager.</summary>
    public static class CheckpointBuildPipeline
    {
        public const string BuildPathArgument = "-checkpointBuildPath";

        public static void BuildWindowsPlayer()
        {
            string output = ReadArgument(BuildPathArgument);
            if (string.IsNullOrWhiteSpace(output))
                throw new BuildFailedException(BuildPathArgument + " is required.");
            output = Path.GetFullPath(output);
            string directory = Path.GetDirectoryName(output);
            if (string.IsNullOrWhiteSpace(directory))
                throw new BuildFailedException("Checkpoint build path has no parent directory.");
            if (File.Exists(output) || Directory.Exists(Path.Combine(directory, "OpenPlan_Data")))
                throw new BuildFailedException("Checkpoint build destination must be empty: " + directory);

            Directory.CreateDirectory(directory);
            ValidateCommittedBuildInputs();
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/OpenPlan/Scenes/MainMenu.unity",
                    "Assets/OpenPlan/Scenes/Office.unity"
                },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Checkpoint Windows build failed: " + report.summary.result);
            if ((report.summary.options & BuildOptions.Development) != 0)
                throw new BuildFailedException("Checkpoint build unexpectedly enabled Development mode.");

            Debug.Log("CHECKPOINT WINDOWS BUILD: PASS path=" + output +
                      " bytes=" + report.summary.totalSize +
                      " result=" + report.summary.result +
                      " options=" + report.summary.options);
        }

        private static void ValidateCommittedBuildInputs()
        {
            CameraZoomProfile profile = AssetDatabase.LoadAssetAtPath<CameraZoomProfile>(
                "Assets/OpenPlan/Resources/CameraZoomProfile.asset");
            if (profile == null || Mathf.Abs(profile.zoomSensitivity - .13f) > .0001f)
                throw new BuildFailedException("Committed camera profile is missing normalized checkpoint sensitivity.");
            OfficeAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<OfficeAssetCatalog>(
                "Assets/OpenPlan/Resources/OpenPlanAssetCatalog.asset");
            if (catalog == null || catalog.GetPrefab("Worker") == null)
                throw new BuildFailedException("Committed worker catalog entry is missing.");
            foreach (string scene in new[]
                     {
                         "Assets/OpenPlan/Scenes/MainMenu.unity",
                         "Assets/OpenPlan/Scenes/Office.unity"
                     })
                if (!File.Exists(scene)) throw new BuildFailedException("Committed scene is missing: " + scene);
        }

        private static string ReadArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase) &&
                    i + 1 < arguments.Length)
                    return arguments[i + 1];
                string prefix = name + "=";
                if (arguments[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arguments[i].Substring(prefix.Length);
            }
            return null;
        }
    }
}
