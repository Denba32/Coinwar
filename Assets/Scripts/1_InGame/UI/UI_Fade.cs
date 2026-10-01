

using Cysharp.Threading.Tasks;
using System.Threading;
using DG.Tweening;

namespace StockGame.Scripts.UI
{
    public class UI_Fade : UIBase
    {
        public void FadeOff()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
        public async UniTask FadeIn(float duration, Ease ease = Ease.Linear, CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return;
            canvasGroup.alpha = 0f;

            await canvasGroup.DOFade(1f, duration)
                .SetEase(ease)
                .SetLink(gameObject)
                .AsyncWaitForCompletion()
                .AsUniTask()
                .AttachExternalCancellation(token);
        }

        public async UniTask FadeOut(float duration, Ease ease = Ease.Linear, CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return;
            canvasGroup.alpha = 1f;
            await canvasGroup.DOFade(0f, duration)
                .SetEase(ease)
                .SetLink(gameObject)
                .AsyncWaitForCompletion()
                .AsUniTask()
                .AttachExternalCancellation(token);
        }
    }
}