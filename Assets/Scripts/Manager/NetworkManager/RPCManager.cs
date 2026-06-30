using Denba.Common;
using StockGame.Scripts.Define;
using StockGame.Scripts.Missions;
using StockGame.Scripts.Players;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    public sealed class RPCManager : NetworkSingleton<RPCManager>
    {
        #region SupplyBox Rpc Method

        /// <summary>
        /// 보급 아이템 사용 시 스킬의 쿨타임을 한번 초기화 시킨다.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void ResetSkillCooldownRpc(RpcParams rpcParams = default)
        {
            var senderClientId = rpcParams.Receive.SenderClientId;
            var sender = NetworkManager.Singleton.ConnectedClients[senderClientId];
            var player = sender.PlayerObject.GetComponent<PlayerNetwork>();
            player.RechargeSkillRpc();
        }

        /// <summary>
        /// 모든 플레이어의 위치를 랜덤한 주요 지역 입구로 이동시킨다. ex) A 플레이어 경찰서 입구로 텔레포트, B 플레이어 은행 입구로 텔레포트
        /// </summary>
        [Rpc(SendTo.Server)]
        public void TeleportPlayersToRandomEntrancesRpc()
        {
            var targets = NetworkManager.Singleton.ConnectedClientsList;
            var count = targets.Count;
            var zones = ZoneManager.Instance.GetRandomZones(count);

            for (int i = 0; i < targets.Count; i++)
            {
                var index = i % zones.Count;
                var zone = zones[index];
                var player = targets[i].PlayerObject.GetComponent<PlayerNetwork>();
                var teleportPoint = zone.GetTeleportPoint();
                player.TeleportOwnerRpc(new Vector3(teleportPoint.position.x, teleportPoint.position.y, 0));
            }
        }

        /// <summary>
        /// 사용자 플레이어의 현재 위치를 랜덤한 플레이어의 위치와 교체.
        /// </summary>
        [Rpc(SendTo.Server)]
        public void SwapPositionWithRandomPlayerRpc(RpcParams rpcParams = default)
        {
            ulong executorClientId = rpcParams.Receive.SenderClientId;
            var executor = NetworkManager.Singleton.ConnectedClients[executorClientId];
            // 자기 자신을 제외한 플레이어 목록
            var targets = NetworkManager.Singleton.ConnectedClientsList
                .Where(x => x.ClientId != executorClientId)
                .ToList();

            // 스왑할 대상이 없으면 종료
            if (targets.Count == 0)
                return;

            targets.Shuffle();

            var target = targets[0];
            var targetPlayer = target.PlayerObject.GetComponent<PlayerNetwork>();
            var executorPlayer = executor.PlayerObject.GetComponent<PlayerNetwork>();

            var targetPlayerPosition = targetPlayer.Root.position;
            var executorPlayerPosition = executorPlayer.Root.position;

            if (targetPlayer.Condition.Value == PlayerConditionType.Arrested || executorPlayer.Condition.Value == PlayerConditionType.Arrested) return;
            targetPlayer.TeleportOwnerRpc(new Vector3(executorPlayerPosition.x, executorPlayerPosition.y, 0));
            executorPlayer.TeleportOwnerRpc(new Vector3(targetPlayerPosition.x, targetPlayerPosition.y, 0));
        }
        #endregion SupplyBox Rpc Method

        #region SOUND_METHOD

        /// <summary>
        /// 후라이팬 시전 사운드 재생
        /// </summary>
        /// <param name="targetClientId"></param>
        public void PlayFryingPanSound(ulong targetClientId)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX230);
            PlayFryingPanSoundRpc(RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
        }
        [Rpc(SendTo.SpecifiedInParams)]
        public void PlayFryingPanSoundRpc(RpcParams param = default)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX230);
        }

        /// <summary>
        /// 후라이팬 피격 사운드 재생
        /// </summary>
        /// <param name="targetClientId"></param>
        public void PlayStunnedSound(ulong targetClientId)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX231);
            PlayFryingPanSoundRpc(RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
        }
        [Rpc(SendTo.SpecifiedInParams)]
        public void PlayStunnedSoundRpc(RpcParams param = default)
        {
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX231);
        }

        public void PlayArrestSound() => Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX236);

        [Rpc(SendTo.SpecifiedInParams)]
        public void PlayArrestSoundRpc(RpcParams param)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX236);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        public void PlayBlindRpc(RpcParams param)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX240);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        public void PlayGumStepSoundRpc(RpcParams param)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX243);
        }

        public void PlayStealSuccess(ulong clientId)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX234);
            PlayStealSuccessRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        public void PlayStealSuccessRpc(RpcParams param)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX234);
        }

        public void PlayStealFailed(ulong clientId)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX235);
            PlayStealFailedRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        public void PlayStealFailedRpc(RpcParams param)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX235);
        }

        #endregion SOUND_METHOD

        #region MISSION_METHOD
        [Rpc(SendTo.ClientsAndHost)]
        public void StartMissionRpc()
        {
            MissionResolver.StartMission();
        }
        #endregion MISSION_METHOD
    }
}