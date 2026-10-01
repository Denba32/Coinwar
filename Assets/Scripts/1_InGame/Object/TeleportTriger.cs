using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using UnityEngine;

public class TeleportTriger : MonoBehaviour
{
    [Header("이동할 목표 위치")]
    public Transform targetPosition;

    [Header("플레이어 태그 이름 (기본값: Player)")]
    public string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            if (!other.TryGetComponent<ClientNetworkTransform>(out var player)) return;
            player?.Teleport(targetPosition.position, Quaternion.identity, Vector3.one);
        }
    }

}
