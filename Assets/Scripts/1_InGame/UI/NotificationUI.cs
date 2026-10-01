using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Utility;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using static StockGame.Scripts.Define.GameDefine;
using static StockGame.Scripts.Define.GameDefine.RoundDefine;

namespace StockGame.Scripts.UI
{
    public enum NotificationType
    {
        Tax,
        Supply,
        Broker
    }
    public struct NotificationContext
    {
        public NotificationType NotificationType { get; }
        public string Value { get; }

        public NotificationContext(NotificationType type, string value)
        {
            NotificationType = type;
            Value = value;
        }
    }

    public class NotificationUI : UIBase
    {
        [SerializeField] private TMP_Text notifyText;
        [SerializeField] private LocalizeStringEvent stringEvent;
        public const float NotificationDelay = 4f;
        public const float FadeOutDuration = 1f;
        private Queue<NotificationContext> queue = new();
        private bool isPlaying = false;

        private EventInstance _currentSfxInstance;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
        }

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);

            if (GameManager.Instance != null)
                GameManager.Instance.CurrentRound.OnValueChanged += OnRoundChanged;
        }

        public override void OnDispose()
        {
            base.OnDispose();

            if (GameManager.Instance != null)
                GameManager.Instance.CurrentRound.OnValueChanged -= OnRoundChanged;

            ForceStopImmediately();
        }

        private void OnRoundChanged(RoundInfo previous, RoundInfo current)
        {
            if (previous.RoundPhase == RoundPhase.Explore && current.RoundPhase != RoundPhase.Explore)
                ForceStopImmediately();
        }

        public void ShowOnlyAlpha()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        public void PushNotification(NotificationContext context)
        {
            queue.Enqueue(context);

            if (!isPlaying)
            {
                var token = Managers.Token.GetToken(this);
                ProcessQueueAsync(token).Forget();
            }
        }

        private async UniTask ProcessQueueAsync(CancellationToken token = default)
        {
            isPlaying = true;

            try
            {
                while (queue.Count > 0 && !token.IsCancellationRequested)
                {
                    var context = queue.Dequeue();
                    SetNotification(context);
                    ShowOnlyAlpha();
                    PlaySfx();

                    await UniTask.WaitForSeconds(NotificationDelay, cancellationToken: token);
                    StopSfx();
                    await HideAsync(FadeOutDuration, token: token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                ForceStopImmediately();
            }
        }

        private void ForceStopImmediately()
        {
            queue.Clear();
            isPlaying = false;

            StopSfx();

            try
            {
                if (canvasGroup != null && DOTween.IsTweening(canvasGroup))
                    DOTween.Kill(canvasGroup, complete: false);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NotificationUI] 트윈 강제 종료 중 예외(무시하고 계속): {e}");
            }

            try
            {
                Managers.Token.CancelAll(this);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NotificationUI] 토큰 취소 중 예외(무시하고 계속): {e}");
            }

            Hide();
        }

        private void PlaySfx()
        {
            StopSfx();
            _currentSfxInstance = RuntimeManager.CreateInstance(ResourceDefine.FMODEvent.SFX263);
            _currentSfxInstance.start();
        }

        private void StopSfx()
        {
            if (_currentSfxInstance.isValid())
            {
                _currentSfxInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _currentSfxInstance.release();
            }
        }

        public void SetNotification(NotificationContext context)
        {
            switch (context.NotificationType)
            {
                case NotificationType.Tax: SetTaxText(context.Value); break;
                case NotificationType.Supply: SetSupplyBoxText(context.Value); break;
                case NotificationType.Broker: SetBrokerText(); break;
                default: return;
            }
        }

        public void SetTaxText(string percent)
        {
            stringEvent.SetLocalization("Notification_String", "tax_notice", value =>
            {
                Debug.Log($"{percent} || {value} || hasToken:{value.Contains("{0}")} || locale:{LocalizationSettings.SelectedLocale.Identifier.Code}");
                notifyText.text = string.Format(value, percent);
            });
        }

        public void SetSupplyBoxText(string zoneKey)
        {
            stringEvent.SetLocalization("Notification_String", "supply_spawn_notice", formatValue =>
            {
                var zoneNameString = new LocalizedString("Map_String", zoneKey);
                var handle = zoneNameString.GetLocalizedStringAsync();
                handle.Completed += op =>
                {
                    notifyText.text = string.Format(formatValue, op.Result);
                };
            });
        }

        public void SetBrokerText()
        {
            stringEvent.SetLocalization("Notification_String", "broker_appear_notice", value =>
            {
                notifyText.text = value; // 포맷 인자 없음
            });
        }

        public void Cancel() => ForceStopImmediately();

        public void Clear() => ForceStopImmediately();
    }
}