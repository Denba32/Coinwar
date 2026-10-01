using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Missions;
using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace StockGame.Scripts.UI.Missions
{
    public class NumberBattleMissionUI : MissionUIBase
    {
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failureSprite;

        [SerializeField] private Image screenImage;
        [SerializeField] private Image playerImage;
        [SerializeField] private Image cpuImage;

        [SerializeField] private NumberBattleButtonUI startButton;
        [SerializeField] private NumberBattleButtonUI stopButton;

        [SerializeField] private List<Sprite> numberSprites = new();

        private bool isStart = false;
        private bool isStop = false;
        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            startButton.OnPress.Subscribe(OnPressStartButton).AddTo(this);
            stopButton.OnPress.Subscribe(OnPressStopButton).AddTo(this);
        }

        private void OnPressStartButton(NumberBattleButtonUI _)
        {
            if (isStart || isStop) return;
            isStart = true;
            startButton?.SetInteractable(false);
            var token = Managers.Token.GetToken(this);
            RouletteAsync(token).Forget();
        }

        private void OnPressStopButton(NumberBattleButtonUI _)
        {
            if (!isStart) return;
            stopButton?.SetInteractable(false);
            isStop = true;
        }

        private async UniTask RouletteAsync(CancellationToken token = default)
        {
            try
            {
                int playerNumber = 0;
                playerImage.enabled = true;
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX246);
                while (!isStop)
                {
                    playerNumber = Random.Range(0, numberSprites.Count);
                    var playerNumberSpirte = numberSprites[playerNumber];
                    playerImage.sprite = playerNumberSpirte;
                    await UniTask.WaitForSeconds(0.2f, cancellationToken: token);
                }

                if (this == null) return;

                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX249);
                int cpuNumber = Random.Range(0, numberSprites.Count);
                var cpuNumberSprite = numberSprites[cpuNumber];
                cpuImage.sprite = cpuNumberSprite;

                var resultToken = Managers.Token.GetToken(this);
                await UniTask.WaitForSeconds(0.5f, cancellationToken: resultToken);

                if (this == null) return;

                playerImage.gameObject.SetActive(false);
                cpuImage.gameObject.SetActive(false);
                if (cpuNumber >= playerNumber) Failed();
                else Success();
                await UniTask.WaitForSeconds(1.0f, cancellationToken: resultToken);

                if (this == null) return;

                Close(Unit.Default);
            }
            catch (OperationCanceledException)
            {
                // UI가 Close/Dispose되어 토큰이 취소된 경우 - 정상적인 종료 경로이므로 무시
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{gameObject?.name}] RouletteAsync 예외 | {ex.Message}");
            }
        }

        private void Success()
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX247);
            screenImage.sprite = successSprite;
            MissionResolver.Notify(new MissionActionEvent(Define.GameDefine.MissionDefine.MissionActionType.CompleteInteraction, Define.GameDefine.MissionDefine.MissionTargetType.NumberGuessGame));
        }

        private void Failed()
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX248);
            screenImage.sprite = failureSprite;
        }
    }
}