using StockGame.Common.CustomCollections;
using UnityEngine;

namespace StockGame.Scripts.Datas
{
    [CreateAssetMenu(fileName = "HackerMissionDataSO", menuName = "ScriptableObject/HackerMissionDataSO")]
    public class HackerMissionDataSO : ScriptableObject
    {
        public CustomDictionary<Sprite, string> HackerQuizDict;
    }
}