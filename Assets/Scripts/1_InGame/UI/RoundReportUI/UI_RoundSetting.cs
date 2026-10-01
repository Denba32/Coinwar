using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Utility;
using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
namespace StockGame.Scripts.UI
{
    public sealed class UI_RoundSetting : UIBase
    {
        private Action onClose;

        [SerializeField] private BaseButton closeButton;
        public BaseButton CloseButton => closeButton;

        [Header("Round 설정")]
        [SerializeField] private Button roundPrevButton;
        [SerializeField] private Button roundNextButton;
        [SerializeField] private TMP_Text roundCountText;

        [Header("미션 설정")]
        [SerializeField] private Button missionPrevButton;
        [SerializeField] private Button missionNextButton;
        [SerializeField] private TMP_Text missionCountText;

        [Header("Time 설정")]
        [SerializeField] private Slider farmingSlider;
        [SerializeField] private TMP_Text farmingSliderText;
        [SerializeField] private LocalizeStringEvent farmingSliderStringEvent;

        [SerializeField] private Slider stockCheckTimeSlider;
        [SerializeField] private TMP_Text stockCheckTimeText;
        [SerializeField] private LocalizeStringEvent stockCheckTimeStringEvent;

        [SerializeField] private Slider stockPurchaseAndSellTimeSlider;
        [SerializeField] private TMP_Text stockPurchaseAndSellTimeText;
        [SerializeField] private LocalizeStringEvent stockPurchaseAndSellTimeStringEvent;

        [SerializeField] private Slider rankingCheckTimeSlider;
        [SerializeField] private TMP_Text rankingCheckTimeText;
        [SerializeField] private LocalizeStringEvent rankingCheckTimeStringEvent;

        [Header("최대 플레이 인원 설정")]
        [SerializeField] private Button maxPlayerPrevButton;
        [SerializeField] private Button maxPlayerNextButton;
        [SerializeField] private TMP_Text maxPlayerCountText;
        public Button RoundPrevButton => roundPrevButton;
        public Button RoundNextButton => roundNextButton;
        public TMP_Text RoundCountText => roundCountText;

        public Button MissionPrevButton => missionPrevButton;
        public Button MissionNextButton => missionNextButton;
        public TMP_Text MissionCountText => missionCountText;

