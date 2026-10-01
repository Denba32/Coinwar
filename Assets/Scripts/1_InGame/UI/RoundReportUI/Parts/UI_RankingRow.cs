using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_RankingRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankingText;
        [SerializeField] private Image playerImage;
        [SerializeField] private TMP_Text profitText;
        [SerializeField] private TMP_Text nicknameText;
        public void UpdateData(int ranking, string nickname, Sprite profileImage, long profit)
        {
            rankingText.text = $"{ranking}";
            nicknameText.text = nickname;
            playerImage.sprite = profileImage;
            profitText.text = $"{profit:N0}";
        }
    }
}
