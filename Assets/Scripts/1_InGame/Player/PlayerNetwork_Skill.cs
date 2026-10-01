using Cysharp.Threading.Tasks;
using FMODUnity;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Missions;
using StockGame.Scripts.UI;
using System.Threading;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Players
{
    public partial class PlayerNetwork : NetworkBehaviour
    {
        #region Stun
        [Rpc(SendTo.Owner, RequireOwnership = false)]
        public void RequestApplyStunRpc(float duration)
        {
            if (!IsOwner) return;
            StateMachine?.ChangeState(StateMachine.StunState, duration);
            Managers.UI.CloseAllByType(GameDefine.UIDefine.UILayer.Popup).Forget();
            RPCManager.Instance.PlayStunnedSoundRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
        }

        #endregion Stun

        #region Blind
        [ServerRpc(RequireOwnership = false)]
        public void RequestApplyBlindServerRpc(float duration)
        {
            ApplyBlindClientRpc(duration, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
            });
        }

        [ClientRpc]
        private void ApplyBlindClientRpc(float duration, ClientRpcParams rpcParams = default)
        {
            if (!IsOwner) return;
            Debug.Log($"[Blind] {duration}초");
        }
        #endregion Blind

        #region Teleport
        [ServerRpc(RequireOwnership = false)]
        public void RequestApplyTeleportServerRpc()
        {
            ApplyTeleportClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
            });
        }

        [ClientRpc]
        private void ApplyTeleportClientRpc(ClientRpcParams rpcParams = default)
        {
            if (!IsOwner) return;
            Debug.Log("[Teleport]");
        }
        #endregion Teleport

        #region Job Skills


        #region Hacker (Blind All)
        // 클라이언트 → 서버 요청
        [Rpc(SendTo.Server)]
        public void RequestBlindRpc(ulong senderClientId, float duration, float visibleRange)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == senderClientId) continue;
                RPCManager.Instance.PlayBlindRpc(RpcTarget.Single(client.ClientId, RpcTargetUse.Temp));
                BlindRpc(duration, visibleRange, RpcTarget.Single(client.ClientId, RpcTargetUse.Temp));
            }
        }

        // 서버 → 특정 클라이언트
        [Rpc(SendTo.SpecifiedInParams)]
        public void BlindRpc(float duration, float visibleRange, RpcParams rpcParams = default)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            localPlayer.BlindAsync(localPlayer, duration, visibleRange).Forget();
        }

        private async UniTask BlindAsync(PlayerNetwork target, float duration, float visibleRange)
        {
            var blindUI = await Managers.UI.Open<BlindUI>(
                Define.GameDefine.UIDefine.UILayer.Transition,
                param: new object[] { target, visibleRange });
            await UniTask.WaitForSeconds(duration, cancellationToken: destroyCancellationToken);
            Managers.UI.Close(blindUI).Forget();
        }
        #endregion Hacker (Blind All)

        #region Poison (Invert Controls)
        [Rpc(SendTo.Owner)]
        public void RequestApplyInvertControlsRpc(float duration)
        {
            if (!IsOwner) return;
            var token = Managers.Token.GetToken(this, nameof(ApplyInvertControlsAsnyc));
            ApplyInvertControlsAsnyc(duration, token).Forget();
        }

        private async UniTask ApplyInvertControlsAsnyc(float duration, CancellationToken token = default)
        {
            var eventInstance = RuntimeManager.CreateInstance(GameDefine.ResourceDefine.FMODEvent.SFX233);
            try
            {
                var prevSpeed = speed.Value;
                if (prevSpeed < 0) return;
                eventInstance.start();
                ChangeCondition(PlayerConditionType.Poisoned);
                skillActionReceiver?.PlayPoisoned();
                await UniTask.WaitForSeconds(duration, cancellationToken: token);
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
                ChangeCondition(PlayerConditionType.None);
                skillActionReceiver?.PlayDefault();
            }
            catch { }
            finally
            {
                if(eventInstance.isValid())
                {
                    eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    eventInstance.release();
                    eventInstance.clearHandle();
                }
            }

        }

        #endregion Poison (Invert Controls)

        #region Gangster (Slow)

        [Rpc(SendTo.Owner)]
        public void ApplySlowRpc(float percent, float duration)
        {
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX243);
            ChangeCondition(PlayerConditionType.Slow);
            SetSpeed(speed.Value * (1f - percent));
            RestoreSpeedWithHoldAsync(DEFAULT_SPEED, duration).Forget();
        }

        private async UniTaskVoid RestoreSpeedWithHoldAsync(float prevSpeed, float duration)
        {
            var token = Managers.Token.GetToken(this, nameof(RestoreSpeedWithHoldAsync));
            float elapsedTime = 0f;
            var eventInstance = RuntimeManager.CreateInstance(GameDefine.ResourceDefine.FMODEvent.SFX244);
            while (elapsedTime < duration && !token.IsCancellationRequested)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    if(IsOwner)
                    {
                        eventInstance.getPlaybackState(out var state);
                        if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED) eventInstance.start();
                    }
   
                    skillActionReceiver.PlayStepOnGumTrying();
                    elapsedTime += Time.deltaTime;
                }
                else
                {
                    if(IsOwner)
                        eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    skillActionReceiver.PlayStepOnGumStart();
                    elapsedTime = 0f;
                }
                await UniTask.Yield(token);
            }
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
            if (!IsOwner) return;
            Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX245);
            skillActionReceiver.PlayStepOnGumSuccess();
            SetSpeed(prevSpeed);
            ChangeCondition(PlayerConditionType.None);
        }
        #endregion Gangster (Slow)

        #region Thief (Steal)
        [Rpc(SendTo.Owner, RequireOwnership = false)]
        public void TryStealRpc()
        {
            skillActionReceiver?.PlaySteal();
        }

        public async UniTask TryStealAsync(PlayerNetwork sender, int amount, float duration, float maxDistance = 3f)
        {
            var token = Managers.Token.GetToken(this, nameof(TryStealAsync));
            float elapsed = 0f;

            while (elapsed < duration) // 일정 시간초 동안 스틸 시도
            {
                if (token.IsCancellationRequested) return;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                elapsed += Time.deltaTime;

                float dist = Vector2.Distance(sender.Receiver.transform.position, Receiver.transform.position);
                if (dist >= maxDistance) // 거리를 벗어나면 실패 처리
                {
                    RPCManager.Instance.PlayStealFailed(OwnerClientId);
                    sender.Receiver?.PlayFailedSteal();
                    PlayDefaultRpc();
                    return;
                }
            }

            PlayDefaultRpc();

            if(amount > 0)
            {
                RPCManager.Instance.PlayStealSuccess(OwnerClientId);
                MissionResolver.Notify(new MissionActionEvent(GameDefine.MissionDefine.MissionActionType.Steal, value: amount));
            }
            else // 0개 스틸 시에도 동일하게 실패 처리
            {
                RPCManager.Instance.PlayStealFailed(OwnerClientId);
                sender.Receiver?.PlayFailedSteal();
                PlayDefaultRpc();
                return;
            }

            RequestStealCoinServerRpc(sender.OwnerClientId, amount);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestStealCoinServerRpc(ulong senderClientId, int amount)
        {
            var victimInfo = GameManager.Instance.GetPlayerInfoByClientId(OwnerClientId);
            var senderInfo = GameManager.Instance.GetPlayerInfoByClientId(senderClientId);
            GameManager.Instance.UpdateCoinByClientId(OwnerClientId, Mathf.Max(0, victimInfo.Coin - amount));
            GameManager.Instance.UpdateCoinByClientId(senderClientId, senderInfo.Coin + amount);
        }
        #endregion Thief (Steal)

        #region Police (Arrest)
        [Rpc(SendTo.Owner)]
        public void ApplyArrestHoldRpc(float holdDuration, float arrestDuration, Vector3 prisonInPos, Vector3 prisonOutPos)
        {
            var token = Managers.Token.GetToken(this, nameof(ApplyArrestHoldAsync));
            ApplyArrestHoldAsync(holdDuration, arrestDuration, prisonInPos, prisonOutPos, token).Forget();
        }

        private async UniTaskVoid ApplyArrestHoldAsync(float holdDuration, float arrestDuration, Vector3 prisonInPos, Vector3 prisonOutPos, CancellationToken token)
        {
            var eventInstance = RuntimeManager.CreateInstance(GameDefine.ResourceDefine.FMODEvent.SFX237);

            try
            {
                ChangeCondition(PlayerConditionType.Arrested);
                Managers.Input.StopPlayerInput();
                await Managers.UI.CloseAllByType(GameDefine.UIDefine.UILayer.Popup);
                await UniTask.WaitForSeconds(holdDuration, cancellationToken: token);

                Teleport(prisonInPos);
                Managers.Input.StartPlayerInput();
                skillActionReceiver?.PlayTryEscape();
                eventInstance.start();
                await UniTask.WaitForSeconds(arrestDuration, cancellationToken: token);
                eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                eventInstance.release();
                eventInstance.clearHandle();
                // 4. 감옥 밖으로 해방
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX238);
                skillActionReceiver?.PlaySuccssEscape();
                await UniTask.WaitForSeconds(1f, cancellationToken: token);
                Teleport(prisonOutPos);
                ChangeCondition(PlayerConditionType.None);
            }
            catch { }
            finally 
            { 
                if(eventInstance.isValid()) 
                {
                    eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    eventInstance.release();
                    eventInstance.clearHandle();
                }
            }

        }
        #endregion Police (Arrest)

        #region Recluse (Isolation)
        public void StartRecluse(float duration, int rewardCoin)
        {
            skillExecutor?.FreezeCooltime();
            RecluseAsync(duration, rewardCoin).Forget();
        }

        private async UniTask RecluseAsync(float duration, int rewardCoin)
        {
            try
            {
                Managers.Input.StopPlayerInput();
                var token = Managers.Token.GetToken(this, nameof(RecluseAsync));
                skillExecutor?.LockSkill(true);
                skillActionReceiver?.PlayRecluse();
                bool failed = false;
                float elapsed = 0f;

                var scanner = skillExecutor.CreateScanner(GameDefine.JobDefine.SkillTargetType.Single);
                ulong targetOwnerId = default;
                while (elapsed < duration || token.IsCancellationRequested)
                {
                    if (token.IsCancellationRequested) break;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: token);
                    elapsed += Time.deltaTime;

                    scanner?.Scan(
                        skillActionReceiver.gameObject,
                        new Vector2(0, Height * 0.5f * transform.localScale.y),
                        skillExecutor.RangeX, skillExecutor.RangeY,
                        skillExecutor.TargetLayer, skillExecutor.RangeType);

                    if (scanner.GetPrimary() != null)
                    {
                        Managers.Game.AddCoinTargetRpc(1, scanner.GetPrimary().OwnerId);
                        targetOwnerId = scanner.GetPrimary().OwnerId;
                        failed = true;
                        break;
                    }
                }
                scanner = null;

                skillExecutor?.LockSkill(false);
                skillExecutor?.UnfreezeCooltime();
                Managers.Input.StartPlayerInput();
                if (failed)
                {
                    RPCManager.Instance.PlayRecluseFailedSound(targetOwnerId);
                    skillActionReceiver?.PlayFailedRecluse();
                    return;
                }

                skillActionReceiver?.PlaySuccessRecluse();
                Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX234);
                MissionResolver.Notify(new MissionActionEvent(
                    Define.GameDefine.MissionDefine.MissionActionType.Detect));
                GameManager.Instance.AddCoinRpc(rewardCoin);
            }
            catch { }
            finally {  }
        }
        #endregion Recluse (Isolation)

        #endregion Job Skills

        #region SupplyBoxEffect

        [Rpc(SendTo.Owner)]
        public void RechargeSkillRpc()
        {
            if (!IsOwner) return;
            skillExecutor?.RechargeSkill();
        }

        [Rpc(SendTo.Owner)]
        public void ShieldSkillRpc()
        {
            if (!IsOwner) return;
            onActivatedShieldSkill?.OnNext(30f);
            var token = Managers.Token.GetToken(this, nameof(ShieldAsync));
            ShieldAsync(token).Forget();
        }

        private async UniTaskVoid ShieldAsync(CancellationToken token)
        {
            isShielded.Value = true;
            var dest = Managers.Game.CurrentTime.Value - 30f;

            await UniTask.WaitUntil(
                () => Managers.Game.CurrentTime.Value <= dest
                      || Managers.Game.CurrentRound.Value.RoundPhase != Define.GameDefine.RoundDefine.RoundPhase.Explore,
                cancellationToken: token);

            isShielded.Value = false;
        }
        #endregion SupplyBoxEffect

        [Rpc(SendTo.Owner)] public void PlayDefaultRpc() => skillActionReceiver.PlayDefault();
    }
}