using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Missions;
using System.Threading;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Skills
{
    public sealed class Gum : DeployObject
    {
        [SerializeField] private CapsuleCollider2D capsuleCollider;
        private GangsterSkill GangsterSkill { get; set; }

        public void SetSkill(GangsterSkill skill)
        {
            GangsterSkill = skill;
            capsuleCollider.size = new Vector2(skill.XRange, skill.YRange);
            capsuleCollider.enabled = false;
        }

        protected override void OnActivate()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new();
            OnActivateCollider(1f, cts.Token).Forget();
        }

        private async UniTask OnActivateCollider(float colliderActivatedTime, CancellationToken token)
        {
            await UniTask.WaitForSeconds(colliderActivatedTime * 0.25f, cancellationToken: token);
            capsuleCollider.enabled = true;
            await LifetimeAsync(GangsterSkill.DeployDuration, token);
        }

        private async UniTask LifetimeAsync(float lifetime, CancellationToken token)
        {
            await UniTask.WaitForSeconds(lifetime, cancellationToken: token);
            capsuleCollider.enabled = false;
            Destroy(gameObject);
        }

        // 겹친 Gum 중 하나만 소비되도록 공통 경로(TryConsumeOn)로 위임한다.
        private void OnTriggerEnter2D(Collider2D other) => TryConsumeOn<ISlow>(other, slowable =>
        {
            slowable.ApplySlow(GangsterSkill.DeployEffectValue, GangsterSkill.PressHoldDuration);
            MissionResolver.Notify(new MissionActionEvent(Define.GameDefine.MissionDefine.MissionActionType.Trigger, Define.GameDefine.MissionDefine.MissionTargetType.Gum));
        });

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log("OnDestroy");
            Managers.Token.Cancel(this);
            GangsterSkill = null;
            capsuleCollider = null;
        }
    }
}