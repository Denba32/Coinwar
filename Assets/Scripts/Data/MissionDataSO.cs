using StockGame.Scripts.Define;
using static StockGame.Scripts.Define.GameDefine;
using UnityEngine;

namespace StockGame.Scripts.Datas
{
    [CreateAssetMenu(fileName = "MissionData", menuName = "ScriptableObject/MissionData")]
    public class MissionDataSO : ScriptableObject
    {
        public int MissionId;
        public string MissionName;
        public string MissionDescription;

        public MissionType MissionType;
        public JobDefine.JobType JobType;
        public int ReceiveCoin;
    }
}