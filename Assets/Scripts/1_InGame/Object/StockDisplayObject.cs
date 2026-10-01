using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.UI;
using System;
using System.Threading;
using static StockGame.Scripts.Define.GameDefine;

namespace StockGame.Scripts.Objects
{
    public class StockDisplayObject : ObjectInteractor
    {
        private Action onClose = null;
        private void Start()
        {
            onClose = OnClosePopup;
        }
        public override void OnInteract(InteractorContext ctx)
        {
            base.OnInteract(ctx);
            Managers.UI.Open<UI_StockDisplay>(uiLayer: UIDefine.UILayer.Popup, param: onClose).Forget();
        }

        private void OnClosePopup()
        {
            var token = Managers.Token.GetToken(this);
            OnCloseActionAsync(token).Forget();
        }

        private async UniTask OnCloseActionAsync(CancellationToken token)
        {
            if (context.IsValid) context.PlayerObject.IsInteracted.Value = false;
            await UniTask.WaitForSeconds(0.5f, cancellationToken: token);
            isInteracting = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Managers.Token.Cancel(this);
        }
    }
}