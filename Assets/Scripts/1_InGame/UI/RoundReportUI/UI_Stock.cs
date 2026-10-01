using StockGame.Scripts.Utility;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI
{
    public class UI_Stock : MonoBehaviour, IDisposable
    {
        [SerializeField] Sprite arrowUp, arrowDown, nonChange;

        [Header("Image")]
        [Space(10)]
        public Image img_StockProfile;
        public Image img_StockRateOfChange;

        [Header("Text")]
        [Space(10)]
        public TMP_Text txt_StockName;
        [SerializeField] private LocalizeStringEvent stringEvent;
        private LocalizedString localizedString;
        public TMP_Text txt_StockPrice;

        public TMP_Text txt_StockRateOfChange;

        public long currentPrice;

        public bool isInitialized = false;

        public void Initialize(NetworkStockInfo stockInfo)
        {
            if (stockInfo == null) return;
            txt_StockName.text = stringEvent.SetLocalization("Stock_String", stockInfo.StockId.ToString());

            img_StockProfile.sprite = stockInfo.GetStockProfile();
            img_StockRateOfChange.sprite = nonChange;
            txt_StockPrice.text = $"{stockInfo.AfterPrice:N0}";
            txt_StockRateOfChange.text = "";
        }

        public void UpdateStock(NetworkStockInfo stockInfo)
        {
            if (img_StockProfile.sprite == null)
                img_StockProfile.sprite = stockInfo.GetStockProfile();

            ChangePrice(stockInfo.AfterPrice, stockInfo);
        }

        public void SetPrice(int price)
        {
            txt_StockPrice.text = $"{price:N0}";
            txt_StockRateOfChange.text = "<color=black>0.00 (0.00)</color>";
            img_StockRateOfChange.sprite = nonChange;
            currentPrice = price;
            isInitialized = true;
        }

        public void ChangePrice(long changedPrice, NetworkStockInfo stockInfo)
        {
            txt_StockPrice.text = $"{changedPrice:N0}";
            var priceDelta = stockInfo.CalculateStockDelta();
            var diffPrice = stockInfo.AfterPrice - stockInfo.BeforePrice;
            if (priceDelta > 0)
            {
                txt_StockRateOfChange.text = $"<color=red>{diffPrice:N0} <size=60%><voffset=-2px>({priceDelta:N2}%)</voffset></size></color>";
                img_StockRateOfChange.sprite = arrowUp;
                img_StockRateOfChange.rectTransform.localEulerAngles = Vector3.zero;
            }
            else if (priceDelta < 0)
            {
                img_StockRateOfChange.sprite = arrowDown;
                img_StockRateOfChange.rectTransform.localEulerAngles = new Vector3(0, 0, 180f);
                txt_StockRateOfChange.text = $"<color=blue>{diffPrice:N0} <size=60%><voffset=-2px>({priceDelta:N2}%)</voffset></size></color>";

            }
            else
            {
                img_StockRateOfChange.sprite = nonChange;
                txt_StockRateOfChange.text = "<color=white>0.00 (0.00)</color>";
                img_StockRateOfChange.rectTransform.localEulerAngles = Vector3.zero;
            }

            currentPrice = changedPrice;
        }

        void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            arrowDown = null;
            arrowUp = null;
            nonChange = null;

            img_StockProfile = null;
            img_StockRateOfChange = null;
            txt_StockName = null;
            txt_StockPrice = null;
            txt_StockRateOfChange = null;
        }
    }
}
