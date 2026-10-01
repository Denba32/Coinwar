using StockGame.Scripts.Maps;
using System.Collections.Generic;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.UI
{
    public class MiniMapZone : MonoBehaviour
    {
        public ZoneType zoneType;

        [SerializeField] private MiniMapMarker normalMissionMarker;
        [SerializeField] private MiniMapMarker jobMissionMarker;

        public void SetProgressNormalMission(List<Mission> missions)
        {
            int normalMissionCount = 0;

            foreach(var mission in missions)
            {
                if(mission.MissionType != Define.MissionType.Normal) continue;
                normalMissionCount++;
            }

            if (normalMissionCount <= 0) return;
            normalMissionMarker.gameObject.SetActive(true);
            normalMissionMarker?.SetMarker(MiniMapMarker.MiniMapMarkerType.NormalMission, normalMissionCount);
        }

        public void SetProgressJobMission(List<Mission> missions)
        {
            foreach (var mission in missions)
            {
                if (mission.MissionType != Define.MissionType.Job) continue;
                jobMissionMarker.gameObject.SetActive(true);
                jobMissionMarker.SetMarker(MiniMapMarker.MiniMapMarkerType.JobMission, 1);
            }
        }
    }
}