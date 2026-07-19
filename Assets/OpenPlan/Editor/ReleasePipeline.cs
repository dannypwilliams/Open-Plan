using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TMPro;

namespace OpenPlan.Editor
{
    public static class ReleasePipeline
    {
        private const string Root = "Assets/OpenPlan";
        private const string ResourcesFolder = Root + "/Resources";
        private const string MaterialsFolder = Root + "/Art/Materials";

        [MenuItem("OPEN PLAN/Generate Complete Project")]
        public static void GenerateProject()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureModels();
            ConfigureTextMeshPro();
            Dictionary<string, Material> materials = CreateMaterials();
            CreateCatalog(materials);
            CreateCameraProfile();
            ConfigureRenderPipeline();
            CreateScenes();
            ConfigureProject();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("OPEN PLAN PROJECT GENERATION: PASS");
        }

        [MenuItem("OPEN PLAN/Build Windows Release")]
        public static void BuildWindows()
        {
            GenerateProject();
            string output = Path.GetFullPath("outputs/OpenPlan-Windows/OpenPlan.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { Root + "/Scenes/MainMenu.unity", Root + "/Scenes/Office.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("OPEN PLAN Windows build failed: " + report.summary.result);
            Debug.Log($"OPEN PLAN WINDOWS BUILD: PASS ({report.summary.totalSize} bytes)");
        }

        [MenuItem("OPEN PLAN/Audit Imported Models")]
        public static void AuditImportedModels()
        {
            foreach (string key in new[] { "FloorSlab", "Desk_A", "Worker", "PartialWall", "Elevator" })
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Art/Models/OP_{key}.fbx");
                if (model == null) { Debug.LogError("AUDIT missing " + key); continue; }
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool started = false;
                var materialNames = new HashSet<string>();
                foreach (Renderer renderer in renderers)
                {
                    if (!started) { bounds = renderer.bounds; started = true; } else bounds.Encapsulate(renderer.bounds);
                    foreach (Material material in renderer.sharedMaterials) if (material != null) materialNames.Add(material.name);
                }
                ModelImporter importer = AssetImporter.GetAtPath($"{Root}/Art/Models/OP_{key}.fbx") as ModelImporter;
                Debug.Log($"AUDIT {key}: bounds={bounds.size} rootScale={model.transform.localScale} importerScale={importer?.globalScale} fileScale={importer?.useFileScale} materials=[{string.Join(",", materialNames)}]");
            }
        }

