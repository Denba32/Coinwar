using StockGame.Scripts.Manager;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public sealed class UIRubbingSlot : MonoBehaviour
    {
        [Header("문질러야 하는 횟수")]
        [SerializeField] private int requiredRubCount = 2;

        [Space]
        [Header("문지를 수 있는 대상 Id")]
        [SerializeField] private int targetItemId;

        [SerializeField] private bool isInside;

        [SerializeField] private Image slotImage;
        private int currentRubbingCount;

        private Subject<Unit> onRubbed = new();
        public IObservable<Unit> OnRubbed => onRubbed;

        public bool IsComplete() => currentRubbingCount >= requiredRubCount;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isInside) return;

            var currentDragItem = UIManager.Instance.CurrentDragItem;
            if (currentDragItem == null) return;
            TryClean(currentDragItem);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            var currentDragItem = UIManager.Instance.CurrentDragItem;
            if (currentDragItem == null) return;
            if (currentDragItem.Id != targetItemId) return;

            isInside = false;
        }

        private void TryClean(IDragItem item)
        {
            if (item.Id != targetItemId || IsComplete()) return;

            currentRubbingCount++;

            Debug.Log($"Clean {currentRubbingCount}/{requiredRubCount}");

            if (currentRubbingCount >= requiredRubCount)
            {
                if (item is IRubbingItem rubbingItem) 
                    rubbingItem.OnRubbed();

                Clear();
            }
        }

        private void Clear()
        {
            slotImage.enabled = false;
            onRubbed?.OnNext(Unit.Default);
        }

        private void OnDestroy()
        {
            onRubbed?.Dispose();
            onRubbed = null;
        }
    }
}