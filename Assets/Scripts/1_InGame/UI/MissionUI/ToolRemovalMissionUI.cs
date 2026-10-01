using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.UI.Missions
{
    public class ToolRemovalMissionUI : MissionUIBase
    {
        [SerializeField] private List<UIRemovalToolItem> removalToolItems = new();
        [SerializeField] private List<UIPickedItem> pickedItems = new();

        [SerializeField] private bool isRemovalToolMission;
        [SerializeField] private bool isPickItemMission;
        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);

            if(isRemovalToolMission)
            {
                if (removalToolItems == null || removalToolItems.Count == 0) return;
                foreach (var toolItem in removalToolItems)
                {
                    toolItem.OnCompleted.Subscribe(NotifySlotFilled).AddTo(disposables);
                }
            }

            if (isPickItemMission)
            {
                if (pickedItems == null || pickedItems.Count == 0) return;
                foreach (var item in pickedItems)
                {
                    item.OnCompleted.Subscribe(NotifySlotFilled).AddTo(disposables);
                }
            }
        }

        private void NotifySlotFilled(Unit _)
        {
            if (CheckMission()) MissionClear().Forget();
        }

        public override bool CheckMission()
        {
            if (isRemovalToolMission && (removalToolItems == null || removalToolItems.Count == 0 || removalToolItems.Count(x => x.IsComplete()) < removalToolItems.Count))
                return false;

            if (isPickItemMission && (pickedItems == null || pickedItems.Count == 0 || pickedItems.Count(x => x.IsComplete()) < pickedItems.Count))
                return false;

            return isRemovalToolMission || isPickItemMission;
        }

        private async UniTask MissionClear()
        {
            Debug.Log("Mission Clear!");
            UIManager.Instance.CurrentDragItem?.ForceDragEnd();
            await ShowSuccess();
        }
    }
}