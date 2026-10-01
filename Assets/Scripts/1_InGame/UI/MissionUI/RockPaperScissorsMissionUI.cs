using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public enum RockPaperScissors
    {
        Scissors = 0, // 빠
        Rock = 1, // 묵
        Paper = 2, // 찌
    }

    public class RockPaperScissorsMissionUI : MissionUIBase
    {
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failureSprite;

        [SerializeField] private List<Sprite> rockPaperScissors;

        [SerializeField] private Image screenImage;
        [SerializeField] private RockPaperScissorsScreenUI playerScreen;
        [SerializeField] private RockPaperScissorsScreenUI cpuScreen;

        [SerializeField] private List<RockPaperScissorsButtonUI> buttons = new();

        private readonly Dictionary<RockPaperScissors, RockPaperScissors> rockPaperScissorsRules = new()
        {
            { RockPaperScissors.Rock, RockPaperScissors.Scissors},
            { RockPaperScissors.Scissors, RockPaperScissors.Paper},
            { RockPaperScissors.Paper, RockPaperScissors.Rock},
        };

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            InitAsync().Forget();
        }

        private UniTask InitAsync()
        {
            playerScreen.gameObject.SetActive(false);
            cpuScreen.gameObject.SetActive(false);
            foreach(var button in buttons)
            {
                button.OnPressed.Subscribe(Press).AddTo(disposables);
            }
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX246);
            return UniTask.CompletedTask;
        }

        private void Press(RockPaperScissors value)
        {
            // 버튼 클릭 후, 모든 버튼 비활성화
            foreach(var button in buttons)
            {
                button?.SetInteractable(false);
            }

            var spriteValue = (int)value;
            var sprite = rockPaperScissors[spriteValue];

            playerScreen.gameObject.SetActive(true);
            playerScreen?.SetScreen(value, sprite);
            ResultCPU();
        }

        private void ResultCPU()
        {
            var random = Random.Range(0, 3);
            var sprite = rockPaperScissors[random];
            var value = (RockPaperScissors)random;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX249);
            cpuScreen.gameObject.SetActive(true);
            cpuScreen?.SetScreen(value, sprite);
            CheckResult().Forget();
        }

        private async UniTask CheckResult()
        {
            var playerValue = playerScreen.InputValue;
            var cpuValue = cpuScreen.InputValue;

            var resultValue = rockPaperScissorsRules[playerValue];

            await UniTask.WaitForSeconds(0.5f, cancellationToken:destroyCancellationToken);
            if (this == null || destroyCancellationToken.IsCancellationRequested) return;

            if (resultValue == cpuValue) // 승리한 경우
            {
                Success();
            }
            else // 실패한 경우
            {
                Failed();
            }

            await UniTask.WaitForSeconds(1f, cancellationToken: destroyCancellationToken);
            if (this == null) return; 
            Close(Unit.Default);
        }

        private void Success()
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX247);
            mission?.Complete();
            screenImage.sprite = successSprite;
            playerScreen.gameObject.SetActive(false);
            cpuScreen.gameObject.SetActive(false);
        }

        private void Failed()
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX248);
            screenImage.sprite = failureSprite;
            playerScreen.gameObject.SetActive(false);
            cpuScreen.gameObject.SetActive(false);
        }
    }
}