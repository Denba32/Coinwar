using Cysharp.Threading.Tasks;
using DG.Tweening;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_RankingPanel : UIBase
    {
        [SerializeField] private UI_ReleaseRoundRanking releaseRoundRanking;
        [SerializeField] private UI_Result resultUI;
        [SerializeField] private Button skipButton;
        [SerializeField] private CanvasGroup winnerGroup;
        [SerializeField] private Image playerImage;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text skipPlayerCountText;

        public UI_ReleaseRoundRanking ReleaseRoundRanking => releaseRoundRanking;
        public UI_Result UIResult => resultUI;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            skipButton.OnClickAsObservableFirst(0.2f).Subscribe(_ => OnClickSkipButton()).AddTo(this);
            Managers.Game.OnSkip.Subscribe(SetSkipCount).AddTo(this);
        }

        public void ShowRoundRanking(List<StockRanking> infos)
        {
            SetSkipCount();
            releaseRoundRanking?.UpdateRanking(infos);
            resultUI?.Hide();
            releaseRoundRanking?.Show();
        }

        /// <summary>
        /// Finish 페이즈 — Score 기준 최종 랭킹을 받아 결과 화면 표시
        /// WinnerUI 표시 → 5초 뒤 FadeOut
        /// </summary>
        public void ShowResult(List<StockRanking> finalRankings)
        {
            SetSkipCount();
            ShowResultAsync(finalRankings, Managers.Token.GetToken(this)).Forget();
            resultUI?.UpdateRanking(finalRankings);
        }

        public async UniTask ShowResultAsync(List<StockRanking> finalRankings, CancellationToken token = default)
        {
            ShowWinner(finalRankings);
            await UniTask.WaitForSeconds(5f, cancellationToken: token);
            await winnerGroup.DOFade(0f, 1f).SetLink(gameObject);
            if (token.IsCancellationRequested) return;

            winnerGroup.interactable = false;
            winnerGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 1등 플레이어의 이미지와 닉네임을 WinnerUI에 표시
        /// </summary>
        private void ShowWinner(List<StockRanking> finalRankings)
        {
            if (finalRankings == null || finalRankings.Count <= 0) return;

            var winner = finalRankings[0];

            // 1등 플레이어 데이터 조회 (PlayerIndex 기준)
            var winnerPlayerData = LobbyManager.Instance.GetPlayerDataByIndex(winner.PlayerIndex);

            // 프로필 이미지
            if (playerImage != null)
                playerImage.sprite = winnerPlayerData.GetProfileImage();

            // 닉네임
            if (nicknameText != null)
                nicknameText.text = winnerPlayerData.GetNickname();

            winnerGroup.alpha = 1f;
            winnerGroup.interactable = true;
            winnerGroup.blocksRaycasts = true;
        }

        public void ActivateSkipButton() => skipButton.interactable = true;

        private void SetSkipCount(int count = 0)
        {
            var allPlayerCount = Managers.Game.JoinInfos.Count;
            skipPlayerCountText.text = $"{count} / {allPlayerCount}";
        }

        private void OnClickSkipButton()
        {
            if (NetworkManager.Singleton.LocalClient != null)
            {
                Debug.Log("Skip");
                skipButton.interactable = false;
                GameManager.Instance.SendSkipVoteServerRpc();
            }
        }
    }
}