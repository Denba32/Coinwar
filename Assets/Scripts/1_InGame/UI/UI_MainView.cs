using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Players;
using StockGame.Scripts.Utility;
using System;
using System.Threading;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.RoundDefine;

namespace StockGame.Scripts.UI
{
    public class UI_MainView : UIBase, IDisposable
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text skillCooltimeText;
        [SerializeField] private MissionDropDown missionDropDown;
        [SerializeField] private BaseButton guideButton;
        [SerializeField] private BaseButton optionButton;
        [SerializeField] private Button minimapButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Image cardImage;
        public TMP_Text TimerText => timerText;
        public TMP_Text CoinText => coinText;
        public TMP_Text SkillCooltimeText => skillCooltimeText;
        public MissionDropDown DropDown => missionDropDown;
        public BaseButton GuideButton => guideButton;
        public BaseButton OptionButton => optionButton;
        public Button MinimapButton => minimapButton;
        public Button SkillButton => skillButton;
        public Button TutorialButton => tutorialButton;

        public Image CardImage => cardImage;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_MainView, UI_MainPresenter>();
        }
    }

    public class UI_MainPresenter : UIPresenter<UI_MainView>
    {
        private GameDefine.JobDefine.NetworkJobInfo currentJobInfo;
        private CancellationTokenSource shieldCts = new();

        private float _duration;

        protected override void OnBind()
        {
            Debug.Log("MainView 초기화");
            View.DropDown.Initialize();
            View.CardImage.gameObject.SetActive(false);
            View.CoinText.text = "0";
            View.GuideButton.OnClickAsObservableFirst().Subscribe(_ => Managers.UI.Open<UI_JobGuideView>(GameDefine.UIDefine.UILayer.Popup).Forget()).AddTo(this);
            View.OptionButton.OnClickAsObservableFirst().Subscribe(_ => Managers.UI.Open<UI_OptionView>(GameDefine.UIDefine.UILayer.Popup).Forget()).AddTo(this);

            GameManager.Instance.CurrentTime.OnValueChanged += OnTimerValueChanged;
            GameManager.Instance.JoinInfos.OnListChanged += OnPlayerDataChanged;

            var player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            player.SkillExecutor?.OnSkillReadyChanged?.Subscribe(SetSkillImage).AddTo(this);
            player.SkillExecutor?.OnSkillCooltime?.Subscribe(SetCooltime).AddTo(this);
            player.OnActivatedSupplySkill.Subscribe(OnActivatedShield).AddTo(this);

            View.SkillButton.enabled = false;
            View.SkillButton?.OnClickAsObservableFirst(0.15f).Subscribe(player.SkillExecutor.Execute).AddTo(this);
            View.MinimapButton.OnClickAsObservableFirst(0.15f).Subscribe(ShowMiniMap).AddTo(this);
            View.TutorialButton?.OnClickAsObservableFirst(0.15f).Subscribe(_ => Managers.UI.Open<TutorialUI>(GameDefine.UIDefine.UILayer.Popup).Forget()).AddTo(this);
            base.OnBind();
        }

        private void ShowMiniMap(Unit _)
        {
            UniTask.Void(async () =>
            {
                var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
                var root = playerObject.GetComponent<PlayerNetwork>().Root;
                var minimapUI = await Managers.UI.Open<MiniMapUI>(GameDefine.UIDefine.UILayer.Popup);
                minimapUI?.SetupPlayer(root);
            });
        }

        private void SetSkillImage(bool isActive)
        {
            if (!currentJobInfo.IsValid) return;
            var sprite = isActive ? currentJobInfo.GetJobSkillIconEnable() : currentJobInfo.GetJobSkillIconDisable();
            View.SkillButton.image.sprite = sprite;
            View.SkillButton.enabled = isActive;
        }

        private void SetCooltime(int cooltime)
        {
            View.SkillCooltimeText.text = cooltime > 0 ? $"{cooltime}" : string.Empty;
        }

        private void OnPlayerDataChanged(NetworkListEvent<GameDefine.NetworkPlayerJoinInfo> changeEvent)
        {
            // View가 이미 파괴된 경우 처리하지 않음
            if (View == null) return;
            if (changeEvent.Value.ClientId != NetworkManager.Singleton.LocalClientId) return;

            var prev = changeEvent.PreviousValue;
            var current = changeEvent.Value;

            if (prev.Coin != current.Coin)
                OnCoinChanged(current.Coin);

            if (prev.JobInfo.JobType != current.JobInfo.JobType)
            {
                Debug.Log("직업 설정");
                currentJobInfo = current.JobInfo;
                var disable = current.JobInfo.GetJobSkillIconDisable();
                // Image가 파괴된 경우 접근하지 않음
                if (View.SkillButton != null && View.SkillButton.image != null)
                    View.SkillButton.image.sprite = disable;
            }
        }

        private void OnCoinChanged(int coin)
        {
            if (View == null || View.CoinText == null) return;
            View.CoinText.text = $"{coin}";
        }

        private void OnTimerValueChanged(int previousValue, int newValue)
        {
            if (View == null || View.TimerText == null) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentRound.Value.RoundPhase != GameDefine.RoundDefine.RoundPhase.Explore) return;

            int minutes = newValue / 60;
            int seconds = newValue % 60;
            View.TimerText.text = $"{minutes:00}:{seconds:00}";
        }

        public void OnActivatedShield(float duration)
        {
            shieldCts?.Cancel();
            shieldCts?.Dispose();

            shieldCts = new CancellationTokenSource();
            _duration = duration;

            RunShieldAsync(shieldCts.Token).Forget();
        }

        private async UniTaskVoid RunShieldAsync(CancellationToken token)
        {
            try
            {
                View.CardImage.gameObject.SetActive(true);

                float targetTime = GameManager.Instance.CurrentTime.Value - _duration;

                await UniTask.WaitUntil(
                    () => GameManager.Instance.CurrentTime.Value <= targetTime
                          || GameManager.Instance.CurrentRound.Value.RoundPhase != RoundPhase.Explore,
                    cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                View.CardImage?.gameObject?.SetActive(false);
                shieldCts?.Cancel();
                shieldCts?.Dispose();
                shieldCts = null;
            }
        }

        /// <summary>
        /// UI가 닫히거나 씬이 전환될 때 이벤트 구독 해제
        /// GameManager 오브젝트가 먼저 파괴될 수 있으므로 null 체크 필수
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            shieldCts?.Cancel();
            shieldCts?.Dispose();

            shieldCts = null;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CurrentTime.OnValueChanged -= OnTimerValueChanged;
                GameManager.Instance.JoinInfos.OnListChanged -= OnPlayerDataChanged;
            }
        }
    }
}