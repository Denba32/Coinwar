using StockGame.Scripts.Define;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_StockPurchase : UIBase
    {
        [SerializeField] private List<UI_StockPurchaseInfo> stockList = new List<UI_StockPurchaseInfo>();

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            InitailizePurchaseInfo();
        }

        private void InitailizePurchaseInfo()
        {
            foreach (var stockUI in stockList)
            {
                stockUI?.Initialize();
            }
        }
        public void UpdateStockEvent(NetworkListEvent<NetworkStockInfo> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Add || changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Value)
            {
                var index = changeEvent.Index;
                if (index < 0 || index >= stockList.Count) return;
                var stockUI = stockList[index];
                var value = changeEvent.Value;
                if (changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Add)
                {
                    stockUI?.Initialize(value);
                }
                else
                {
                    UpdateStock(value, index);
                }
            }
        }

        private void UpdateStock(NetworkStockInfo stock, int index)
        {
            stockList[index]?.UpdateStockInfo(stock);
        }

        public void UpdateStock(NetworkList<NetworkStockInfo> stockInfos)
        {
            for (int i = 0; i < stockInfos.Count; i++)
            {
                var stockInfo = stockInfos[i];
                UpdateStock(stockInfo, i);
            }
        }

        /// <summary>
        /// 현재 존재하는 주식 데이터로 UI 슬롯을 초기화한다.
        /// 씬 로드 이전에 데이터가 생성되어 Add 이벤트를 놓친 경우 대비. 멱등.
        /// </summary>
        public void SeedStocks(NetworkList<NetworkStockInfo> stockInfos)
        {
            if (stockInfos == null) return;
            for (int i = 0; i < stockInfos.Count && i < stockList.Count; i++)
                stockList[i]?.Initialize(stockInfos[i]);
        }

        public void ResetUI()
        {
            foreach (var stock in stockList)
            {
                stock?.ResetUI();
            }
        }

        public void Clear()
        {
            foreach (var stock in stockList)
            {
                stock?.Clear();
            }
        }
    }
}