        public Slider FarmingSlider => farmingSlider;
        public TMP_Text FarmingSliderText => farmingSliderText;
        public LocalizeStringEvent FarmingSliderStringEvent => farmingSliderStringEvent;
        public Slider StockCheckTimeSlider => stockCheckTimeSlider;
        public TMP_Text StockCheckTimeText => stockCheckTimeText;
        public LocalizeStringEvent StockCheckTimeStringEvent => stockCheckTimeStringEvent;
        public Slider StockPurchaseAndSellTimeSlider => stockPurchaseAndSellTimeSlider;
        public TMP_Text StockPurchaseAndSellTimeText => stockPurchaseAndSellTimeText;
        public LocalizeStringEvent StockPurchaseAndSellTimeStringEvent => stockPurchaseAndSellTimeStringEvent;
        public Slider RankingCheckTimeSlider => rankingCheckTimeSlider;
        public TMP_Text RankingCheckTimeText => rankingCheckTimeText;
        public LocalizeStringEvent RankingCheckTimeStringEvent => rankingCheckTimeStringEvent;
        public Button MaxPlayerPrevButton => maxPlayerPrevButton;
        public Button MaxPlayerNextButton => maxPlayerNextButton;
        public TMP_Text MaxPlayerCountText => maxPlayerCountText;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            foreach (var item in args)
            {
                if (item is Action action)
                {
                    onClose = action;
                }
            }
            Managers.Input.StopPlayerInput();
        }
        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_RoundSetting, UI_RoundSettingPresenter>();
        }
        public override UniTask OnClose(params object[] args)
        {
            onClose?.Invoke();
            onClose = null;
            Managers.Input.StartPlayerInput();
            return base.OnClose(args);
        }
    }

    public sealed class UI_RoundSettingPresenter : UIPresenter<UI_RoundSetting>
    {
        MapData mapData;

        private string secondsFormat = string.Empty;
        private string minutesFormat = string.Empty;

        protected override void OnBind()
        {
            base.OnBind();
            mapData = GameManager.Instance.MapData;
            View.RoundCountText.text = $"{mapData.MaxRound}";
            View.MissionCountText.text = $"{mapData.MissionCount}";
            View.MaxPlayerCountText.text = $"{MultiplayManager.Instance.MaxUserCount}";

            View.StockCheckTimeStringEvent.SetLocalization("RoundSetting_String", "seconds", value =>
            {
                secondsFormat = value;
                View.StockCheckTimeText.text = string.Format(secondsFormat, mapData.StockCheckTime);
            });

            View.StockPurchaseAndSellTimeStringEvent.SetLocalization("RoundSetting_String", "seconds", value =>
            {
                View.StockPurchaseAndSellTimeText.text = string.Format(value, mapData.StockPurchaseAndSellTime);
            });

            View.RankingCheckTimeStringEvent.SetLocalization("RoundSetting_String", "seconds", value =>
            {
                View.RankingCheckTimeText.text = string.Format(value, mapData.ReleaseRankingTime);
            });

            View.FarmingSliderStringEvent.SetLocalization("RoundSetting_String", "minutes", value =>
            {
                minutesFormat = value;
                View.FarmingSliderText.text = string.Format(minutesFormat, mapData.ExploreTime);
            });

            View.StockCheckTimeSlider.value = mapData.StockCheckTime;
            View.StockPurchaseAndSellTimeSlider.value = mapData.StockPurchaseAndSellTime;
            View.RankingCheckTimeSlider.value = mapData.ReleaseRankingTime;
            View.FarmingSlider.value = mapData.ExploreTime;

            View.CloseButton.OnClickAsObservableFirst().Subscribe(_ => View.Close()).AddTo(this);

            View.StockCheckTimeSlider.OnValueChangedAsObservable().Subscribe(SetStockCheckTime).AddTo(this);
            View.StockPurchaseAndSellTimeSlider.OnValueChangedAsObservable().Subscribe(SetStockPurchaseAndSellTime).AddTo(this);
            View.RankingCheckTimeSlider.OnValueChangedAsObservable().Subscribe(SetReleaseRankingTime).AddTo(this);
            View.FarmingSlider.OnValueChangedAsObservable().Subscribe(SetExploreTime).AddTo(this);

            View.RoundNextButton.OnClickAsObservableFirst(0.1f).Subscribe(AddRound).AddTo(this);
            View.RoundPrevButton.OnClickAsObservableFirst(0.1f).Subscribe(MinusRound).AddTo(this);

            View.MissionNextButton.OnClickAsObservableFirst(0.1f).Subscribe(AddMission).AddTo(this);
            View.MissionPrevButton.OnClickAsObservableFirst(0.1f).Subscribe(MinusMission).AddTo(this);

            View.MaxPlayerNextButton.OnClickAsObservableFirst(0.1f).Subscribe(AddPlayerCount).AddTo(this);
            View.MaxPlayerPrevButton.OnClickAsObservableFirst(0.1f).Subscribe(MinusPlayerCount).AddTo(this);
        }

        private void AddRound(Unit _)
        {
            mapData.MaxRound++;
            View.RoundCountText.text = $"{mapData.MaxRound}";
        }

        private void MinusRound(Unit _)
        {
            mapData.MaxRound--;
            View.RoundCountText.text = $"{mapData.MaxRound}";
        }

        private void AddMission(Unit _)
        {
            mapData.MissionCount++;
            View.MissionCountText.text = $"{mapData.MissionCount}";
        }

        private void MinusMission(Unit _)
        {
            mapData.MissionCount--;
            View.MissionCountText.text = $"{mapData.MissionCount}";
        }

        private void AddPlayerCount(Unit _)
        {
            MultiplayManager.Instance.MaxUserCount++;
            View.MaxPlayerCountText.text = $"{MultiplayManager.Instance.MaxUserCount}";
        }

        private void MinusPlayerCount(Unit _)
        {
            MultiplayManager.Instance.MaxUserCount--;
            View.MaxPlayerCountText.text = $"{MultiplayManager.Instance.MaxUserCount}";
        }

        private void SetStockCheckTime(float value)
        {
            var seconds = (int)value;
            mapData.StockCheckTime = seconds;
            View.StockCheckTimeText.text = string.Format(secondsFormat, seconds);
        }

        private void SetStockPurchaseAndSellTime(float value)
        {
            var seconds = (int)value;
            mapData.StockPurchaseAndSellTime = seconds;
            View.StockPurchaseAndSellTimeText.text = string.Format(secondsFormat, seconds);
        }

        private void SetReleaseRankingTime(float value)
        {
            var seconds = (int)value;
            mapData.ReleaseRankingTime = seconds;
            View.RankingCheckTimeText.text = string.Format(secondsFormat, seconds);
        }

        private void SetExploreTime(float value)
        {
            var minutes = (int)value;
            mapData.ExploreTime = minutes;
            View.FarmingSliderText.text = string.Format(minutesFormat, minutes);
        }
    }
}