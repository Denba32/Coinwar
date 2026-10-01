using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Objects
{
    public class NetworkObjectInteractor : NetworkBehaviour, IInteractable, IHighlightable
    {
        protected NetworkVariable<bool> isInteracting = new NetworkVariable<bool>(default, writePerm:NetworkVariableWritePermission.Owner);
        protected NetworkVariable<bool> isLocked = new NetworkVariable<bool>(default, writePerm: NetworkVariableWritePermission.Owner);
        private bool _isProcessingInteract = false; // 로컬 즉시 가드 (소유자 응답 대기 중 재입력 방지)

        [SerializeField] private Sprite enterSprite = null;
        [SerializeField] private Sprite exitSprite = null;
        [SerializeField] private SpriteRenderer spriteRenderer = null;

        protected InteractorContext context;

        public bool CanInteract(InteractorContext ctx) => !isLocked.Value && !isInteracting.Value && !_isProcessingInteract;
        public string GetPrompt() => isLocked.Value ? "Lock" : "Unlock";

        public virtual void Interact(InteractorContext ctx)
        {
            if (!CanInteract(ctx)) return;
            if (ctx.IsValid) ctx.PlayerObject.IsInteracted.Value = false;

            _isProcessingInteract = true; // 동기적으로 즉시 잠금 — 소유자 응답을 기다림
            context = ctx;

            // 상호작용 권한을 소유자(호스트)에게 요청한다. 실제 점유 여부는 소유자만 판정한다.
            RequestInteractClaimRpc();
        }

        public void SetHighlight(bool value)
        {
            if (isLocked == null || isLocked.Value || isInteracting == null || isInteracting.Value) return;
            if (spriteRenderer == null) return;
            spriteRenderer.sprite = value ? enterSprite : exitSprite;
        }

        [Rpc(SendTo.Owner, RequireOwnership = false)]
        private void RequestInteractClaimRpc(RpcParams rpcParams = default)
        {
            if (!IsOwner) return;

            bool granted = !isLocked.Value && !isInteracting.Value;
            if (granted)
            {
                isInteracting.Value = true;
                isLocked.Value = true;
            }

            RespondInteractClaimRpc(granted, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void RespondInteractClaimRpc(bool granted, RpcParams rpcParams)
        {
            _isProcessingInteract = false;

            // granted == false 이면 다른 플레이어가 이미 점유한 것이므로 아무것도 하지 않는다.
            if (granted) OnInteractGranted(context);
        }

        // 점유에 성공한(= 단 한 명의) 클라이언트에서만 호출된다.
        protected virtual void OnInteractGranted(InteractorContext ctx) { }

        [Rpc(SendTo.Owner)]
        public void ActiveInteractRpc(bool isActive)
        {
            if (!IsOwner) return;
            isInteracting.Value = isActive;
        }

        [Rpc(SendTo.Owner)]
        public void ActiveLockRpc(bool isActive)
        {
            if (!IsOwner) return;
            isLocked.Value = isActive;
        }

        [Rpc(SendTo.Owner)]
        public void RequestDespawnRpc()
        {
            if (!IsOwner) return;
            NetworkObject.Despawn();
        }
    }
}
