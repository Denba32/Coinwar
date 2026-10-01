using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class RockPaperScissorsScreenUI : MonoBehaviour
    {
        [SerializeField] private Image screenImage;

        private RockPaperScissors inputValue;
        public RockPaperScissors InputValue => inputValue;
        public void SetScreen(RockPaperScissors inputValue, Sprite sprite)
        {
            this.inputValue = inputValue;
            screenImage.sprite = sprite;
            screenImage.SetNativeSize();
        }
    }
}