using StockGame.Scripts.Manager;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UniRx;

namespace StockGame.Scripts.Players
{
    [DisallowMultipleComponent]
    public class PlayerController : NetworkBehaviour
    {
        public Vector3 dir = Vector3.zero;

        public Rigidbody2D rigid;

        [SerializeField]
        private float movementSpeed;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner) return;
            InputManager.Instance.OnMove.Subscribe(SetDirection).AddTo(gameObject);
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (IsLocalPlayer)
                Move();
        }
        private void SetDirection(Vector2 dir)
        {
            this.dir = dir;
        }

        private void Move()
        {
            float deltaTime = 1.0f / NetworkManager.NetworkTickSystem.TickRate;
            rigid.velocity = dir * movementSpeed * deltaTime;
        }
    }
}