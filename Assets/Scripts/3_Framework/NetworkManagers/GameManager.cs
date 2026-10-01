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
    public partial class GameManager : NetworkSingleton<GameManager>
    {
        private enum PhaseTransitionType
        {
            FadeIn,
            FadeOut
        }

        #region Local Field
        private bool isGameFlowRunning = false;
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

        public NetworkVariable<int> SkipIndex;
        #endregion Global Field

        #region GAME_CORE_EVENT

        private Subject<Unit> onGameStart = new();
        private Subject<Unit> onGameFinish = new();
        private Subject<List<StockRanking>> onReleaseRanking = new();
        private Subject<Unit> onRoundFinish = new();
        private Subject<List<StockRanking>> onFinalRoundEnded = new();
        private Subject<int> onSkip = new();
        private Subject<Unit> onFinishExplore = new();
        public IObservable<Unit> OnGameStart => onGameStart;
        public IObservable<Unit> OnGameFinish => onGameFinish;
        public IObservable<List<StockRanking>> OnReleaseRanking => onReleaseRanking;
        public IObservable<Unit> OnRoundFinish => onRoundFinish;
        public IObservable<List<StockRanking>> OnFinalRoundEnded => onFinalRoundEnded;
        public IObservable<int> OnSkip => onSkip;
        public IObservable<Unit> OnFinishExplore => onFinishExplore;

        private Action onPlayerSkip = null;
        #endregion

        #region JoinInfo_Update_Method

        /// <summary>
        /// 서버가 클라이언트의 직업을 직접적으로 변경
        /// </summary>
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

            // 동시에 Player 데이터도 변경
            client.PlayerObject.GetComponent<PlayerNetwork>()?.InitializeJobRpc(jobInfo);
        }

        /// <summary>
        /// 소지금 정보 변경
        /// </summary>
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
        public void UpdateMoneyByClientId(ulong clientId, long money)
        {
            var info = GetPlayerInfoByClientId(clientId);
            var index = joinInfos.IndexOf(info);
            if (index < 0) return;
            info.Money = money;
            joinInfos[index] = info;
        }

        /// <summary>
        /// 소지금 추가
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void AddMoneyRpc(long money, RpcParams rpcParams = default)
        {
            var ownerId = rpcParams.Receive.SenderClientId;
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.Money += money;
            joinInfos[index] = localPlayerInfo;
        }

        /// <summary>
        /// Coin 정보 업데이트
        /// </summary>
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
        /// 코인 추가
        /// </summary>
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

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void AddCoinTargetRpc(int coin, ulong targetOwnerId, RpcParams rpcParams = default)
        {
            var ownerId = targetOwnerId;
            var localPlayerInfo = GetPlayerInfoByClientId(ownerId);
            var index = joinInfos.IndexOf(localPlayerInfo);
            if (index < 0) return;
            localPlayerInfo.Coin += coin;
            joinInfos[index] = localPlayerInfo;
        }

        /// <summary>
        /// ClientId를 타겟으로 코인 변경
        /// </summary>
        public void UpdateCoinByClientId(ulong clientId, int coin)
        {
            var info = GetPlayerInfoByClientId(clientId);
            var index = joinInfos.IndexOf(info);
            if (index < 0) return;
            info.Coin = coin;
            joinInfos[index] = info;
        }

        [Rpc(SendTo.Server)]
        public void UpdateCoinByClientIdRpc(ulong clientId, int coin)
        {
            var info = GetPlayerInfoByClientId(clientId);
            var index = joinInfos.IndexOf(info);
            if (index < 0) return;
            info.Coin = coin;
            joinInfos[index] = info;
        }

        #endregion JoinInfo_Update_Method

        #region LIFECYCLE
        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
                currentTime.Value = 1;

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
                UpdateCoinByClientId(NetworkManager.Singleton.LocalClientId, 1000);
