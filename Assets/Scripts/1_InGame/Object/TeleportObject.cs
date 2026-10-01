using UnityEngine;
namespace StockGame.Scripts.Objects
{
    public class TeleportObject : ObjectInteractor
    {
        [SerializeField] private Transform destination;

        public override void OnInteract(InteractorContext ctx)
        {
            base.OnInteract(ctx);
            isInteracting = false;
            if (ctx.IsValid) ctx.PlayerObject.Teleport(destination);
        }
    }
}