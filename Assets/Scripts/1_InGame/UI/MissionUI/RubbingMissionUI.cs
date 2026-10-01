using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public sealed class RubbingMissionUI : MissionUIBase
    {
        [SerializeField] private List<UIRubbingSlot> slots = new();

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            if (slots == null || slots.Count == 0) return;
            foreach (var slot in slots)
            {
                slot?.OnRubbed.Subscribe(NotifySlotFilled).AddTo(disposables);
            }
        }

        public override bool CheckMission()
        {
            return slots.Count(x => x.IsComplete()) >= slots.Count;
        }

        private void NotifySlotFilled(Unit _)
        {
            if (CheckMission())
            {
                MissionClear().Forget();
            }
        }

        private async UniTask MissionClear()
        {
            Debug.Log("Mission Clear!");
            UIManager.Instance.CurrentDragItem?.ForceDragEnd();
            await ShowSuccess();
        }
    }
}