using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UniRx;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine;

namespace StockGame.Scripts.UI.Missions
{
    public class FlashArithmeticChallengeMissionUI : DragAndDropMissionUI
    {
        private const int count = 4;
        private CancellationTokenSource linkedCts;
        private List<int> prices = new List<int>()
        {
            10, 50, 100, 500, 1000, 5000, 10000, 50000
        };

        private List<int> corrects = new List<int>(count);
        [SerializeField] private float timer;

        [SerializeField] private MoneyCounterDropSlotUI slot;
        [SerializeField] private TMP_Text requirecPriceText;
        [SerializeField] private RectTransform interactBlocker;
        [SerializeField] private float flashPriceWaitForSeconds;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            interactBlocker.gameObject.SetActive(true);
            prices.Shuffle();
            for (int i = 0; i < count; i++)
            {
                corrects.Add(prices[i]);
            }

            slot.Initialize(corrects);
            InitAsync().Forget();
        }

        private async UniTask InitAsync()
        {
            var token = Managers.Token.GetToken(this, nameof(StartTimer));
            var waitToken = Managers.Token.GetToken(this, "Wait");

            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, waitToken, destroyCancellationToken);

            slot?.OnSuccess.Subscribe(_ =>
            {
                Managers.Token.Cancel(this, nameof(StartTimer));
                ShowSuccess().Forget();
            }).AddTo(disposables);

            slot?.OnFailure.Subscribe(_ =>
            {
                Managers.Token.Cancel(this, nameof(StartTimer));
                ShowFailed().Forget();
            }).AddTo(disposables);

            await UniTask.WaitForSeconds(2f, cancellationToken: linkedCts.Token);
            if (linkedCts.IsCancellationRequested) return;
            foreach (var correct in corrects)
            {
                Managers.Sound?.PlaySfx(ResourceDefine.FMODEvent.SFX216);
                SetRequiredPrice(correct);
                if (waitToken.IsCancellationRequested) return;
                await UniTask.WaitForSeconds(flashPriceWaitForSeconds, cancellationToken: linkedCts.Token);
                if (linkedCts.IsCancellationRequested) return;
            }
            if (linkedCts.IsCancellationRequested) return;
            requirecPriceText.gameObject?.SetActive(false);
            interactBlocker.gameObject?.SetActive(false);

            // Timer Ω√¿€
            StartTimer(token).Forget();
        }

        private async UniTask StartTimer(CancellationToken token)
        {
            await UniTask.WaitForSeconds(timer, cancellationToken: token);
            UIManager.Instance.CurrentDragItem?.ForceDragEnd();
            ShowFailed().Forget();
        }

        private void SetRequiredPrice(int price)
        {
            requirecPriceText.text = $"{price}";
        }

        public override void OnDispose()
        {
            base.OnDispose();
            UIManager.Instance.CurrentDragItem?.ForceDragEnd();
            linkedCts?.Cancel();
            linkedCts?.Dispose();
            linkedCts = null;
        }
    }
}