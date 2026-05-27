using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Define;
using StockGame.Scripts.Missions;
using StockGame.Utility;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.Manager
{
    public class MissionManager : NetworkSingleton<MissionManager>
    {
        private CompositeDisposable _disposables = new();

        #region MISSION_DATA
        Dictionary<int, Mission> missions;
        Dictionary<JobDefine.JobType, List<Mission>> jobMissions;
        Dictionary<int, List<MissionObject>> objects = new();
        #endregion MISSION_DATA

        private List<Mission> currentLocalNormalMissions;
        private List<Mission> currentLocalJobMissions;

        private MissionResolverGroup _resolverGroup;

        public override UniTask Initialize()
        {
            missions?.Clear();
            jobMissions?.Clear();
            currentLocalNormalMissions?.Clear();
            currentLocalJobMissions?.Clear();

            missions = new();
            jobMissions = new();
            currentLocalNormalMissions = new();
            currentLocalJobMissions = new();

            var missionTable = Managers.Master.GetAllData("MissionTable");

            foreach (var row in missionTable)
            {
                Mission missionData = row.Get<MissionType>("MissionType") == MissionType.Job
                    ? new JobMission(row)
                    : new NormalMission(row);

                if (missionData.MissionType == MissionType.Job)
                {
                    if (!jobMissions.TryGetValue(missionData.JobType, out var list))
                    {
                        list = new List<Mission>();
                        jobMissions.Add(missionData.JobType, list);
                    }
                    list.Add(missionData);
                    continue;
                }

                if (this.missions.ContainsKey(missionData.MissionId)) continue;
                this.missions.Add(missionData.MissionId, missionData);
            }

            return base.Initialize();
        }

        public void Initialize(List<MissionObject> missionObjects)
        {
            if (missionObjects == null || missionObjects.Count <= 0)
            {
                Debug.LogError("Map Data Is Null");
                return;
            }
            RegisterAll(missionObjects);
        }


        [Rpc(SendTo.SpecifiedInParams)]
        public void RequestMissionRpc(JobDefine.JobType jobType, RpcParams rpcParams = default)
        {
            RequestMission(jobType);
            LockAndUnLock();
        }

        public void RequestMission(JobDefine.JobType jobType)
        {
            _disposables?.Dispose();
            _disposables = new();

            _resolverGroup?.Dispose();
            _resolverGroup = null;

            currentLocalNormalMissions?.Clear();
            currentLocalJobMissions?.Clear();

            var missionList = missions.Values.ToList();
            missionList.Shuffle();
            Queue<Mission> missionQueue = new(missionList);

            for (int i = 0; i < 5; i++)
            {
                var mission = missionQueue.Dequeue();
                mission.ResetMission();
                mission.OnCompleted.Subscribe(completedMission =>
                {
                    var coin = Managers.Game.GetLocalPlayerInfo().Coin;
                    GameManager.Instance.UpdateCoinServerRpc(coin + completedMission.Reward);
                }).AddTo(_disposables);
                currentLocalNormalMissions?.Add(mission);
            }

            if (!jobMissions.TryGetValue(jobType, out var jobMissionPool))
            {
                Debug.LogError($"[MissionManager] jobType '{jobType}'에 해당하는 미션 풀이 없습니다.");
                return;
            }

            var sendJobMissions = jobMissionPool.ToList();
            sendJobMissions.Shuffle();
            Queue<Mission> jobMissionQueue = new(sendJobMissions);

            var selectedJobMission = jobMissionQueue.Dequeue();
            selectedJobMission.ResetMission();
            selectedJobMission.OnCompleted.Subscribe(completedMission =>
            {
                var coin = Managers.Game.GetLocalPlayerInfo().Coin;
                Debug.Log($"완료 코인 수 : {coin}");
                GameManager.Instance.UpdateCoinServerRpc(coin + completedMission.Reward);
            }).AddTo(_disposables);

            if (selectedJobMission.Condition.MissionFilterType == MissionFilterType.MissionZone)
                ZoneManager.Instance.RegistMissionZone(selectedJobMission as JobMission);

            currentLocalJobMissions?.Add(selectedJobMission);
            _resolverGroup = new MissionResolverGroup(currentLocalJobMissions);
        }

        public void RegisterAll(List<MissionObject> missionObjects)
        {
            if (missionObjects == null) return;
            disposables?.Dispose();
            disposables?.Clear();
            disposables = new();

            objects?.Clear();
            objects = new();

            foreach (var missionObject in missionObjects)
                Register(missionObject);
        }

        public void Register(MissionObject missionObject)
        {
            if (!objects.TryGetValue(missionObject.MissionId, out var list))
            {
                list = new List<MissionObject>();
                objects[missionObject.MissionId] = list;
            }
            list?.Add(missionObject);

            if (!missions.TryGetValue(missionObject.MissionId, out var mission)) return;
            if (mission.OnCompleted != null)
                disposables.Add(mission.OnCompleted.Subscribe(missionObject.Complete));

            if (mission.OnReset != null)
                disposables.Add(mission.OnReset.Subscribe(missionObject.ResetMission));
        }

        public void UnRegister(MissionObject missionObject)
        {
            objects?.Remove(missionObject.MissionId);
        }

        public void UnRegisterAll()
        {
            objects?.Clear();
            disposables.Dispose();
        }

        public void ResetAllMission()
        {
            if (currentLocalNormalMissions == null || currentLocalNormalMissions.Count <= 0
                || currentLocalJobMissions == null || currentLocalJobMissions.Count <= 0) return;

            foreach (var normalMission in currentLocalNormalMissions)
                normalMission?.ResetMission();

            foreach (var jobMission in currentLocalJobMissions)
                jobMission?.ResetMission();
        }

        public void LockAndUnLock()
        {
            if (currentLocalJobMissions == null || currentLocalJobMissions.Count <= 0
                || currentLocalNormalMissions == null || currentLocalNormalMissions.Count <= 0
                || objects == null || objects.Count <= 0) return;

            foreach (var objList in objects.Values)
            {
                if (objList == null || objList.Count <= 0) break;
                foreach (var obj in objList)
                    obj.Lock(true);
            }

            foreach (var normal in currentLocalNormalMissions)
            {
                if (!objects.TryGetValue(normal.MissionId, out var missionObj)) continue;
                if (missionObj == null || missionObj.Count <= 0) break;
                foreach (var obj in missionObj)
                    obj.Lock(false);
            }

            foreach (var job in currentLocalJobMissions)
            {
                if (!objects.TryGetValue(job.MissionId, out var missionObj)) continue;
                if (missionObj == null || missionObj.Count <= 0) break;
                foreach (var obj in missionObj)
                    obj.Lock(false);
            }
        }

        public Mission GetMission(int missionId)
        {
            if (!missions.TryGetValue(missionId, out var mission))
            {
                Debug.Log($"{missionId}에 대한 미션 데이터를 찾을 수 없습니다");
                return null;
            }

            if (mission.IsCompleted) return null;
            return mission;
        }

        public Mission GetJobMission(JobType jobType, int missionId)
        {
            if(!jobMissions.TryGetValue(jobType, out var missionList))
            {
                Debug.Log($"{missionId}에 대한 미션 데이터를 찾을 수 없습니다");
                return null;
            }
            var mission = missionList.FirstOrDefault(x => x.MissionId == missionId);
            if (mission.IsCompleted) return null;
            return mission;
        }

        public List<Mission> GetAllGlobalMission()
        {
            if (missions == null) return null;

            List<Mission> missionList = new();
            foreach (var mission in missions.Values)
                missionList.Add(mission);

            return missionList;
        }

        public List<Mission> GetLocalPlayerNormalMission() => currentLocalNormalMissions;
        public List<Mission> GetLocalPlayerJobMission() => currentLocalJobMissions;

        public List<MissionObject> GetMissionObjectByMission(Mission mission)
        {
            if (!objects.TryGetValue(mission.MissionId, out var missionObj)) return null;
            return missionObj;
        }

        public override void OnDestroy()
        {
            _resolverGroup?.Dispose();
            _disposables?.Dispose();
            base.OnDestroy();
        }
    }
}