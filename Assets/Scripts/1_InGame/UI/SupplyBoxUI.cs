using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using StockGame.Scripts.Datas;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Players;
using StockGame.Scripts.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class SupplyBoxUI : UIBase
    {
        [SerializeField] private Animator boxAnimator;
        [SerializeField] private Image boxImage;
        [SerializeField] private Image cardImage;
        [SerializeField] private List<SupplyDataSO> supplyDataList = new();
        [SerializeField] private LocalizeSpriteEvent spriteEvent;

        private const string Open = "Open";

        private EventInstance eventInstance;

        private InteractorContext ctx;
        private Action onClose = null;
        private CancellationTokenSource linkedCts;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            supplyDataList.Shuffle();
        }

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            foreach (var arg in args)
            {
                if (arg is InteractorContext ctx)
                {
                    this.ctx = ctx;
                }
                else if (arg is Action action)
                {
                    onClose = action;
                }
            }

            var token = Managers.Token.GetToken(this);
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, destroyCancellationToken);
            OpenSupplyBoxAsync(supplyDataList.First(), linkedCts.Token).Forget();
        }

        private async UniTask OpenSupplyBoxAsync(SupplyDataSO supplyData, CancellationToken token = default)
        {
            try
            {
                eventInstance = RuntimeManager.CreateInstance(GameDefine.ResourceDefine.FMODEvent.SFX262);
                eventInstance.start();
                boxAnimator.Play(Open);
                await UniTask.WaitForSeconds(2f, cancellationToken: token);
                if (token.IsCancellationRequested || this == null) return;

                boxImage.gameObject.SetActive(false);
                cardImage.sprite = spriteEvent.SetLocalization("SupplyCard_Asset", supplyData.SupplyEvent.ToString());
                cardImage.gameObject.SetActive(true);
                await UniTask.WaitForSeconds(3f, cancellationToken: token);
                if (token.IsCancellationRequested || this == null) return;

                switch (supplyData.SupplyEvent)
                {
                    case GameDefine.SupplyBoxEvent.None:
                        break;

                    case GameDefine.SupplyBoxEvent.Shield:
                        ApplySkillNullificationShield();
                        break;

                    case GameDefine.SupplyBoxEvent.ResetSkill:
                        ResetSkillCooldown();
                        break;

                    case GameDefine.SupplyBoxEvent.TeleportPlayer:
                        TeleportPlayersToRandomEntrances();
                        break;

                    case GameDefine.SupplyBoxEvent.SwapPlayer:
                        SwapPositionWithRandomPlayer();
                        break;

                    case GameDefine.SupplyBoxEvent.SpawnBarricade:
                        SpawnRandomBarricade();
                        break;

                    default:
                        Debug.LogWarning($"Unhandled Supply Event : {supplyData.SupplyEvent}");
                        break;
                }
                Close();
            }
            catch (OperationCanceledException)
            {
                // [FIX] Destroy(gameObject) 대신 정식 Close()를 호출해, UIManager 목록 정리와
                // OnClose/Dispose 정리 로직을 동일하게 거치도록 한다.
                if (this != null) Close();
            }
        }

        /// <summary>
        /// 보급 아이템 사용 시 스킬 효과를 1회 무시하는 쉴드를 30초간 얻는다.
        /// </summary>
        private void ApplySkillNullificationShield() => ctx.PlayerObject.GetComponent<PlayerNetwork>().ShieldSkillRpc();

        /// <summary>
        /// 보급 아이템 사용 시 스킬의 쿨타임을 한번 초기화 시킨다.
        /// </summary>
        private void ResetSkillCooldown() => RPCManager.Instance.ResetSkillCooldownRpc();

        /// <summary>
        /// 모든 플레이어의 위치를 랜덤한 주요 지역 입구로 이동시킨다. ex) A 플레이어 경찰서 입구로 텔레포트, B 플레이어 은행 입구로 텔레포트
        /// </summary>
        private void TeleportPlayersToRandomEntrances() => RPCManager.Instance.TeleportPlayersToRandomEntrancesRpc();

        /// <summary>
        /// 사용자 플레이어의 현재 위치를 랜덤한 플레이어의 위치와 각각 바꾼다.
        /// </summary>
        private void SwapPositionWithRandomPlayer() => RPCManager.Instance.SwapPositionWithRandomPlayerRpc();

        /// <summary>
        /// 주요 지역 입구 봉쇄 오브젝트 10초간 랜덤한 3곳에 생성
        /// 생성 위치 : 촌장집 정원 입구, 경찰서 입구, 은행 입구, 남극기지 입구, 벙커 입구, 낚시터 입구, 베이스 캠프 입구, 항구 시장 입구
        /// </summary>
        private void SpawnRandomBarricade()
        {
            ZoneManager.Instance.ActiveBarricadeRpc();
        }

        public override void OnDispose()
        {
            base.OnDispose();

            onClose?.Invoke();
            onClose = null;

            linkedCts?.Cancel();
            linkedCts?.Dispose();
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }
    }
}