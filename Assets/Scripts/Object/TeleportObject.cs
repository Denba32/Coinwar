using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UnityEngine; 
namespace StockGame.Scripts.Objects
{
    public class TeleportObject : ObjectInteractor
    {
        [SerializeField] private Transform destination;

        public override void Interact(InteractorContext ctx)
        {
            base.Interact(ctx);
            isInteracting = false;
            if(ctx.IsValid) ctx.PlayerObject.Teleport(destination);
        }
    }
}