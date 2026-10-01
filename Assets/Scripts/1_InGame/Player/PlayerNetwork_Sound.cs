using StockGame.Scripts.Manager;
using Unity.Netcode;

namespace StockGame.Scripts.Players
{
    public partial class PlayerNetwork : NetworkBehaviour
    {
        public void StopFootStepSound()
        {
            triggerEvent.StopFootStep();
        }
    }
}