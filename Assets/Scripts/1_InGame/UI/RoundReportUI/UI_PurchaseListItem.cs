using StockGame.Scripts.Utility;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI
{
    public class UI_PurchaseListItem : MonoBehaviour, IDisposable
    {
        [SerializeField] private Image stockImage;
        [SerializeField] private TMP_Text purchasedStockNameText;
        [SerializeField] private TMP_Text purchasedCountText;
        [SerializeField] private LocalizeStringEvent stringEvent;
        [SerializeField] private LocalizeStringEvent countStringEvent;

        private string share;


        private NetworkPurchasedStock stockInfo;
        public void ReportPurchase(NetworkPurchasedStock stockInfo)
        {
            this.stockInfo = stockInfo;
            stockImage.sprite = stockInfo.GetStockProfile();
            purchasedStockNameText.text = stringEvent.SetLocalization("Stock_String", stockInfo.StockId.ToString());
            if (countStringEvent.StringReference == null || countStringEvent.StringReference.IsEmpty)
                share = countStringEvent.SetLocalization("Main_String", "share");
            purchasedCountText.text = $"{stockInfo.PurchaseCount} {share}";
        }

        public void Dispose()
        {
            purchasedStockNameText = null;
        }
    }
}