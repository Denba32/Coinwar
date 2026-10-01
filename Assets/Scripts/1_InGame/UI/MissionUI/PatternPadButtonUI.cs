using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class PatternPadButtonUI : MonoBehaviour
    {
        private const string PRESS_DOWN = "Pressed";
        private const string PRESS_UP = "Normal";

        private IDisposable disposable;

        [SerializeField] private Animator animator;
        [SerializeField] private Button button;
        [SerializeField] private int index;

        private Subject<int> onClickButton = new();
        public IObservable<int> OnClickButton => onClickButton;

        public int Index => index;

        private void Start()
        {
            disposable = button?.OnClickAsObservable()?.Subscribe(_ => onClickButton?.OnNext(index));
        }

        public void PressDown()
        {
            if (animator == null) return;
            animator?.Play(PRESS_DOWN);
        }

        public void PressUp()
        {
            if (animator == null) return;
            animator?.Play(PRESS_UP);
        }

        private void OnDestroy()
        {
            disposable?.Dispose();
        }
    }
}