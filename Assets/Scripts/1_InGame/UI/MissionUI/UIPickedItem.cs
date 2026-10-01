using FMOD.Studio;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public interface IPickedItem : IPointerClickHandler
    {
        void Move(Vector2 delta);
        void ForceEndPickup();
    }
    public class UIPickedItem : MonoBehaviour, IPickedItem
    {
        [Header("Picked Rect")]
        [SerializeField] protected RectTransform rect;
        [SerializeField] protected RectTransform draggableBoundary;

        [SerializeField] protected Canvas canvas;
        [SerializeField] protected CanvasGroup group;
        [SerializeField] protected Image image;

        [Header("요구되는 클리어 횟수")]
        [SerializeField] protected int requiredCount;
        [Header("현재 완료한 횟수")]
        [SerializeField] protected int currentCompleteCount;
        [SerializeField] private Vector2 offset;


        protected Subject<Unit> onCompleted = new();
        public IObservable<Unit> OnCompleted => onCompleted;
        public bool IsComplete() => requiredCount <= currentCompleteCount;

        public void Move(Vector2 delta)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                Input.mousePosition,
                canvas.worldCamera,
                out pos
            );

            rect.anchoredPosition = pos + offset;
            ClampToCanvas();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var manager = UIManager.Instance;
            var current = manager.CurrentPickedItem;

            bool isThisPicked = ReferenceEquals(current, this);

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (isThisPicked) OnPickUse();
                else if (current == null) 
                {
                    Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX223);
                    manager.Pick(this);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (isThisPicked) manager.Pick(null);
            }
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

        public virtual void OnPickUse()
        {
            Debug.Log("OnPickUse");
        }

        public void ForceEndPickup()
        {
            UIManager.Instance.Pick(null);
        }
    }
}