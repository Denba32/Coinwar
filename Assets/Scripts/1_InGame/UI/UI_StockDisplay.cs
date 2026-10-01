using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.UI.RoundReport;
using StockGame.Scripts.Utility;
using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI
{
    public class UI_StockDisplay : UIBase
    {
        [SerializeField] private List<UI_Stock> stockList = new();
        [SerializeField] private Button stockDisplaySkipButton = null;
        [SerializeField] private TMP_Text moneyText = null;

        [SerializeField] private UI_PurchaseList purchaseList;

        private Action onClose;
        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            StockManager.Instance.StockInfos.OnListChanged += UpdateStock;
            stockDisplaySkipButton.OnClickAsObservableFirst().Subscribe(_ =>
            {
                Close();
            }).AddTo(this);

            foreach (var arg in args)
            {
                if (arg is not Action action) continue;
                onClose = action;
                break;
            }

            moneyText.text = GameManager.Instance.GetLocalPlayerInfo().Money.ToString("N0");
            var stockInfos = StockManager.Instance.StockInfos;
            UpdateStock(stockInfos);
            purchaseList?.UpdateData();
            Managers.Input.StopPlayerInput();
        }

        public override UniTask OnClose(params object[] args)
        {
            Managers.Input.StartPlayerInput();
            return base.OnClose(args);
        }

        public override void Close()
        {
            base.Close();
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX251);
        }

        private void UpdateStock(NetworkListEvent<NetworkStockInfo> changeEvent)
        {
            var index = changeEvent.Index;
            var value = changeEvent.Value;

            stockList[index]?.UpdateStock(value);
        }

        private void UpdateStock(NetworkList<NetworkStockInfo> stockInfos)
        {
            if (stockInfos == null || stockInfos.Count <= 0)
            {
                Debug.Log("Stock 정보 없다");
                return;
            }

            for (int i = 0; i < stockInfos.Count; i++)
            {
                var stock = stockInfos[i];
                stockList[i]?.Initialize(stock);
                stockList[i]?.UpdateStock(stock);
            }
        }
        public override void OnDispose()
        {
            base.OnDispose();
            onClose?.Invoke();
            StockManager.Instance.StockInfos.OnListChanged -= UpdateStock;
        }
    }
}
