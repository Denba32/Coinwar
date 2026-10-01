using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class KeyPadButtonUI : MonoBehaviour
    {
        private const string PRESS_DOWN = "Pressed";
        private const string PRESS_UP = "Normal";
        private IDisposable onClickButton;
        [Header("버튼")]
        [SerializeField] private Button button;

        [Header("애니메이터")]
        [SerializeField] private Animator animator;

        [Header("인식하는 Keypad의 숫자")]
        [SerializeField] private int index;
        public int KeyNumber => index;

        private Subject<int> onClick = new();
        public IObservable<int> OnClickAsObservable => onClick;

        private void Start()
        {
            onClickButton = button.OnClickAsObservable().Subscribe(OnClick);
        }

        public void OnClick(Unit _)
        {
            onClick?.OnNext(index);
        }

        private void OnDestroy()
        {
            onClickButton?.Dispose();
            onClick?.Dispose();
        }

        public void PressDown()
        {
            if (animator == null) return;
            animator.Play(PRESS_DOWN);
        }

        public void PressUp()
        {
            if (animator == null) return;
            animator.Play(PRESS_UP);
        }
    }
}