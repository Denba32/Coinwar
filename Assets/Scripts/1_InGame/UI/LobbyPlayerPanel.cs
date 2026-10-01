using StockGame.Scripts.Datas;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.UI
{
    public class LobbyPlayerPanel : MonoBehaviour
    {
        [SerializeField] private PlayerInfoItemUI playerInfoItemPrefab;
        [SerializeField] private Transform content;
        private List<PlayerInfoItemUI> playerInfoList = new();

        public void Initialize()
        {
            LobbyManager.Instance.PlayerDataList.OnListChanged += OnChangedPlayer;
            RefreshItems();
        }

        private void OnChangedPlayer(NetworkListEvent<NetworkPlayerData> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<NetworkPlayerData>.EventType.Add:
                case NetworkListEvent<NetworkPlayerData>.EventType.Value:
                case NetworkListEvent<NetworkPlayerData>.EventType.RemoveAt:
                    RefreshItems();
                    break;
                default: break;
            }
        }

        private void RefreshItems()
        {
            ClearItem();

            var playerDataList = LobbyManager.Instance.PlayerDataList;
            var sortedList = new List<NetworkPlayerData>(playerDataList.Count);
            for (int i = 0; i < playerDataList.Count; i++)
                sortedList.Add(playerDataList[i]);

            sortedList.Sort((a, b) => a.Index.CompareTo(b.Index));

            for (int i = 0; i < sortedList.Count; i++)
            {
                var item = Instantiate(playerInfoItemPrefab, content);
                item.SetData(sortedList[i]);
                playerInfoList.Add(item);
            }
        }

        private void ClearItem()
        {
            if (playerInfoList == null || playerInfoList.Count <= 0) return;
            foreach (var item in playerInfoList)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            playerInfoList.Clear();
        }

        private void OnDestroy()
        {
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.PlayerDataList.OnListChanged -= OnChangedPlayer;
        }
    }
}