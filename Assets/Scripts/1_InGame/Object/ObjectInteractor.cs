using UnityEngine;

namespace StockGame.Scripts.Objects
{
    public class ObjectInteractor : MonoBehaviour, IInteractable, IHighlightable
    {
        [SerializeField] protected bool isInteracting = false;
        [SerializeField] protected bool locked = false;

        [SerializeField] private Sprite enterSprite = null;
        [SerializeField] private Sprite exitSprite = null;
        [SerializeField] private SpriteRenderer spriteRenderer = null;
        protected InteractorContext context;
        public bool CanInteract(InteractorContext ctx) => !locked && !isInteracting;
        public string GetPrompt() => locked ? "Lock" : "Unlock";

        public void Interact(InteractorContext ctx)
        {
            if (isInteracting) return;
            if(ctx.IsValid) ctx.PlayerObject.IsInteracted.Value = false;
            context = ctx;
            isInteracting = true;
            OnInteract(ctx);
        }

        public virtual void OnInteract(InteractorContext ctx) { }

        public virtual void SetHighlight(bool isActive)
        {
            if (locked) return;
            if (spriteRenderer == null) return;
            spriteRenderer.sprite = isActive ? enterSprite : exitSprite;
        }

        protected virtual void OnDestroy() { }
    }
}