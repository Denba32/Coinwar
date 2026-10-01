using UnityEngine;
namespace StockGame.Scripts.UI.Missions
{
    public class TentHammerUI : MonoBehaviour
    {
        private const string INTERACT_BOUNDS = "InteractBounds";
        private Collider2D target;

        [SerializeField] private RectTransform rect;
        public RectTransform Rect => rect;
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag(INTERACT_BOUNDS)) return;
            target = collision;
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag(INTERACT_BOUNDS)) return;
            target = null;
        }

        public Collider2D GetTarget => target;
    }
}