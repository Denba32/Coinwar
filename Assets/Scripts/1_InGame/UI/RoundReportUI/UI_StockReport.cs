using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using StockGame.Scripts.Define;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_StockReport : UIBase
    {
        [SerializeField] List<UI_Stock> stockList = new List<UI_Stock>();

        private bool _isInitialized = false;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            _isInitialized = false;
        }

        public void UpdateStock(NetworkListEvent<NetworkStockInfo> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Add || changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Value)
            {
                var index = changeEvent.Index;
                if (index < 0 || index >= stockList.Count) return;

                var stock = stockList[index];
                if (stock == null) return;

                var value = changeEvent.Value;

                if (changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Add)
                {
                    stock.Initialize(value);

                    if (index == stockList.Count - 1)
                        _isInitialized = true;
                }
                else
                {
                    stock.UpdateStock(value);
                }
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

            if (stockInfos.Count > 0)
                _isInitialized = true;
        }
    }
}