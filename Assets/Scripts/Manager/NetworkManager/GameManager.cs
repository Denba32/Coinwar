using Cysharp.Threading.Tasks;
using Denba.Common;
using FMODUnity;
using StockGame.Scripts.Define;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Objects;
using StockGame.Scripts.Players;
using StockGame.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private enum PhaseTransitionType
        {
            FadeIn,
            FadeOut
        }

        #region Local Field
        private bool isFirstStart = false;
        private List<(ulong clientId, string newsContext)> newsContextList = new List<(ulong clientId, string newsContext)>();
        private List<ulong> currentWinnerList = new();
        private MapData mapData;
        private HashSet<ulong> _readyClients = new();
        private HashSet<ulong> _phaseDoneClients = new();
        public MapData MapData => mapData;
        #endregion Local Field

        #region Global Field
        private NetworkVariable<bool> isGameStart;
        private NetworkList<NetworkPlayerJoinInfo> joinInfos;
        private NetworkVariable<int> currentTime;
        private NetworkVariable<RoundInfo> currentRound;
        public NetworkList<NetworkPlayerJoinInfo> JoinInfos => joinInfos;
        public NetworkVariable<int> CurrentTime => currentTime;
        public NetworkVariable<RoundInfo> CurrentRound => currentRound;
        public NetworkVariable<bool> IsGameStart => isGameStart;
        #endregion Global Field

        #region GAME_CORE_EVENT
        private Subject<Unit> onGameStart = new();
        private Subject<Unit> onGameFinish = new();
        private Subject<List<StockRanking>> onReleaseRanking = new();
        private Subject<Unit> onRoundFinish = new();
        private Subject<List<StockRanking>> onFinalRoundEnded = new();
        public IObservable<Unit> OnGameStart => onGameStart;
        public IObservable<Unit> OnGameFinish => onGameFinish;
        public IObservable<List<StockRanking>> OnReleaseRanking => onReleaseRanking;
        public IObservable<Unit> OnRoundFinish => onRoundFinish;
        public IObservable<List<StockRanking>> OnFinalRoundEnded => onFinalRoundEnded;

        private Action onPlayerSkip = null;
        #endregion

        #region JoinInfo_Update_Method

        public void UpdateJobInfo(NetworkJobInfo jobInfo, ulong ownerId)
        {
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.JobInfo = jobInfo;
            joinInfos[index] = localPlayerInfo;

            if (!NetworkManager.ConnectedClients.TryGetValue(ownerId, out var client))
            {
                Debug.LogError($"{ownerId}에 해당하는 플레이어를 찾을 수 없습니다");
                return;
            }

            client.PlayerObject.GetComponent<PlayerNetwork>()?.InitializeJobRpc(jobInfo);
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

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void AddCoinRpc(int coin, RpcParams rpcParams = default)
        {
            var ownerId = rpcParams.Receive.SenderClientId;
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.Coin += coin;
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

        #endregion JoinInfo_Update_Method

        #region LIFECYCLE
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
                currentTime.Value = 1;

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
                UpdateCoinByClientId(NetworkManager.Singleton.LocalClientId, 10);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[GameManager] OnNetworkSpawn 호출됨. IsServer={IsServer}, InstanceID={GetInstanceID()}");
            if (!IsServer) return;
            Managers.Token.CancelAll(this);
            isGameFlowRunning = false;
            if (isGameStart != null) isGameStart.Value = false;

            var dataSO = Managers.Resource.Load<MapDataSO>("Maps/MAP001", ResourceDirectory.Datas);
            mapData = new MapData(dataSO);

            joinInfos?.Clear();
            _readyClients.Clear();
            _phaseDoneClients.Clear();
            currentWinnerList?.Clear();
            newsContextList?.Clear();
            isFirstStart = false;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Debug.Log($"[GameManager] OnNetworkDespawn 호출됨. InstanceID={GetInstanceID()}");
            Managers.Token.Cancel(this, nameof(RunGameFlow));
            Managers.Token.CancelAll(this);
        }
        #endregion

        public override UniTask Initialize()
        {
            currentRound = new(writePerm: NetworkVariableWritePermission.Server);
            currentTime = new(writePerm: NetworkVariableWritePermission.Server);
            joinInfos = new(writePerm: NetworkVariableWritePermission.Server);
            isGameStart = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Server);
            return base.Initialize();
        }

        // 게임 참가중인 로컬 플레이어 정보 반환
        public NetworkPlayerJoinInfo GetLocalPlayerInfo()
        {
            var localClientId = NetworkManager.LocalClientId;
            return GetPlayerInfoByClientId(localClientId);
        }

        // 게임 참가중인 로컬 플레이어 정보 반환
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

        #region 게임 시작 전 클라이언트 처리

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
                Debug.Log("[GameManager] 모든 클라이언트 준비 완료 → joinInfos 구성 후 게임 시작");

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
        public void GameStart() => isGameStart.Value = true;
        public async UniTask GameEnd()
        {
            if (!IsServer || !IsSpawned) return;

            joinInfos?.Clear();
            onGameFinish?.OnNext(Unit.Default);

            JobManager.Instance.ResetAllocateQueue();

            await UniTask.WaitForSeconds(1f);
            SetPlayerControlEnabledRpc(true);
            isGameStart.Value = false;
            await NetworkSceneManager.Instance.ChangeScene(SceneEnum.LobbyScene, useNetworkSceneManager: true);
        }

        private bool isGameFlowRunning = false;

        public async UniTask RunGameFlow()
        {
            if (!IsServer || !IsSpawned) return;

            // 토큰 취소 메커니즘이 어떤 이유로든 실패하더라도, 물리적으로 동시에 두 개의
            // RunGameFlow가 실행되는 것을 막는 최종 방어선.
            if (isGameFlowRunning)
            {
                Debug.LogWarning("[GameManager] RunGameFlow가 이미 실행 중입니다. 중복 호출을 무시합니다.");
                return;
            }
            isGameFlowRunning = true;

            // 이전 세션(혹은 이전 RunGameFlow 호출)이 아직 살아있다면 명시적으로 정지시키고,
            // 이번 실행만을 위한 새 토큰을 발급한다. 이렇게 하면 "방 나갔다가 다시 시작" 시
            // 이전 RunGameFlow 루프가 새 루프와 동시에 도는 것을 막을 수 있다.
            var token = Managers.Token.GetToken(this, nameof(RunGameFlow));

            try
            {
                StockManager.Instance.InitializeByGameStart();
                for (int roundIndex = 1; roundIndex <= mapData.MaxRound; roundIndex++)
                {
                    if (token.IsCancellationRequested) return;
                    await RunRound(roundIndex);
                    if (!IsSpawned || token.IsCancellationRequested) return;
                }

                if (token.IsCancellationRequested) return;
                await SetRoundInfo(mapData.MaxRound, RoundPhase.Finish, 60);
                await ShowFinalResult(Managers.Token.GetToken(this, nameof(ShowFinalResult)));
                await GameEnd();
            }
            catch (OperationCanceledException)
            {
                // 인원 부족으로 인한 강제 로비 복귀, 씬 전환, 새 세션 시작 등으로 인한 정상적인 취소.
                // 별도의 에러 처리 없이 그대로 흐름을 종료한다.
                Debug.Log("[GameManager] RunGameFlow 취소됨 (정상 종료 경로)");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                Managers.Token.Cancel(this);
            }
            finally
            {
                isGameFlowRunning = false;
            }
        }


        private async UniTask RunRound(int roundIndex)
        {
            if (!IsServer || !IsSpawned) return;

            var token = Managers.Token;
            SetPlayerControlEnabledRpc(false);

            await SetRoundInfo(roundIndex, RoundPhase.StockInfo, mapData.StockCheckTime);
            await RunStockInfoPhase(token.GetToken(this, nameof(RunStockInfoPhase)));

            if (!IsServer || !IsSpawned) return; // await 도중 디스폰/셧다운된 경우 RPC 전송 시도 자체를 막음
            ResetToPlayerPositionRpc();
            await SetRoundInfo(roundIndex, RoundPhase.Explore, mapData.ExploreTime * 60);
            await RunExplorePhase(token.GetToken(this, nameof(RunExplorePhase)));

            currentWinnerList?.Clear();
            newsContextList?.Clear();

            if (!IsServer || !IsSpawned) return;
            BroadcastExploreFinishRpc();
            ConvertCoinToMoney();

            await SetRoundInfo(roundIndex, RoundPhase.StockSell, mapData.StockSellTime);
            await RunSellPhase(token.GetToken(this, nameof(RunSellPhase)));

            await SetRoundInfo(roundIndex, RoundPhase.StockPurchase, mapData.StockPurchaseTime);
            await RunPurchasePhase(token.GetToken(this, nameof(RunPurchasePhase)));

            await SetRoundInfo(roundIndex, RoundPhase.ReleaseRanking, mapData.ReleaseRankingTime);
            await RunRoundReleaseRankingPhase(token.GetToken(this, nameof(RunRoundReleaseRankingPhase)));

            if (!IsServer || !IsSpawned) return;
            BroadcastRoundFinishRpc();
        }

        private async UniTask ShowFinalResult(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;
            var rankingList = CalculateRankingScore();
            var json = JsonUtility.ToJson(new StockRankingWrapper { Rankings = rankingList });
            BroadcastFinalRoundEndRpc(json); // 전파

            StartTimerAsync(60, token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(ShowFinalResult));
        }

        #endregion

        #region ROUND_PHASE
        /// <summary>
        /// 페이즈 별 라운드 정보 변경
        /// </summary>
        /// <param name="roundIndex"></param>
        /// <param name="phase"></param>
        /// <param name="remainingTime"></param>
        /// <returns></returns>
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

            if (!isFirstStart)
            {
                PhaseTransitionRpc(PhaseTransitionType.FadeOut);
            }
            else
            {
                isFirstStart = false;
            }
        }

        /// <summary>
        /// 주식 정보 페이즈 로직 처리
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask RunStockInfoPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;

            AssignJobsAndMissions();
            StockManager.Instance.ChangeTaxByRandom(); // 세금 변경

            Debug.Log("RoundIndex: " + CurrentRound.Value.RoundIndex);
            if (CurrentRound.Value.RoundIndex > 1)
            {
                foreach (var winner in currentWinnerList)
                    NotifyWinnerNewsWriteRpc(RpcTarget.Single(winner, RpcTargetUse.Temp));
            }

            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );

            Managers.Token.Cancel(this, nameof(RunStockInfoPhase));
        }

        /// <summary>
        /// 파밍 탐험 페이즈 로직 처리
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask RunExplorePhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;

            var updatePriceToken = Managers.Token.GetToken(this, "UpdateStockInfo");
            var supplyBoxToken = Managers.Token.GetToken(this, nameof(SpawnSupplyBox));
            SetPlayerControlEnabledRpc(true);
            RPCManager.Instance.StartMissionRpc();

            newsContextList.Sort((a, b) => b.clientId.CompareTo(a.clientId));
            var contexts = string.Join("|", newsContextList.Select(n => n.newsContext));
            BroadcastNotificationRpc(contexts);
            BroadcastCountdownSFXRpc();
            int totalTime = CurrentRound.Value.RemainingTime;
            StartTimerAsync(totalTime, token).Forget();

            // currentTime 기준으로 분 경계마다 트리거
            TriggerOnMinuteBoundary(totalTime, updatePriceToken, supplyBoxToken, token).Forget();

            await UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token);
            SetPlayerControlEnabledRpc(false);
            Managers.Token.Cancel(this, "UpdateStockInfo");
            Managers.Token.Cancel(this, nameof(RunExplorePhase));
            Managers.Token.Cancel(this, nameof(SpawnSupplyBox));
        }

        /// <summary>
        /// 구매 페이즈 로직 처리
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask RunPurchasePhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;
            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );

            Managers.Token.Cancel(this, nameof(RunPurchasePhase));
        }

        /// <summary>
        /// 판매 페이즈 로직 처리
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask RunSellPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;
            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );

            Managers.Token.Cancel(this, nameof(RunSellPhase));
        }

        /// <summary>
        /// 라운드 랭킹 결과 페이즈 로직 처리
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTask RunRoundReleaseRankingPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned) return;
            CalculateTax(); // 세금 계산
            StockManager.Instance.UpdateStock(); // 주가 변경

            // 세금/주가 반영 후 랭킹 계산
            var rankingList = CalculateRankingScore();
            var json = JsonUtility.ToJson(new StockRankingWrapper { Rankings = rankingList });
            BroadcastReleaseRankingRpc(json); // 전파

            StartTimerAsync(CurrentRound.Value.RemainingTime, token).Forget();
            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(RunRoundReleaseRankingPhase));
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
                    await UniTask.WaitForSeconds(1f, cancellationToken: token);
                    CurrentTime.Value--;
                }
            }
            catch (OperationCanceledException) { }
        }
        private async UniTaskVoid TriggerOnMinuteBoundary(int totalTime, CancellationToken updatePriceToken, CancellationToken supplyBoxToken, CancellationToken phaseToken)
        {
            int nextTriggerTime = totalTime - 60;

            while (nextTriggerTime > 0 && !phaseToken.IsCancellationRequested)
            {
                // currentTime이 정확히 분 경계에 도달할 때까지 대기
                await UniTask.WaitUntil(
                    () => currentTime.Value <= nextTriggerTime,
                    cancellationToken: phaseToken
                );

                if (phaseToken.IsCancellationRequested) break;

                if (!updatePriceToken.IsCancellationRequested)
                    StockManager.Instance.UpdateStock();

                if (!supplyBoxToken.IsCancellationRequested)
                    SpawnSupplyBox();
                nextTriggerTime -= 60;
            }
        }
        #endregion

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
        #endregion

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
        private void CalculateTax()
        {
            if (!IsServer || !IsSpawned) return;
            var currentTax = StockManager.Instance.CurrentTax.Value;
            foreach (var joinInfo in joinInfos)
            {
                var taxedMoney = joinInfo.Money * currentTax;
                var floored = (long)(Mathf.Floor(taxedMoney / 100f) * 100f);
                Debug.Log($"세금 반영 금액 : {taxedMoney} | 100의 자리 수 까지 내림 처리 : {floored}");
                UpdateMoneyByClientId(joinInfo.ClientId, (long)Mathf.Max(0, joinInfo.Money - floored));
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastCountdownSFXRpc()
        {
            PlayCountdownSFXAsync().Forget();
        }

        private async UniTask PlayCountdownSFXAsync()
        {
            if (currentRound.Value.RoundPhase != RoundPhase.Explore) return;
            var token = Managers.Token.GetToken(this, nameof(PlayCountdownSFXAsync));
            try
            {
                int lastPlayedCount = -1;

                while (!token.IsCancellationRequested)
                {
                    // 라운드가 바뀌거나 Explore 페이즈가 끝나면 즉시 중단
                    if (currentRound.Value.RoundPhase != RoundPhase.Explore) return;

                    int displayCount = currentTime.Value;

                    if (displayCount > 4 || displayCount < 0)
                    {
                        await UniTask.Yield(cancellationToken: token);
                        continue;
                    }

                    if (displayCount != lastPlayedCount)
                    {
                        lastPlayedCount = displayCount;

                        var eventInstance = RuntimeManager.CreateInstance(ResourceDefine.FMODEvent.SFX265);
                        eventInstance.setParameterByName("Countdown", displayCount);
                        eventInstance.start();
                        eventInstance.release();

                        if (displayCount == 0) return; // 0까지 재생했으면 카운트다운 종료
                    }

                    await UniTask.Yield(cancellationToken: token);
                }
            }
            catch (OperationCanceledException)
            {
                // 라운드 종료/전환으로 정상 취소된 경우 — 별도 처리 불필요
            }
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastRoundFinishRpc()
        {
            onRoundFinish?.OnNext(Unit.Default);
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastExploreFinishRpc()
        {
            UIManager.Instance.Clear();
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastNotificationRpc(string contextsCombined)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX266);
            UniTask.Void(async () =>
            {
                var popup = await Managers.UI.Open<NotificationUI>(UIDefine.UILayer.Popup);
                popup.Initilaize(48, UIDefine.UILayer.Popup);
                int tax = Mathf.RoundToInt(StockManager.Instance.CurrentTax.Value * 100f);
                popup.SetTaxText(tax);
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX263);
                await UniTask.WaitForSeconds(10f, cancellationToken: popup.destroyCancellationToken);
                await popup.HideAsync(1.0f, false);

                if (CurrentRound.Value.RoundIndex < 2) return;

                var contexts = contextsCombined.Split('|');
                foreach (var context in contexts)
                {
                    var newsPopup = await Managers.UI.Open<NewsUI>(UIDefine.UILayer.Popup);
                    newsPopup?.Initilaize(48, UIDefine.UILayer.Popup);
                    newsPopup.ShowNews(context);
                    Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX264);
                    await UniTask.WaitForSeconds(5f, cancellationToken: newsPopup.destroyCancellationToken);
                    newsPopup.Close();
                }
            });
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastShowSupplyNotificationRpc(string zoneName)
        {
            UniTask.Void(async () =>
            {
                var popup = await Managers.UI.Open<NotificationUI>(UIDefine.UILayer.Popup);
                popup?.Initilaize(49, UIDefine.UILayer.Popup);
                popup.SetSupplyBoxText(zoneName);
                Managers.Sound.PlaySfx(ResourceDefine.FMODEvent.SFX263);
                await UniTask.WaitForSeconds(10f, cancellationToken: popup.destroyCancellationToken);
                await popup.HideAsync(1f, false);
            });
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastReleaseRankingRpc(string rankingJson)
        {
            var rankingList = JsonUtility.FromJson<StockRankingWrapper>(rankingJson).Rankings;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX267);
            onReleaseRanking?.OnNext(rankingList);
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastFinalRoundEndRpc(string rankingJson)
        {
            var rankingList = JsonUtility.FromJson<StockRankingWrapper>(rankingJson).Rankings;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX268);
            onFinalRoundEnded?.OnNext(rankingList);
        }


        [ServerRpc(RequireOwnership = false)]
        private void NotifyPhaseDoneServerRpc(ServerRpcParams rpcParams = default)
        {
            _phaseDoneClients.Add(rpcParams.Receive.SenderClientId);
        }

        /// <summary>
        /// 특정 유저에게 글 작성을 UI를 열게 함
        /// </summary>
        /// <param name="param"></param>
        [Rpc(SendTo.SpecifiedInParams)]
        private void NotifyWinnerNewsWriteRpc(RpcParams param)
        {
            UniTask.Void(async (ct) =>
            {
                await UniTask.WaitUntil(() => currentRound.Value.RoundPhase == RoundPhase.StockInfo, cancellationToken: ct);
                var popup = await Managers.UI.Open<NewsUI>(UIDefine.UILayer.Popup);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                    popup.destroyCancellationToken,
                    ct
                );

                // StockInfo 페이즈가 끝나면 (RoundPhase가 변경되면) 자동 닫힘
                await UniTask.WaitUntil(
                    () => currentRound.Value.RoundPhase != RoundPhase.StockInfo,
                    cancellationToken: cts.Token
                );

                popup?.Close();

            }, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

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

        private void SpawnSupplyBox(CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return;
            var zones = ZoneManager.Instance.GetRandomZones(1, new List<ZoneType>(1) { ZoneType.Port, ZoneType.Bunker });
            foreach (var zone in zones)
            {
                var entrance = zone.GetEntrance;
                SpawnManager.Instance.Spawn(nameof(SupplyBox), new Vector3(entrance.position.x, entrance.position.y, 0), Quaternion.identity, NetworkManager.ServerClientId);
                BroadcastShowSupplyNotificationRpc(zone.GetZoneName());
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void RegistNewsRpc(string context, RpcParams param = default)
        {
            var clientId = param.Receive.SenderClientId;
            newsContextList?.Add((clientId, context));
        }

        /// <summary>
        /// 주가 + 소지금을 계산하여 랭킹 계산 함수
        /// </summary>
        /// <returns></returns>
        public List<StockRanking> CalculateRankingScore()
        {
            var profitList = new List<StockRanking>();
            foreach (var player in joinInfos)
            {
                var temp = player;
                long totalMoney = StockManager.Instance.CalculateTotalStock(player.ClientId); // 전체 주식 계산
                profitList.Add(new StockRanking
                {
                    Ranking = 0,
                    PlayerIndex = temp.Index,
                    ClientId = temp.ClientId,
                    Profit = totalMoney + player.Money,
                    CharacterType = player.CharacterType,
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

                profitList[i] = rankingData;
            }

            var winnerList = profitList.Where(x => x.Ranking == 1);
            foreach (var winner in winnerList)
                currentWinnerList.Add(winner.ClientId);

            return profitList;
        }

        /// <summary>
        /// 직업 배정 후 JobType을 직접 각 클라이언트에 전달
        /// joinInfos 동기화 타이밍 문제 없이 즉시 미션 배정 가능
        /// </summary>
        private void AssignJobsAndMissions()
        {
            if (!IsServer) return;

            int playerCount = joinInfos.Count;
            JobManager.Instance.PrepareQueueForRound(playerCount);

            foreach (var playerData in joinInfos)
            {
                var newJob = JobManager.Instance.GetJobByRandom();
                if (!newJob.IsValid)
                {
                    Debug.LogError("Job 정보가 없다");
                    return;
                }
                UpdateJobInfo(newJob, playerData.ClientId);

                // joinInfos 동기화를 기다리지 않고 JobType을 직접 해당 클라이언트에 전달
                MissionManager.Instance.RequestMissionRpc(
                    newJob.JobType,
                    RpcTarget.Single(playerData.ClientId, RpcTargetUse.Temp)
                );
            }
        }

        #region 유저 나갈 시 처리
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

        public void RemovePlayerLocalStates(ulong clientId)
        {
            if (!IsServer) return;
            currentWinnerList.Remove(clientId);
            newsContextList.RemoveAll(x => x.clientId == clientId);
            _readyClients.Remove(clientId);
            _phaseDoneClients.Remove(clientId);

            HandlePlayerLeftInMainScene();
        }

        /// <summary>
        /// MainScene에서 유저가 나갔을 때의 공통 처리.
        /// 1) 남은 인원이 1명 이하면, Ready 대기 중이든 게임 플레이 중이든 무조건 로비로 강제 복귀.
        ///    (혼자 남은 상태로 게임을 시작/진행시키는 것을 최우선으로 차단)
        /// 2) 남은 인원이 2명 이상이고, 아직 게임 시작 전(Ready 대기 중)이라면
        ///    퇴장으로 인해 줄어든 total 기준으로 즉시 재평가하여 로딩 화면이 멈추는 것을 방지.
        /// 3) 남은 인원이 2명 이상이고 이미 게임이 진행 중이라면 별도 처리 없이 그대로 진행.
        /// </summary>
        private void HandlePlayerLeftInMainScene()
        {
            if (NetworkSceneManager.Instance.CurrentSceneId != SceneEnum.MainScene) return;

            int remaining = NetworkManager.ConnectedClients.Count;
            Debug.Log($"[GameManager] MainScene 유저 퇴장 → 남은 인원: {remaining}, isFirstStart: {isFirstStart}");

            // 1) 인원 부족은 게임 시작 여부와 무관하게 최우선으로 처리
            if (remaining <= 1)
            {
                Debug.Log("[GameManager] 남은 인원 1명 이하 → 강제로 로비씬으로 이동");
                _readyClients.Clear();
                ForceReturnToLobby().Forget();
                return;
            }

            // 2) 아직 게임이 시작되지 않았다면(Ready 대기 단계) 줄어든 total 기준으로 재평가
            if (!isFirstStart && _readyClients.Count > 0)
            {
                Debug.Log($"[GameManager] 유저 퇴장으로 Ready 카운트 재평가: {_readyClients.Count} / {remaining}");

                if (_readyClients.Count >= remaining)
                {
                    _readyClients.Clear();
                    Debug.Log("[GameManager] 퇴장 처리 후 남은 클라이언트 모두 준비 완료 → 게임 시작");

                    BuildJoinInfosFromLobby();
                    BroadcastGameReadyRpc();
                    RunGameFlow().Forget();
                }
            }

            // 3) 이미 게임 진행 중이고 인원이 2명 이상이면 별도 처리 없음
        }

        private async UniTask ForceReturnToLobby()
        {
            if (!IsServer || !IsSpawned) return;

            Managers.Token.CancelAll(this);

            isFirstStart = false;
            joinInfos?.Clear();
            onGameFinish?.OnNext(Unit.Default);

            JobManager.Instance.ResetAllocateQueue();
            isGameStart.Value = false;

            await NetworkSceneManager.Instance.ChangeScene(SceneEnum.LobbyScene, useNetworkSceneManager: true);
        }
        #endregion 유저 나갈 시 처리

        // JsonUtility는 List 직렬화를 직접 못하므로 wrapper 필요
        [Serializable]
        private class StockRankingWrapper
        {
            public List<StockRanking> Rankings;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Managers.Token.CancelAll(this);

            currentTime?.Dispose();
            currentRound?.Dispose();

            onGameStart?.Dispose();
            onGameFinish?.Dispose();
            onRoundFinish?.Dispose();
            onReleaseRanking?.Dispose();

            onGameStart = null;
            onGameFinish = null;
            onRoundFinish = null;
            onReleaseRanking = null;
            onPlayerSkip = null;
        }
    }
}