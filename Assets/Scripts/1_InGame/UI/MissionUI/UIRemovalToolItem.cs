using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class UIRemovalToolItem : MonoBehaviour, IDragItem
    {
        private const string INTERACT_TARGET = "InteractBounds";

        [Header("Drag 상호작용 범위")]
        [SerializeField] private RectTransform rect;

        [Space]
        [Header("Drag 할 수 있는 범위")]
        [SerializeField] private RectTransform draggableBoundary;

        [Space]
        [Header("제거 인식 범위")]
        [SerializeField] private Collider2D interactionBounds;

        [Space]
        [Header("제거 대상 부착 범위")]
        [SerializeField] private RectTransform attachmentArea;

        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image toolImage;

        [SerializeField] private bool isChangedDesign = false;
        [SerializeField] private bool isAttachable = false;
        [SerializeField] private List<Sprite> changedSprites = new();

        [Space]
        [Header("필요한 개수")]
        [SerializeField] private int requiredCount = 0;

        [Space]
        [Header("현재 충족한 개수")]
        [SerializeField] private int completedCount = 0;

        private Subject<Unit> onCompleted = new Subject<Unit>();
        public IObservable<Unit> OnCompleted => onCompleted;

        private bool isDragable = true;
        private Transform startParent;

        public int Id => -1;

        public RectTransform Bounds => interactionBounds.transform as RectTransform;
        public bool IsComplete() => requiredCount <= completedCount;

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
            if (!isDragable) return;
            UIManager.Instance.SetDrag(null);
            group.blocksRaycasts = true;

            if (transform.parent == canvas.transform)
            {
                transform.SetParent(startParent);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag(INTERACT_TARGET)) return;
            if (collision.transform is not RectTransform itemRect) return;

            if (isAttachable)
            {
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX217);
                itemRect.PlaceRandomlyInRect(attachmentArea);
                itemRect.transform.SetParent(attachmentArea);
            }
            completedCount++;
            if (isChangedDesign)
            {
                ChangeSpirte();
            }
            CheckMission();
        }

        private void ChangeSpirte()
        {
            var index = Mathf.Clamp(completedCount, 0, changedSprites.Count - 1);
            var sprite = changedSprites[index];
            toolImage.sprite = sprite;
        }

        private void CheckMission()
        {
            if (completedCount < requiredCount) return;
            onCompleted?.OnNext(Unit.Default);
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
        public void ForceDragEnd()
        {
            OnEndDrag(null);
            isDragable = false;
        }

        public void OnPointerDown(PointerEventData eventData){ }
    }
}