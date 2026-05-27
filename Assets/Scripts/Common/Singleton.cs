using System;
using TRPG_Project.Scripts.Common;

namespace Denba.Common
{
    public abstract class Singleton<T> : DisposableContainer where T : Singleton<T>, new()
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (T)Activator.CreateInstance(typeof(T));
                }
                return instance;
            }
        }

        public bool isLoaded = false;
        public virtual void Initialize() { }
    }
}