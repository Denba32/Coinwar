using StockGame.Scripts.Manager;
using System.Threading;
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

        public virtual void Initialize()
        {
            Debug.Log("DeployObject Initialize");
            var gameManager = Managers.Game;
            if (gameManager == null) return;
            gameManager.CurrentRound.OnValueChanged += OnRoundChanged;
        }

        private void OnRoundChanged(RoundDefine.RoundInfo previousValue, RoundDefine.RoundInfo newValue)
        {
            if (newValue.RoundPhase != RoundDefine.RoundPhase.Explore)
            {
                Destroy(gameObject);
            }
        }
        public virtual void Activate() { }
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