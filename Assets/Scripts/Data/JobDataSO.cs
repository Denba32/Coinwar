
using StockGame.Scripts.Define;
using UnityEngine;

namespace StockGame.Scripts.Datas
{
    [CreateAssetMenu(fileName = "JobDataSO", menuName = "ScriptableObject/JobData")]
    public class JobDataSO : ScriptableObject
    {
        public int JobIndex;
        public string JobName;
        public string JobDescription;
        public GameDefine.JobDefine.JobType JobType;
    }
}