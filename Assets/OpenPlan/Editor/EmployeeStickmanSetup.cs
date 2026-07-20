using System.IO;
using UnityEditor;
using UnityEngine;

namespace OpenPlan.Editor
{
    // One-click integration of the PolyOne "Free Pack - Stick Man" as the employee
    // visual. Idempotent. Does NOT modify the third-party FBX/prefab: it only fixes
    // the FBX import rig settings (so the humanoid avatar builds), creates a
    // project-owned employee prefab, and repoints the shared "Worker" catalog entry.
    // The original OP_Worker prefab is preserved under the "WorkerLegacy" key.
    public static class EmployeeStickmanSetup
    {
        private const string ModelPath = "Assets/PolyOne/Free Stickman/Model/Free Pack - Stick Man.fbx";
        private const string ControllerPath = "Assets/PolyOne/Free Stickman/Animation/Controler/Stickman_Controler.controller";
        private const string PrefabDir = "Assets/_Project/Prefabs/Employees";
        private const string PrefabPath = PrefabDir + "/EmployeeStickman.prefab";
        private const string CatalogPath = "Assets/OpenPlan/Resources/OpenPlanAssetCatalog.asset";

        [MenuItem("OPEN PLAN/Employees/Set Up Stickman Employees")]
        public static void Setup()
        {
            FixRig();
            GameObject prefab = BuildPrefab();
            if (prefab != null) WireCatalog(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("STICKMAN_SETUP DONE");
        }

        // Batchmode entry point (self-quits).
        public static void SetupBatch()
        {
            Setup();
            EditorApplication.Exit(0);
        }

        [MenuItem("OPEN PLAN/Employees/Revert To Legacy Worker")]
        public static void Revert()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<OfficeAssetCatalog>(CatalogPath);
            if (catalog == null) return;
            var list = catalog.prefabs;
            GameObject legacy = null;
            for (int i = 0; i < list.Count; i++) if (list[i].key == "WorkerLegacy") legacy = list[i].prefab;
            if (legacy == null) { Debug.LogWarning("STICKMAN_SETUP no WorkerLegacy entry to revert to"); return; }
            for (int i = 0; i < list.Count; i++)
                if (list[i].key == "Worker") { var e = list[i]; e.prefab = legacy; list[i] = e; }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("STICKMAN_SETUP reverted Worker -> legacy OP_Worker");
        }

        private static void FixRig()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) { Debug.LogError("STICKMAN_SETUP no importer at " + ModelPath); return; }
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Avatar avatar = FindAvatar();
            Debug.Log($"STICKMAN_SETUP rig avatarValid={(avatar != null && avatar.isValid)} isHuman={(avatar != null && avatar.isHuman)}");
        }

        private static GameObject BuildPrefab()
        {
            EnsureFolder(PrefabDir);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) { Debug.LogError("STICKMAN_SETUP no model at " + ModelPath); return null; }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            Animator anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            Avatar avatar = FindAvatar();
            if (avatar != null) anim.avatar = avatar;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            bool controllerSet = anim.runtimeAnimatorController != null;
            bool avatarSet = anim.avatar != null;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log($"STICKMAN_SETUP prefab saved={(saved != null)} controllerSet={controllerSet} avatarSet={avatarSet}");
            return saved;
        }

        private static void WireCatalog(GameObject prefab)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<OfficeAssetCatalog>(CatalogPath);
            if (catalog == null) { Debug.LogError("STICKMAN_SETUP no catalog at " + CatalogPath); return; }
            var list = catalog.prefabs;
            int workerIdx = -1;
            bool hasLegacy = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].key == "Worker") workerIdx = i;
                if (list[i].key == "WorkerLegacy") hasLegacy = true;
            }
            if (workerIdx < 0) { Debug.LogError("STICKMAN_SETUP no 'Worker' entry in catalog"); return; }
            if (!hasLegacy)
                list.Add(new OfficeAssetCatalog.NamedPrefab { key = "WorkerLegacy", prefab = list[workerIdx].prefab });
            var entry = list[workerIdx];
            entry.prefab = prefab;
            list[workerIdx] = entry;
            EditorUtility.SetDirty(catalog);
            Debug.Log($"STICKMAN_SETUP catalog wired Worker -> {prefab.name}; legacyPreserved={!hasLegacy}");
        }

        private static Avatar FindAvatar()
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
                if (o is Avatar a) return a;
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
