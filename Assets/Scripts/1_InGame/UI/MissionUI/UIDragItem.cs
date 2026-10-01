using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public interface IDragItem : IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        int Id { get; }
        RectTransform Bounds { get; }
        void ForceDragEnd();
    }

    public interface IDropItem
    {
        void ApplyDrop(RectTransform pivot, int sortingOrder, bool isRandomlyDrop, float randomRange = 0f);
    }
    public class UIDragItem : MonoBehaviour, IDragItem, IDropItem
    {
        [Header("Drag 상호작용 범위")]
        [SerializeField] private RectTransform rect;

        [Space]
        [Header("Drag 할 수 있는 범위")]
        [SerializeField] private RectTransform draggableBoundary;

        [Space]
        [Header("Slot에 인식되는 범위")]
        [SerializeField] private RectTransform bounds;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Transform startParent;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private Image dragItemImage;

        private bool isDragable = true;
        private Vector2 rawDragPosition; // 클램프되지 않은 실제 드래그 위치 (커서 추적용)

        [Space]
        [Header("Drag 번호 - Slot과 매치되는 Id를 설정")]
        [SerializeField] private int id;
        public int Id => id;
        public RectTransform Bounds => bounds;
        public RectTransform Rect => rect;


#if UNITY_EDITOR
        [ContextMenu(nameof(AutoInputComponent))]
        private void AutoInputComponent()
        {
            rect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            group = GetComponent<CanvasGroup>();
            sortingGroup = GetComponent<SortingGroup>();
        }

#endif
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isDragable) return;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX223);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            startParent = transform.parent;
            transform.SetParent(canvas.transform);
            group.blocksRaycasts = false;
            rawDragPosition = rect.anchoredPosition; // 드래그 시작 시점 위치로 초기화
            UIManager.Instance.SetDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            rawDragPosition += eventData.delta / canvas.scaleFactor; // 클램프 없이 커서 움직임 그대로 누적
            rect.anchoredPosition = ClampToCanvas(rawDragPosition);   // 화면에 보여줄 때만 클램프
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX210);
            if (!isDragable) return;
            UIManager.Instance.SetDrag(null);
            group.blocksRaycasts = true;
            if (transform.parent == canvas.transform)
            {
                transform.SetParent(startParent);
            }
        }
        private Vector2 ClampToCanvas(Vector2 pos)
        {
            Vector2 min = draggableBoundary.rect.min - rect.rect.min;
            Vector2 max = draggableBoundary.rect.max - rect.rect.max;

            pos.x = Mathf.Clamp(pos.x, min.x, max.x);
            pos.y = Mathf.Clamp(pos.y, min.y, max.y);

            return pos;
        }
        public void ApplyDrop(RectTransform pivot, int sortingOrder, bool isRandomlyDrop, float randomRange = 0f)
        {
            isDragable = false;
            rect.SetParent(pivot);
            float randomX = 0;
            if(isRandomlyDrop) randomX = Random.Range(-randomRange, randomRange);
            rect.anchoredPosition = Vector2.zero + new Vector2(randomX, 0);
            sortingGroup.sortingOrder = sortingOrder;
            Color dropColor = Color.white;
            if (dragItemImage == null) return;
            ColorUtility.TryParseHtmlString("#878787", out dropColor);
            dragItemImage.color = dropColor;
        }

        public void ChangeImage(Sprite sprite)
        {
            dragItemImage.sprite = sprite;
        }

        public void ForceDragEnd() { }
    }
}