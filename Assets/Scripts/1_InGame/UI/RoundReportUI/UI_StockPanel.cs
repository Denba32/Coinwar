using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_StockPanel : UIBase
    {
        [Space]
        [Header("Stock Report")]
        [SerializeField] private UI_StockReport stockReport = null;

        [Space]
        [Header("Stock Timer")]
        [SerializeField] private UI_StockTimer stockTimer = null;

        [Space]
        [Header("Purchase And Sell")]
        [SerializeField] private UI_PurchaseAndSell purchaseAndSell = null;

        [Space]
        [Header("Stock Purchase List")]
        [SerializeField] private UI_PurchaseList purchaseList = null;

        public UI_StockTimer StockTimer => stockTimer;
        public UI_StockReport StockReport => stockReport;
        public UI_PurchaseAndSell PurchaseAndSell => purchaseAndSell;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            stockReport?.Initilaize(sortOrder, uiLayer);
            stockTimer?.Initilaize(sortOrder, uiLayer);
            purchaseList?.Initilaize(sortOrder, uiLayer);
            purchaseAndSell?.Initilaize(sortOrder, uiLayer);
            purchaseAndSell?.Setup(purchaseList, stockTimer);

            stockTimer?.OnClickAsObservable?.Subscribe(OnClickSkip).AddTo(this);
            purchaseAndSell?.Sell?.OnClickAsObservable.Subscribe(OnClickSkip).AddTo(this);
        }

        private void OnClickSkip(Unit _)
        {
            if (NetworkManager.Singleton.LocalClient == null) return;
            Managers.Game.SendSkipVoteServerRpc();
            stockTimer?.SetSkipInteract(false);
            purchaseAndSell?.SetSkipInteract(false);
        }

        public void ShowStockReport()
        {
            stockReport?.Show();
            stockTimer?.Show();
            purchaseList?.Show();
            purchaseAndSell?.Hide();
        }

        public void ShowPurchase()
        {
            purchaseAndSell?.ShowPurchase();
            stockReport?.Hide();
            stockTimer?.Show();
        }

        public void OnTimerValueChanged(int newValue)
        {
            stockTimer?.OnTimerValueChanged(newValue);
            purchaseAndSell?.OnTimerValueChanged(newValue);
        }

        public void UpdateStock(NetworkListEvent<NetworkStockInfo> changeEvent)
        {
            stockReport?.UpdateStock(changeEvent);
            purchaseAndSell?.UpdateStock(changeEvent);
        }

        /// <summary>
        /// 씬 로드 이전에 이미 생성된 주식 데이터를 UI에 시드한다.
        /// (호스트가 GameStart 시 데이터를 미리 만들어 Add 이벤트를 놓치는 경우 대비)
        /// </summary>
        public void SeedStocks(NetworkList<NetworkStockInfo> stockInfos)
        {
            if (stockInfos == null || stockInfos.Count == 0) return;
            stockReport?.SeedStocks(stockInfos);
            purchaseAndSell?.SeedStocks(stockInfos);
        }

        public void UpdateCash(long cash)
        {
            purchaseAndSell?.OnUpdatedCash(cash);
            purchaseList?.OnUpdatedCash(cash);
        }

        public void UpdateItem(NetworkPurchasedStock stockInfo)
        {
            purchaseAndSell?.UpdateItem(stockInfo);
        }
    }
}