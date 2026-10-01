using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TRPG_Project.Scripts.Common
{
    public class DisposableContainer : IDisposable, ICollection<IDisposable>
    {
        private readonly List<IDisposable> _disposables = new();
        
        public DisposableContainer(){}

        public void Add(IDisposable item)
        {
            if (item == null) return;
            _disposables.Add(item);
        }

        public bool Remove(IDisposable item)
        {
            return _disposables.Remove(item);
        }

        public void Clear()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }

        public bool Contains(IDisposable item)
        {
            return _disposables.Contains(item);
        }

        public void CopyTo(IDisposable[] array, int arrayIndex)
        {
            _disposables.CopyTo(array, arrayIndex);
        }

        public int Count => _disposables.Count;
        public bool IsReadOnly => false;

        public IEnumerator<IDisposable> GetEnumerator()
        {
            return _disposables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Dispose()
        {
            for (int i = _disposables.Count - 1; i >= 0; i--)
            {
                try
                {
                    _disposables[i]?.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _disposables.Clear();
        }
    }
}