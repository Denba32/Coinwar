using System.Collections.Generic;
using UniRx;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.UI.Missions
{
    public class GlobalMissionUI : MonoBehaviour
    {
        private CompositeDisposable _disposables = new();
        [SerializeField] private MissionItemUI missionItemPrefab;
        [SerializeField] private RectTransform content;

        private List<MissionItemUI> globalMissionUIList = new();

        public void UpdateMission(List<Mission> missions)
        {
            if (missions == null || missions.Count == 0) return;
            Clear();

            _disposables = new();
            int index = 1;
            foreach (Mission mission in missions)
            {
                var globalMissionUI = Instantiate<MissionItemUI>(missionItemPrefab, content);
                globalMissionUI?.SetMission(mission, index);
                globalMissionUIList?.Add(globalMissionUI);
                mission.OnCompleted.Subscribe(globalMissionUI.OnCompleteMission).AddTo(_disposables);
                index++;
            }
        }

        private void Clear()
        {
            _disposables?.Dispose();
            _disposables.Clear();

            if (globalMissionUIList == null) return;
            foreach(var globalMission in globalMissionUIList)
            {
                Destroy(globalMission.gameObject);
            }
            globalMissionUIList?.Clear();
        }
    }
}