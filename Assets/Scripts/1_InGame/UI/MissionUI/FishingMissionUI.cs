using Cysharp.Threading.Tasks;
using DG.Tweening;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public sealed class FishingMissionUI : MissionUIBase
    {
        [SerializeField] private Image timerSlider;
        [SerializeField] private Image lineImage;
        [SerializeField] private Button lilButton;

        [Space]
        [Header("미션 진행 시간")]
        [SerializeField] private float missionTime = 5f;

        [Space]
        [Header("릴 손잡이")]
        [SerializeField] private RectTransform handlePivot;
        [SerializeField] private Ease ease = Ease.Linear;

        [Space]
        [Header("릴 감는 회전 속도 : 수치가 낮을 수록 빠름")]
        [SerializeField] private float duration;

        [Space]
        [Header("완료 클릭 횟수")]
        [SerializeField] private int completedClickCount;

        private int currentClickCount = 0;
        private bool isInteractable = true;

        private Tween handleTween;
        private Tween lineTween;
        private Tween timerTween;
        private const float END_VALUE = 60f;
        private const float START_VALUE = 616f;

        private float diff => START_VALUE - END_VALUE;
        private float gainValue => diff / completedClickCount;
        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);

            lilButton?.OnClickAsObservableWithThrottle(duration).Subscribe(_ => ReelInLine(duration).Forget()).AddTo(disposables);
            StartTimer(missionTime).Forget();
        }

        public override bool CheckMission()
        {
            if (currentClickCount >= completedClickCount)
            {
                isInteractable = false;
                timerTween?.Kill();
                ShowSuccess().Forget();
            }
            return base.CheckMission();
        }

        private UniTask ReelInLine(float duration)
        {
            if (!isInteractable) return UniTask.CompletedTask;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX218);
            lineTween?.Kill();
            lineTween = lineImage.rectTransform
                .DOSizeDelta(
                    new Vector2(
                        lineImage.rectTransform.sizeDelta.x,
                        lineImage.rectTransform.sizeDelta.y - gainValue),
                    duration)
                .SetEase(ease)
                .SetLink(gameObject);

            handleTween = handlePivot
                .DOLocalRotate(new Vector3(0, 0, -360f), duration, RotateMode.FastBeyond360)
                .SetEase(ease)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    handlePivot.localEulerAngles = Vector3.zero;
                    currentClickCount++;
                    CheckMission();
                });
            return UniTask.CompletedTask;
        }

        private UniTask StartTimer(float duration)
        {
            timerTween = timerSlider.DOFillAmount(0f, duration)
                .SetEase(ease)
                .SetLink(gameObject)
                .OnComplete(() => ShowFailed().Forget());

            return UniTask.CompletedTask;
        }
    }
}