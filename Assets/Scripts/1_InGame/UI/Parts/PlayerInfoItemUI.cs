using StockGame.Scripts.Datas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class PlayerInfoItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private Image profileImage;

        public void SetData(NetworkPlayerData playerData)
        {
            nicknameText.text = playerData.GetNickname();
            profileImage.sprite = playerData.GetProfileImage();
        }
    }
}