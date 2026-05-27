using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UniRx;

namespace Denba.Common
{
    public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
    {
        protected CompositeDisposable disposables = new();
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();
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

        public override void OnDestroy()
        {
            base.OnDestroy();
            disposables?.Dispose();
        }

        public virtual UniTask Initialize() { return UniTask.CompletedTask; }
    }
}