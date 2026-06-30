using Denba.Common;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
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

        private Dictionary<GameObject, BGMExecutor> bgmExecutorDict = new Dictionary<GameObject, BGMExecutor>();
        [SerializeField] private LayerMask bgmZoneLayerMask;

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

        public void ClearZones()
        {
            zoneDict.Clear();
            missionPoint.Clear();
        }

        public void RegistAllBgmExecutors(BGMExecutor[] executors)
        {
            if (executors == null || executors.Length == 0) return;
            foreach (var executor in executors)
                RegistBgmExecutor(executor);
        }

        public void RegistBgmExecutor(BGMExecutor executor)
        {
            bgmExecutorDict[executor.gameObject] = executor;
        }

        /// <summary>
        /// 등록된 BGM 영역 정보를 모두 비운다. 씬을 나갈 때(Exit) 호출.
        /// </summary>
        public void ClearBgmExecutors()
        {
            bgmExecutorDict.Clear();
        }

        /// <summary>
        /// Zone과 BGM 영역을 한 번에 모두 비운다. 보통 SceneController.Exit()에서 이거 하나만 호출하면 된다.
        /// </summary>
        public void ClearAll()
        {
            ClearZones();
            ClearBgmExecutors();
        }

        public BGMExecutor GetBgmExecutorByPosition(Vector3 position)
        {
            Collider2D[] points = new Collider2D[2];
            Physics2D.OverlapPointNonAlloc(new Vector2(position.x, position.y), points, bgmZoneLayerMask);
            if (points == null || points.Length <= 0) return null;

            foreach (var point in points)
            {
                if (point == null) continue;
                if (!bgmExecutorDict.TryGetValue(point.gameObject, out var executor)) continue;
                return executor;
            }

            return null;
        }

        public void RegistMissionZone(JobMission mission)
        {
            if (mission == null || mission.Condition.MissionFilterType != MissionFilterType.MissionZone) return;
            missionPoint?.Clear();
            missionPoint = new();
            missionPoint[mission] = GetRandomZones(3);
        }

        public bool CheckMissionPoint(JobMission mission, Vector3 position)
        {
            if (!missionPoint.TryGetValue(mission, out var list)) return false;
            var zone = GetZoneByPosition(position);
            bool isFind = false;
            foreach (var li in list)
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

            foreach (var point in points)
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

        public List<Zone> GetRandomZones(int count)
        {
            var list = zoneDict.Values.ToList();
            list.Shuffle();

            List<Zone> returnZone = new List<Zone>();
            for (int i = 0; i < count; i++)
            {
                returnZone?.Add(list[i]);
            }

            return returnZone;
        }

        public List<Zone> GetRandomZones(int count, List<ZoneType> excludedZoneTypes)
        {
            var excludedSet = excludedZoneTypes != null && excludedZoneTypes.Count > 0
                ? new HashSet<ZoneType>(excludedZoneTypes)
                : null;

            var result = zoneDict.Values
                .Where(x => excludedSet == null || !excludedSet.Contains(x.ZoneType))
                .ToList();

            result.Shuffle();
            return result.Take(count).ToList();
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
            foreach (var pointKvp in missionPoint)
            {
                var list = pointKvp.Value;
                foreach (var point in list)
                {
                    text += point.GetZoneName() + ",";
                }
            }
            return text.TrimEnd(',');
        }

        [Rpc(SendTo.Server)]
        public void ActiveBarricadeRpc()
        {
            var zones = GetRandomZones(3, new List<ZoneType>() { ZoneType.Square, ZoneType.Port });
            foreach (var zone in zones)
            {
                zone.GetBarricade.SetActiveGlobalRpc(true);
            }
        }
    }
}