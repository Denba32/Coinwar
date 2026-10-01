using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using StockGame.Scripts.Define;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class UIBroomItem : UIPickedItem
    {
        private const string INTERACT_TARGET = "InteractBounds";

        [Header("먼지 프리팹")]
        [SerializeField] private UIDust dustPrefab;

        [Space]
        [Header("시간 내 눌러야하는 횟수")]
        [SerializeField] private int requiredClickCount = 5;

        [Header("상호작용 범위")]
        [SerializeField] private RectTransform interactableBounds;

        [Space]
        [Header("타이머 슬라이더")]
        [SerializeField] private Slider timerSlider;

        [Space]
        [Header("제거 시도 가이드")]
        [SerializeField] private TMP_Text removeTryGuideText;

        [Space]
        [Header("거미줄 제거 시간")]
        [SerializeField] private float seconds;

        [Space]
        [Header("변경되는 스프라이트")]
        [SerializeField] private List<Sprite> sprites = new();

        private EventInstance spiderSound = default;

        private Action onForceDestroy = null;

        private int currentClickCount;
        public int CurrentClickCount
        {
            get => currentClickCount;
            set
            {
                currentClickCount = value;
                SetTimes(currentClickCount);
            }
        }

        private bool isCleaning;

        private CancellationTokenSource cts;
        private Collider2D target;

        private void Start()
        {
            ActivateUI(false);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag(INTERACT_TARGET)) return;
            target = collision;
            ResetBroom();
            ActivateUI(true);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.CompareTag(INTERACT_TARGET)) return;
            target = null;
            ActivateUI(false);
            cts?.Cancel();
            onForceDestroy?.Invoke();
            onForceDestroy = null;
        }
        public override void OnPickUse()
        {
            base.OnPickUse();

            if (target == null)
            {
                cts?.Cancel();
                return;
            }

            if (isCleaning)
            {
                if(!spiderSound.isValid())
                {
                    spiderSound = RuntimeManager.CreateInstance(GameDefine.ResourceDefine.FMODEvent.SFX213);
                }

                spiderSound.getPlaybackState(out var state);

                if(state == PLAYBACK_STATE.STOPPED)
                {
                    spiderSound.start();
                }

                CurrentClickCount++;
                if (currentClickCount >= requiredClickCount)
                {
                    cts?.Cancel();
                    OnCleanSuccess();
                }
                return;
            }

            CleanAsync().Forget();
        }

        private async UniTaskVoid CleanAsync()
        {
            isCleaning = true;
            CurrentClickCount++;

            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();

            var dust = Instantiate(dustPrefab, target.transform);
            dust.RectTransform.anchoredPosition = Vector2.zero;
            onForceDestroy = dust.ForceDestroy;
            if (target.transform is not RectTransform targetRectTransform) return;
            Debug.Log($"{targetRectTransform.anchoredPosition} | {target.offset} | {dust.RectTransform.anchoredPosition}");
            dust.RectTransform.anchoredPosition = target.offset;

            float duration = dust.Play(seconds);
            try
            {
                while (duration > 0)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
                    duration -= Time.deltaTime;
                    // 부드럽게 감소
                    timerSlider.value = duration;
                }
                // 시간 초과
                ResetState();
                ResetBroom();
            }
            catch (OperationCanceledException)
            {
                // 성공해서 Cancel 된 경우
            }
            // 여기 도달하면 시간 초과 = 실패
            ResetState();
        }

        private void OnCleanSuccess()
        {
            // 성공 처리
            Debug.Log("청소 성공");
            target.gameObject.SetActive(false);
            target = null;
            currentCompleteCount++;
            Debug.Log($"Clean success: {currentCompleteCount}");
            var spriteIndex = Mathf.Clamp(currentCompleteCount, 0, sprites.Count - 1);
            var sprite = sprites[spriteIndex];
            image.sprite = sprite;
            ResetState();

            if(currentCompleteCount >= requiredCount)
            {
                ForceEndPickup();
                onCompleted?.OnNext(Unit.Default);
            }
        }

        private void ActivateUI(bool active)
        {
            timerSlider.gameObject.SetActive(active);
            removeTryGuideText.gameObject.SetActive(active);
        }

        private void ResetBroom()
        {
            timerSlider.maxValue = seconds;
            timerSlider.minValue = 0;

            timerSlider.value = seconds;
            currentClickCount = 0;
            SetTimes(currentClickCount);
        }

        private void SetTimes(int times)
        {
            removeTryGuideText.text = $"{times} / {requiredClickCount}";
        }

        private void ResetState()
        {
            isCleaning = false;
            currentClickCount = 0;

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }
}