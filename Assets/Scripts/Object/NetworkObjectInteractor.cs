using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Objects
{
    public class NetworkObjectInteractor : NetworkBehaviour, IInteractable, IHighlightable
    {
        protected NetworkVariable<bool> isInteracting = new NetworkVariable<bool>(default, writePerm:NetworkVariableWritePermission.Owner);
        protected NetworkVariable<bool> isLocked = new NetworkVariable<bool>(default, writePerm: NetworkVariableWritePermission.Owner);
        private bool _isProcessingInteract = false; // 로컬 즉시 가드

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
            _isProcessingInteract = true; // 동기적으로 즉시 잠금 — RPC 왕복 기다리지 않음
            context = ctx;
            ActiveInteractRpc(true);
        }

        public void SetHighlight(bool value)
        {
            if (isLocked == null || isLocked.Value || isInteracting == null || isInteracting.Value) return;
            if (spriteRenderer == null) return;
            spriteRenderer.sprite = value ? enterSprite : exitSprite;
        }

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