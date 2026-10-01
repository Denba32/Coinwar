using StockGame.Scripts.Datas;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace StockGame.Scripts.UI
{
    public class UI_PlayerScore : MonoBehaviour
    {
        public Image img_PlayerProfile;
        public TMP_Text txt_Rank;
        public TMP_Text txt_PlayerName;
        public TMP_Text txt_Score;

        public void Initialize(NetworkPlayerData playerData)
        {
            txt_PlayerName.text = playerData.NickName.ToString();
            txt_Rank.text = "-";
            txt_Score.text = "0";
        }
        public void Initialize(Sprite profile, int rank, string playerName, int score)
        {
            if (profile != null) img_PlayerProfile.sprite = profile;
            txt_Rank.text = $"{rank}";
            txt_PlayerName.text = playerName;
            txt_Score.text = $"{score}";
        }

        //public void UpdateScore(RoundData roundData)
        //{
        //    var playerData = roundData.playerData;
        //    txt_PlayerName.text = playerData.NickName.ToString();
        //    txt_Rank.text = roundData.rank.ToString();
        //    txt_Score.text = roundData.score.ToString();
        //}
    }
}