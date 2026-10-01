using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_ResultRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankingText;
        [SerializeField] private Image playerImage;
        [SerializeField] private TMP_Text playerNicknameText;
        [SerializeField] private TMP_Text profitText;

        public void UpdateData(int ranking, string nickname, Sprite profileImage, long profit)
        {
            rankingText.text = $"{ranking}";
            playerImage.sprite = profileImage;
            playerNicknameText.text = nickname;
            profitText.text = $"{profit:N0}";
        }
    }
}