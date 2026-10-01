using Cysharp.Threading.Tasks;
using DG.Tweening;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
namespace StockGame.Scripts.UI.Missions
{
    public class TentPegMissionUI : MissionUIBase, IPointerDownHandler
    {
        [Header("해머 관련 컴포넌트")]        
        [SerializeField] private RectTransform leftDestination;
        [SerializeField] private RectTransform rightDestination;

        [SerializeField] private TentHammerUI tentHammerUI;
        [SerializeField] private float hammerMoveDuration;

        private float originalY;

        [Header("못 관련 컴포넌트")]
        [SerializeField] private TentNailUI tentNail;
        [SerializeField] private RectTransform nailDestination;

        private float verticalSpacing;

        [Header("클릭 영역")]
        [SerializeField] private RectTransform clickableBoundary;
        private Tween moveTween;
        private Tween hitTween;
        private bool hitResolved;

        [Header("충돌 이펙트")]
        [SerializeField] private TentHammerHitEffect tentHammerHitEffectPrefab;
        private bool isClickable = false;
        private CancellationTokenSource hammerReturnCts = new();

        [Header("성공 조건")]
        [SerializeField] private int requiredHitCount;

        private int currentHitCount = 0;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            originalY = leftDestination.anchoredPosition.y;
            verticalSpacing = (tentNail.Rect.anchoredPosition.y - nailDestination.anchoredPosition.y) / (requiredHitCount * 1f);
            InitAsync().Forget();
        }
        private async UniTask InitAsync()
        {
            tentHammerUI.Rect.anchoredPosition = leftDestination.anchoredPosition;
            await UniTask.WaitForSeconds(0.5f, cancellationToken: destroyCancellationToken);
            if (destroyCancellationToken.IsCancellationRequested) return;
            StartMoveLoop();
            isClickable = true;
        }

        private void StartMoveLoop()
        {
            if (tentHammerUI.gameObject == null) return;

            moveTween = tentHammerUI.Rect
                .DOAnchorPos(rightDestination.anchoredPosition, hammerMoveDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(tentHammerUI.gameObject);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isClickable) return;
            if (IsInsideClickableArea(eventData))
            {
                StopAndHit();
            }
        }

        private void StopAndHit()
        {
            moveTween?.Kill();
            isClickable = false;
            var target = tentHammerUI.GetTarget;
            if (target == null)
            {
                ShowFailed().Forget();
                return;
            }

            // target != null 이면 유효한 타격으로 확정한다.
            // CheckCollision(시각적 타이밍 감지)이 맞추지 못하더라도 트윈 완료 시점에 반드시
            // ResolveHit 이 호출되므로, 망치가 바닥에 박힌 채 멈추는 상황은 발생하지 않는다.
            hitResolved = false;
            hitTween = tentHammerUI.Rect
                .DOAnchorPosY(nailDestination.anchoredPosition.y, 0.5f)
                .SetEase(Ease.Linear)
                .OnUpdate(CheckCollision)
                .OnComplete(() => ResolveHit(null))
                .SetLink(gameObject);
        }

        private void CheckCollision()
        {
            if (hitResolved) return;
            if (IsColliding(tentHammerUI.Rect, tentNail.Rect, out var hitPoint))
            {
                ResolveHit(hitPoint);
            }
        }

        // 한 번의 타격을 확정 처리한다. CheckCollision(겹침 감지) 또는 hitTween 완료 콜백에서
        // 호출되며, hitResolved 가드로 중복 처리를 막는다.
        private void ResolveHit(Vector2? hitPoint)
        {
            if (hitResolved) return;
            hitResolved = true;

            hitTween?.Kill();

            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX214);
            currentHitCount++;
            SpawnEffect(hitPoint ?? Vector2.zero);

            Vector2 pos = tentNail.Rect.anchoredPosition;
            pos.y -= verticalSpacing;
            tentNail.Rect.anchoredPosition = pos;

            HandleReturn().Forget();
        }

        private async UniTask HandleReturn()
        {
            hammerReturnCts?.Cancel();
            hammerReturnCts = new();
            await UniTask.WaitForSeconds(0.5f, cancellationToken: hammerReturnCts.Token);
            if (hammerReturnCts.IsCancellationRequested) return;
            if (this == null) return;

            if (currentHitCount >= requiredHitCount)
            {
                ShowSuccess().Forget();
                return;
            }
            tentHammerUI.Rect.anchoredPosition = leftDestination.anchoredPosition;
            StartMoveLoop();
            isClickable = true;
        }

        private void SpawnEffect(Vector2 pos)
        {
            var effect = Instantiate(tentHammerHitEffectPrefab, tentNail.transform);
            effect.Rect.anchoredPosition = pos;
            effect.Play();
        }

        private bool IsColliding(RectTransform a, RectTransform b, out Vector2 hitPoint)
        {
            Rect rectA = GetWorldRect(a);
            Rect rectB = GetWorldRect(b);

            if (!rectA.Overlaps(rectB))
            {
                hitPoint = Vector2.zero;
                return false;
            }

            float xMin = Mathf.Max(rectA.xMin, rectB.xMin);
            float yMin = Mathf.Max(rectA.yMin, rectB.yMin);
            float xMax = Mathf.Min(rectA.xMax, rectB.xMax);
            float yMax = Mathf.Min(rectA.yMax, rectB.yMax);

            Rect overlapRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            Vector2 worldCenter = overlapRect.center;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tentNail.Rect,
                RectTransformUtility.WorldToScreenPoint(null, worldCenter),
                null,
                out hitPoint
            );

            return true;
        }
        private Rect GetWorldRect(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            // 회전된 RectTransform(예: -30° 기울어진 못)도 올바르게 감싸도록 4개 코너의 min/max로 AABB를 만든다.
            float xMin = corners[0].x, xMax = corners[0].x;
            float yMin = corners[0].y, yMax = corners[0].y;
            for (int i = 1; i < 4; i++)
            {
                xMin = Mathf.Min(xMin, corners[i].x);
                xMax = Mathf.Max(xMax, corners[i].x);
                yMin = Mathf.Min(yMin, corners[i].y);
                yMax = Mathf.Max(yMax, corners[i].y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private bool IsInsideClickableArea(PointerEventData eventData)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                clickableBoundary,
                eventData.position,
                eventData.pressEventCamera
            );
        }

        public override void OnDispose()
        {
            base.OnDispose();
            tentHammerUI = null;


            hammerReturnCts?.Cancel();
            hammerReturnCts?.Dispose();
            hammerReturnCts = null;
        }
    }
}