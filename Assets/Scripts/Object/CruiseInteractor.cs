using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
namespace StockGame.Scripts.Objects
{
    public sealed class CruiseInteractor : TeleportObject
    {
        public override void Interact(InteractorContext ctx)
        {
            base.Interact(ctx);
            Managers.Sound.PlaySfxIfNotPlaying(GameDefine.ResourceDefine.FMODEvent.SFX261);
        }
    }
}