using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public sealed class ToolDrivenMissionUI : MissionUIBase
    {
        [SerializeField] private List<ToolDrivenGoalSlot> slots = new();
        [SerializeField] private List<UIDrivenItem> drivenItem = new();
        [SerializeField] private List<RectTransform> randomPositions = new();

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            if (slots == null || slots.Count == 0) return;
            randomPositions.Shuffle();

            for (int i = 0; i < drivenItem.Count; i++)
            {
                drivenItem[i].Rect.anchoredPosition = randomPositions[i].anchoredPosition;
            }

            foreach (var slot in slots)
            {
                slot?.OnCompleteDriven.Subscribe(NotifySlotFilled).AddTo(disposables);
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