using Cysharp.Threading.Tasks;
using Denba.Common;
using Steamworks;
using System;
using System.Threading;
using UniRx;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.SteamDefine;

namespace StockGame.Scripts.Manager
{
    public sealed class SteamService : Singleton<SteamService>
    {
        private const float InitializeTimeoutSeconds = 10f;

        protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;
        private HAuthTicket authTicket = HAuthTicket.Invalid;

        public bool IsInitialized { get; private set; }

        public override async UniTask InitializeAsync(CancellationToken token = default)
        {
            IsInitialized = false;

            try
            {
                await UniTask.WaitUntil(() => SteamManager.Initialized, cancellationToken: token)
                    .Timeout(TimeSpan.FromSeconds(InitializeTimeoutSeconds));
            }
            catch (TimeoutException)
            {
                Debug.LogError("[SteamService] Steam 초기화 시간 초과. Steam 클라이언트가 실행 중인지 확인하세요.");
                return;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[SteamService] Steam 초기화가 취소되었습니다.");
                return;
            }

            if (!SteamManager.Initialized)
            {
                Debug.LogError("[SteamService] SteamManager 초기화 실패.");
                return;
            }

            m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated).AddTo(this);

            IsInitialized = true;
        }

        private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
        {
            if (pCallback.m_bActive != 0)
            {
                Debug.Log("[SteamService] Steam Overlay activated");
            }
            else
            {
                Debug.Log("[SteamService] Steam Overlay closed");
            }
        }

        /// <summary>
        /// 서버 인증용 Steam 티켓을 생성합니다.
        /// 주의: 이 티켓은 반드시 서버에서 ISteamUserAuth(BeginAuthSession)로 검증해야
        /// 실질적인 위변조 방지 효과가 있습니다. 클라이언트 단독 검증만으로는 우회가 가능합니다.
        /// </summary>
        public SteamAuthData CreateTicket()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SteamService] Steam이 초기화되지 않아 티켓을 생성할 수 없습니다.");
                return null;
            }

            byte[] ticketBuffer = new byte[1024];
            uint ticketSize;
            SteamNetworkingIdentity identity = new SteamNetworkingIdentity();

            authTicket = SteamUser.GetAuthSessionTicket(
                ticketBuffer,
                ticketBuffer.Length,
                out ticketSize,
                ref identity
            );

            if (authTicket == HAuthTicket.Invalid)
            {
                Debug.LogError("[SteamService] Steam Ticket 생성 실패");
                return null;
            }

            byte[] ticket = new byte[ticketSize];
            Array.Copy(ticketBuffer, ticket, ticketSize);

            return new SteamAuthData()
            {
                steamId = SteamUser.GetSteamID().m_SteamID,
                ticket = ticket
            };
        }

        /// <summary>
        /// 현재 세션의 인증 티켓을 취소합니다. 앱 종료 시 반드시 호출해야 합니다.
        /// </summary>
        public void CancelAuthTicket()
        {
            if (authTicket == HAuthTicket.Invalid) return;

            try
            {
                SteamUser.CancelAuthTicket(authTicket);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SteamService] 인증 티켓 취소 중 오류: {e.Message}");
            }
            finally
            {
                authTicket = HAuthTicket.Invalid;
            }
        }

        /// <summary>
        /// 게임 라이센스 보유 여부를 확인합니다.
        /// 예외 발생 시 안전하게 '미구매(false)'로 처리합니다 (fail-closed).
        /// 클라이언트 단독 체크는 우회 가능하므로, 중요한 서버 로직은
        /// CreateTicket()으로 생성한 티켓의 서버 사이드 검증과 병행해야 합니다.
        /// </summary>
        public bool CheckLicense()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[SteamService] Steam이 초기화되지 않아 라이센스를 확인할 수 없습니다.");
                return false;
            }

            try
            {
                return SteamApps.BIsSubscribed()
                    || SteamApps.BIsSubscribedFromFamilySharing()
                    || SteamApps.BIsSubscribedFromFreeWeekend();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SteamService] 라이센스 확인 중 오류 발생: {e.Message}");
                return false; // 실패 시 미구매로 간주 (fail-closed)
            }
        }
    }
}