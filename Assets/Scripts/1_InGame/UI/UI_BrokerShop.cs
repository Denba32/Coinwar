using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.UI.RoundReport;
using System;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public sealed class UI_BrokerShop : UIBase
    {
        private Action onClose;

        [SerializeField] private UI_PurchaseAndSell purchaseAndSell;
        [SerializeField] private UI_PurchaseList purchaseList;
        [SerializeField] private Button skipButton;

        private long convertedMoney = 0;
        private int convertedCoin = 0;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            purchaseList?.Initilaize(sortOrder, uiLayer);
            purchaseAndSell?.Initilaize(sortOrder, uiLayer);
            purchaseAndSell?.Setup(purchaseList, null);

            skipButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close();
            }).AddTo(this);
        }

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            if (args != null && args.Length > 0)
            {
                foreach (var item in args)
                {
                    if (item is Action action)
                        onClose = action;
                }
            }

            purchaseList?.UpdateData();

            var stockManager = StockManager.Instance;
            if (stockManager != null)
            {
                stockManager.StockInfos.OnListChanged += purchaseAndSell.UpdateStock;

                stockManager.OnPurchase.Subscribe(purchaseAndSell.UpdateItem).AddTo(this); // 구매 시 변경 처리
                stockManager.OnSell.Subscribe(purchaseAndSell.UpdateItem).AddTo(this);
                purchaseAndSell?.UpdateStock(stockManager.StockInfos);
                purchaseAndSell?.ResetUI();
            }

            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.JoinInfos.OnListChanged += OnPlayerDataChanged;
            }

            var localPlayer = gameManager.GetLocalPlayerInfo();
            convertedCoin = localPlayer.Coin - 1; // 현재 소지한 코인 저장
            convertedMoney = convertedCoin * GameDefine.StockDefine.CoinValueInWon;
            Managers.Game.AddMoneyRpc(convertedMoney); // 소지한 코인을 소지금으로 전환
            Managers.Game.UpdateCoinServerRpc(0); // 전환된 코인 수정
            UpdateCash(localPlayer.Money + convertedMoney);
            Managers.Input.StopPlayerInput();
        }

        public override UniTask OnClose(params object[] args)
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                var localPlayer = gameManager.GetLocalPlayerInfo();
                if (localPlayer.Money > 0)
                {
                    int creatableCoinCount = (int)(localPlayer.Money / GameDefine.StockDefine.CoinValueInWon);
                    creatableCoinCount = Mathf.Min(creatableCoinCount, convertedCoin);
                    var convertTargetMoney = (long)(GameDefine.StockDefine.CoinValueInWon * creatableCoinCount);
                    Managers.Game.AddMoneyRpc(-convertTargetMoney);
                    Managers.Game.UpdateCoinServerRpc(creatableCoinCount);
                }
            }

            Managers.Input.StartPlayerInput();
            return base.OnClose(args);
        }

        public override void Close()
        {
            base.Close();
            onClose?.Invoke();
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX251);
        }

        private void OnPlayerDataChanged(NetworkListEvent<GameDefine.NetworkPlayerJoinInfo> changeEvent)
        {
            if (changeEvent.Value.ClientId != NetworkManager.Singleton.LocalClientId) return;
            var prev = changeEvent.PreviousValue;
            var current = changeEvent.Value;

            if (prev.Money != current.Money)
                UpdateCash(current.Money);
        }

        private void UpdateCash(long cash)
        {
            purchaseList?.OnUpdatedCash(cash);
            purchaseAndSell?.OnUpdatedCash(cash);
        }

        public override void OnDispose()
        {
            base.OnDispose();
            var stockManager = StockManager.Instance;
            if (stockManager != null)
                stockManager.StockInfos.OnListChanged -= purchaseAndSell.UpdateStock;
            var gameManager = GameManager.Instance;
            if (gameManager != null)
                gameManager.JoinInfos.OnListChanged -= OnPlayerDataChanged;
        }
    }
}