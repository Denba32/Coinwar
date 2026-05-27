using Denba.Common;
using StockGame.Scripts.Maps;
using StockGame.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.Manager
{
    public class ZoneManager : NonPersistantMonoSingleton<ZoneManager>
    {
        private Dictionary<GameObject, Zone> zoneDict = new Dictionary<GameObject, Zone>();
        private Dictionary<JobMission, List<Zone>> missionPoint = new Dictionary<JobMission, List<Zone>>();
        public Dictionary<GameObject, Zone> ZoneDict => zoneDict;
        [SerializeField] private LayerMask layerMask; 

        public void RegistAll(Zone[] zones)
        {
            if (zones == null || zones.Length == 0) return;
            foreach (Zone zone in zones)
                Regist(zone);
        }

        public void Regist(Zone zone)
        {
            zoneDict[zone.gameObject] = zone;
        }

        public void RegistMissionZone(JobMission mission)
        {
            if (mission == null || mission.Condition.MissionFilterType != MissionFilterType.MissionZone) return;
            missionPoint?.Clear();
            missionPoint = new();
            missionPoint[mission] = GetRandomZones();
        }

        public bool CheckMissionPoint(JobMission mission, Vector3 position)
        {
            if (!missionPoint.TryGetValue(mission, out var list)) return false;
            var zone = GetZoneByPosition(position);
            bool isFind = false;
            foreach(var li in list)
            {
                if (zone == li) isFind = true;
            }
            if (isFind) list.Remove(zone);

            return isFind;
        }

        public Zone GetZoneByPosition(Vector3 position)
        {
            Collider2D[] points = new Collider2D[2];
            Physics2D.OverlapPointNonAlloc(new Vector2(position.x, position.y), points, layerMask);
            if (points == null || points.Length <= 0) return null;

            foreach(var point in points)
            {
                if (point == null) continue;
                if (!zoneDict.TryGetValue(point.gameObject, out var zone)) continue;
                return zone;
            }

            return null;
        }

        public List<Zone> GetZoneByMissionObjects(Mission mission)
        {
            var missionObjects = MissionManager.Instance.GetMissionObjectByMission(mission);
            if (missionObjects == null || missionObjects.Count <= 0) return null;
            List<Zone> zones = new List<Zone>();
            foreach (var obj in missionObjects)
            {
                zones.Add(GetZoneByPosition(obj.transform.position));
            }
            return zones;
        }

        public List<Zone> GetRandomZones(int count = 3)
        {
            var list = zoneDict.Values.ToList();
            list.Shuffle();

            List<Zone> returnZone = new List<Zone>();
            for(int i = 0; i < count; i++)
            {
                returnZone?.Add(list[i]);
            }

            return returnZone;
        }

        public Transform GetTeleportPositionByRandom()
        {
            if (zoneDict == null || zoneDict.Count <= 0) return null;
            var zoneList = zoneDict.Values.ToList();
            var random = Random.Range(0, zoneList.Count);
            var zone = zoneList[random];
            return zone.GetTeleportPoint();
        }

        public string GetMissionZoneToText()
        {
            if (missionPoint.Count <= 0) return string.Empty;
            string text = string.Empty;
            foreach(var pointKvp in missionPoint)
            {
                var list = pointKvp.Value;
                foreach(var point in list)
                {
                    text += point.GetZoneName() + ",";
                }
            }
            return text.TrimEnd(',');
        }
    }
}