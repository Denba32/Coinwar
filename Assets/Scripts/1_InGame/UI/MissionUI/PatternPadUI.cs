using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public class PatternPadUI : MonoBehaviour
    {
        [SerializeField] private List<PatternPadButtonUI> buttonList = new();
        [SerializeField] private RectTransform interactBlocker;
        private List<int> correctPatterns;

        [SerializeField] private List<InputIndicatorUI> inputIndicators = new();
        private bool isPreviewPad;

        private int currentIndex = 0;
        private CompositeDisposable disposables = new();

        private Subject<Unit> onSuccess = new();
        private Subject<Unit> onFailure = new();

        public IObservable<Unit> OnSuccess => onSuccess;
        public IObservable<Unit> OnFailure => onFailure;

        public void Initialize(List<int> correctPatterns, bool isPreview)
        {
            this.correctPatterns = correctPatterns;
            isPreviewPad = isPreview;
            if (buttonList == null || buttonList.Count <= 0) return;
            foreach(var button in buttonList)
            {
                button.OnClickButton.Subscribe(Check).AddTo(disposables);
            }
            BlockInteract(true);
        }

        private void Check(int index)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX215);
            if (correctPatterns[currentIndex] == index)
            {
                if(inputIndicators.Count > currentIndex) inputIndicators[currentIndex].Correct();
                currentIndex++;
                if(currentIndex == correctPatterns.Count)
                {
                    onSuccess?.OnNext(Unit.Default);
                }
                return;
            }
            onFailure?.OnNext(Unit.Default);
        }

        public void BlockInteract(bool active)
        {
            if (interactBlocker.gameObject == null) return;
            interactBlocker.gameObject.SetActive(active);
        }

        public async UniTask ShowPreview(CancellationToken token = default)
        {
            foreach (var correctPattern in correctPatterns)
            {
                var button = buttonList.Find(button => button.Index == correctPattern);
                if (button == null) break;
                button.PressDown();
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX216);
                await UniTask.WaitForSeconds(1f, cancellationToken:token);
                if (token.IsCancellationRequested) return;
                button.PressUp();
            }
        }

        private void OnDestroy()
        {
            disposables?.Dispose();
            onSuccess?.Dispose();
            onFailure?.Dispose();
        }
    }
}