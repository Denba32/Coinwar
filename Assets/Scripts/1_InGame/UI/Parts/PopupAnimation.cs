using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

namespace StockGame.Scripts.UI
{
    public enum PopupAnimationType
    {
        Open,
        Success,
        Failed,
        Close
    }
    public class PopupAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;

        private void Start()
        {
            OpenAnimation(0.5f, Ease.OutSine).Forget();
        }

        public async UniTask OpenAnimation(float duration, Ease ease = Ease.Linear)
        {
            var height = Screen.height;
            rect.anchoredPosition = new Vector2(0, height);
            await rect.DOLocalMoveY(0, duration)
                .SetEase(ease)
                .SetLink(gameObject)
                .ToUniTask(cancellationToken: destroyCancellationToken);
        }

        public async UniTask CloseAnimation(float duration, Ease ease = Ease.Linear)
        {
            var height = Screen.height;
            rect.anchoredPosition = Vector2.zero;
            await rect.DOLocalMoveY(-height, duration)
                .SetEase(ease)
                .SetLink(gameObject)
                .ToUniTask(cancellationToken: destroyCancellationToken);
        }
    }
}