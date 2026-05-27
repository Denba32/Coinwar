using Denba.Common;
using System.Collections.Generic;
using System.Threading;

namespace StockGame.Scripts.Manager
{
    public class CancellationTokenManager : Singleton<CancellationTokenManager>
    {
        private readonly Dictionary<string, CancellationTokenSource> _ctsDictionary = new();

        /// <summary>
        /// key에 해당하는 Token 반환. 이미 있으면 Cancel 후 새로 생성.
        /// </summary>
        public CancellationToken GetToken(object caller, string taskName = "default")
        {
            var key = BuildKey(caller, taskName);

            if (_ctsDictionary.TryGetValue(key, out var existing))
            {
                existing.Cancel();
                _ctsDictionary.Remove(key);
            }

            var cts = new CancellationTokenSource();
            _ctsDictionary[key] = cts;
            return cts.Token;
        }

        /// <summary>
        /// key에 해당하는 작업 취소
        /// </summary>
        public void Cancel(object caller, string taskName = "default")
        {
            var key = BuildKey(caller, taskName);
            if (_ctsDictionary.TryGetValue(key, out var cts))
            {
                cts.Cancel();
                _ctsDictionary.Remove(key);
            }
        }

        /// <summary>
        /// 해당 caller의 모든 작업 취소
        /// </summary>
        public void CancelAll(object caller)
        {
            var prefix = $"{caller.GetType().Name}_";
            var keysToRemove = new List<string>();

            foreach (var key in _ctsDictionary.Keys)
            {
                if (key.StartsWith(prefix))
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
            {
                _ctsDictionary[key].Cancel();
                _ctsDictionary.Remove(key);
            }
        }

        private string BuildKey(object caller, string taskName)
        {
            return $"{caller.GetType().Name}_{taskName}";
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var cts in _ctsDictionary.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _ctsDictionary.Clear();
        }
    }
}