using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public class PriceCalculationTaskMissionUI : MissionUIBase
    {
        [SerializeField] private TMP_InputField priceInputField;
        [SerializeField] private List<PriceCalculationTaskCoin> coinLocations;

        [SerializeField] private List<Sprite> coinSprites;

        private CancellationTokenSource linkedCts;
        private List<int> prices = new List<int>()
        {
            500, 100, 50, 10
        };

        [SerializeField] private float timer;

        private int totalPrice = 0;


        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            priceInputField?.ActivateInputField();
            InitAsync().Forget();
        }

        private UniTask InitAsync()
        {
            foreach(var coinLocation in coinLocations)
            {
                var randomCoin = GetRandomCoin();
                coinLocation.SetCoin(randomCoin.Item1);
                totalPrice += randomCoin.Item2;
            }

            priceInputField.OnValueChangedAsObservable().Subscribe(CheckPrice).AddTo(disposables);
            var token = Managers.Token.GetToken(this, nameof(StartTimer));
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, destroyCancellationToken);
            StartTimer(linkedCts.Token).Forget();
            return UniTask.CompletedTask;
        }

        private void CheckPrice(string price)
        {
            if (!int.TryParse(price, out var inputPrice)) return;
            if(totalPrice == inputPrice)
            {
                priceInputField.enabled = false;
                Managers.Token.Cancel(this, nameof(StartTimer));
                ShowSuccess().Forget();
            }
        }

        protected override UniTask ShowFailed()
        {
            priceInputField.enabled = false;
            return base.ShowFailed();
        }


        private async UniTask StartTimer(CancellationToken token)
        {
            if(token.IsCancellationRequested) return;
            await UniTask.WaitForSeconds(timer, cancellationToken: token);
            if (token.IsCancellationRequested) return;
            ShowFailed().Forget();
        }

        private (Sprite, int) GetRandomCoin()
        {
            var random = Random.Range(0, prices.Count);
            return (coinSprites[random], prices[random]);
        }

        public override void OnDispose()
        {
            base.OnDispose();
            linkedCts?.Cancel();
            linkedCts?.Dispose();
            linkedCts = null;
        }
    }
}