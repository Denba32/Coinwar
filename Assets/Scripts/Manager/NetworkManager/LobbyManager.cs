using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Datas;
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
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="rpcParams"></param>
        [ServerRpc(RequireOwnership = false)]
        public void SubmitPlayerDataServerRpc(string nickname, ServerRpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;

            var playerData = new NetworkPlayerData
            {
                Index = PlayerDataList.Count + 1,
                ClientId = clientId,
                NickName = nickname,
                CharacterType = CharacterType.Penguin
            };

            PlayerDataList?.Add(playerData);
        }

        private void ReassignIndexes()
        {
            for (int i = 0; i < PlayerDataList.Count; i++)
            {
                var data = PlayerDataList[i];
                data.Index = i;
                PlayerDataList[i] = data;
            }
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
            ReassignIndexes();
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