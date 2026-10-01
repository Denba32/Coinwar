using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using StockGame.Utility;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class KeypadUI : MonoBehaviour
    {
        private CompositeDisposable _disposables = new();

        [SerializeField] private int maxKeyCount = 4;

        [Header("키패드 버튼")]
        [SerializeField] private List<KeyPadButtonUI> padButtons = new();

        [Header("입력한 키")]
        [SerializeField] private KeyPadInputNumber keyPadInputNumberPrefab;

        [SerializeField] private RectTransform keyPadInputBlocker;

        private List<int> correctKeys = new();
        private List<int> inputKeys = new();
        private List<int> numbers = new List<int>
        {
            1, 2, 3, 4, 5, 6, 7, 8 ,9
        };

        [SerializeField] private HorizontalLayoutGroup inputNumberContent;

        private Subject<Unit> onSuccess = new();
        public IObservable<Unit> OnSuccess => onSuccess;

        private Subject<Unit> onFailed = new();
        public IObservable<Unit> OnFailed => onFailed;

        public void Initialize()
        {
            if (padButtons == null || padButtons.Count == 0) return;
            SetLayout();
            foreach (var padButton in padButtons)
            {
                padButton.OnClickAsObservable.Subscribe(Press).AddTo(_disposables);
            }

            numbers.Shuffle();

            for(int i = 0; i < maxKeyCount; i++)
            {
                correctKeys.Add(numbers[i]);
            }

            PreviewNumber().Forget();
        }

        private void SetLayout()
        {
            if (inputNumberContent.transform is not RectTransform layoutTransform) return;
            var width = layoutTransform.rect.width;
            var count = maxKeyCount;
            var itemWidth = keyPadInputNumberPrefab.Rect.rect.width;
            float spacing = (width - (count * itemWidth)) / (count + 1);

            inputNumberContent.spacing = spacing;

            var padding = inputNumberContent.padding;
            padding.left = Mathf.RoundToInt(spacing);
            inputNumberContent.padding = padding;
        }

        private async UniTask PreviewNumber()
        {
            keyPadInputBlocker.gameObject.SetActive(true);

            foreach (var correctKey in correctKeys)
            {
                var key = padButtons.Find(key => key.KeyNumber == correctKey);
                key.PressDown();
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX216);
                await UniTask.WaitForSeconds(1f, cancellationToken:destroyCancellationToken);
                if(destroyCancellationToken.IsCancellationRequested) return;
                key.PressUp();
            }

            keyPadInputBlocker.gameObject.SetActive(false);
        }

        private void Press(int number)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX215);
            inputKeys?.Add(number);
            var inputNumber = Instantiate(keyPadInputNumberPrefab, inputNumberContent.transform);
            inputNumber.SetNumber(number);
            CheckKey();
        }

        private void CheckKey()
        {
            var keyCount = correctKeys.Count;
            var inputCount = inputKeys.Count;

            if(keyCount == inputCount)
            {
                var isCorrect = VerifyCode();
                if(isCorrect)
                {
                    onSuccess?.OnNext(Unit.Default);
                }
                else
                {
                    onFailed?.OnNext(Unit.Default);
                }
            }
        }

        private bool VerifyCode()
        {
            for(int i = 0; i < inputKeys.Count; i++)
            {
                if (correctKeys[i] == inputKeys[i])
                    continue;
                return false;
            }
            return true;
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
}
