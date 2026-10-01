using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UI_PurchaseList : UIBase
    {
        [SerializeField] private TMP_Text userCashText = null;
        [SerializeField] private RectTransform content = null;
        [SerializeField] private UI_PurchaseListItem purchaseListItemPrefab = null;
        private Dictionary<int, UI_PurchaseListItem> purchaseDict = new();

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            userCashText.text = $"{GameManager.Instance.GetLocalPlayerInfo().Money:N0}";
        }
        public void OnUpdatedCash(long cash)
        {
            userCashText.text = $"{cash:N0}";
        }

        public void UpdateData()
        {
            var localClientId = NetworkManager.Singleton.LocalClientId;
            var list = StockManager.Instance.GetPurchasedStockListByOwnerId(localClientId);
            if (list == null || list.Count == 0) return;
            
            foreach(var li in list)
            {
                var purchasedItem = Instantiate<UI_PurchaseListItem>(purchaseListItemPrefab, content);
                purchaseDict[li.StockId] = purchasedItem;
                purchasedItem?.ReportPurchase(li);
            }

            SortItems();
        }

        public void UpdateItem(NetworkPurchasedStock stockInfo)
        {
            Debug.Log("PurchaseList Update Item");
            if (!purchaseDict.TryGetValue(stockInfo.StockId, out var value))
            {
                value = Instantiate<UI_PurchaseListItem>(purchaseListItemPrefab, content);
                purchaseDict[stockInfo.StockId] = value;
            }

            if(stockInfo.PurchaseCount == 0)
            {
                purchaseDict.Remove(stockInfo.StockId);
                Destroy(value.gameObject);
                return;
            }
            value?.ReportPurchase(stockInfo);

            SortItems();
        }

        private void SortItems()
        {
            var sorted = new List<KeyValuePair<int, UI_PurchaseListItem>>(purchaseDict);
            sorted.Sort((a, b) => a.Key.CompareTo(b.Key));

            for (int i = 0; i < sorted.Count; i++)
                sorted[i].Value.transform.SetSiblingIndex(i);
        }

        public void Clear()
        {
            foreach (var item in purchaseDict.Values)
            {
                item?.Dispose();
                Destroy(item.gameObject);
            }
            purchaseDict?.Clear();
        }
    }
}