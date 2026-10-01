using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UniRx;
using Cysharp.Threading.Tasks;

namespace StockGame.Scripts.UI.Missions
{
    public class DragAndDropMissionUI : MissionUIBase
    {
        [SerializeField] private List<UIDropSlot> slots = new();

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            if (slots == null || slots.Count == 0) return;
            foreach(var slot in slots)
            {
                slot?.OnSlotFilled.Subscribe(NotifySlotFilled).AddTo(disposables);
            }
        }

        public override bool CheckMission()
        {
            return slots.Count(x => x.IsComplete()) >= slots.Count;
        }

        private void NotifySlotFilled(Unit _)
        {
            if (CheckMission()) MissionClear().Forget();
        }

        private async UniTask MissionClear()
        {
            Debug.Log("Mission Clear!");
            await ShowSuccess();
        }
    }
}