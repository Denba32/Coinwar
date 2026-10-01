using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace StockGame.Scripts.UI.Missions
{
    public interface IMoneyDragItem
    {
        int Money { get; }
    }
    public class MoneyDragItemUI : MonoBehaviour, IDragItem, IDropItem, IMoneyDragItem
    {
        [SerializeField] private int money;
        [SerializeField] private int id;
        [SerializeField] private RectTransform rect;
        [SerializeField] private RectTransform draggableBoundary;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Transform startParent;
        [SerializeField] private SortingGroup sortingGroup;

        private bool isDragable = true;

        public int Id => id;
        public int Money => money;
        public RectTransform Bounds => rect;

        public void ForceDragEnd()
        {
            isDragable = false;
            UIManager.Instance.SetDrag(null);
            group.blocksRaycasts = true;

            transform.SetParent(startParent);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            startParent = transform.parent;
            transform.SetParent(canvas.transform);
            group.blocksRaycasts = false;
            UIManager.Instance.SetDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
            ClampToCanvas();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX211);
            if (!isDragable) return;
            UIManager.Instance.SetDrag(null);
            group.blocksRaycasts = true;
            
            transform.SetParent(startParent);
        }

        void ClampToCanvas()
        {
            Vector2 pos = rect.anchoredPosition;

            Vector2 min = draggableBoundary.rect.min - rect.rect.min;
            Vector2 max = draggableBoundary.rect.max - rect.rect.max;

            pos.x = Mathf.Clamp(pos.x, min.x, max.x);
            pos.y = Mathf.Clamp(pos.y, min.y, max.y);

            rect.anchoredPosition = pos;
        }

        public void ApplyDrop(RectTransform pivot, int sortingOrder, bool isRandomlyDrop, float randomRange = 0f)
        {
            isDragable = false;
            rect.SetParent(pivot);
            float randomX = 0;
            if (isRandomlyDrop) randomX = Random.Range(-randomRange, randomRange);
            rect.anchoredPosition = Vector2.zero + new Vector2(randomX, 0);
            sortingGroup.sortingOrder = sortingOrder;
        }

        public void OnPointerDown(PointerEventData eventData) { }
    }
}