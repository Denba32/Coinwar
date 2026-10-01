
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public sealed class ToolDrivenGoalSlot : MonoBehaviour
    {
        private const string MOVABLE = "Movable";
        [Header("가져와야 하는 오브젝트 횟수")]
        [SerializeField] private int requiredDrivenCount = 2;

        [Header("오브젝트가 범위 안에 들어왔을 때, 해당 대상을 랜덤한 위치에 이동시킬 영역")]
        [SerializeField] private RectTransform randomBounds;
        private int currentDrivenCount;

        private Subject<Unit> onCompleteDriven = new();
        public IObservable<Unit> OnCompleteDriven => onCompleteDriven;

        public bool IsComplete() => currentDrivenCount >= requiredDrivenCount;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag(MOVABLE)) return;
            var drivenItem = collision?.GetComponent<IDrivenItem>();
            if (drivenItem == null) return;
            Vector2 itemSize = drivenItem.Rect.rect.size;
            Rect boundsRect = randomBounds.rect;

            float randomX = UnityEngine.Random.Range(
                boundsRect.xMin + itemSize.x * drivenItem.Rect.pivot.x,
                boundsRect.xMax - itemSize.x * (1 - drivenItem.Rect.pivot.x)
            );

            float randomY = UnityEngine.Random.Range(
                boundsRect.yMin + itemSize.y * drivenItem.Rect.pivot.y,
                boundsRect.yMax - itemSize.y * (1 - drivenItem.Rect.pivot.y)
            );
            Vector2 localPoint = new Vector2(randomX, randomY);

            Vector3 worldPoint = randomBounds.TransformPoint(localPoint);
            Vector3 parentLocalPoint =
                drivenItem.Rect.parent.InverseTransformPoint(worldPoint);

            drivenItem.Rect.anchoredPosition = parentLocalPoint;

            drivenItem?.DeactiveMove();

            currentDrivenCount++;
            CheckMission();
        }

        private void CheckMission()
        {
            if(currentDrivenCount < requiredDrivenCount) return;
            onCompleteDriven?.OnNext(Unit.Default);
        }

        private void OnDestroy()
        {
            onCompleteDriven?.Dispose();
            onCompleteDriven = null;
        }
    }
}