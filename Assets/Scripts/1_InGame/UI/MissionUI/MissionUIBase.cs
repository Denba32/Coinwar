using Cysharp.Threading.Tasks;
using DG.Tweening;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.UI.Missions
{
    public interface IMission
    {
        bool CheckMission();
    }

    public abstract class MissionUIBase : UIBase, IMission
    {
        protected bool isSuccess = false;
        protected bool isFailed = false;

        protected Mission mission;
        protected CompositeDisposable disposables = new CompositeDisposable();
        [SerializeField] protected Button closeButton;
        [SerializeField] protected PopupAnimation popupAnimation;
        [SerializeField] private RectTransform jugdeTransform;
        [SerializeField] protected TMP_Text successText;
        [SerializeField] protected TMP_Text failedText;

        protected bool isPopupAnimation = true;
        private Action onClose;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            closeButton.OnClickAsObservableFirst().Subscribe(Close).AddTo(disposables);
        }

        public override void OnOpen(params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                foreach (var item in args)
                {
                    if (item is Mission missionArg)
                    {
                        mission = missionArg;
                    }
                    else if (item is Action action)
                    {
                        onClose = action;
                    }
                }
            }

            InputManager.Instance?.StopPlayerInput();

            if (uiLayer == GameDefine.UIDefine.UILayer.Popup)
            {
                popupAnimation?.OpenAnimation(0.5f, Ease.OutQuad).Forget();
            }

            if (mission == null)
            {
                Debug.LogWarning($"{gameObject.name} : Mission Data가 없습니다");
            }
        }
        public override async UniTask OnClose(params object[] args)
        {
            await base.OnClose(args);

            if (isPopupAnimation)
            {
                await popupAnimation.CloseAnimation(0.5f, Ease.OutQuad);
            }
            InputManager.Instance?.StartPlayerInput();
        }

        public virtual bool CheckMission()
        {
            return false;
        }

        protected void Close(Unit _) 
        {
            Close();
        }

        protected virtual async UniTask ShowSuccess()
        {
            if (isClosing || this == null) return;
            if (isSuccess || isFailed) return;
            try
            {
                isSuccess = true;
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX252);
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX254);
                mission?.Complete();
                Managers.Input.StopUIInput();
                await ShowJudgeText(successText);
                Managers.Input.StartUIInput();
            }
            catch(Exception e) 
            { 
                Debug.Log(e); 
            }
            finally
            {
                Managers.Input.StartUIInput();
            }
        }

        protected virtual async UniTask ShowFailed()
        {
            if (isClosing || this == null) return;
            if (isSuccess || isFailed) return;
            try
            {
                isFailed = true;
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX253);
                Managers.Input.StopUIInput();
                await ShowJudgeText(failedText);
                Managers.Input.StartUIInput();
            }
            catch (Exception e) 
            { 
                Debug.Log(e); 
            }
            finally 
            { 
                Managers.Input.StartUIInput();
            }
        }

        private async UniTask ShowJudgeText(TMP_Text judgeText)
        {
            if (isClosing || this == null) return;

            try
            {
                jugdeTransform.gameObject.SetActive(true);

                var sizeY = judgeText.rectTransform.rect.size.y * 0.5f;
                var height = Screen.height * 0.5f;

                judgeText.rectTransform.anchoredPosition = new Vector2(0, sizeY + height);
                judgeText.gameObject.SetActive(true);

                await judgeText.rectTransform
                    .DOLocalMoveY(0f, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject)
                    .ToUniTask(cancellationToken: destroyCancellationToken);

                if (isClosing || this == null) return;

                await UniTask.WaitForSeconds(2.0f, cancellationToken: destroyCancellationToken);

                if (isClosing || this == null) return;

                await judgeText.rectTransform
                    .DOLocalMoveY(-(sizeY + height), 0.5f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject)
                    .ToUniTask(cancellationToken: destroyCancellationToken);

                if (isClosing || this == null) return;

                isPopupAnimation = false;
                Close();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[{gameObject?.name}] ShowJudgeText 예외 | {ex.Message}");
            }
        }

        public override void OnDispose()
        {
            disposables?.Dispose();
            onClose?.Invoke();
            disposables = null;
            onClose = null;
        }
    }
}