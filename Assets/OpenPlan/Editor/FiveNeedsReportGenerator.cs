using System.IO;
using UnityEditor;
using UnityEngine;

namespace OpenPlan.Editor
{
    public static class FiveNeedsReportGenerator
    {
        [MenuItem("Open Plan/Generate Prompt 01 Need Report")]
        public static void Generate()
        {
            var results = FiveNeedsDeterministicMatrix.RunAll();
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "outputs", "TestResults"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "01_FiveNeeds_Deterministic_Report.md");
            File.WriteAllText(path, FiveNeedsDeterministicMatrix.BuildMarkdown(results));
            Debug.Log("FIVE NEED REPORT WRITTEN: " + path);
        }

        public static void GenerateFromCommandLine()
        {
            Generate();
            EditorApplication.Exit(0);
        }
    }
}
