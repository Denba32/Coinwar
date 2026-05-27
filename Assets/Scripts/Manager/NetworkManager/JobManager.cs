using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Manager;
using StockGame.Utility;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;
using static StockGame.Scripts.Define.GameDefine.ProbabilityDefine;

namespace StockGame.Scripts.Manager
{
    public class JobManager : NetworkSingleton<JobManager>
    {
        private List<JobInfo> jobList = new();
        private Queue<JobInfo> jobAllocateQueue;
        private Dictionary<int, JobSkillBase> skillDict = new();
        private Dictionary<int, List<JobProbability>> jobProbabiliyDict = new();

        public override UniTask Initialize()
        {
            jobList?.Clear();
            jobAllocateQueue = null; // 명시적으로 null 처리
            skillDict?.Clear();

            var jobTypeMap = new Dictionary<int, JobType>();
            var jobTable = Managers.Master.GetTable("JobTable").GetAllData();
            foreach (var row in jobTable)
            {
                var job = new JobInfo(row);
                jobList.Add(job);
                jobTypeMap[job.JobId] = job.JobType;
            }

            var paramGroups = new Dictionary<int, List<SkillParameter>>();
            foreach (var row in Managers.Master.GetTable("JobSkillTable").GetAllData())
            {
                var param = new SkillParameter(row);
                if (!paramGroups.ContainsKey(param.JobId))
                    paramGroups[param.JobId] = new();
                paramGroups[param.JobId].Add(param);
            }

            Dictionary<int, SkillParameterSet> skillParameterDict = new();

            foreach (var (jobId, paramList) in paramGroups)
            {
                var paramSet = new SkillParameterSet(paramList);
                skillParameterDict[jobId] = paramSet;

                if (!jobTypeMap.TryGetValue(paramSet.JobId, out var jobType)) continue;

                JobSkillBase skill = jobType switch
                {
                    JobType.FryingPanKiller => new FryingPanKillSkill(paramSet),
                    JobType.InsanePharmacist => new InsanePharmacistSkill(paramSet),
                    JobType.Gambler => new GamblerSkill(paramSet),
                    JobType.Police => new PoliceSkill(paramSet),
                    JobType.Hacker => new HackerSkill(paramSet),
                    JobType.Recluse => new RecluseSkill(paramSet),
                    JobType.Thief => new ThiefSkill(paramSet),
                    JobType.Gangster => new GangsterSkill(paramSet),
                    _ => null
                };

                if (skill == null) continue;
                skillDict[jobId] = skill;

                var jobInfo = jobList.FirstOrDefault(j => j.JobId == paramSet.JobId);
                jobInfo?.SetSkill(skill);
            }

            var probabilityTable = Managers.Master.GetTable("ProbabilityTable").GetAllData();
            foreach (var row in probabilityTable)
            {
                var probability = new JobProbability(row);
                if (!jobProbabiliyDict.ContainsKey(probability.JobId))
                    jobProbabiliyDict[probability.JobId] = new List<JobProbability>();
                jobProbabiliyDict[probability.JobId].Add(probability);
            }

            return base.Initialize();
        }

        public void ResetAllocateQueue()
        {
            jobAllocateQueue = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestJobServerRpc(ulong ownerId)
        {
            RefreshQueueIfNeeded();
            var playerInfo = GameManager.Instance.GetLocalPlayerInfo();
            if (playerInfo == null) return;
            var job = jobAllocateQueue?.Dequeue();
            GameManager.Instance.UpdateJobInfoServerRpc(new NetworkJobInfo(job), ownerId);
        }

        public NetworkJobInfo GetJobByType(JobType type)
        {
            var job = jobList.FirstOrDefault(x => x.JobType == type);
            return new NetworkJobInfo(job);
        }

        public NetworkJobInfo GetJobByRandom()
        {
            Debug.Log("GetJobByRandom");
            RefreshQueueIfNeeded();
            var job = jobAllocateQueue.Dequeue();
            Debug.Log(job.JobName);
            return new NetworkJobInfo(job);
        }

        private void RefreshQueueIfNeeded()
        {
            if (jobAllocateQueue == null || jobAllocateQueue.Count <= 0)
            {
                jobList.Shuffle();
                jobAllocateQueue = new(jobList);
            }
        }

        public JobInfo GetJobInfoByIndex(int index)
        {
            return jobList.FirstOrDefault(job => job.JobId == index);
        }

        public JobSkillBase GetJobSkillByJobId(int jobId)
        {
            skillDict.TryGetValue(jobId, out var skill);
            return skill;
        }

        public float GetRandomRewardByJobId(int jobId)
        {
            if (!jobProbabiliyDict.TryGetValue(jobId, out var value)) return 0;
            return value.GetRandomByJobProbability();
        }
    }
}