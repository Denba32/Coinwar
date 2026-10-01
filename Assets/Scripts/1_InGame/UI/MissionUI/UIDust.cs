using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public class UIDust : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip playClip;

        public RectTransform RectTransform => rectTransform;

        private CancellationTokenSource cts;

        /// <summary>
        /// 애니메이션 재생 후 길이 반환
        /// </summary>
        public float Play()
        {
            if (animator == null || playClip == null)
                return 0f;

            animator.Play(playClip.name, 0, 0f);

            cts?.Dispose();
            cts = new CancellationTokenSource();

            AutoDestroyAsync(playClip.length, cts.Token).Forget();

            return playClip.length;
        }

        public float Play(float duration)
        {
            if (animator == null || playClip == null)
                return 0f;

            animator.Play(playClip.name, 0, 0f);

            cts?.Dispose();
            cts = new CancellationTokenSource();

            AutoDestroyAsync(duration, cts.Token).Forget();

            return duration;
        }

        /// <summary>
        /// 외부에서 강제 제거
        /// </summary>
        public void ForceDestroy()
        {
            if (cts.IsCancellationRequested) return;

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
            Destroy(gameObject);
        }

        private async UniTaskVoid AutoDestroyAsync(float delay, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
                if (token.IsCancellationRequested) return;
                Destroy(gameObject);
            }
            catch (OperationCanceledException)
            {
                // 성공으로 인해 취소된 경우
            }
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }
    }
}