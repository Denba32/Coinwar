using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI
{
    public sealed class UI_StockSellInfo : MonoBehaviour
    {
        private NetworkStockInfo stockInfo;
        private int index;

        [SerializeField] Sprite arrowUp, arrowDown, nonChange;

        [SerializeField] private Image stockProfileImg;
        [SerializeField] private TMP_Text stockNameText;
        [SerializeField] private Image img_StockRateOfChange;
        [SerializeField] private TMP_Text stockPriceText;
        [SerializeField] private TMP_Text holdingStockInfoText;
        [SerializeField] private LocalizeStringEvent stringEvent;
        [SerializeField] private LocalizeStringEvent holdingCountEvent;
        [SerializeField] private TMP_InputField stockSellInput;
        [SerializeField] private Button sellStockButton;
        [SerializeField] private Button sellAllStockButton;

        private int sellCount = 0;
        private string holdingCountFormat = string.Empty;

        public void Initialize()
        {
            stockSellInput.OnValueChangedAsObservable().Subscribe(UpdateSellCount).AddTo(this);
            sellStockButton.OnClickAsObservable().Subscribe(OnSellStockByCount).AddTo(this);
            sellAllStockButton.OnClickAsObservable().Subscribe(OnSellAllStock).AddTo(this);
        }

        public void SetData(NetworkStockInfo stockInfo, int index)
        {
            this.stockInfo = stockInfo;
            this.index = index;
            SetLocalization();
        }

        public void SetData(NetworkStockInfo stockInfo)
        {
            this.stockInfo = stockInfo;
        }

        private void SetLocalization()
        {
            if(stockInfo == null || stockInfo.StockId <= 0) return;
            stockNameText.text = stringEvent.SetLocalization("Stock_String", stockInfo.StockId.ToString());
            holdingCountEvent.SetLocalization("Main_String", "shares_held", value =>
            {
                holdingCountFormat = value;
                UpdateHoldingStockCount();
            });
        }

        public void UpdateStock(NetworkStockInfo stockInfo)
        {
            this.stockInfo = stockInfo;
            if (stringEvent.StringReference == null || stringEvent.StringReference.IsEmpty)
            {
                stockNameText.text = stringEvent.SetLocalization("Stock_String", stockInfo.StockId.ToString());
            }

            if (stockProfileImg.sprite == null)
                stockProfileImg.sprite = stockInfo.GetStockProfile();
            ChangePrice(stockInfo.AfterPrice, stockInfo);
        }

        public void UpdateHoldingStockCount()
        {
            var purchasedStock = StockManager.Instance.GetPurchaseStock(NetworkManager.Singleton.LocalClientId, stockInfo.StockId);
            var count = purchasedStock != null ? purchasedStock.PurchaseCount : 0;
            UpdateHoldingStockCount(count);
        }

        public void UpdateHoldingStockCount(int count)
        {
            holdingStockInfoText.text = string.Format(holdingCountFormat, count);
        }

        private void UpdateSellCount(string count)
        {
            if (count.StartsWith('0'))
            {
                stockSellInput.text = stockSellInput.text.TrimStart('0');
            }

            if (string.IsNullOrEmpty(count))
            {
                sellCount = 0;
                return;
            }
            if (!int.TryParse(count, out var value)) return;
            sellCount = value;
        }

        public void ChangePrice(long changedPrice, NetworkStockInfo stockInfo)
        {
            var priceDelta = stockInfo.CalculateStockDelta();
            if (priceDelta > 0)
            {
                stockPriceText.text = $"<color=red>{changedPrice:N0} <size=60%><voffset=-2px>({priceDelta:N2}%)</voffset></size></color>";
                img_StockRateOfChange.sprite = arrowUp;
                img_StockRateOfChange.rectTransform.localEulerAngles = Vector3.zero;
            }
            else if (priceDelta < 0)
            {
                img_StockRateOfChange.sprite = arrowDown;
                img_StockRateOfChange.rectTransform.localEulerAngles = new Vector3(0, 0, 180f);
                stockPriceText.text = $"<color=blue>{changedPrice:N0} <size=60%><voffset=-2px>({priceDelta:N2}%)</voffset></size></color>";

            }
            else
            {
                img_StockRateOfChange.sprite = nonChange;
                stockPriceText.text = $"<color=white>0.00 (0.00)</color>";
                img_StockRateOfChange.rectTransform.localEulerAngles = Vector3.zero;
            }
        }

        public void ResetUI()
        {
            sellCount = 0;
            stockSellInput.text = string.Empty;
        }

        private void OnSellStockByCount(Unit _)
        {
            if (sellCount <= 0) return;
            StockManager.Instance.Sell(stockInfo.StockId, sellCount);
        }

        private void OnSellAllStock(Unit _)
        {
            StockManager.Instance.SellAll(stockInfo.StockId);
        }
    }
}