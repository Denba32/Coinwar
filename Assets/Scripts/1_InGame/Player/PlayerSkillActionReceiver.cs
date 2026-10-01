using System;
using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Players
{
    public class PlayerSkillActionReceiver : MonoBehaviour, ISkillReceiver,
        IStunnable, ISlow, IArrestHoldable, IInvertControllable, ISteakable
    {
        [SerializeField] private Animator skillReceivedAnimator;
        private PlayerNetwork _player;

        // 스킬 수신 1건을 직렬화하는 블록. 처리 중이면 이후 수신 요청을 모두 Reject 한다.
        private readonly SkillReceiveGate _skillGate = new();
        private NetworkVariable<PlayerConditionType>.OnValueChangedDelegate _conditionHandler;

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

        /// <summary>현재 이 수신자가 스킬 1건을 처리 중인지. (외부 조회용)</summary>
        public bool IsProcessingSkill => _skillGate.IsProcessing;

        public void Initialize(PlayerNetwork player)
        {
            // 재초기화 시 중복 구독 방지
            if (_player != null && _conditionHandler != null)
                _player.Condition.OnValueChanged -= _conditionHandler;

            _player = player;

            // 상태이상이 해제(None 복귀)되면 게이트를 풀어 다음 스킬 수신을 허용한다.
            _conditionHandler = OnConditionChanged;
            _player.Condition.OnValueChanged += _conditionHandler;
        }

        private void OnConditionChanged(PlayerConditionType previous, PlayerConditionType current)
        {
            if (current == PlayerConditionType.None)
                _skillGate.Release();
        }

        private void Update()
        {
            // Release() 통지가 유실돼도 게이트가 영구 잠기지 않도록 하는 안전장치.
            _skillGate.Tick();
        }

        public bool IsOwnerClient(ulong clientId) => _player.OwnerClientId == clientId;

        /// <summary>
        /// 스킬의 최종 실행 지점. 스캔 스킬이든 설치물(Gum/Poison)이든 모두 이 경로로 들어온다.
        /// 수신 가능하면 <paramref name="apply"/>를 실행하고 결과를 <see cref="SkillContext.Complete"/>로 통지한다.
        ///
        /// 접수하는 순간 게이트를 점유하므로, 같은 프레임에 몰린 후속 요청
        /// (겹친 설치물, 스킬 연타)은 전부 <see cref="SkillResult.Rejected"/> 된다.
        /// </summary>
        public void ReceiveSkill(SkillContext context, Action apply)
        {
            if (_skillGate.IsProcessing || !CanReceiveInternal())
            {
                context.Complete(SkillResult.Rejected);
                return;
            }

            _skillGate.TryAcquire();   // 접수 즉시 점유 → 이후 요청은 위에서 Reject
            apply();                   // ApplyStun / ApplySlow / ApplyInvertControls / ArrestHold ...
            context.Complete(SkillResult.Accepted);
        }

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

        #region  SKILL_ACTION_PROCESS
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
            _player.TryStealRpc(); // 훔쳐지는 대상이 애니메이션 실행 호출
            _player.TryStealAsync(sender, amount, successDelay).Forget();
        }
        public void ArrestHold(float holdDuration, float arrestDuration, Vector3 prisonInPos, Vector3 prisonOutPos)
        {
            _player.ApplyArrestHoldRpc(holdDuration, arrestDuration, prisonInPos, prisonOutPos);
        }

        /// <summary>
        /// 순수 수신 가능 상태 판정 (게이트 / 상호작용 제외). <see cref="ReceiveSkill"/> 내부 판정용.
        /// 상호작용 여부는 스킬 종류별로 다르므로(도둑은 상호작용 중에도 대상 가능) 여기서 제외한다.
        /// </summary>
        private bool CanReceiveInternal()
        {
            return _player.Condition.Value == PlayerConditionType.None
                && !_player.IsShielded.Value
                && !IsPlayingSkill();
        }

        /// <summary>
        /// 외부(스캐너 / CanExecuteOn / UI 준비상태) 판정용.
        /// "처리 중" 게이트 + 상호작용까지 포함하므로, 스킬 처리 중/상호작용 중에는
        /// 스캐너가 이 대상을 후보에서 제외한다.
        /// </summary>
        public bool CanReceive()
            => !_skillGate.IsProcessing && !_player.IsInteracted.Value && CanReceiveInternal();

        /// <summary>
        /// 도둑용 판정. 상호작용은 제외하고 나머지 조건만 본다.
        /// </summary>
        public bool CanReceiveSteal() => !_skillGate.IsProcessing && CanReceiveInternal();

        public bool IsPlayingSkill()
        {
            var stateInfo = skillReceivedAnimator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(UseCoin) || stateInfo.IsName(Arrest) || stateInfo.IsName(Hacking);
        }
        #endregion SKILL_ACTION_PROCESS

        private void OnDestroy()
        {
            if (_player != null && _conditionHandler != null)
                _player.Condition.OnValueChanged -= _conditionHandler;
        }
    }
}