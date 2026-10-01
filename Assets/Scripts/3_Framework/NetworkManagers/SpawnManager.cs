using Denba.Common;
using StockGame.Scripts.Objects;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    public class SpawnManager : NetworkSingleton<SpawnManager>
    {
        private Dictionary<string, GameObject> _prefabCache;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            BuildPrefabCache();
        }

        private void BuildPrefabCache()
        {
            _prefabCache = new Dictionary<string, GameObject>();

            var prefabs = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs;
            foreach (var p in prefabs)
            {
                if (p.Prefab != null && !_prefabCache.ContainsKey(p.Prefab.name))
                {
                    _prefabCache.Add(p.Prefab.name, p.Prefab);
                }
            }
        }

        private GameObject GetPrefab(string name)
        {
            if (!_prefabCache.TryGetValue(name, out var prefab))
            {
                Debug.LogError($"[SpawnManager] Prefab not found : {name}");
                return null;
            }
            return prefab;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestDespawnServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
        {
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var obj)) return;
            obj.Despawn(true);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DespawnAllOfClientServerRpc(ulong clientId)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                return;

            foreach (var obj in client.OwnedObjects.ToArray())
            {
                obj.Despawn(true);
            }
        }

        public NetworkObject Spawn(string prefabName, Vector3 pos, Quaternion rot, ulong ownerClientId, NetworkObject parent = null)
        {
            if (!IsServer)
            {
                Debug.LogError("[SpawnManager] Spawn can only be called on Server");
                return null;
            }

            var prefab = GetPrefab(prefabName);
            if (prefab == null)
                return null;

            var obj = Instantiate(prefab, pos, rot);
            var netObj = obj.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError($"[SpawnManager] NetworkObject missing on prefab : {prefabName}");
                Destroy(obj);
                return null;
            }

            // Owner 설정
            if (ownerClientId == NetworkManager.ServerClientId)
            {
                netObj.Spawn();
            }
            else
            {
                netObj.SpawnWithOwnership(ownerClientId);
            }

            return netObj;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestSpawnServerRpc(string prefabName, Vector3 pos, Quaternion rot, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            var senderId = rpcParams.Receive.SenderClientId;
            Spawn(prefabName, pos, rot, senderId);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void RequestSpawnBrokerRpc(Vector3 position)
        {
            var mafiaObject = Managers.Resource.Load<BrokerObject>("Object/BrokerObject");
            mafiaObject?.Initialize();
            mafiaObject.transform.position = position;
        }

        public void RequestSpawnBroker(Vector3 position)
        {
            var mafiaObject = Managers.Resource.Load<BrokerObject>("Object/BrokerObject");
            mafiaObject?.Initialize();
            mafiaObject.transform.position = position;
        }
    }
}