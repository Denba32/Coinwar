using Cysharp.Threading.Tasks;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public class ClickPatternMissionUI : MissionUIBase
    {
        [SerializeField] private int maxCount = 6;

        private List<int> numbers = new List<int>()
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9
        };

        private List<int> correctNumbers = new();

        [SerializeField] private PatternPadUI previewPatternPad;
        [SerializeField] private PatternPadUI interactPatternPad;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);

            numbers.Shuffle();

            for (int i = 0; i < maxCount; i++)
            {
                correctNumbers?.Add(numbers[i]);
            }

            previewPatternPad?.Initialize(correctNumbers, true);
            interactPatternPad?.Initialize(correctNumbers, false);

            interactPatternPad.OnSuccess?.Subscribe(_ => ShowSuccess().Forget()).AddTo(disposables);
            interactPatternPad.OnFailure?.Subscribe(_ => ShowFailed().Forget()).AddTo(disposables);

            InitAsync().Forget();
        }

        private async UniTask InitAsync()
        {
            await UniTask.WaitForSeconds(1f, cancellationToken:destroyCancellationToken);
            if (destroyCancellationToken.IsCancellationRequested) return;
            await previewPatternPad.ShowPreview(destroyCancellationToken);
            interactPatternPad.BlockInteract(false);
        }
    }
}