using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.UI;
using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    public class MultiplayManager : MonoSingleton<MultiplayManager>
    {
        private int maxUserCount = 2;
        public int MaxUserCount
        {
            get => maxUserCount;
            set
            {
                maxUserCount = value;
                maxUserCount = Mathf.Clamp(maxUserCount, 2, 8);
            }
        }
        public string Nickname = string.Empty;
        public string JoinCode = string.Empty;

        public NetworkManager Network => NetworkManager.Singleton;

        private bool isInitialize = false;
        private UniTask? _initTask = null; // 진행 중인 초기화 태스크 캐싱

        private async UniTask EnsureInitializedAsync()
        {
            if (isInitialize) return;

            // 이미 초기화 중이면 같은 태스크 대기
            if (_initTask.HasValue)
            {
                await _initTask.Value;
                return;
            }

            _initTask = InitializeCoreAsync();
            await _initTask.Value;
            _initTask = null;
        }

        private async UniTask InitializeCoreAsync()
        {
            try
            {
                // 이미 초기화된 경우 스킵
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log("Player signed in successfully.");
                }
                else
                {
                    Debug.Log("Player is already signed in.");
                }

                Network.ConnectionApprovalCallback = OnConnectionApprovalCallback;
                isInitialize = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Initialization failed: {e.Message}");
                throw;
            }

        }

        private async UniTask<Allocation> CreateRelayData(int maxConnections)
        {
            try
            {
                await EnsureInitializedAsync();

                MaxUserCount = maxConnections;
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                return allocation;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to allocate Relay server: {ex.Message}");
                throw;
            }
        }

        public async UniTask<bool> CreateRelayServer(int maxConnections, string userName)
        {
            try
            {
                var allocation = await CreateRelayData(maxConnections);

                if (Network.NetworkConfig.NetworkTransport is not UnityTransport transport)
                    return false;

                Debug.Log($"[Host] ConnectionApproval: {Network.NetworkConfig.ConnectionApproval}");

                transport.SetRelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                Nickname = userName;
                LogNetworkConfig();

                var started = Network.StartHost();
                Debug.Log($"[Host] StartHost 결과: {started}");
                return started;
            }
            catch (RelayServiceException ex)
            {
                Debug.LogError(ex);
            }
            return false;
        }

        /* Client */
        public async UniTask<bool> JoinToRelayServer(string joincode, string userName)
        {
            try
            {
                await EnsureInitializedAsync();

                if (Network.IsHost)
                {
                    Debug.LogError("Host cannot join relay");
                    return false;
                }

                Debug.Log($"Check Join Code : {joincode}");

                var client = await RelayService.Instance.JoinAllocationAsync(joincode);
                Debug.Log($"[Join] JoinAllocation 성공 - AllocationId: {client.AllocationId}");
                Network.NetworkConfig.ConnectionApproval = true;

                if (Network.NetworkConfig.NetworkTransport is not UnityTransport transport)
                {
                    Debug.LogError("Invalid Transport");
                    return false;
                }

                transport.SetRelayServerData(
                    client.RelayServer.IpV4,
                    (ushort)client.RelayServer.Port,
                    client.AllocationIdBytes,
                    client.Key,
                    client.ConnectionData,
                    client.HostConnectionData
                );

                Nickname = userName;
                JoinCode = joincode;

                LogNetworkConfig();

                if (!Network.StartClient())
                    return false;

                var result = await WaitForConnectionAsync();

                if (!result.success)
                {
                    var alert = await UIManager.Instance.Open<UI_AlertView>(Define.GameDefine.UIDefine.UILayer.Popup);
                    alert.SetAlert(string.IsNullOrEmpty(result.reason) ? "연결에 실패했습니다." : result.reason);
                }

                return result.success;
            }
            catch (RelayServiceException e)
            {
                var alert = await UIManager.Instance.Open<UI_AlertView>(Define.GameDefine.UIDefine.UILayer.Popup);
                alert.SetAlert(e.Reason.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return false;
        }

        private async UniTask<(bool success, string reason)> WaitForConnectionAsync(float timeoutSeconds = 10f)
        {
            bool connected = false;
            bool failed = false;
            string disconnectReason = string.Empty;

            // Client 본인이 연결됐을 때
            void OnConnected(ulong clientId)
            {
                if (clientId == Network.LocalClientId)
                {
                    Debug.Log($"[Join] 연결 성공 - ClientId: {clientId}");
                    connected = true;
                }
            }

            // 연결 거부 or 끊김
            void OnDisconnected(ulong clientId)
            {
                if (clientId == Network.LocalClientId)
                {
                    disconnectReason = Network.DisconnectReason;
                    Debug.Log($"[Join] 연결 실패 - Reason: {disconnectReason}");
                    failed = true;
                }
            }

            Network.OnClientConnectedCallback += OnConnected;
            Network.OnClientDisconnectCallback += OnDisconnected;

            try
            {
                var token = Managers.Token.GetToken(this, nameof(WaitForConnectionAsync));
                await UniTask.WaitUntil(() => connected || failed, cancellationToken: token)
                    .Timeout(TimeSpan.FromSeconds(timeoutSeconds));

                return (connected, disconnectReason);
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("Connection timed out");
                Managers.Token.Cancel(this, nameof(WaitForConnectionAsync));
                Network.Shutdown();
                
                return (false, "연결 시간이 초과됐습니다.");
            }
            finally
            {
                Network.OnClientConnectedCallback -= OnConnected;
                Network.OnClientDisconnectCallback -= OnDisconnected;
            }
        }

        private void OnConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            Debug.Log("[Approval] 콜백 호출됨");

            if (!Network.IsServer)
            {
                Debug.Log("[Approval] IsServer false - 리턴");
                return;
            }

            int currentUserCount = Network.ConnectedClientsList.Count;
            Debug.Log($"[Approval] 현재 인원: {currentUserCount}, 최대: {maxUserCount}");

            if (currentUserCount >= maxUserCount)
            {
                response.Approved = false;
                response.Reason = "방이 가득 찼습니다.";
                Debug.Log("[Approval] 거부 - 방 가득참");
            }
            else
            {
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.PlayerPrefabHash = null;
                response.Position = Vector3.zero;
                response.Rotation = Quaternion.identity;
                response.Pending = false;
                Debug.Log("[Approval] 승인됨");
            }
        }

        private void LogNetworkConfig()
        {
            var config = Network.NetworkConfig;
            Debug.Log($"[NetworkConfig] " +
                $"ConnectionApproval: {config.ConnectionApproval} | " +
                $"ProtocolVersion: {config.ProtocolVersion} | " +
                $"TickRate: {config.TickRate} | " +
                $"ClientConnectionBufferTimeout: {config.ClientConnectionBufferTimeout} | " +
                $"EnableSceneManagement: {config.EnableSceneManagement} | " +
                $"RecycleNetworkIds: {config.RecycleNetworkIds} | " +
                $"NetworkIdRecycleDelay: {config.NetworkIdRecycleDelay} | " +
                $"EnsureNetworkVariableLengthSafety: {config.EnsureNetworkVariableLengthSafety} | " +
                $"ForceSamePrefabs: {config.ForceSamePrefabs} | " +
                $"PlayerPrefab: {config.PlayerPrefab}"
            );
        }
    }
}