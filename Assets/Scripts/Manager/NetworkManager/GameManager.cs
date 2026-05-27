using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Define;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Missions;
using System;
using System.Collections.Generic;
using System.Threading;
using UniRx;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;
using static StockGame.Scripts.Define.GameDefine.RoundDefine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.Manager
{
    public class GameManager : NetworkSingleton<GameManager>
    {
        #region Local Field
        private bool isFirstStart = false;
        private MapData mapData;
        private HashSet<ulong> _readyClients = new();
        private HashSet<ulong> _phaseDoneClients = new();
        public MapData MapData => mapData;
        #endregion Local Field

        #region Global Field
        private NetworkList<NetworkPlayerJoinInfo> joinInfos;
        private NetworkVariable<int> currentTime;
        private NetworkVariable<RoundInfo> currentRound;
        public NetworkList<NetworkPlayerJoinInfo> JoinInfos => joinInfos;
        public NetworkVariable<int> CurrentTime => currentTime;
        public NetworkVariable<RoundInfo> CurrentRound => currentRound;
        #endregion Global Field

        #region GAME_CORE_EVENT
        private Subject<Unit> onGameStart = new();
        private Subject<Unit> onGameFinish = new();
        private Subject<Unit> onRoundFinish = new();
        public IObservable<Unit> OnGameStart => onGameStart;
        public IObservable<Unit> OnGameFinish => onGameFinish;
        public IObservable<Unit> OnRoundFinish => onRoundFinish;

        private Action onPlayerSkip = null;
        #endregion

        #region JoinInfo_Update_Method

        /// <summary>
        /// Job 정보 변경
        /// </summary>
        /// <param name="jobInfo"></param>
        /// <param name="ownerId"></param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateJobInfoServerRpc(NetworkJobInfo jobInfo, ulong ownerId)
        {
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.JobInfo = jobInfo;
            joinInfos[index] = localPlayerInfo;
        }

        /// <summary>
        /// 소지금 정보 변경
        /// </summary>
        /// <param name="money"></param>
        /// <param name="rpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateMoneyServerRpc(long money, ServerRpcParams rpcParams = default)
        {
            var ownerId = rpcParams.Receive.SenderClientId;
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.Money = money;
            joinInfos[index] = localPlayerInfo;
        }

        /// <summary>
        /// ClientId를 타겟으로 유저의 소지금 변경
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="money"></param>
        public void UpdateMoneyByClientId(ulong clientId, long money)
        {
            var info = GetPlayerInfoByClientId(clientId);
            var index = joinInfos.IndexOf(info);
            if (index < 0) return;
            info.Money = money;
            joinInfos[index] = info;
        }

        /// <summary>
        /// Coin 정보 업데이트
        /// </summary>
        /// <param name="coin"></param>
        /// <param name="rpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateCoinServerRpc(int coin, ServerRpcParams rpcParams = default)
        {
            var ownerId = rpcParams.Receive.SenderClientId;
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.Coin = coin;
            joinInfos[index] = localPlayerInfo;
        }

        /// <summary>
        /// 점수 정보 업데이트
        /// </summary>
        /// <param name="score"></param>
        /// <param name="rpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        public void UpdateScoreServerRpc(int score, ServerRpcParams rpcParams = default)
        {
            var ownerId = rpcParams.Receive.SenderClientId;
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.Score = score;
            joinInfos[index] = localPlayerInfo;
        }

        /// <summary>
        /// ClientId를 타겟으로 코인 변경
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="coin"></param>
        public void UpdateCoinByClientId(ulong clientId, int coin)
        {
            var info = GetPlayerInfoByClientId(clientId);
            var index = joinInfos.IndexOf(info);
            if (index < 0) return;
            info.Coin = coin;
            joinInfos[index] = info;
        }

        /// <summary>
        /// 도둑의 훔치기 행동 시의 처리
        /// </summary>
        /// <param name="senderClientId"></param>
        /// <param name="amount"></param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestStealCoinServerRpc(ulong senderClientId, int amount)
        {
            var victimInfo = GetPlayerInfoByClientId(OwnerClientId);
            var senderInfo = GetPlayerInfoByClientId(senderClientId);

            UpdateCoinByClientId(OwnerClientId, Mathf.Max(0, victimInfo.Coin - amount));
            UpdateCoinByClientId(senderClientId, senderInfo.Coin + amount);
        }


        /// <summary>
        /// PlayerIndex를 이용하여 점수 변경
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="score"></param>
        private void UpdateScoreByPlayerIndex(int playerIndex, int score)
        {
            for (int i = 0; i < joinInfos.Count; i++)
            {
                if (joinInfos[i].Index != playerIndex) continue;
                var info = joinInfos[i];
                info.Score = score;
                joinInfos[i] = info;
                break;
            }
        }

        #endregion JoinInfo_Update_Method

        private enum PhaseTransitionType
        {
            FadeIn,
            FadeOut
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
                currentTime.Value = 1;

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
                UpdateCoinByClientId(NetworkManager.Singleton.LocalClientId, 10);
        }

        #region LIFECYCLE

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) return;
            var dataSO = Managers.Resource.Load<MapDataSO>("Maps/MAP001", ResourceDirectory.Datas);
            mapData = new MapData(dataSO);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Managers.Token.CancelAll(this);
        }

        public override UniTask Initialize()
        {
            currentRound = new(writePerm: NetworkVariableWritePermission.Server);
            currentTime = new(writePerm: NetworkVariableWritePermission.Server);
            joinInfos = new(writePerm: NetworkVariableWritePermission.Server);
            return base.Initialize();
        }

        public NetworkPlayerJoinInfo GetLocalPlayerInfo()
        {
            var localClientId = NetworkManager.LocalClientId;
            return GetPlayerInfoByClientId(localClientId);
        }

        public NetworkPlayerJoinInfo GetPlayerInfoByClientId(ulong clientId)
        {
            if (joinInfos == null || joinInfos.Count <= 0) return default;
            NetworkPlayerJoinInfo info = default;
            foreach (var joinInfo in joinInfos)
            {
                if (joinInfo.ClientId == clientId)
                {
                    info = joinInfo;
                    break;
                }
            }
            return info;
        }
        #endregion

        #region CLIENT_READY

        [ServerRpc(RequireOwnership = false)]
        public void NotifyClientReadyServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            if (NetworkSceneManager.Instance.CurrentSceneId != SceneEnum.MainScene) return;

            var clientId = rpcParams.Receive.SenderClientId;
            _readyClients.Add(clientId);

            int total = NetworkManager.ConnectedClients.Count;
            Debug.Log($"[GameManager] 클라이언트 Ready: {_readyClients.Count} / {total} (clientId: {clientId})");

            // 모든 유저가 Ready 완료 시, 참여하는 플레이어의 인게임 데이터 업데이트
            // 도중에 나가는 유저를 방지하기 위해서
            if (_readyClients.Count >= total)
            {
                // 대기 인원 초기화
                _readyClients.Clear();
                Debug.Log("[GameManager] 모든 클라이언트 준비 완료 → joinInfos 구성 후 게임 시작");

                // LobbyManager.PlayerDataList 기반으로 joinInfos 한 번에 구성
                // 각 클라이언트가 개별로 요청하지 않고 서버가 일괄 처리
                BuildJoinInfosFromLobby();

                // 모든 유저에게 FadeOut과 로딩 화면 제거를 요청
                BroadcastGameReadyRpc();
                // 동시에 게임 플로우 시작
                RunGameFlow().Forget();
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

        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastGameReadyRpc()
        {
            isFirstStart = true;
            Managers.Instance.Clear();
            FadeManager.Instance.FadeOut(2.0f, DG.Tweening.Ease.OutQuad).Forget();
        }
        #endregion

        #region GAME_FLOW

        public async UniTask GameEnd()
        {
            if (!IsServer || !IsSpawned) return;

            joinInfos?.Clear();
            onGameFinish?.OnNext(Unit.Default);

            JobManager.Instance.ResetAllocateQueue();

            await UniTask.WaitForSeconds(1f);
            RestoreInputRpc();

            await NetworkSceneManager.Instance.ChangeScene(SceneEnum.LobbyScene, useNetworkSceneManager: true);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RestoreInputRpc()
        {
            Managers.Input.StartAllInput();
        }

        public async UniTask RunGameFlow()
        {
            if (!IsServer || !IsSpawned) return;

            StockManager.Instance.InitializeByGameStart();
            for (int roundIndex = 1; roundIndex <= mapData.MaxRound; roundIndex++)
            {
                await RunRound(roundIndex);
                if (!IsSpawned) return;
            }

            await SetRoundInfo(mapData.MaxRound, RoundPhase.Finish, 60);
            await ShowFinalResult(Managers.Token.GetToken(this, nameof(ShowFinalResult)));
            await GameEnd();
        }

        private async UniTask RunRound(int roundIndex)
        {
            if (!IsServer || !IsSpawned) return;

            var token = Managers.Token;
            SetPlayerControlEnabledRpc(false);

            await SetRoundInfo(roundIndex, RoundPhase.StockInfo, mapData.StockCheckTime);
            await RunStockInfoPhase(token.GetToken(this, nameof(RunStockInfoPhase)));

            await SetRoundInfo(roundIndex, RoundPhase.Explore, mapData.ExploreTime * 60);
            await RunExplorePhase(token.GetToken(this, nameof(RunExplorePhase)));

            BroadcastExploreFinishRpc();
            ConvertCoinToMoney();

            await SetRoundInfo(roundIndex, RoundPhase.StockPurchase, mapData.StockPurchaseTime);
            await RunPurchasePhase(token.GetToken(this, nameof(RunPurchasePhase)));

            await SetRoundInfo(roundIndex, RoundPhase.ReleaseRanking, mapData.ReleaseRankingTime);
            await RunRoundResultPhase(token.GetToken(this, nameof(RunRoundResultPhase)));
            if (!IsSpawned) return;

            for (int i = 0; i < joinInfos.Count; i++)
            {
                var join = joinInfos[i];
                join.Money = 0;
                joinInfos[i] = join;
            }
            BroadcastRoundFinishRpc();
        }

        private async UniTask ShowFinalResult(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;

            StartTimerAsync(60, token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(ShowFinalResult));
        }

        #endregion

        private void ConvertCoinToMoney()
        {
            for (int i = 0; i < joinInfos.Count; i++)
            {
                var join = joinInfos[i];
                join.Money += join.Coin * 1000000;
                join.Coin = 0;
                joinInfos[i] = join;
            }
        }

        private void CommitRoundScore()
        {
            var rankingList = CalculateRankingScore();
            foreach (var data in rankingList)
                UpdateScoreByPlayerIndex(data.PlayerIndex, data.Score + data.RoundScore);
        }

        #region ROUND_PHASE

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastRoundFinishRpc()
        {
            GetLocalPlayerInfo().ResetData();
            onRoundFinish?.OnNext(Unit.Default);
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastExploreFinishRpc()
        {
            UIManager.Instance.Clear();
        }

        private async UniTask RunStockInfoPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;

            AssignJobsAndMissions();

            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(RunStockInfoPhase));
        }

        private async UniTask RunPurchasePhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;
            Managers.Sound.StopBgm();
            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(RunPurchasePhase));
        }

        private async UniTask RunExplorePhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;

            var updatePriceToken = Managers.Token.GetToken(this, "UpdateStockInfo");
            SetPlayerControlEnabledRpc(true);
            MissionResolver.StartMission();
            StockManager.Instance.UpdateStockPrices(1, updatePriceToken).Forget();
            Managers.Sound.PlaySound("BGM/BGM101", SoundType.BGM);
            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();

            await UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token);
            SetPlayerControlEnabledRpc(false);
            Managers.Token.Cancel(this, "UpdateStockInfo");
            Managers.Token.Cancel(this, nameof(RunExplorePhase));
        }

        private async UniTask RunRoundResultPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;
            CommitRoundScore();
            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();
            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(RunRoundResultPhase));
        }

        #endregion

        #region TIMER

        public async UniTask StartTimerAsync(int duration, CancellationToken token)
        {
            if (!IsServer) return;

            CurrentTime.Value = duration;

            try
            {
                while (CurrentTime.Value > 0 && !token.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
                    CurrentTime.Value--;
                }
            }
            catch (OperationCanceledException) { }
        }

        #endregion

        [Rpc(SendTo.ClientsAndHost)]
        private void PhaseTransitionRpc(PhaseTransitionType type)
        {
            RunPhaseTransition(type).Forget();
        }

        private async UniTaskVoid RunPhaseTransition(PhaseTransitionType type)
        {
            try
            {
                var fadeToken = Managers.Token.GetToken(this, nameof(RunPhaseTransition));
                if (type == PhaseTransitionType.FadeIn)
                {
                    await FadeManager.Instance.FadeIn(.5f, continuous: true, token: fadeToken);
                    NotifyPhaseDoneServerRpc();
                }
                else
                {
                    await FadeManager.Instance.FadeOut(.5f, token: fadeToken);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("OperationCanceledException RunPhaseTransition");
                Managers.Token.Cancel(this, nameof(RunPhaseTransition));
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyPhaseDoneServerRpc(ServerRpcParams rpcParams = default)
        {
            _phaseDoneClients.Add(rpcParams.Receive.SenderClientId);
        }

        #region SKIP

        private async UniTask WaitAllPlayersSkip(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            int skipCount = 0;

            var skipSource = new UniTaskCompletionSource();
            Action handler = null;

            handler = () =>
            {
                skipCount++;
                if (skipCount >= NetworkManager.ConnectedClients.Count)
                    skipSource.TrySetResult();
            };

            onPlayerSkip += handler;

            try
            {
                await skipSource.Task.AttachExternalCancellation(token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                onPlayerSkip -= handler;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SendSkipVoteServerRpc(ServerRpcParams rpcParams = default)
        {
            Debug.Log($"클라이언트 {rpcParams.Receive.SenderClientId} 가 스킵을 눌렀습니다.");
            onPlayerSkip?.Invoke();
        }

        #endregion

        #region PHASE_TRANSITION

        private async UniTask WaitFadeIn(CancellationToken token)
        {
            if (!IsServer || token.IsCancellationRequested || !IsSpawned) return;
            _phaseDoneClients.Clear();

            PhaseTransitionRpc(PhaseTransitionType.FadeIn);
            int total = NetworkManager.ConnectedClients.Count;

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => _phaseDoneClients.Count >= total, cancellationToken: token),
                UniTask.Delay(5000, cancellationToken: token)
            );
        }

        private async UniTask SetRoundInfo(int roundIndex, RoundPhase phase, int remainingTime)
        {
            if (!IsServer || !IsSpawned) return;
            if (!isFirstStart)
            {
                var firstFadeToken = Managers.Token.GetToken(this, "FirstFade");
                await WaitFadeIn(firstFadeToken);
            }

            var round = currentRound.Value;
            round.RoundIndex = roundIndex;
            round.RoundPhase = phase;
            round.RemainingTime = remainingTime;
            currentRound.Value = round;

            ResetToPlayerPositionRpc();

            if (!isFirstStart)
            {
                PhaseTransitionRpc(PhaseTransitionType.FadeOut);
            }
            else
            {
                isFirstStart = false;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void ResetToPlayerPositionRpc()
        {
            var localPlayer = NetworkManager.Singleton.LocalClient;
            if (localPlayer == null) return;

            var teleportTransform = ZoneManager.Instance.GetTeleportPositionByRandom();
            localPlayer.PlayerObject
                .GetComponentInChildren<ClientNetworkTransform>()
                .Teleport(teleportTransform.position, Quaternion.identity, Vector3.one);
        }

        [Rpc(SendTo.Everyone)]
        private void SetPlayerControlEnabledRpc(bool isActive)
        {
            if (isActive)
            {
                Debug.Log("Start Input");
                Managers.Input.StartAllInput();
            }
            else
            {
                Debug.Log("Stop Input");
                Managers.Input.StopAllInput();
            }
        }

        #endregion

        public List<StockRanking> CalculateRankingScore()
        {
            StockManager.Instance.UpdateStock();
            var profitList = new List<StockRanking>();
            foreach (var player in joinInfos)
            {
                var temp = player;
                long totalMoney = StockManager.Instance.CalculateTotalStock(player.ClientId);
                profitList.Add(new StockRanking
                {
                    Ranking = 0,
                    PlayerIndex = temp.Index,
                    Profit = totalMoney,
                    CharacterType = player.CharacterType,
                    Score = temp.Score,
                    RoundScore = 0,
                });
            }

            profitList.Sort((a, b) => b.Profit.CompareTo(a.Profit));

            for (int i = 0; i < profitList.Count; i++)
            {
                var rankingData = profitList[i];
                var prevRanking = i > 0 ? profitList[i - 1] : null;
                rankingData.Ranking = prevRanking != null && prevRanking.Profit == rankingData.Profit
                    ? prevRanking.Ranking
                    : i + 1;

                rankingData.RoundScore = GetScoreFromRank(rankingData.Ranking);
                profitList[i] = rankingData;
            }

            return profitList;
        }

        private void AssignJobsAndMissions()
        {
            if (!IsServer) return;

            foreach (var playerData in LobbyManager.Instance.PlayerDataList)
            {
                var newJob = JobManager.Instance.GetJobByRandom();
                UpdateJobInfoServerRpc(newJob, playerData.ClientId);

                MissionManager.Instance.RequestMissionRpc(
                    newJob.JobType,
                    RpcTarget.Single(playerData.ClientId, RpcTargetUse.Temp)
                );
            }
        }

        public void RemoveJoinInfo(ulong clientId)
        {
            if (!IsServer) return;
            for (int i = 0; i < joinInfos.Count; i++)
            {
                if (joinInfos[i].ClientId != clientId) continue;
                joinInfos.RemoveAt(i);
                break;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Managers.Token.CancelAll(this);

            currentTime?.Dispose();
            currentRound?.Dispose();

            onGameStart = null;
            onGameFinish = null;
            onRoundFinish = null;
            onPlayerSkip = null;
        }
    }
}