        private static void EnsureFolders()
        {
            string[] folders = { ResourcesFolder, MaterialsFolder, Root + "/Scenes", Root + "/Art/Prefabs" };
            foreach (string folder in folders)
            {
                string current = "Assets";
                foreach (string segment in folder.Split('/').Skip(1))
                {
                    string next = current + "/" + segment;
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segment);
                    current = next;
                }
            }
        }

        private static void ConfigureModels()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Art/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
                importer.globalScale = 1f;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.meshCompression = ModelImporterMeshCompression.Low;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var colors = new Dictionary<string, Color>
            {
                { "walnut", Hex("704735") }, { "light_wood", Hex("A66E43") }, { "cream", Hex("D7C7A8") },
                { "burgundy", Hex("6A3440") }, { "carpet", Hex("4A464C") }, { "metal", Hex("716A62") },
                { "dark", Hex("161A1B") }, { "cyan", Hex("42B8C4") }, { "blue", Hex("315C68") },
                { "amber", Hex("FF7A2D") }, { "green", Hex("6FC38B") }, { "paper", Hex("E7D7B7") },
                { "cardboard", Hex("A56E3C") }, { "leaf", Hex("397052") }, { "skin", Hex("C7825D") },
                { "coral", Hex("E75B4D") }
            };
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            var result = new Dictionary<string, Material>();
            foreach (KeyValuePair<string, Color> pair in colors)
            {
                string path = $"{MaterialsFolder}/OP_{pair.Key}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = "OP_" + pair.Key };
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetColor("_BaseColor", pair.Value);
                material.SetFloat("_Smoothness", pair.Key == "metal" ? .34f : .14f);
                material.SetFloat("_Metallic", pair.Key == "metal" || pair.Key == "dark" ? .18f : 0f);
                if (pair.Key == "cyan" || pair.Key == "blue" || pair.Key == "amber" || pair.Key == "green")
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", pair.Value * (pair.Key == "amber" ? 1.5f : .85f));
                }
                EditorUtility.SetDirty(material);
                result[pair.Key] = material;
            }
            return result;
        }

        private static void ConfigureTextMeshPro()
        {
            const string canonical = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(canonical) == null)
            {
                AssetDatabase.DeleteAsset(ResourcesFolder + "/TMP Settings.asset");
                UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
                string essentials = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
                AssetDatabase.ImportPackage(essentials, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(canonical) == null)
                throw new FileNotFoundException("Failed to import TextMesh Pro essential resources.", canonical);
        }

        private static OfficeAssetCatalog CreateCatalog(Dictionary<string, Material> materials)
        {
            string path = ResourcesFolder + "/OpenPlanAssetCatalog.asset";
            OfficeAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<OfficeAssetCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<OfficeAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, path);
            }
            catalog.prefabs.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Art/Models" }))
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                string key = Path.GetFileNameWithoutExtension(modelPath).Replace("OP_", string.Empty);
                catalog.prefabs.Add(new OfficeAssetCatalog.NamedPrefab { key = key, prefab = model });
            }
            catalog.prefabs.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            catalog.materials.Clear();
            foreach (KeyValuePair<string, Material> pair in materials)
                catalog.materials.Add(new OfficeAssetCatalog.NamedMaterial { key = pair.Key, material = pair.Value });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void CreateCameraProfile()
        {
            string path = ResourcesFolder + "/CameraZoomProfile.asset";
            CameraZoomProfile profile = AssetDatabase.LoadAssetAtPath<CameraZoomProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CameraZoomProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.closeSize = 4.8f;
            profile.overviewSize = 18.5f;
            profile.zoomSensitivity = .012f;
            profile.panSensitivity = .018f;
            profile.smoothTime = .16f;
            EditorUtility.SetDirty(profile);
        }

        private static void ConfigureRenderPipeline()
        {
            string rendererPath = ResourcesFolder + "/OpenPlanRenderer.asset";
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "Open Plan Forward Renderer";
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            string path = ResourcesFolder + "/OpenPlanURP.asset";
            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            bool invalid = asset == null;
            if (!invalid)
            {
                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty list = serialized.FindProperty("m_RendererDataList");
                invalid = list == null || list.arraySize == 0 || list.GetArrayElementAtIndex(0).objectReferenceValue == null;
            }
            if (invalid)
            {
                if (asset != null) AssetDatabase.DeleteAsset(path);
                asset = UniversalRenderPipelineAsset.Create(renderer);
                asset.name = "Open Plan URP";
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.renderScale = 1f;
            asset.msaaSampleCount = 4;
            asset.shadowDistance = 55f;
            asset.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = asset;
            QualitySettings.renderPipeline = asset;
            EditorUtility.SetDirty(asset);
        }

        private static void CreateScenes()
        {
            Scene menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Main Menu").AddComponent<MainMenuController>();
            EditorSceneManager.SaveScene(menu, Root + "/Scenes/MainMenu.unity");

            Scene office = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("OPEN PLAN");
            root.AddComponent<SimulationSpeedController>();
            root.AddComponent<OfficeDirector>();
            EditorSceneManager.SaveScene(office, Root + "/Scenes/Office.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Root + "/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene(Root + "/Scenes/Office.unity", true)
            };
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Liminal Ledger";
            PlayerSettings.productName = "OPEN PLAN";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorUserBuildSettings.SetPlatformSettings("Standalone", "CreateSolution", "false");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
