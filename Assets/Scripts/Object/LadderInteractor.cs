using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
namespace StockGame.Scripts.Objects
{
    public sealed class LadderInteractor : TeleportObject
    {
        public override void Interact(InteractorContext ctx)
        {
            base.Interact(ctx);
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX260);
        }
    }
}