using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI
{
    public class UI_StockPurchaseInfo : MonoBehaviour
    {
        [SerializeField] Sprite arrowUp, arrowDown, nonChange;

        private NetworkStockInfo stockInfo;
        [SerializeField] private Image stockProfileImg;
        [SerializeField] private TMP_Text stockNameText;
        [SerializeField] private Image img_StockRateOfChange;
        [SerializeField] private TMP_Text stockPriceText;
        [SerializeField] private LocalizeStringEvent stringEvent;
        [SerializeField] private TMP_InputField inputStockCount;
        [SerializeField] private Button purchaseStockButton;
        [SerializeField] private Button purchaseAllStockButton;

        [SerializeField] private int purchaseCount = 0;
        public long currentPrice;

        public int PurchaseCount => purchaseCount;
        public void Initialize()
        {
            purchaseStockButton.OnClickAsObservableFirst(0.1f).Subscribe(_ => Purchase()).AddTo(gameObject);
            purchaseAllStockButton.OnClickAsObservableFirst(0.1f).Subscribe(_ => PurchaseAll()).AddTo(gameObject);
            inputStockCount.OnValueChangedAsObservable().Subscribe(_ => UpdatePurchaseCount()).AddTo(gameObject);
        }
        public void Initialize(NetworkStockInfo stockInfo)
        {
            stockProfileImg.sprite = stockInfo.GetStockProfile();
            stockNameText.text = stringEvent.SetLocalization("Stock_String", stockInfo.StockId.ToString());
            ChangePrice(stockInfo.AfterPrice, stockInfo);
        }

        public void ResetUI()
        {
            purchaseCount = 0;
            inputStockCount.text = string.Empty;
        }

        private void UpdatePurchaseCount()
        {
            if (inputStockCount.text.StartsWith('0'))
            {
                inputStockCount.text = inputStockCount.text.TrimStart('0');
            }
            var text = inputStockCount.text;
            if (!int.TryParse(text, out int result)) return;
            purchaseCount = result;
        }

        public void UpdateStockInfo(NetworkStockInfo stockInfo)
        {
            this.stockInfo = stockInfo;
            if (stringEvent.StringReference == null || stringEvent.StringReference.IsEmpty)
            {
                stockNameText.text = stringEvent.SetLocalization("Stock_String", stockInfo.StockId.ToString());
            }

            stockProfileImg.sprite = stockInfo.GetStockProfile();
            Debug.Log($"Purchase Phase Stock Info - {stockInfo.AfterPrice}");
            ChangePrice(stockInfo.AfterPrice, stockInfo);
        }

        public void ChangePrice(long changedPrice, NetworkStockInfo stockInfo)
        {
            stockPriceText.text = $"{changedPrice:N0}";
            var priceDelta = stockInfo.CalculateStockDelta();
            var diffPrice = stockInfo.AfterPrice - stockInfo.BeforePrice;
            if (priceDelta > 0)
            {
                stockPriceText.text = $"<color=red>{stockInfo.AfterPrice:N0}<size=60%><voffset=-2px> ({priceDelta:N2}%)</voffset></size></color>";
                img_StockRateOfChange.sprite = arrowUp;
                img_StockRateOfChange.rectTransform.localEulerAngles = Vector3.zero;
            }
            else if (priceDelta < 0)
            {
                img_StockRateOfChange.sprite = arrowDown;
                img_StockRateOfChange.rectTransform.localEulerAngles = new Vector3(0, 0, 180f);
                stockPriceText.text = $"<color=blue>{stockInfo.AfterPrice:N0}<size=60%><voffset=-2px> ({priceDelta:N2}%)</voffset></size></color>";

            }
            else
            {
                img_StockRateOfChange.sprite = nonChange;
                stockPriceText.text = $"<color=white>{stockInfo.AfterPrice:N0}<size=60%><voffset=-2px> (0.00%)</voffset></size></color>";
                img_StockRateOfChange.rectTransform.localEulerAngles = Vector3.zero;
            }

            currentPrice = changedPrice;
        }

        private void Purchase()
        {
            if (purchaseCount <= 0) return;
            Debug.Log($"Purchase : {purchaseCount}");
            StockManager.Instance.Purchase(stockInfo.StockId, purchaseCount);
        }

        private void PurchaseAll()
        {
            StockManager.Instance.PurchaseAll(stockInfo.StockId);
        }

        public void Clear()
        {
            inputStockCount.text = $"{0}";
        }
    }
}