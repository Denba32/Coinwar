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
        Debug.Log($"{OwnerClientId}���� {name} ������Ʈ�� ������ �߽��ϴ�");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Debug.Log($"{OwnerClientId}���� {name} ������Ʈ�� ���� �߽��ϴ�");
    }

    public void Dispose()
    {
        NetObject = null;
    }
}
