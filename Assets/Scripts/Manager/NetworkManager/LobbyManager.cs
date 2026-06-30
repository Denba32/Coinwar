using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Datas;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    public class LobbyManager : NetworkSingleton<LobbyManager>
    {
        public NetworkList<NetworkPlayerData> PlayerDataList;
        public NetworkVariable<FixedString32Bytes> JoinCode;

        public override UniTask Initialize()
        {
            PlayerDataList = new NetworkList<NetworkPlayerData>(default);
            JoinCode = new();

            disposables?.Add(PlayerDataList);
            disposables?.Add(JoinCode);
            return base.Initialize();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.OnClientStopped += OnClientStopped;
            if (!IsServer) return;
            PlayerDataList.Clear();
            JoinCode.Value = default;
            SetJoinCode(MultiplayManager.Instance.JoinCode);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Debug.Log("OnNetworkDespawn");
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.OnClientStopped -= OnClientStopped;
        }

        private void OnClientStopped(bool _)
        {
            Debug.Log("OnClientStopped");
            NetworkManager.Shutdown();
            NetworkSceneManager.Instance.ChangeScene(Define.SceneEnum.TitleScene, useNetworkSceneManager: false).Forget();
        }

        /// <summary>
        /// 유저가 접근 시도 시
        /// 퇴장한 유저의 빈 Index 자리를 우선적으로 채워서 배정한다.
        /// (Index는 캐릭터 프로필 이미지 리소스 경로에 사용되므로, 기존 유저의 Index를 절대 건드리지 않는다)
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="rpcParams"></param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SubmitPlayerDataRpc(string nickname, RpcParams rpcParams = default)
        {
            if (!IsOwner || !IsServer) return;
            var clientId = rpcParams.Receive.SenderClientId;

            var playerData = new NetworkPlayerData
            {
                Index = GetFirstEmptyIndex(),
                ClientId = clientId,
                NickName = nickname,
                CharacterType = CharacterType.Penguin
            };

            PlayerDataList?.Add(playerData);
        }

        /// <summary>
        /// 현재 사용 중이지 않은 가장 작은 Index를 반환한다.
        /// 캐릭터 프로필 이미지 리소스(CHA{type}{Index:D2})는 1번부터 존재하므로 1부터 탐색한다.
        /// 최대 인원(방 설정값) 범위 내에서 빈 자리를 찾고, 다 차있다면 Count+1을 그대로 반환한다.
        /// </summary>
        private int GetFirstEmptyIndex()
        {
            var maxPlayerCount = MultiplayManager.Instance.MaxUserCount; // 방 생성/설정 시 정해지는 최대 인원
            var usedIndexes = new HashSet<int>();
            for (int i = 0; i < PlayerDataList.Count; i++)
                usedIndexes.Add(PlayerDataList[i].Index);

            for (int i = 1; i <= maxPlayerCount; i++)
            {
                if (!usedIndexes.Contains(i))
                    return i;
            }

            Debug.LogWarning("[LobbyManager] 빈 Index를 찾지 못했습니다. 최대 인원을 초과했는지 확인하세요.");
            return PlayerDataList.Count + 1;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer)
            {
                if (!NetworkManager.IsConnectedClient) return;
                NetworkManager.Shutdown();
                var token = Managers.Token.GetToken(this, nameof(OnClientDisconnected));
                Managers.NetworkScene.ChangeScene(Define.SceneEnum.TitleScene, useNetworkSceneManager: false, token: token).Forget();
                return;
            }

            for (int i = 0; i < PlayerDataList.Count; i++)
            {
                if (PlayerDataList[i].ClientId == clientId)
                {
                    PlayerDataList.RemoveAt(i);
                    break;
                }
            }

            GameManager.Instance?.RemoveJoinInfo(clientId);
            GameManager.Instance?.RemovePlayerLocalStates(clientId);
            StockManager.Instance?.RemovePlayerStocks(clientId);
        }

        private void SetJoinCode(string joinCode)
        {
            JoinCode.Value = new FixedString32Bytes(joinCode);
        }

        public string GetJoinCode() => JoinCode.Value.ToString();

        public NetworkPlayerData GetPlayerData()
        {
            NetworkPlayerData playerData = default;
            var localClientId = NetworkManager.LocalClientId;
            for (int i = 0; i < PlayerDataList.Count; i++)
            {
                var data = PlayerDataList[i];
                if (data.ClientId == localClientId)
                {
                    playerData = data;
                }
            }
            return playerData;
        }

        public NetworkPlayerData GetPlayerDataByClientId(ulong clientId)
        {
            for (int i = 0; i < PlayerDataList.Count; i++)
            {
                if (PlayerDataList[i].ClientId == clientId)
                    return PlayerDataList[i];
            }
            return default;
        }

        public NetworkPlayerData GetPlayerDataByIndex(int index)
        {
            for (int i = 0; i < PlayerDataList.Count; i++)
            {
                if (PlayerDataList[i].Index == index)
                    return PlayerDataList[i];
            }
            return default;
        }
    }
}