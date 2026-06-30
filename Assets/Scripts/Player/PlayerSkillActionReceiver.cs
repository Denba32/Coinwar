using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Players
{
    public class PlayerSkillActionReceiver : MonoBehaviour, ISkillReceiver,
        IStunnable, ISlow, IArrestHoldable, IInvertControllable, ISteakable
    {
        [SerializeField] private Animator skillReceivedAnimator;
        private PlayerNetwork _player;

        private const string Poisoned = "Poisoned";
        private const string UseCoin = "UseCoin";
        private const string Arrest = "Arrest";
        private const string TryEscape = "TryEscape";
        private const string SuccessEscape = "SuccessEscape";
        private const string Hacking = "Hacking";
        private const string Default = "Default";
        private const string StartRecluse = "StartRecluse";
        private const string SuccessRecluse = "SuccessRecluse";
        private const string FailedRecluse = "FailedRecluse";
        private const string StepOnGumStart = "StepOnGumStart";
        private const string StepOnGumTrying = "StepOnGumTrying";
        private const string StepOnGumSuccess = "StepOnGumSuccess";
        private const string Steal = "Steal";
        private const string FailedSteal = "FailedSteal";
        private const string GambleCoin = "GambleCoin";

        public PlayerNetwork Player => _player;

        public ulong OwnerId => _player.OwnerClientId;

        public void Initialize(PlayerNetwork player)
        {
            _player = player;
        }

        public bool IsOwnerClient(ulong clientId) => _player.OwnerClientId == clientId;

        #region Animation
        public void PlayDefault() => skillReceivedAnimator?.Play(Default);
        public void PlayStepOnGumStart() => skillReceivedAnimator?.Play(StepOnGumStart, 0);
        public void PlayStepOnGumTrying() => skillReceivedAnimator?.Play(StepOnGumTrying, 0);
        public void PlayStepOnGumSuccess() => skillReceivedAnimator?.Play(StepOnGumSuccess, 0);
        public void GambledCoin(int coin)
        {
            skillReceivedAnimator.SetFloat(GambleCoin, coin);
            skillReceivedAnimator?.Play(UseCoin, 0);
        }
        public void PlayArrest()
        {
            var stateInfo = skillReceivedAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(Arrest)) return;
            RPCManager.Instance.PlayArrestSound();
            skillReceivedAnimator?.Play(Arrest, 0);
        }
        public void PlayTryEscape() => skillReceivedAnimator?.Play(TryEscape, 0);
        public void PlaySuccssEscape() => skillReceivedAnimator?.Play(SuccessEscape, 0);
        public void PlayHacking() => skillReceivedAnimator?.Play(Hacking, 0);
        public void PlayPoisoned() => skillReceivedAnimator?.Play(Poisoned, 0);
        public void PlayRecluse() => skillReceivedAnimator?.Play(StartRecluse, 0);
        public void PlaySuccessRecluse() => skillReceivedAnimator?.Play(SuccessRecluse, 0);
        public void PlayFailedRecluse() => skillReceivedAnimator?.Play(FailedRecluse, 0);
        public void PlaySteal() => skillReceivedAnimator?.Play(Steal, 0);
        public void PlayFailedSteal() => skillReceivedAnimator?.Play(FailedSteal, 0);

        #endregion Animation

        public void ApplyStun(float duration)
        {
            _player.RequestApplyStunRpc(duration);
        }

        public void ApplySlow(float percent, float duration)
        {
            _player.ApplySlowRpc(percent, duration);
        }

        public void TryBlind(float duration)
        {
            _player.RequestApplyBlindServerRpc(duration);
        }

        public void ApplyInvertControls(float duration, ulong sender)
        {
            if (_player.OwnerClientId == sender) return;
            _player.RequestApplyInvertControlsRpc(duration);
        }

        public void ApplyStealCoin(PlayerNetwork sender, float successDelay, int amount)
        {
            _player.TryStealRpc();
            _player.TryStealAsync(sender, amount, successDelay).Forget();
        }

        public bool CanReceive()
        {
            return !_player.IsInteracted.Value
                && _player.Condition.Value == PlayerConditionType.None
                && !_player.IsShielded.Value
                && !IsPlayingSkill();
        }

        /// <summary>
        /// 도둑의 경우 상호작용 제외
        /// </summary>
        /// <returns></returns>
        public bool CanReceiveSteal()
        {
            return _player.Condition.Value == PlayerConditionType.None
                && !_player.IsShielded.Value
                && !IsPlayingSkill();
        }

        public bool IsPlayingSkill()
        {
            var stateInfo = skillReceivedAnimator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(UseCoin) || stateInfo.IsName(Arrest) || stateInfo.IsName(Hacking);
        }

        public void ArrestHold(float holdDuration, float arrestDuration, Vector3 prisonInPos, Vector3 prisonOutPos)
        {
            _player.ApplyArrestHoldRpc(holdDuration, arrestDuration, prisonInPos, prisonOutPos);
        }
    }
}