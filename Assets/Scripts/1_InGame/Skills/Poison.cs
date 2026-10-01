using Cysharp.Threading.Tasks;
using StockGame.Scripts.Missions;
using System.Threading;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Skills
{
    public sealed class Poison : DeployObject
    {
        [SerializeField] private CapsuleCollider2D capsuleCollider;
        [SerializeField] private AnimationClip clip;
        
        private const string ActivateTrigger = "Activate";
        private InsanePharmacistSkill InsanePharmacistSkill { get; set; }

        public void SetSkill(InsanePharmacistSkill skill)
        {
            InsanePharmacistSkill = skill;
            capsuleCollider.size = new Vector2(skill.XRange, skill.YRange);
            capsuleCollider.enabled = false;
        }

        protected override void OnActivate()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new();

            animator.SetTrigger(ActivateTrigger);
            var length = clip.length;
            OnActivateCollider(length, cts.Token).Forget();
        }

        private async UniTask OnActivateCollider(float colliderActivatedTime, CancellationToken token)
        {
            await UniTask.WaitForSeconds(colliderActivatedTime * 0.25f, cancellationToken:token);
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

        // 겹친 Poison 중 하나만 소비되도록 공통 경로(TryConsumeOn)로 위임한다.
        private void OnTriggerEnter2D(Collider2D other) => TryConsumeOn<IInvertControllable>(other, invertControllable =>
        {
            invertControllable.ApplyInvertControls(InsanePharmacistSkill.DeployEffectTime, localClientId);
            MissionResolver.Notify(new MissionActionEvent(Define.GameDefine.MissionDefine.MissionActionType.Trigger, Define.GameDefine.MissionDefine.MissionTargetType.Poison));
        });

        protected override void OnDestroy()
        {
            base.OnDestroy();
            InsanePharmacistSkill = null;
            capsuleCollider = null;
            clip = null;
        }
    }
}