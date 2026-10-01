using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UniRx;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StockGame.Scripts.UI.Missions
{

    public class UIDropSlot : MonoBehaviour, IDropHandler
    {
        private const string INTERACT_BOUNDS = "InteractBounds";

        [Space]
        [Header("Root")]
        [SerializeField] private RectTransform root;

        [Space]
        [Header("Collider")]
        [SerializeField] private RectTransform interactBounds;

        [Space]
        [Header("Slot Drop 성공 시 이동 위치 피봇")]
        [SerializeField] private RectTransform interactSuccessPivot = null;

        [Space]
        [Header("필요한 개수")]
        [SerializeField] private int requiredCount = 0;

        [Space]
        [Header("현재 충족한 개수")]
        [SerializeField] private int completedCount = 0;
        [SerializeField] private int targetItemId;

        [Space]
        [Header("Drop 체크 허용 범위 0 - 1")]
        [Range(0f, 1f)]
        [SerializeField] private float threshold;

        [Space]
        [Header("Drop 시 X축으로 랜덤한 위치에 고정")]
        [SerializeField] private bool isRandomlyDrop = false;
        [Space]
        [Header("랜덤 시 범위")]
        [SerializeField] private float randomRange = 0f;

        private Subject<Unit> onSlotFilled = new Subject<Unit>();
        public IObservable<Unit> OnSlotFilled => onSlotFilled;


        public void OnDrop(PointerEventData eventData)
        {
            Debug.Log("OnDrop");
            if (IsComplete()) return;
            if (UIManager.Instance.CurrentDragItem == null) return;
            var dragItem = UIManager.Instance.CurrentDragItem;
            if (!dragItem.Bounds.Overlaps(interactBounds, threshold: threshold)) return;
            if (dragItem.Id == targetItemId)
            {
                if (dragItem is IDropItem dropItem)
                    dropItem.ApplyDrop(interactSuccessPivot, completedCount, isRandomlyDrop, randomRange);
                completedCount++;
                if (IsComplete())
                {
                    onSlotFilled?.OnNext(Unit.Default);
                }
            }
        }

        public bool IsComplete() => requiredCount <= completedCount;

        private void OnDestroy()
        {
            onSlotFilled?.Dispose();
            onSlotFilled = null;
        }

#if UNITY_EDITOR
        [ContextMenu(nameof(AutoInputComponent))]
        private void AutoInputComponent()
        {
            var parent = transform;
            while (parent.parent != null)
            {
                parent = parent.parent;
            }
            root = parent.GetComponent<RectTransform>();
            var rects = GetComponentsInChildren<RectTransform>();
            foreach (var rect in rects)
            {
                if (rect.CompareTag(INTERACT_BOUNDS))
                {
                    interactBounds = rect;
                    break;
                }
            }

            int count = 0;

            var targets = root.GetComponentsInChildren<UIDragItem>();
            if (targets == null || targets.Length == 0) return;
            foreach (var target in targets)
            {
                if (target.Id == targetItemId) count++;
            }
            requiredCount = count;

            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
#endif
    }
}