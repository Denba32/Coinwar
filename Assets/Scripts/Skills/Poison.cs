using Cysharp.Threading.Tasks;
using StockGame.Scripts.Missions;
using StockGame.Scripts.Players;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Skills
{
    public sealed class Poison : DeployObject
    {
        [SerializeField] private CapsuleCollider2D capsuleCollider;
        [SerializeField] private AnimationClip clip;
        
        private const string ActivateTrigger = "Activate";
        private ulong localClientId;
        private InsanePharmacistSkill InsanePharmacistSkill { get; set; }

        public void SetSkill(InsanePharmacistSkill skill)
        {
            InsanePharmacistSkill = skill;
            localClientId = NetworkManager.Singleton.LocalClientId;
            capsuleCollider.size = new Vector2(skill.XRange, skill.YRange);
            capsuleCollider.enabled = false;
        }
        public override void Activate()
        {
            base.Activate();
            cts?.Cancel();
            cts?.Dispose();
            cts = new();

            animator.SetTrigger(ActivateTrigger);
            var length = clip.length;
            OnActivateCollider(length, cts.Token).Forget();
        }

        private async UniTask OnActivateCollider(float colliderActivatedTime, CancellationToken token)
        {
            await UniTask.WaitForSeconds(colliderActivatedTime, cancellationToken:token);
            capsuleCollider.enabled = true;
            await LifetimeAsync(InsanePharmacistSkill.DeployDuration, token);
        }

        private async UniTask LifetimeAsync(float lifetime, CancellationToken token)
        {
            await UniTask.WaitForSeconds(lifetime, cancellationToken: token);
            Debug.Log("Lifetime Async 호출 여부 확인");
            capsuleCollider.enabled = true;
            if(token.IsCancellationRequested) return;
            Debug.Log("Destroy!");
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("SkillReceiver")) return;

            // 대상 파악 후 스킬 피격 Request
            if ((effectLayer.value & (1 << other.gameObject.layer)) == 0) return;
            var receiver = other?.GetComponent<ISkillReceiver>();
            if (receiver is PlayerSkillActionReceiver skillReceiver && skillReceiver.IsOwnerClient(localClientId)) return;
            if (!receiver.CanReceive()) return;
            if (receiver is not IInvertControllable invertControllable) return;
            invertControllable.ApplyInvertControls(InsanePharmacistSkill.DeployEffectTime, localClientId);
            MissionResolver.Notify(new MissionActionEvent(Define.GameDefine.MissionDefine.MissionActionType.Trigger, Define.GameDefine.MissionDefine.MissionTargetType.Poison));
            Destroy(gameObject);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InsanePharmacistSkill = null;
            capsuleCollider = null;
            clip = null;
        }
    }
}