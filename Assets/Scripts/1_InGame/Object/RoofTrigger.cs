using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Objects
{
    public class RoofTrigger : MonoBehaviour
    {
        private const string PlayerTag = "Player";
        [SerializeField] private GameObject roofObject;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(PlayerTag)) return;
            if (!IsLocalPlayer(other)) return;
            SetRoofVisible(false);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag(PlayerTag)) return;
            if (!IsLocalPlayer(other)) return;
            SetRoofVisible(true);
        }
        private bool IsLocalPlayer(Collider2D other)
        {
            var networkObject = other.GetComponentInParent<NetworkObject>();
            return networkObject != null && networkObject.IsLocalPlayer;
        }
        private void SetRoofVisible(bool visible)
        {
            if (roofObject != null)
                roofObject.SetActive(visible);
        }
    }
}