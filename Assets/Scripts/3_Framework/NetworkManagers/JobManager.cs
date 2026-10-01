using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Linq;
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
            jobAllocateQueue = null;
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            ResetAllocateQueue();
        }

        /// <summary>
        /// 게임 종료 시 큐 초기화 — 다음 게임에서 직업 배정이 편향되지 않도록
        /// </summary>
        public void ResetAllocateQueue()
        {
            jobAllocateQueue = null;
        }

        public NetworkJobInfo GetJobByType(JobType type)
        {
            var job = jobList.FirstOrDefault(x => x.JobType == type);
            return new NetworkJobInfo(job);
        }

        public NetworkJobInfo GetJobByRandom()
        {
            var job = jobAllocateQueue?.Dequeue();
            if (job == null)
            {
                Debug.LogError("Job 정보 없음");
                return default;
            }
            return new NetworkJobInfo(job);
        }

        public void PrepareQueueForRound(int playerCount)
        {
            if (playerCount > jobList.Count)
                Debug.LogWarning($"[JobManager] 인원({playerCount}) > 직업 종류({jobList.Count}), 중복 불가피");

            if (jobAllocateQueue == null || jobAllocateQueue.Count < playerCount)
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