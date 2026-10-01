using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.RoundDefine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_RoundReport : UIBase
    {
        [SerializeField] private TMP_Text roundText;

        [Header("주식 패널")]
        [SerializeField] private UI_StockPanel stockPanel;
        [Header("랭킹 패널")]
        [SerializeField] private UI_RankingPanel rankingPanel;
        [Header("전환 버튼")]
        [SerializeField] private Button transitionButton;
        public TMP_Text RoundText => roundText;
        public UI_StockPanel StockPanel => stockPanel;
        public UI_RankingPanel RankingPanel => rankingPanel;
        public Button TransitionButton => transitionButton;
        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            stockPanel?.Initilaize(sortOrder, uiLayer);
            rankingPanel?.Initilaize(sortOrder, uiLayer);

            Bind<UI_RoundReport, UI_RoundReportPresenter>();
        }
    }

    public sealed class UI_RoundReportPresenter : UIPresenter<UI_RoundReport>
    {
        private bool isRankingPage = true;
        protected override void OnBind()
        {
            base.OnBind();
            var gameManager = GameManager.Instance;

            if (gameManager == null) return;
            gameManager.CurrentTime.ObserveEveryValueChanged(time => time.Value).Subscribe(View.StockPanel.OnTimerValueChanged).AddTo(this); // 타이머 설정

            gameManager.CurrentRound.ObserveEveryValueChanged(roundInfo => roundInfo.Value).Subscribe(SetRoundInfo).AddTo(this);

            gameManager.OnReleaseRanking.Subscribe(View.RankingPanel.ShowRoundRanking).AddTo(this);
            gameManager.OnFinalRoundEnded.Subscribe(View.RankingPanel.ShowResult).AddTo(this);

            gameManager.JoinInfos.OnListChanged -= OnPlayerDataChanged;
            gameManager.JoinInfos.OnListChanged += OnPlayerDataChanged;

            var stockManager = StockManager.Instance;
            if(stockManager == null) return;
            stockManager.OnPurchase.Subscribe(View.StockPanel.UpdateItem).AddTo(this); // 구매 시 변경 처리
            stockManager.OnSell.Subscribe(View.StockPanel.UpdateItem).AddTo(this);

            stockManager.StockInfos.OnListChanged -= OnStockInfosChanged;
            stockManager.StockInfos.OnListChanged += OnStockInfosChanged;

            // 씬 로드 이전에 주식 데이터가 이미 생성된 경우(호스트가 GameStart 시 사전 생성)
            // OnListChanged Add 이벤트가 이미 지나가 UI가 빈 상태로 남는 것을 방지하기 위해
            // 현재 리스트 상태를 직접 시드한다.
            View.StockPanel?.SeedStocks(stockManager.StockInfos);

            View.TransitionButton?.OnClickAsObservable().Subscribe(TransitionPage).AddTo(this);
            
            Disposable.Create(() =>
            {
                if (stockManager == null) return;
                stockManager.StockInfos.OnListChanged -= OnStockInfosChanged;
            }).AddTo(this);

            View.StockPanel?.Show();
            View.StockPanel?.ShowStockReport();
            View.RankingPanel?.Hide();
        }

        private void TransitionPage(Unit _)
        {
            isRankingPage = !isRankingPage;
            if(isRankingPage)
            {
                View.RankingPanel.Show();
                View.StockPanel.Hide();
            }
            else
            {
                View.RankingPanel.Hide();
                View.StockPanel.Show();
                View.StockPanel.ShowStockReport();
            }
        }
        private void OnStockInfosChanged(NetworkListEvent<NetworkStockInfo> changeEvent)
        {
            View.StockPanel?.UpdateStock(changeEvent);
        }

        private void OnPlayerDataChanged(NetworkListEvent<GameDefine.NetworkPlayerJoinInfo> changeEvent)
        {
            if (changeEvent.Value.ClientId != NetworkManager.Singleton.LocalClientId) return;
            var prev = changeEvent.PreviousValue;
            var current = changeEvent.Value;

            if (prev.Money != current.Money)
                View.StockPanel?.UpdateCash(current.Money);
        }

        private void SetRoundInfo(RoundInfo roundInfo)
        {
            View.StockPanel.StockTimer?.ActivateSkipButton();
            View.StockPanel.PurchaseAndSell.Sell?.ActivateSkipButton();
            View.StockPanel.PurchaseAndSell?.StockTimer.ActivateSkipButton();
            View.RankingPanel?.ActivateSkipButton();
            View.Show();
            View.TransitionButton.gameObject.SetActive(roundInfo.RoundPhase == RoundPhase.Finish);

            switch (roundInfo.RoundPhase)
            {
                case RoundPhase.RoundInfo:
                    View.StockPanel?.Hide();
                    View.RankingPanel?.Hide();
                    View.RoundText.gameObject.SetActive(true);
                    View.RoundText.text = $"ROUND {roundInfo.RoundIndex}";
                    break;
                case RoundPhase.StockInfo:
                    View.RoundText.gameObject.SetActive(false);
                    View.StockPanel?.Show();
                    View.StockPanel.ShowStockReport();
                    View.RankingPanel?.Hide();
                    break;

                case RoundPhase.Explore:
                    View.Hide();
                    break;

                case RoundPhase.StockPurchaseAndSell:
                    View.RoundText.gameObject.SetActive(false);
                    View.StockPanel?.Show();
                    View.StockPanel.ShowPurchase();
                    View.RankingPanel?.Hide();
                    break;

                case RoundPhase.ReleaseRanking:
                    View.RoundText.gameObject.SetActive(false);
                    View.StockPanel?.Hide();
                    View.RankingPanel?.Show();
                    break;

                case RoundPhase.Finish:
                    View.RoundText.gameObject.SetActive(false);
                    View.StockPanel?.Hide();
                    View.RankingPanel?.Show();
                    View.RankingPanel?.ReleaseRoundRanking?.Hide();
                    View.RankingPanel?.UIResult?.Show();
                    break;
            }
        }
    }
}