#endif
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

            Managers.UI.ClearNotify();
            Managers.Token.CancelAll(this);
        }

        #endregion

        public override UniTask Initialize()
        {
            currentRound = new(writePerm: NetworkVariableWritePermission.Server);
            currentTime = new(writePerm: NetworkVariableWritePermission.Server);
            joinInfos = new(writePerm: NetworkVariableWritePermission.Server);
            isGameStart = new NetworkVariable<bool>(false, writePerm: NetworkVariableWritePermission.Server);
            SkipIndex = new(0, writePerm: NetworkVariableWritePermission.Server);
            return base.Initialize();
        }

        // 게임 참가중인 로컬 플레이어 정보 반환
        public NetworkPlayerJoinInfo GetLocalPlayerInfo()
        {
            var localClientId = NetworkManager.LocalClientId;
            return GetPlayerInfoByClientId(localClientId);
        }

        // ClientId로 참가중인 플레이어 정보 반환
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

        #region GAME_FLOW
        public void GameStart()
        {
            if (!IsServer) return;
            if (isGameStart.Value)
            {
                Debug.LogWarning("[GameManager] GameStart 중복 호출 무시 (이미 게임 시작 상태)");
                return;
            }

            isGameStart.Value = true;

            _readyClients.Clear();
            _phaseDoneClients.Clear();
            BuildJoinInfosFromLobby();
            StockManager.Instance?.InitializeByGameStart();
            BroadcastGameStartRpc();
        }

        public async UniTask GameEnd(CancellationToken token = default)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;

            joinInfos?.Clear();
            onGameFinish?.OnNext(Unit.Default);

            JobManager.Instance.ResetAllocateQueue();
            await UniTask.WaitForSeconds(1f, cancellationToken: token);
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;

            SetPlayerControlEnabledRpc(true);
            isGameStart.Value = false;
            await NetworkSceneManager.Instance.ChangeScene(SceneEnum.LobbyScene, useNetworkSceneManager: true);
        }

        private async UniTask ShowFinalResult(CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;
            var rankingList = CalculateRankingScore();
            var json = JsonUtility.ToJson(new StockRankingWrapper { Rankings = rankingList });
            BroadcastFinalRoundEndRpc(json);
            var timerToken = Managers.Token.GetToken(this, nameof(StartTimerAsync));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timerToken, token);
            StartTimerAsync(60, linkedCts.Token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(StartTimerAsync));
        }

        #endregion

        #region ROUND_PHASE
        /// <summary>
        /// 페이즈 별 라운드 정보 변경
        /// </summary>
        /// <param name="token">RunGameFlow에서부터 관통되어 내려오는 MainScene 세션 토큰</param>
        private async UniTask SetRoundInfo(int roundIndex, RoundPhase phase, int remainingTime, CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;
            if (!isFirstStart)
            {
                await WaitFadeIn(token);
                if (token.IsCancellationRequested) return;
            }

            var round = currentRound.Value;
            round.RoundIndex = roundIndex;
            round.RoundPhase = phase;
            round.RemainingTime = remainingTime;
            currentRound.Value = round;

            if (!isFirstStart)
                PhaseTransitionRpc(PhaseTransitionType.FadeOut);
            else
                isFirstStart = false;
        }

        private async UniTask RunRoundInfoPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;
            var timerToken = Managers.Token.GetToken(this, nameof(StartTimerAsync));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timerToken, token);
            StartTimerAsync(CurrentRound.Value.RemainingTime, linkedCts.Token).Forget();
            await UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token);
            Managers.Token.Cancel(this, nameof(StartTimerAsync));
        }

        /// <summary>
        /// 주식 정보 페이즈 로직 처리
        /// </summary>
        private async UniTask RunStockInfoPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;

            AssignJobsAndMissions();
            StockManager.Instance.ChangeTaxByRandom();

            Debug.Log("RoundIndex: " + CurrentRound.Value.RoundIndex);
            if (CurrentRound.Value.RoundIndex > 1)
            {
                foreach (var winner in currentWinnerList)
                    NotifyWinnerNewsWriteRpc(RpcTarget.Single(winner, RpcTargetUse.Temp));
            }
            var timerToken = Managers.Token.GetToken(this, nameof(StartTimerAsync));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timerToken, token);
            StartTimerAsync(CurrentRound.Value.RemainingTime, linkedCts.Token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );

            Managers.Token.Cancel(this, nameof(StartTimerAsync));
        }

        /// <summary>
        /// 파밍 탐험 페이즈 로직 처리
        /// </summary>
        private async UniTask RunExplorePhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;
            var updatePriceToken = Managers.Token.GetToken(this, "UpdateStockInfo");
            var supplyBoxToken = Managers.Token.GetToken(this, nameof(SpawnSupplyBox));
            SetPlayerControlEnabledRpc(true);
            RPCManager.Instance.StartMissionRpc();

            newsContextList.Sort((a, b) => b.clientId.CompareTo(a.clientId));
            var contexts = string.Join("|", newsContextList.Select(n => n.newsContext));
            BroadcastNotificationRpc(contexts); // 세금 알림 + 비틱 게시판 알림
            BroadcastCountdownSFXRpc();
            int totalTime = CurrentRound.Value.RemainingTime;
            var timerToken = Managers.Token.GetToken(this, nameof(StartTimerAsync));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timerToken, token);
            StartTimerAsync(totalTime, linkedCts.Token).Forget();

            TriggerOnMinuteBoundary(totalTime, updatePriceToken, supplyBoxToken, token).Forget();
            BroadcastSpawnBroker(totalTime, token);
            await UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token);
            Managers.Token.Cancel(this, "UpdateStockInfo");
            Managers.Token.Cancel(this, nameof(SpawnSupplyBox));
            Managers.Token.Cancel(this, nameof(StartTimerAsync));
            Managers.Token.Cancel(this, nameof(BroadcastNotificationRpc));

            if (token.IsCancellationRequested) return;
            SetPlayerControlEnabledRpc(false);
        }

        /// <summary>
        /// 구매 페이즈 로직 처리
        /// </summary>
        private async UniTask RunPurchasePhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;
            var timerToken = Managers.Token.GetToken(this, nameof(StartTimerAsync));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timerToken, token);
            StartTimerAsync(CurrentRound.Value.RemainingTime, linkedCts.Token).Forget();

            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(StartTimerAsync));
        }

        /// <summary>
        /// 라운드 랭킹 결과 페이즈 로직 처리
        /// </summary>
        private async UniTask RunRoundReleaseRankingPhase(CancellationToken token)
        {
            if (!IsServer || !IsSpawned || token.IsCancellationRequested) return;
            CalculateTax();
            StockManager.Instance.UpdateStock();

            // 세금/주가 반영 후 랭킹 계산
            var rankingList = CalculateRankingScore();
            var json = JsonUtility.ToJson(new StockRankingWrapper { Rankings = rankingList });
            BroadcastReleaseRankingRpc(json);
            var timerToken = Managers.Token.GetToken(this, nameof(StartTimerAsync));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timerToken, token);
            StartTimerAsync(CurrentRound.Value.RemainingTime, linkedCts.Token).Forget();
            await UniTask.WhenAny(
                UniTask.WaitUntil(() => currentTime.Value <= 0, cancellationToken: token),
                WaitAllPlayersSkip(token)
            );
            Managers.Token.Cancel(this, nameof(StartTimerAsync));
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
                await UniTask.WaitUntil(() => currentTime.Value <= nextTriggerTime, cancellationToken: phaseToken);
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
                BroadcastSkipRpc(skipCount);
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

        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastSkipRpc(int skipCount)
        {
            onSkip?.OnNext(skipCount);
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

        #region Game Event Flow (Broker, News, SupplyBox etc...)
        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastGameStartRpc()
        {
            onGameStart?.OnNext(Unit.Default);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastCountdownSFXRpc()
        {
            PlayCountdownSFXAsync().Forget();
        }

        private void BroadcastSpawnBroker(int totalTime, CancellationToken token)
        {
            var exploreTime = mapData.ExploreTime;
            var brokerAppearanceTime = exploreTime * 60 * 0.5f;

            bool overlapsWithMinuteBoundary = Mathf.Approximately((totalTime - brokerAppearanceTime) % 60f, 0f);

            UniTask.Void(async (ct) =>
            {
                await UniTask.WaitUntil(() => currentTime.Value <= brokerAppearanceTime, cancellationToken: ct);
                if (!IsServer || !IsSpawned) return;

                if (overlapsWithMinuteBoundary)
                {
                    // 겹치는 틱에서만, 보급 상자 쪽 RPC가 먼저 전송된 뒤 브로커 RPC가 나가도록 한 프레임 양보
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
                }

                var zones = ZoneManager.Instance.GetRandomZones(2, new List<ZoneType>() { ZoneType.PoliceOffice, ZoneType.Port });
                if (zones == null || zones.Count == 0) return;

                var zoneTypes = zones.Select(z => (int)z.ZoneType).ToArray();
                var combinedZoneNames = string.Join(", ", zones.Select(z => z.GetZoneName()));

                BroadcastActivateBrokerRpc(zoneTypes, combinedZoneNames); // 이 RPC 전송이 늦춰짐
            }, cancellationToken: token);
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastActivateBrokerRpc(int[] zoneTypes, string combinedZoneNames)
        {
            foreach (var typeValue in zoneTypes)
            {
                var zone = ZoneManager.Instance.GetZoneByType((ZoneType)typeValue);
                if (zone == null) continue;
                var brokerObject = zone.GetBrokerObject;
                if (brokerObject == null) continue;

                brokerObject.gameObject.SetActive(true);
                brokerObject.Initialize();
            }

            Managers.UI.Notify(new NotificationContext(NotificationType.Broker, combinedZoneNames));
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

                        if (displayCount == 0) return;
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
            onFinishExplore?.OnNext(Unit.Default);
            UIManager.Instance.Clear();
        }

        /// <summary>
        /// Explore 페이즈 시 알림 흐름 묶음 처리.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastNotificationRpc(string contextsCombined)
        {
            Managers.Sound.PlaySfx(ResourceDefine.FMODEvent.SFX266);
            var token = Managers.Token.GetToken(this, nameof(BroadcastNotificationRpc));

            UniTask.Void(async (ct) =>
            {
                try
                {
                    var tax = Mathf.RoundToInt(StockManager.Instance.CurrentTax.Value * 100f).ToString();
                    Managers.UI.Notify(new NotificationContext(NotificationType.Tax, tax));

                    if (CurrentRound.Value.RoundIndex < 2) return; // 라운드 2 미만은 스킵

                    await UniTask.WaitForSeconds(
                        NotificationUI.NotificationDelay + NotificationUI.FadeOutDuration,
                        cancellationToken: token);
                    if (token.IsCancellationRequested) return;

                    var contexts = contextsCombined.Split('|');

                    var newsPopup = await Managers.UI.Open<NewsUI>(UIDefine.UILayer.Alert);
                    if (newsPopup == null) return;

                    if (token.IsCancellationRequested)
                    {
                        newsPopup.Close();
                        return;
                    }

                    using var newsToken = CancellationTokenSource.CreateLinkedTokenSource(
                        newsPopup.destroyCancellationToken, token);

                    foreach (var context in contexts)
                    {
                        newsPopup.Show();
                        newsPopup.ShowNews(context);
                        Managers.Sound.PlaySfx(ResourceDefine.FMODEvent.SFX264);
                        await UniTask.WaitForSeconds(NewsUI.NewsDisplayDuration, cancellationToken: newsToken.Token);
                        await newsPopup.HideAsync(NewsUI.NewsFadeOutDuration, false);
                    }

                    newsPopup.Close();
                }
                catch (OperationCanceledException) { }
            }, cancellationToken: token);
        }

        [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
        private void BroadcastShowSupplyNotificationRpc(string zoneName)
        {
            Managers.UI.Notify(new NotificationContext(NotificationType.Supply, zoneName));
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
        /// 특정 유저에게 글 작성을 UI를 열게 함.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void NotifyWinnerNewsWriteRpc(RpcParams param)
        {
            var token = Managers.Token.GetToken(this, nameof(NotifyWinnerNewsWriteRpc));

            UniTask.Void(async (ct) =>
            {
                try
                {
                    await UniTask.WaitUntil(() => currentRound.Value.RoundPhase == RoundPhase.StockInfo, cancellationToken: token);
                    if (token.IsCancellationRequested) return;

                    var popup = await Managers.UI.Open<NewsUI>(UIDefine.UILayer.Alert);
                    if (popup == null) return;
                    popup?.Initilaize(41, UIDefine.UILayer.Alert);
                    if (token.IsCancellationRequested)
                    {
                        popup.Close();
                        return;
                    }

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                        popup.destroyCancellationToken,
                        token
                    );

                    // StockInfo 페이즈가 끝나면 (RoundPhase가 변경되면) 자동 닫힘
                    await UniTask.WaitUntil(
                        () => currentRound.Value.RoundPhase != RoundPhase.StockInfo,
                        cancellationToken: cts.Token
                    );

                    popup?.Close();
                }
                catch (OperationCanceledException) { }
            }, cancellationToken: token);
        }
        private void SpawnSupplyBox(CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return;
            var zones = ZoneManager.Instance.GetRandomZones(1, new List<ZoneType>(1) { ZoneType.Port, ZoneType.Bunker });
            foreach (var zone in zones)
            {
                var entrance = zone.GetEntrance;
                SpawnManager.Instance.Spawn(nameof(SupplyBox), new Vector3(entrance.position.x, entrance.position.y, 0), Quaternion.identity, NetworkManager.ServerClientId);
                BroadcastShowSupplyNotificationRpc(zone.GetZoneKey());
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void RegistNewsRpc(string context, RpcParams param = default)
        {
            var clientId = param.Receive.SenderClientId;
            newsContextList?.Add((clientId, context));
        }
        #endregion  Game Event Flow (Broker, News, SupplyBox etc...)

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
            foreach (var joinInfo in joinInfos) // 소지금이 1억 넘게 있을 경우 float로 들고 있을 시 발생할 버그 수정
            {
                long taxPercent = (long)Mathf.Round(currentTax * 100f);
                long tax = joinInfo.Money * taxPercent / 100L;
                tax = tax / 100L * 100L;
                UpdateMoneyByClientId(joinInfo.ClientId, Math.Max(0, joinInfo.Money - tax));
            }
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

        /// <summary>
        /// 주가 + 소지금을 계산하여 랭킹 계산 함수
        /// </summary>
        public List<StockRanking> CalculateRankingScore()
        {
            var profitList = new List<StockRanking>();
            foreach (var player in joinInfos)
            {
                var temp = player;
                long totalMoney = StockManager.Instance.CalculateTotalStock(player.ClientId);
                profitList.Add(new StockRanking
                {
                    Ranking = 0,
                    PlayerIndex = temp.Index,
                    ClientId = temp.ClientId,
                    Nickname = temp.Nickname.Value,
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
                    mapData.MissionCount,
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

        private void HandlePlayerLeftInMainScene()
        {
            if (NetworkSceneManager.Instance.CurrentSceneId != SceneEnum.MainScene) return;

            int remaining = NetworkManager.ConnectedClients.Count;
            Debug.Log($"[GameManager] MainScene 유저 퇴장 → 남은 인원: {remaining}, isFirstStart: {isFirstStart}");

            if (remaining <= 1)
            {
                Debug.Log("[GameManager] 남은 인원 1명 이하 → 강제로 로비씬으로 이동");
                _readyClients.Clear();
                ForceReturnToLobby().Forget();
                return;
            }

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
        }

        private async UniTask ForceReturnToLobby()
        {
            if (!IsServer || !IsSpawned) return;

            ForceStopGame();

            isFirstStart = false;
            joinInfos?.Clear();
            onGameFinish?.OnNext(Unit.Default);

            JobManager.Instance.ResetAllocateQueue();
            isGameStart.Value = false;

            await NetworkSceneManager.Instance.ChangeScene(SceneEnum.LobbyScene, useNetworkSceneManager: true);
        }

        public void ForceStopGame()
        {
            Managers.Sound.PlayStopAllSfxSound();
            Managers.Token.CancelAll(this);
            isFirstStart = false;
            isGameFlowRunning = false;
        }

        #endregion 유저 나갈 시 처리

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
            onSkip?.Dispose();
            onFinishExplore?.Dispose();

            onGameStart = null;
            onGameFinish = null;
            onRoundFinish = null;
            onReleaseRanking = null;
            onPlayerSkip = null;
            onSkip = null;
            onFinishExplore = null;
        }
    }
}