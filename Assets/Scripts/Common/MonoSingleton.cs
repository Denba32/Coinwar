using UniRx;
using UnityEngine;

namespace Denba.Common
{
    public abstract class NonPersistantMonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static bool isInitialed = false;
        protected CompositeDisposable _disposables;
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name);
                        instance = obj.AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this as T;
        }

        public virtual void Init()
        {
            if (isInitialed) return;
            _disposables = new();
        }

        public virtual void Clear() { }
        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static bool isInitialed = false;
        protected CompositeDisposable disposables = new();
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    return null;
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;

            DontDestroyOnLoad(gameObject);
        }

        public virtual void Initialize()
        {
            if (isInitialed) return;
        }

        public virtual void Clear() { }

        private void OnDestroy()
        {
            Dispose();
        }
        public virtual void Dispose()
        {
            disposables?.Dispose();
            disposables = null;
        }
    }
}