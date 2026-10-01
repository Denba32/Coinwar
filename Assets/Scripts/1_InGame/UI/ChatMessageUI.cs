using StockGame.Scripts.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Chat
{
    public class ChatMessageUI : MonoBehaviour
    {
        [SerializeField] private Image profileImage;
        [SerializeField] private TMP_Text messageText;

        public void SetData(ChatLog log)
        {
            messageText.text = log.ChatLogMessage;
            profileImage.sprite = LobbyManager.Instance.GetPlayerDataByClientId(log.ClientId).GetProfileImage();
        }
    }
}