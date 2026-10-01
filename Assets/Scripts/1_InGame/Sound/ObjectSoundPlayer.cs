using StockGame.Scripts.Manager;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Sounds
{
    public class ObjectSoundPlayer : MonoBehaviour
    {
        [SerializeField] private NetworkObject netObject;
        
        /// <summary>
        /// 글로벌로 처리할 때 사용
        /// </summary>
        /// <param name="path"></param>
        public void PlaySFX(string path)
        {
            if (!netObject.IsOwner) return;
            Managers.Sound?.PlaySfx(path);
        }


        /// <summary>
        /// 로컬로 처리할 때 사용
        /// </summary>
        /// <param name="path"></param>
        public void PlaySFXLocal(string path)
        {
            Managers.Sound?.PlaySfx(path);
        }
    }
}