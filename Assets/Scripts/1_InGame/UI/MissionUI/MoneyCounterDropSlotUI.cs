#if UNITY_EDITOR
#endif

using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace StockGame.Scripts.UI.Missions
{
    public class MoneyCounterDropSlotUI : MonoBehaviour, IDropHandler
    {
        private Subject<Unit> onSuccess = new();
        private Subject<Unit> onFailure = new();

        private List<int> corrects;
        private int completedCount = 0;

        [SerializeField] private RectTransform interactBounds;
        [SerializeField] private RectTransform interactSuccessPivot;
        [SerializeField] private float threshold;

        [SerializeField] private int targetItemId;

        [SerializeField] private bool isRandomlyDrop = false;
        [Space]
        [Header("·£´ý ½Ã ¹üÀ§")]
        [SerializeField] private float randomRange = 0f;

        public IObservable<Unit> OnSuccess => onSuccess;
        public IObservable<Unit> OnFailure => onFailure;
        public void Initialize(List<int> corrects)
        {
            this.corrects = corrects;
        }

        public bool IsComplete() => corrects.Count <= completedCount;

        public void OnDrop(PointerEventData eventData)
        {
            if (IsComplete()) return;
            var dragItem = UIManager.Instance.CurrentDragItem;
            if (!dragItem.Bounds.Overlaps(interactBounds, threshold: threshold)) return;

            if(dragItem.Id == targetItemId)
            {
                if (dragItem is not IMoneyDragItem moneyDragItem) return;
                if(CheckMoney(moneyDragItem))
                {
                    if (dragItem is IDropItem dropItem)
                    {
                        dropItem.ApplyDrop(interactSuccessPivot, completedCount, isRandomlyDrop, randomRange);
                        completedCount++;
                        if (IsComplete())
                        {
                            onSuccess?.OnNext(Unit.Default);
                        }
                    }
                    return;
                }
                onFailure?.OnNext(Unit.Default);
            }
        }

        private bool CheckMoney(IMoneyDragItem moneyItem)
        {
            return corrects.Contains(moneyItem.Money);
        }
    }
}