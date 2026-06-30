using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;using StockGame.Scripts.UI;

using System;
using System.Threading;
using Unity.Netcode;

namespace StockGame.Scripts.Objects
{
    public class LobbyMicObject : ObjectInteractor
    {
        private Action onClose = null;

        private void Start()
        {
            onClose = OnClosePopup;
        }

        private void OnClosePopup()
        {
            var token = Managers.Token.GetToken(this, nameof(OnClosePopup));
            OnCloseActionAsync(token).Forget();
        }
        private async UniTask OnCloseActionAsync(CancellationToken token)
        {
            if(context.IsValid) context.PlayerObject.IsInteracted.Value = false;
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
            isInteracting = false;
        }

        public override void Interact(InteractorContext ctx)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            base.Interact(ctx);
            Managers.UI.Open<UI_RoundSetting>(Define.GameDefine.UIDefine.UILayer.Popup, param: onClose).Forget();
        }

        public override void SetHighlight(bool isActive)
        {
            if (!NetworkManager.Singleton.IsHost) return;
            base.SetHighlight(isActive);
        }
    }
}