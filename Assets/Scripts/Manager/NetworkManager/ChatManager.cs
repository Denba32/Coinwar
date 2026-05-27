using Denba.Common;
using System;
using System.Collections.Generic;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    public class ChatLog
    {
        public DateTime ChatLogTime { get; }
        public string ChatLogPlayerName { get; }
        public string ChatLogMessage { get; }
        public ulong ClientId { get; }

        public ChatLog(DateTime chatLogTime, string chatLogPlayerName, string chatLogMessage, ulong clientId)
        {
            ChatLogTime = chatLogTime;
            ChatLogPlayerName = chatLogPlayerName;
            ChatLogMessage = chatLogMessage;
            ClientId = clientId;
        }
    }

    public class ChatManager : NetworkSingleton<ChatManager>
    {
        private readonly Dictionary<ulong, List<ChatLog>> chatHistory = new();
        private readonly List<ChatLog> chatLogs = new();

        public RectTransform ChatPanelContent { get; private set; }

        private Subject<ChatLog> onMessageReceived = new();
        public IObservable<ChatLog> OnMessageReceived => onMessageReceived;

        public void SetChatPanelContent(RectTransform panelContent)
        {
            ChatPanelContent = panelContent;
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void RequestMessageRpc(ulong clientId, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            var nickname = LobbyManager.Instance.GetPlayerDataByClientId(clientId).GetNickname();
            var profile = LobbyManager.Instance.GetPlayerDataByClientId(clientId).GetProfileImage();

            // 서버 시간 기준으로 timestamp 생성
            var timestamp = DateTime.Now.ToBinary(); // DateTime → long 직렬화

            if (!chatHistory.ContainsKey(clientId))
                chatHistory[clientId] = new List<ChatLog>();

            BroadcastMessageRpc(nickname, message, timestamp, clientId);
        }

        [Rpc(SendTo.Everyone)]
        private void BroadcastMessageRpc(string nickname, string message, long timestamp, ulong clientId)
        {
            var log = new ChatLog(
                DateTime.FromBinary(timestamp),
                nickname,
                message,
                clientId
            );

            chatLogs.Add(log);

            if (!chatHistory.ContainsKey(NetworkManager.LocalClientId))
                chatHistory[NetworkManager.LocalClientId] = new List<ChatLog>();
            chatHistory[NetworkManager.LocalClientId].Add(log);

            onMessageReceived?.OnNext(log);
        }

        public List<ChatLog> GetChatLogs() => chatLogs;

        public List<ChatLog> GetChatLogsByClientId(ulong clientId)
        {
            chatHistory.TryGetValue(clientId, out var logs);
            return logs;
        }

        public void Clear()
        {
            chatHistory?.Clear();
            chatLogs?.Clear();
        }

        public override void OnDestroy()
        {
            onMessageReceived?.Dispose();
            onMessageReceived = null;
            base.OnDestroy();
        }
    }
}
