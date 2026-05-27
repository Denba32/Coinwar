using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    /// <summary>
    /// 리소스를 제어
    /// Resources 폴더를 활용
    /// </summary>
    public class ResourceManager : Singleton<ResourceManager>
    {
        private Dictionary<string, UnityEngine.Object> dict = new Dictionary<string, UnityEngine.Object>();

        public async UniTask InitAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            try
            {
                var config = Managers.Instance.ResourcePath;
                if (config == null || config.resourceAddress == null || config.resourceAddress.Count == 0)
                {
                    Debug.LogWarning("[ResourceManager] ResourcePathConfigSO가 없거나 비어있음");
                    return;
                }

                foreach (var address in config.resourceAddress)
                {
                    if (string.IsNullOrEmpty(address)) continue;
                    await LoadAllAsync<UnityEngine.Object>(address, token);
                }
            }
            catch
            {
                throw;
            }
            Debug.Log($"[ResourceManager] 초기화 완료 - 총 {dict.Count}개 로드");
        }

        public T Load<T>(string path = "", ResourceDirectory directory = ResourceDirectory.Prefabs) where T : UnityEngine.Object
        {
            string loadName = string.IsNullOrEmpty(path) ? typeof(T).Name : path;
            string _path = $"{directory.ToString()}/{loadName}";
            dict.TryGetValue(_path, out var cachedObject);
            if (cachedObject != null) return ObjectConverter<T>(cachedObject);
            var obj = Resources.Load<T>(_path);
            if (obj == null) return null;
            dict[_path] = obj;
            return obj;
        }

        public async UniTask<T> LoadAsync<T>(string path = "", ResourceDirectory directory = ResourceDirectory.Prefabs, CancellationToken token = default) where T : UnityEngine.Object
        {
            if(token.IsCancellationRequested) return null;
            string loadName = string.IsNullOrEmpty(path) ? nameof(T) : path;
            string _path = $"{directory.ToString()}/{loadName}";

            if (dict.TryGetValue(_path, out var cached))
                return ObjectConverter<T>(cached);

            var obj = await Resources.LoadAsync<T>(_path).ToUniTask(cancellationToken:token);
            if (obj == null) return null;
            dict[_path] = obj;

            return ObjectConverter<T>(obj);
        }

        public List<T> LoadAll<T>(string path = "", ResourceDirectory directory = ResourceDirectory.Prefabs) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[ResourceManager] LoadAllAsync: path가 비어있음");
                return null;
            }
            string loadName = string.IsNullOrEmpty(path) ? nameof(T) : path;
            string _path = $"{directory.ToString()}/{loadName}";

            var objs = Resources.LoadAll<T>(_path);
            if (objs == null || objs.Length == 0)
            {
                Debug.LogWarning($"[ResourceManager] LoadAll 결과 없음: {_path}");
                return null;
            }

            foreach (var obj in objs)
            {
                var key = $"{_path}/{obj.name}";
                if (!dict.ContainsKey(key))
                    dict[key] = obj;
            }
            return objs.ToList();
        }

        public async UniTask<T> LoadAllAsync<T>(string path = "", CancellationToken token = default) where T : UnityEngine.Object
        {
            if (token.IsCancellationRequested) return null;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[ResourceManager] LoadAllAsync: path가 비어있음");
                return null;
            }

            var objs = Resources.LoadAll<T>(path);
            if (objs == null || objs.Length == 0)
            {
                Debug.LogWarning($"[ResourceManager] LoadAll 결과 없음: {path}");
                return null;
            }

            foreach (var obj in objs)
            {
                var key = $"{path}/{obj.name}";
                if (!dict.ContainsKey(key))
                    dict[key] = obj;
            }

            await UniTask.Yield(token);
            return objs[0] as T;
        }

        public List<T> GetByParentFolder<T>(string folderName) where T : UnityEngine.Object
        {
            var result = new List<T>();

            foreach (var kv in dict)
            {
                var path = kv.Key.Replace("\\", "/");
                var lastSlash = path.LastIndexOf('/');

                if (lastSlash <= 0) continue;

                // 파일명 바로 위 폴더 추출
                var parentPath = path.Substring(0, lastSlash);
                var parentFolder = parentPath.Substring(parentPath.LastIndexOf('/') + 1);

                if (parentFolder == folderName)
                {
                    if (kv.Value is T value)
                        result.Add(value);
                    else
                    {
                        var converted = ObjectConverter<T>(kv.Value);
                        if (converted != null)
                            result.Add(converted);
                    }
                }
            }

            return result;
        }

        private T ObjectConverter<T>(UnityEngine.Object obj) where T : UnityEngine.Object
        {
            if (obj is T value)
                return value;

            if (typeof(T) == typeof(Sprite) && obj is Texture2D texture)
            {
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                return sprite as T;
            }

            if (obj is Component component)
                return component.GetComponent<T>();

            if (obj is GameObject go)
                return go.GetComponent<T>();

            return null;
        }

        public T Instantiate<T>(string path, ResourceDirectory directory = ResourceDirectory.Prefabs) where T : UnityEngine.Object
        {
            var gm = Load<T>(path, directory);

            if (gm == null)
            {
                Debug.LogError($"{path} 경로에 {typeof(T).Name} 오브젝트가 없습니다.");
                return null;
            }

            var obj = InstantiateInternal<T>(gm);
            obj.name = gm.name;

            return obj;
        }

        public T Instantiate<T>(UnityEngine.Object obj) where T : UnityEngine.Object
        {
            if (obj == null) return null;

            var instance = InstantiateInternal<T>(obj);

            if (instance != null)
                instance.name = obj.name;

            return instance;
        }

        private T InstantiateInternal<T>(UnityEngine.Object obj) where T : UnityEngine.Object
        {
            if (obj is T t)
                return UnityEngine.Object.Instantiate(t);

            if (obj is GameObject go)
                return UnityEngine.Object.Instantiate(go).GetComponent<T>();

            if (obj is Component c)
                return UnityEngine.Object.Instantiate(c.gameObject).GetComponent<T>();

            return null;
        }

        public new void Clear()
        {
            foreach (var obj in dict.Values)
            {
                if (obj != null)
                    Resources.UnloadAsset(obj);
            }

            dict.Clear();

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        public async UniTask ClearAsync()
        {
            foreach (var obj in dict.Values)
            {
                if (obj != null)
                    Resources.UnloadAsset(obj);
            }

            dict.Clear();

            await Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}