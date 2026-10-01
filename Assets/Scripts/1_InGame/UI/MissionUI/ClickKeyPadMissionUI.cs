using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;

namespace StockGame.Scripts.UI.Missions
{
    public class ClickKeyPadMissionUI : MissionUIBase
    {
        [SerializeField] private KeypadUI keypad;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            keypad.OnSuccess.Subscribe(_ => ShowSuccess().Forget()).AddTo(disposables);
            keypad.OnFailed.Subscribe(_ => ShowFailed().Forget()).AddTo(disposables);
            InitAsync().Forget();
        }

        private async UniTask InitAsync()
        {
            await UniTask.WaitForSeconds(1f, cancellationToken:destroyCancellationToken);
            if (destroyCancellationToken.IsCancellationRequested) return;
            keypad.Initialize();
        }
    }
}