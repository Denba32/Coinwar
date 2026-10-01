using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public sealed class MissionDropDown : MonoBehaviour
    {
        [SerializeField] private RectTransform normalMissionContent;
        [SerializeField] private RectTransform jobMissionContent;

        [SerializeField] private CanvasGroup dropDownGroup;
        [SerializeField] private Button dropDownButton;
        [SerializeField] private MissionDropDownItem itemPrefab;

        private List<MissionDropDownItem> items = new();
        private bool isOpened = false;
        public void Initialize()
        {
            OnClickDropdown(Unit.Default);
            dropDownButton.OnClickAsObservable().Subscribe(OnClickDropdown).AddTo(this);
            GameManager.Instance.CurrentRound.OnValueChanged += OnValueChanged;
            Debug.Log("DropDown Initialize");
        }

        private void OnValueChanged(GameDefine.RoundDefine.RoundInfo previousValue, GameDefine.RoundDefine.RoundInfo newValue)
        {
            if (newValue.RoundPhase != GameDefine.RoundDefine.RoundPhase.Explore) return;
            ClearUI();
            var missionManager = MissionManager.Instance;
            AddNormalMission(missionManager.GetLocalPlayerNormalMission());
            AddJobMission(missionManager.GetLocalPlayerJobMission());
        }

        public void AddNormalMission(List<GameDefine.MissionDefine.Mission> missionList)
        {
            if (missionList == null || missionList.Count <= 0) return;
            int index = 1;
            foreach(var mission in missionList)
            {
                var missionItem = Instantiate(itemPrefab, normalMissionContent);
                missionItem?.Initialize(mission, index);
                items?.Add(missionItem);
                index++;
            }
        }

        public void AddJobMission(List<GameDefine.MissionDefine.Mission> missionList)
        {
            if (missionList == null || missionList.Count <= 0) return;
            int index = 1;
            foreach (var mission in missionList)
            {
                var missionItem = Instantiate(itemPrefab, jobMissionContent);
                missionItem?.Initialize(mission, index);
                items?.Add(missionItem);
                index++;
            }
        }

        private void OnClickDropdown(Unit _)
        {
            isOpened = !isOpened;
            dropDownButton.transform.localEulerAngles = isOpened ? Vector3.zero : new Vector3(0, 0, 180);
            dropDownGroup.interactable = isOpened;
            dropDownGroup.blocksRaycasts = isOpened;
            dropDownGroup.alpha = isOpened ? 1 : 0;
        }

        private void ClearUI()
        {
            if (gameObject == null) return;
            foreach(var item in items)
            {
                item.Clear();
                Destroy(item.gameObject);
            }
            items?.Clear();
        }

        private void OnDestroy()
        {
            var gameManager = GameManager.Instance;
            if(gameManager != null)
                gameManager.CurrentRound.OnValueChanged -= OnValueChanged;
        }
    }
}
