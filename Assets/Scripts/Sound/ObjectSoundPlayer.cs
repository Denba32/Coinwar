using StockGame.Scripts.Manager;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Sounds
{
    public class ObjectSoundPlayer : MonoBehaviour
    {
        [SerializeField] private NetworkObject netObject;
        
        public void PlaySFX(string path)
        {
            if (!netObject.IsOwner) return;
            Managers.Sound?.PlaySfx(path);
        }
    }
}