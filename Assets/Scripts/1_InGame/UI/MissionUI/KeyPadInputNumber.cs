using TMPro;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public class KeyPadInputNumber : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private TMP_Text numberText;
        public RectTransform Rect => rect;
        public void SetNumber(int number)
        {
            numberText.text = number.ToString();
        }
    }
}
