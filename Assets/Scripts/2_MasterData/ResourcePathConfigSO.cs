using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StockGame.Scripts.Datas
{
    [CreateAssetMenu(fileName = "ResourcePathConfigSO", menuName = "ScriptableObject/Resources/ResourcePathConfigSO", order = 0)]
    public class ResourcePathConfigSO : ScriptableObject
    {
        public List<Object> resourceTargets;
        public List<string> resourceAddress;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (resourceTargets == null || resourceTargets.Count <= 0) return;

            resourceAddress = new List<string>();

            foreach (var target in resourceTargets)
            {
                if (target == null) continue;

                // Assets/.../Resources/Xxx/Yyy.prefab
                // → Xxx/Yyy
                var fullPath = AssetDatabase.GetAssetPath(target);
                const string resourcesFolder = "/Resources/";
                var idx = fullPath.IndexOf(resourcesFolder);

                if (idx < 0)
                {
                    Debug.LogWarning($"[ResourcePathConfigSO] Resources 폴더 외부 에셋: {fullPath}");
                    continue;
                }

                // Resources/ 이후 경로에서 확장자 제거
                var resourcePath = fullPath.Substring(idx + resourcesFolder.Length);
                resourcePath = System.IO.Path.ChangeExtension(resourcePath, null);

                resourceAddress.Add(resourcePath);
            }

            EditorUtility.SetDirty(this);
        }
#endif
    }
}