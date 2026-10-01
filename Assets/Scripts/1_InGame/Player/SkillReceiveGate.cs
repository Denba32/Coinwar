using UnityEngine;

namespace StockGame.Scripts.Players
{
    /// <summary>
    /// 스킬 수신 1건을 직렬화하는 단순 블록(block) 모듈.
    ///
    /// 수신자가 스킬을 처리 중이면(<see cref="IsProcessing"/> == true) 그 사이 들어오는
    /// 모든 스킬 수신 요청을 거부하도록 하기 위한 상태 홀더다.
    ///
    /// 필요한 이유: 스턴/슬로우/중독 등은 RPC → Owner의 NetworkVariable 변경 → 재복제까지
    /// 여러 프레임이 걸린다. 그 사이에는 <c>CanReceive()</c>가 아직 true라서
    /// 같은 프레임에 겹친 설치물(Gum 2개)이나 스킬 연타가 모두 판정을 통과해
    /// 중복으로 효과가 적용되고 설치물이 여러 개 파괴된다.
    /// 이 게이트는 "접수 즉시" 로컬에서 잠기므로 그 창을 닫는다.
    /// </summary>
    public sealed class SkillReceiveGate
    {
        private bool _isProcessing;
        private float _autoReleaseAt;

        /// <summary>현재 스킬 1건을 처리 중인지 여부.</summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// 게이트 점유를 시도한다. 이미 처리 중이면 false.
        /// </summary>
        /// <param name="maxHoldSeconds">
        /// <see cref="Release"/> 호출이 유실될 경우를 대비한 자동 해제 시간(초).
        /// (예: 효과 적용 대상이 Owner 검증에서 튕겨 상태 변화가 아예 없는 경우)
        /// </param>
        public bool TryAcquire(float maxHoldSeconds = 1f)
        {
            if (_isProcessing) return false;

            _isProcessing = true;
            _autoReleaseAt = Time.time + maxHoldSeconds;
            return true;
        }

        /// <summary>처리 완료 처리. 다음 스킬 수신을 허용한다.</summary>
        public void Release() => _isProcessing = false;

        /// <summary>
        /// 소유 <see cref="MonoBehaviour"/>의 Update에서 매 프레임 호출.
        /// <see cref="Release"/>가 유실돼도 maxHoldSeconds 후 자동 복구한다.
        /// </summary>
        public void Tick()
        {
            if (_isProcessing && Time.time >= _autoReleaseAt)
                _isProcessing = false;
        }
    }
}
