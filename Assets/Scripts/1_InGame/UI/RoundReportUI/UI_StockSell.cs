using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public sealed class UI_StockSell : UIBase
    {
        [SerializeField] private List<UI_StockSellInfo> stockSellList = new List<UI_StockSellInfo>();
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private UI_StockTimer stockTimer;

        private Dictionary<int, UI_StockSellInfo> dict = new();
        public IObservable<Unit> OnClickAsObservable => stockTimer.OnClickAsObservable;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            stockTimer?.Initilaize(sortOrder, uiLayer);
            InitializeSellInfo();
            OnUpdatedCash(0);
        }

        /// <summary>
        /// 버튼 등 UI 자체 초기화. 주식 데이터 존재 여부와 무관하게 항상 수행한다.
        /// (데이터가 비어있을 때 for문이 한 번도 안 돌아 버튼 구독이 누락되던 버그 수정)
        /// </summary>
        private void InitializeSellInfo()
        {
            foreach (var sellUI in stockSellList)
                sellUI?.Initialize();

            SeedStocks(StockManager.Instance?.StockInfos);
        }

        /// <summary>
        /// 현재 존재하는 주식 데이터를 UI 슬롯에 반영한다.
        /// GameStart 시점에 데이터가 미리 생성되어 OnListChanged Add 이벤트를 놓친 경우에도
        /// 안전하게 재호출할 수 있도록 멱등하게 동작한다.
        /// </summary>
        public void SeedStocks(NetworkList<NetworkStockInfo> stockInfos)
        {
            if (stockInfos == null) return;
            for (int index = 0; index < stockInfos.Count && index < stockSellList.Count; index++)
            {
                var sellUI = stockSellList[index];
                if (sellUI == null) continue;
                var stock = stockInfos[index];
                sellUI.SetData(stock, index);
                sellUI.UpdateHoldingStockCount();
                dict[stock.StockId] = sellUI;
            }
        }

        public void SetSkipInteract(bool isActive) => stockTimer.SetSkipInteract(isActive);

        public void UpdateStockEvent(NetworkListEvent<NetworkStockInfo> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Add || changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Value)
            {
                var index = changeEvent.Index;
                if (index < 0 || index >= stockSellList.Count) return;

                var stockUI = stockSellList[index];
                if (stockUI == null) return;
                var value = changeEvent.Value;

                if (changeEvent.Type == NetworkListEvent<NetworkStockInfo>.EventType.Add)
                {
                    // index를 함께 넘겨 로컬라이제이션까지 수행 (SetData(value) 단독 호출 시 주식명이 비어있던 버그 수정)
                    stockUI.SetData(value, index);
                    stockUI.UpdateHoldingStockCount();
                    dict[value.StockId] = stockUI;
                }
                else
                {
                    dict[value.StockId] = stockUI;
                    stockUI.UpdateStock(value);
                }
            }
        }

        public void UpdateStock(NetworkList<NetworkStockInfo> stockInfos)
        {
            for (int i = 0; i < stockInfos.Count; i++)
            {
                var stockInfo = stockInfos[i];
                UpdateStock(stockInfo, i);
            }
        }

        private void UpdateStock(NetworkStockInfo stock, int index)
        {
            stockSellList[index]?.UpdateStock(stock);
        }

        public void UpdateStock(int stockId)
        {
            if (stockId < 0) return;
            if (!dict.TryGetValue(stockId, out var data)) return;
            data?.UpdateHoldingStockCount();
        }

        public void UpdateData(int stockId, int count)
        {
            if (stockId < 0)
            {
                Debug.Log("StockID가 0이어서 return");
                return;
            }
            if (!dict.TryGetValue(stockId, out var data))
            {
                Debug.Log("Dict에서 Value를 찾을 수 없음");
                return;
            }
            Debug.Log($"{stockId} | {data.name}");
            data?.UpdateHoldingStockCount(count);
        }

        public void ResetUI()
        {
            foreach (var stock in stockSellList)
            {
                stock?.ResetUI();
                stock?.UpdateHoldingStockCount();
            }
        }

        public void SetSkipInteractable(bool isActive) => stockTimer.SetSkipInteract(isActive);

        public void OnTimerValueChanged(int newValue)
        {
            if (Managers.Game.CurrentRound.Value.RoundPhase == GameDefine.RoundDefine.RoundPhase.StockPurchaseAndSell)
            {
                int totalSeconds = newValue;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;

                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        public void ActivateSkipButton()
        {
            stockTimer.ActivateSkipButton();
        }

        public void OnUpdatedCash(long cash) => cashText.text = $"{cash:N0}";
        public void Clear() { }
    }
}