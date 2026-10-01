using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class PriceCalculationTaskCoin : MonoBehaviour
    {
        [SerializeField] private Image coinImage;

        public void SetCoin(Sprite sprite)
        {
            coinImage.sprite = sprite;
            coinImage.SetNativeSize();
        }
    }
}