using StockGame.Scripts.Manager;
using StockGame.Scripts.Players;
using System;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Skills
{
    public interface IObjectDeployable
    {
        void Activate();
    }

    public abstract class DeployObject : MonoBehaviour, IObjectDeployable
    {
        [SerializeField] protected Animator animator;
        [SerializeField] protected LayerMask effectLayer;

        protected CancellationTokenSource cts = new();
        protected ulong OwnerId { get; set; }
        protected JobSkillBase Skill { get; private set; }

        // 설치한 본인 판별용 (자기 설치물엔 안 걸리도록). Gum/Poison 공통.
        protected ulong localClientId;

        // 효과가 한 번 소비되면 다시는 트리거되지 않도록 하는 재진입 가드.
        private bool _consumed;

        public virtual void Initialize()
        {
            localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            Debug.Log("DeployObject Initialize");
            var gameManager = Managers.Game;
            if (gameManager == null) return;
            gameManager.CurrentRound.OnValueChanged += OnRoundChanged;
        }
        
        protected void TryConsumeOn<T>(Collider2D other, Action<T> apply) where T : class
        {
            if (_consumed) return;
            if (!other.CompareTag("SkillReceiver")) return;
            if ((effectLayer.value & (1 << other.gameObject.layer)) == 0) return;

            var receiver = other.GetComponent<ISkillReceiver>();
            if (receiver == null) return;

            // 설치한 본인은 자기 설치물에 걸리지 않는다.
            if (receiver is PlayerSkillActionReceiver playerReceiver && playerReceiver.IsOwnerClient(localClientId)) return;

            if (receiver is not T effect) return;

            var context = new SkillContext(null, receiver, result =>
            {
                if (result != SkillResult.Accepted) return;
                _consumed = true;
                Destroy(gameObject);
            });

            receiver.ReceiveSkill(context, () => apply(effect));
        }
        private void OnRoundChanged(RoundDefine.RoundInfo previousValue, RoundDefine.RoundInfo newValue)
        {
            if (newValue.RoundPhase != RoundDefine.RoundPhase.Explore)
            {
                Destroy(gameObject);
            }
        }
        public void Activate() { OnActivate(); }
        protected abstract void OnActivate();
        protected virtual void OnDestroy()
        {
            var gameManager = Managers.Game;
            if (gameManager?.CurrentRound != null)
                gameManager.CurrentRound.OnValueChanged -= OnRoundChanged;

            cts?.Cancel();
            cts?.Dispose();

            cts = null;
            animator = null;
            Skill = null;
        }
    }
}