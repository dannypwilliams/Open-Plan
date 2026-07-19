using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPlan
{
    [CreateAssetMenu(menuName = "Open Plan/Asset Catalog")]
    public sealed class OfficeAssetCatalog : ScriptableObject
    {
        [Serializable] public struct NamedPrefab { public string key; public GameObject prefab; }
        [Serializable] public struct NamedMaterial { public string key; public Material material; }

        public List<NamedPrefab> prefabs = new List<NamedPrefab>();
        public List<NamedMaterial> materials = new List<NamedMaterial>();

        public GameObject GetPrefab(string key)
        {
            for (int i = 0; i < prefabs.Count; i++)
                if (prefabs[i].key == key) return prefabs[i].prefab;
            return null;
        }

        public Material GetMaterial(string key)
        {
            for (int i = 0; i < materials.Count; i++)
                if (materials[i].key == key) return materials[i].material;
            return null;
        }

        public GameObject Spawn(string key, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            GameObject source = GetPrefab(key);
            if (source == null)
            {
                Debug.LogError($"OPEN PLAN missing generated FBX asset: {key}");
                return new GameObject("Missing_" + key);
            }
            GameObject wrapper = new GameObject(key);
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.SetPositionAndRotation(position, rotation);
            GameObject visual = Instantiate(source, wrapper.transform, false);
            visual.name = key + "_Visual";
            visual.transform.localPosition = Vector3.zero;
            // Preserve the FBX importer's axis-conversion rotation. Gameplay lives on
            // the unscaled wrapper, while this child retains the model's authored basis.
            visual.transform.localScale = Vector3.Scale(visual.transform.localScale, scale);
            ApplySharedMaterials(wrapper);
            return wrapper;
        }

        public void ApplySharedMaterials(GameObject instance)
        {
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i] == null) continue;
                    string source = slots[i].name.ToLowerInvariant();
                    string key = source.Replace("op_", string.Empty).Replace(" (instance)", string.Empty);
                    Material shared = GetMaterial(key);
                    if (shared != null) slots[i] = shared;
                }
                renderer.sharedMaterials = slots;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }
    }
}
