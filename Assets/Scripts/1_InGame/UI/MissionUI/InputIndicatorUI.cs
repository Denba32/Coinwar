using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public class InputIndicatorUI : MonoBehaviour
    {
        [SerializeField] private GameObject correctObject;
        public void Correct()
        {
            correctObject.gameObject.SetActive(true);
        }
    }
}