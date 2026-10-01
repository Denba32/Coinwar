using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
namespace StockGame.Scripts.UI.Missions
{
    public class TentHammerHitEffect : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip clip;

        [SerializeField] private RectTransform rect;
        public RectTransform Rect => rect;

        private CancellationTokenSource cts = new();
        public void Play()
        {
            animator.Play(clip.name);
            AutoDestroy().Forget();
        }

        private async UniTask AutoDestroy()
        {
            var length = clip.length;
            await UniTask.WaitForSeconds(length, cancellationToken:cts.Token);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}