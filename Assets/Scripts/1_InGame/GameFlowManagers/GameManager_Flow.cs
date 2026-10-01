using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Define;
using System;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine;
using static StockGame.Scripts.Define.GameDefine.RoundDefine;

namespace StockGame.Scripts.Manager
{
    public partial class GameManager : NetworkSingleton<GameManager>
    {
        /// <summary>
        /// 게임 시작 시, 로딩 화면과 페이드인 | 페이드 아웃 처리를 맞추기 위해서 사용
        /// </summary>
        /// <param name="rpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        public void NotifyClientReadyServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (NetworkSceneManager.Instance.CurrentSceneId != SceneEnum.MainScene) return;

            var clientId = rpcParams.Receive.SenderClientId;
            _readyClients.Add(clientId);

            int total = NetworkManager.ConnectedClients.Count;
            Debug.Log($"[GameManager] 클라이언트 Ready: {_readyClients.Count} / {total} (clientId: {clientId})");

            if (_readyClients.Count >= total)
            {
                // 대기 인원 초기화
                _readyClients.Clear();
                Debug.Log("[GameManager] 모든 클라이언트 준비 완료 → 게임 시작");

                // joinInfos / 주식 데이터는 GameStart()에서 이미 구성됨.
                // 혹시 누락된 경우(비정상 경로)에만 방어적으로 재구성한다.
                EnsureGameDataPrepared();

                // 모든 유저에게 FadeOut과 로딩 화면 제거를 요청
                BroadcastGameReadyRpc();
                // 동시에 게임 플로우 시작
                RunGameFlow().Forget();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastGameReadyRpc()
        {
            isFirstStart = true;
            Managers.Instance.Clear();
            FadeManager.Instance.FadeOut(2.0f, DG.Tweening.Ease.OutQuad).Forget();
        }

        /// <summary>
        /// GameStart() 시점에 데이터가 정상적으로 구성되지 않았을 때를 대비한 방어적 재구성.
        /// 정상 흐름에서는 아무 것도 하지 않는다.
        /// </summary>
        private void EnsureGameDataPrepared()
        {
            if (!IsServer) return;

            if (joinInfos == null || joinInfos.Count == 0)
            {
                Debug.LogWarning("[GameManager] joinInfos가 비어있어 재구성한다.");
                BuildJoinInfosFromLobby();
            }

            var stockManager = StockManager.Instance;
            if (stockManager != null && (stockManager.StockInfos == null || stockManager.StockInfos.Count == 0))
            {
                Debug.LogWarning("[GameManager] StockInfos가 비어있어 재구성한다.");
                stockManager.InitializeByGameStart();
            }
        }

        private void BuildJoinInfosFromLobby()
        {
            if (!IsServer) return;
            joinInfos?.Clear();
            foreach (var playerData in LobbyManager.Instance.PlayerDataList)
            {
                var playerInfo = new NetworkPlayerJoinInfo
                {
                    Index = playerData.Index,
                    ClientId = playerData.ClientId,
                    Nickname = playerData.NickName,
                    CharacterType = playerData.CharacterType,
                    JobInfo = new(),
                    Money = 0,
                    Coin = 0,
                    Score = 0,
                };
                joinInfos.Add(playerInfo);
            }
        }

        /// <summary>
        /// 게임 전체 흐름의 시작점.
        /// </summary>
        public async UniTask RunGameFlow()
        {
            if (!IsServer || !IsSpawned) return;

            if (isGameFlowRunning)
            {
                Debug.LogWarning("[GameManager] RunGameFlow가 이미 실행 중입니다. 중복 호출을 무시합니다.");
                return;
            }

            isGameFlowRunning = true;
            var token = Managers.Token.GetToken(this, nameof(RunGameFlow));

            try
            {
                // 데이터는 GameStart()에서 이미 구성됨. 누락 시에만 방어적으로 보정.
                EnsureGameDataPrepared();
                for (int roundIndex = 1; roundIndex <= mapData.MaxRound; roundIndex++)
                {
                    if (token.IsCancellationRequested) return;
                    await RunRound(roundIndex, token);
                    if (!IsSpawned || token.IsCancellationRequested) return;
                }

                if (token.IsCancellationRequested) return;
                await SetRoundInfo(mapData.MaxRound, RoundPhase.Finish, 60, token);
                if (token.IsCancellationRequested) return;
                await ShowFinalResult(token);
                if (token.IsCancellationRequested) return;
                await GameEnd(token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[GameManager] RunGameFlow 취소됨 (정상 종료 경로)");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                ForceStopGame();
            }
            finally
            {
                isGameFlowRunning = false;
                Managers.UI.ClearWithoutTransition().Forget();
            }
        }

        /// <summary>
        /// 한 라운드 전체 진행.
        /// </summary>
        private async UniTask RunRound(int roundIndex, CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;
            SetPlayerControlEnabledRpc(false);

            await SetRoundInfo(roundIndex, RoundPhase.RoundInfo, 3, token);
            if (token.IsCancellationRequested) return;
            await RunRoundInfoPhase(token);
            if (token.IsCancellationRequested) return;

            await SetRoundInfo(roundIndex, RoundPhase.StockInfo, mapData.StockCheckTime, token);
            if (token.IsCancellationRequested) return;
            await RunStockInfoPhase(token);
            if (token.IsCancellationRequested) return;

            if (!IsServer || !IsSpawned) return; // await 도중 디스폰/셧다운된 경우 RPC 전송 시도 자체를 막음
            ResetToPlayerPositionRpc();
            await SetRoundInfo(roundIndex, RoundPhase.Explore, mapData.ExploreTime * 60, token);
            if (token.IsCancellationRequested) return;
            await RunExplorePhase(token);
            if (token.IsCancellationRequested) return;

            if (!IsServer || !IsSpawned) return;
            BroadcastExploreFinishRpc();
            currentWinnerList?.Clear();
            newsContextList?.Clear();
            ConvertCoinToMoney();

            await SetRoundInfo(roundIndex, RoundPhase.StockPurchaseAndSell, mapData.StockPurchaseAndSellTime, token);
            if (token.IsCancellationRequested) return;
            await RunPurchasePhase(token);
            if (token.IsCancellationRequested) return;

            await SetRoundInfo(roundIndex, RoundPhase.ReleaseRanking, mapData.ReleaseRankingTime, token);
            if (token.IsCancellationRequested) return;
            await RunRoundReleaseRankingPhase(token);
            if (token.IsCancellationRequested) return;

            if (!IsServer || !IsSpawned) return;
            BroadcastRoundFinishRpc();
        }
    }
}