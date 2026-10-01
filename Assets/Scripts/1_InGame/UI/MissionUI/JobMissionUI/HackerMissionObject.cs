using System;
using StockGame.Scripts.Define;
using Unity.Netcode;
using static StockGame.Scripts.Define.GameDefine.JobDefine;

namespace StockGame.Scripts.UI.Missions.JobMissions
{
    public class HackerMissionObject : MissionObject, ISkillReceiver
    {
        public ulong OwnerId => NetworkManager.Singleton.LocalClientId;
        public bool CanReceive() => true;

        /// <summary>
        /// 미션 오브젝트는 네트워크 상태이상이 없으므로 즉시 적용하고 결과만 통지한다.
        /// </summary>
        public void ReceiveSkill(SkillContext context, Action apply)
        {
            if (!CanReceive())
            {
                context.Complete(SkillResult.Rejected);
                return;
            }

            apply();
            context.Complete(SkillResult.Accepted);
        }

        public override void Complete(GameDefine.MissionDefine.Mission mission)
        {
            base.Complete(mission);
            locked = false;
        }
    }
}