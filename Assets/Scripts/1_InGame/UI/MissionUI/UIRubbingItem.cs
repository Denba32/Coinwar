using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public interface IRubbingItem
    {
        void OnRubbed();
    }
    public sealed class UIRubbingItem : MonoBehaviour, IDragItem, IRubbingItem
    {
        [Header("Drag 상호작용 범위")]
        [SerializeField] private RectTransform rect;

        [Space]
        [Header("Drag 할 수 있는 범위")]
        [SerializeField] private RectTransform draggableBoundary;

        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Transform startParent;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField] private RectTransform bounds;

        [SerializeField] private Image rubbingItemImage;

        private bool isDragable = true;

        [SerializeField] private List<Sprite> rubbingChangeSprites = new();
        [SerializeField] private int id;

        public int Id => id;

        private int status = -1;
        private Vector2 rawDragPosition; // 클램프되지 않은 실제 드래그 위치 (커서 추적용)

        public RectTransform Bounds => bounds;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            startParent = transform.parent;
            rawDragPosition = rect.anchoredPosition; // 드래그 시작 시점 위치로 초기화
            transform.SetParent(canvas.transform);
            group.blocksRaycasts = false;
            UIManager.Instance.SetDrag(this);
        }

        public void OnRubbed()
        {
            if (rubbingChangeSprites == null) return;
            if(rubbingChangeSprites.Count == 0) return;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX219);
            status++;
            status = Mathf.Clamp(status, 0, rubbingChangeSprites.Count - 1);
            rubbingItemImage.sprite = rubbingChangeSprites[status];
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragable) return;
            rawDragPosition += eventData.delta / canvas.scaleFactor; // 클램프 없이 커서 움직임 그대로 누적
            rect.anchoredPosition = ClampToCanvas(rawDragPosition);   // 화면에 보여줄 때만 클램프
        }

        public void OnEndDrag(PointerEventData eventData)
        {
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

        public void ForceDragEnd()
        {
            OnEndDrag(null);
            isDragable = false;
        }

        public void OnPointerDown(PointerEventData eventData) { }
    }
}