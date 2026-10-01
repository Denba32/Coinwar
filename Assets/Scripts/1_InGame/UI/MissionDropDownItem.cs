using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.UI
{
    public sealed class MissionDropDownItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text missionText;
        [SerializeField] private LocalizeStringEvent stringEvent;

        private enum MissionKind { Normal, Job }

        private int index;
        private string description;
        private MissionKind kind;
        private bool isCompleted;

        // Job 미션 진행 상태 캐싱 (로케일 변경 시 재조합용)
        private JobMission currentJobMission;
        private int currentCount;
        private int requireCount;
        private string zoneText; // 로컬라이즈된 존 이름 캐싱

        public void Initialize(Mission mission, int index)
        {
            this.index = index;
            mission.OnCompleted.Subscribe(OnComplete).AddTo(this);

            if (mission.MissionType == MissionType.Normal)
                SetNormalMissionData(mission);
            else if (mission.MissionType == MissionType.Job)
                SetJobMissionData(mission as JobMission);
        }

        private void SetNormalMissionData(Mission mission)
        {
            kind = MissionKind.Normal;

            stringEvent.OnUpdateString.AddListener(OnDescriptionUpdated);
            stringEvent.StringReference = new LocalizedString("Mission_String", mission.MissionId.ToString());
            stringEvent.RefreshString();
        }

        private void SetJobMissionData(JobMission mission)
        {
            kind = MissionKind.Job;
            currentJobMission = mission;
            currentCount = 0;
            requireCount = mission.RequireCount;

            mission.OnProgressChanged.Subscribe(Progress).AddTo(this);

            stringEvent.OnUpdateString.AddListener(OnDescriptionUpdated);
            stringEvent.StringReference = new LocalizedString("Mission_String", mission.MissionId.ToString());
            stringEvent.RefreshString();

            RefreshZoneSuffix();
        }

        private void Progress(JobMission jobMission)
        {
            currentJobMission = jobMission;
            currentCount = jobMission.CurrentCount;
            requireCount = jobMission.RequireCount;
            RefreshDisplay();
        }

        private void OnDescriptionUpdated(string localizedValue)
        {
            description = localizedValue;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            switch (kind)
            {
                case MissionKind.Normal:
                    missionText.text = $"{index}.{description}";
                    break;

                case MissionKind.Job:
                    missionText.text = string.IsNullOrEmpty(zoneText)
                        ? $"{description}({currentCount}/{requireCount})"
                        : $"{description}({currentCount}/{requireCount})\n({zoneText})";
                    break;
            }

            if (isCompleted)
                missionText.fontStyle |= FontStyles.Strikethrough;
        }

        private async void RefreshZoneSuffix()
        {
            zoneText = string.Empty;

            if (currentJobMission == null) return;
            if (currentJobMission.Condition.MissionFilterType != MissionFilterType.MissionZone) return;

            var zoneKeys = ZoneManager.Instance.GetMissionZoneKeys();
            if (zoneKeys.Count <= 0) return;

            var tasks = zoneKeys.Select(async key =>
            {
                var localizedString = new LocalizedString("Map_String", key);
                var handle = localizedString.GetLocalizedStringAsync();
                await handle.Task;
                return handle.Result;
            });

            var localizedNames = await UniTask.WhenAll(tasks);
            zoneText = string.Join(",", localizedNames);

            RefreshDisplay();
        }

        public void OnComplete(Mission mission)
        {
            isCompleted = true;
            missionText.fontStyle |= FontStyles.Strikethrough;
        }

        public void Clear()
        {
            if (stringEvent != null)
                stringEvent.OnUpdateString.RemoveListener(OnDescriptionUpdated);

            missionText.text = string.Empty;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}