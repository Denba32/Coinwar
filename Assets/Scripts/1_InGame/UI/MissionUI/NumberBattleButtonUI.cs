using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class NumberBattleButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Sprite pressedUpSprite;
        [SerializeField] private Sprite pressedDownSprite;

        private bool isPressed;
        private IDisposable disposable;

        private Subject<NumberBattleButtonUI> onPress = new();
        public IObservable<NumberBattleButtonUI> OnPress => onPress;

        private void Start()
        {
            disposable = button.OnClickAsObservable().Subscribe(_ => Press().Forget());
        }

        public async UniTask Press()
        {
            if (isPressed) return;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX220);
            isPressed = true;
            onPress?.OnNext(this);
            var token = Managers.Token.GetToken(this);
            buttonImage.sprite = pressedDownSprite;
            await UniTask.WaitForSeconds(0.5f, cancellationToken: token);
            buttonImage.sprite = pressedUpSprite;
            isPressed = false;
        }

        public void SetInteractable(bool isActive)
        {
            button.enabled = isActive;
        }
        private void OnDestroy()
        {
            Managers.Token.Cancel(this);
            disposable?.Dispose();
        }
    }
}