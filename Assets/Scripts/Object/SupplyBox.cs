using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.UI;
using System;
using System.Threading;
using UnityEngine;

namespace StockGame.Scripts.Objects
{
    public sealed class SupplyBox : NetworkObjectInteractor
    {
        private Action onClose = null;

        private void Start()
        {
            onClose = OnClosePopup;
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner) return;

            AutoDestroyAsync().Forget();
        }

        private async UniTask AutoDestroyAsync()
        {
            var value = GameManager.Instance.CurrentTime.Value;
            var dest = Mathf.Max(value - 45f, 0);
            await UniTask.WaitUntil(() => GameManager.Instance.CurrentTime.Value == dest, cancellationToken:destroyCancellationToken);
            if (destroyCancellationToken.IsCancellationRequested) return;
            RequestDespawnRpc();
        }
        
        public override void Interact(InteractorContext ctx)
        {
            base.Interact(ctx);
            ActiveInteractRpc(true);
            ActiveLockRpc(true);
            var token = Managers.Token.GetToken(this);
            InteractAsync(ctx, token: token).Forget();
        }

        private async UniTask InteractAsync(InteractorContext ctx, CancellationToken token = default)
        {
            await Managers.UI.Open<SupplyBoxUI>(Define.GameDefine.UIDefine.UILayer.Popup, string.Empty, ctx, onClose);
            RequestDespawnRpc();
        }
        private void OnClosePopup()
        {
            if (context.IsValid) context.PlayerObject.IsInteracted.Value = false;
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            if (!IsOwner) return;
            isInteracting?.Dispose();
            isLocked?.Dispose();
            onClose -= OnClosePopup;
            isInteracting = null;
            isLocked = null;
            onClose = null;
        }
    }
}