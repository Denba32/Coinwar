using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public interface IDrivenItem
    {
        void DeactiveMove();
        void TryMove(Vector3 delta);
        Collider2D Collider { get; }
        RectTransform Rect { get; }
    }
    public sealed class UIDrivenItem : MonoBehaviour, IDrivenItem
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private RectTransform bounds;
        [SerializeField] private Collider2D col;

        private bool isMovable = true;
        public Collider2D Collider => col;

        public RectTransform Rect => rect;

        public void DeactiveMove() => isMovable = false;

        public void TryMove(Vector3 delta)
        {
            if (!isMovable) return;
            Vector3 nextPos = rect.anchoredPosition3D + delta;

            if (!IsInsideBounds(nextPos))
                return;

            rect.anchoredPosition3D = nextPos;
        }
        private bool IsInsideBounds(Vector3 targetAnchoredPos)
        {
            Vector2 size = rect.rect.size;

            Vector2 min = (Vector2)targetAnchoredPos - size * rect.pivot;
            Vector2 max = min + size;

            Rect boundsRect = bounds.rect;

            Vector2 boundsMin = boundsRect.min;
            Vector2 boundsMax = boundsRect.max;

            bool inside =
                min.x >= boundsMin.x &&
                min.y >= boundsMin.y &&
                max.x <= boundsMax.x &&
                max.y <= boundsMax.y;

            return inside;
        }
    }
}