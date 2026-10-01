using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UnityEngine;

namespace StockGame.Scripts.Players
{
    public class PlayerBgmListener : MonoBehaviour
    {
        [SerializeField] private PlayerPositionTracker tracker;

        private void Start()
        {
            tracker.OnBgmZoneChanged += OnBgmZoneChanged;
        }

        private void OnDestroy()
        {
            if (tracker != null)
                tracker.OnBgmZoneChanged -= OnBgmZoneChanged;
        }

        private void OnBgmZoneChanged(BgmZoneState state)
        {
            if (!state.IsExplore)
            {
                Managers.Sound?.ClearStack();
                return; // Explore가 아님 → 무음으로 끝
            }

            Managers.Sound.PushBGM(GameDefine.ResourceDefine.FMODEvent.BGM101);

            if (state.Zone != null)
            {
                Managers.Sound.PushBGM(state.Zone.Value);
            }
        }
    }
}