using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class RockPaperScissorsButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Sprite pressedUpSprite;
        [SerializeField] private Sprite pressedDownSprite;

        [SerializeField] RockPaperScissors buttonType;

        private Subject<RockPaperScissors> onPressed = new();
        public IObservable<RockPaperScissors> OnPressed => onPressed;
        private bool isPressed;
        private IDisposable disposable;

        private void Start()
        {
            disposable = button.OnClickAsObservable().Subscribe(_ => Press().Forget());
        }

        public async UniTask Press()
        {
            if (isPressed) return;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX220);
            isPressed = true;
            buttonImage.sprite = pressedDownSprite;
            onPressed?.OnNext(buttonType);
            await UniTask.WaitForSeconds(0.5f);
            buttonImage.sprite = pressedUpSprite;
            isPressed = false;
        }

        public void SetInteractable(bool isActive)
        {
            button.enabled = isActive;
        }
        private void OnDestroy()
        {
            disposable?.Dispose();
        }
    }
}