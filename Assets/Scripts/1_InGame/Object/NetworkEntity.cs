using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkEntity : NetworkBehaviour, IDisposable
{
    public NetworkObject NetObject;
    public bool destroyWithScene = false;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[NetworkEntity] {name} spawned (OwnerClientId: {OwnerClientId})");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log($"[NetworkEntity] {name} despawned (OwnerClientId: {OwnerClientId})");
    }

    public void Dispose()
    {
        NetObject = null;
    }
}
