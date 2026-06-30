using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Missions;
using StockGame.Scripts.Players;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.Skills
{
    public sealed class Gum : DeployObject
    {
        [SerializeField] private CapsuleCollider2D capsuleCollider;
        private GangsterSkill GangsterSkill { get; set; }
        private ulong localClientId;

        public void SetSkill(GangsterSkill skill)
        {
            GangsterSkill = skill;
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
            OnActivateCollider(1f, cts.Token).Forget();
        }

        private async UniTask OnActivateCollider(float colliderActivatedTime, CancellationToken token)
        {
            await UniTask.WaitForSeconds(colliderActivatedTime, cancellationToken: token);
            capsuleCollider.enabled = true;
            await LifetimeAsync(GangsterSkill.DeployDuration, token);
        }

        private async UniTask LifetimeAsync(float lifetime, CancellationToken token)
        {
            await UniTask.WaitForSeconds(lifetime, cancellationToken: token);
            capsuleCollider.enabled = false;
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("SkillReceiver")) return;
            if ((effectLayer.value & (1 << other.gameObject.layer)) == 0) return;
            var receiver = other?.GetComponent<ISkillReceiver>();
            if (receiver == null) return;
            if (receiver is PlayerSkillActionReceiver skillReceiver && skillReceiver.IsOwnerClient(localClientId)) return;
            if (!receiver.CanReceive()) return;
            if (receiver is not ISlow slowable) return;
            slowable.ApplySlow(GangsterSkill.DeployEffectValue, GangsterSkill.PressHoldDuration);
            MissionResolver.Notify(new MissionActionEvent(Define.GameDefine.MissionDefine.MissionActionType.Trigger, Define.GameDefine.MissionDefine.MissionTargetType.Gum));
            Destroy(gameObject);
        }

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