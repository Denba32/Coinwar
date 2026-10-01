using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_PurchaseAndSell : UIBase
    {
        [SerializeField] private Toggle purchaseToggle;
        [SerializeField] private Toggle sellToggle;

        [Space]
        [Header("Stock Purchase")]
        [SerializeField] private UI_StockPurchase purchase;

        [Space]
        [Header("Stock Sell")]
        [SerializeField] private UI_StockSell sell;

        private UI_PurchaseList purchaseList = null;
        private UI_StockTimer stockTimer = null;

        public UI_StockPurchase Purchase => purchase;
        public UI_StockSell Sell => sell;
        public UI_StockTimer StockTimer => stockTimer;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            purchase?.Initilaize(sortOrder, uiLayer);
            sell?.Initilaize(sortOrder, uiLayer);

            purchaseToggle.OnValueChangedAsObservable().Subscribe(OnPurchasePress).AddTo(this);
            sellToggle.OnValueChangedAsObservable().Subscribe(OnSellPress).AddTo(this);
        }

        public void Setup(UI_PurchaseList purchaseList, UI_StockTimer timer)
        {
            this.purchaseList = purchaseList;
            stockTimer = timer;
        }

        public void SetSkipInteract(bool isActive) => sell?.SetSkipInteract(isActive);
        private void OnSellPress(bool isActive)
        {
            if (isActive)
            {
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX250);
                sell.Show();
            }
            else sell.Hide();
        }

        private void OnPurchasePress(bool isActive)
        {
            if (isActive)
            {
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX250);
                purchase.Show();
                stockTimer?.Show();
            }
            else
            {
                purchase.Hide();
                stockTimer?.Hide();
            }
        }

        public void ShowPurchase()
        {
            ResetUI();
            Show();
            Clear();
            sell?.ActivateSkipButton();
            purchaseToggle.isOn = true;
            sellToggle.isOn = false;
            purchaseList?.Show();
        }

        public void OnUpdatedCash(long cash)
        {
            purchaseList?.OnUpdatedCash(cash);
            sell?.OnUpdatedCash(cash);
        }

        public void UpdateItem(NetworkPurchasedStock stockInfo)
        {
            purchaseList?.UpdateItem(stockInfo);
            sell?.UpdateData(stockInfo.StockId, stockInfo.PurchaseCount);
        }

        public void UpdateStock(NetworkListEvent<NetworkStockInfo> changeEvent)
        {
            purchase?.UpdateStockEvent(changeEvent);
            sell?.UpdateStockEvent(changeEvent);
        }

        public void UpdateStock(NetworkList<NetworkStockInfo> stockInfos)
        {
            if (stockInfos == null || stockInfos.Count == 0) return;
            purchase?.UpdateStock(stockInfos);
            sell?.UpdateStock(stockInfos);
        }

        /// <summary>
        /// 씬 로드 이전에 생성된 주식 데이터를 UI에 즉시 반영. 멱등.
        /// </summary>
        public void SeedStocks(NetworkList<NetworkStockInfo> stockInfos)
        {
            purchase?.SeedStocks(stockInfos);
            sell?.SeedStocks(stockInfos);
        }

        public void OnTimerValueChanged(int newValue)
        {
            sell?.OnTimerValueChanged(newValue);
        }

        public void ResetUI()
        {
            purchase?.ResetUI();
            sell?.ResetUI();
        }

        public void Clear()
        {
            purchase.Clear();
            sell.Clear();
        }
    }
}