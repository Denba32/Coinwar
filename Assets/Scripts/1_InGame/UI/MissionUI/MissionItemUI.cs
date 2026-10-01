using TMPro;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.UI.Missions
{
    public class MissionItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text missionDescriptionText;
        [SerializeField] private Color completedMissionColor;
        [SerializeField] private Color inProgressMissionColor;

        private int index;

        public void SetMission(Mission mission, int index)
        {
            missionDescriptionText.text = $"{index}. {mission.MissionDescription}";
            missionDescriptionText.fontStyle = FontStyles.Normal;
            missionDescriptionText.color = inProgressMissionColor;
        }
            
        public void OnCompleteMission(Mission mission)
        {
            Debug.Log($"{mission.MissionName} 미션 클리어"); 
            missionDescriptionText.fontStyle = FontStyles.Strikethrough;
            missionDescriptionText.color = completedMissionColor;
        }
    }
}