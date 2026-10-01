using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using System;
using UnityEngine;

namespace StockGame.Scripts.Players
{
    public readonly struct BgmZoneState
    {
        public readonly bool IsExplore;
        public readonly ZoneType? Zone;

        public BgmZoneState(bool isExplore, ZoneType? zone)
        {
            IsExplore = isExplore;
            Zone = zone;
        }

        public override bool Equals(object obj) =>
            obj is BgmZoneState other && IsExplore == other.IsExplore && Zone == other.Zone;

        public override int GetHashCode() => (IsExplore, Zone).GetHashCode();

        public static bool operator ==(BgmZoneState a, BgmZoneState b) => a.Equals(b);
        public static bool operator !=(BgmZoneState a, BgmZoneState b) => !a.Equals(b);
    }

    public class PlayerPositionTracker : MonoBehaviour
    {
        [SerializeField] private PlayerNetwork playerNetwork;
        [SerializeField] private LayerMask footstepZoneLayer;

        private const float CheckInterval = 0.15f;
        private float _timer;

        private readonly Collider2D[] _overlapBuffer = new Collider2D[4];

        public BgmZoneState CurrentBgmState { get; private set; } = new BgmZoneState(false, null);
        public FootstepSurfaceType CurrentSurface { get; private set; } = FootstepSurfaceType.Default;

        public event Action<BgmZoneState> OnBgmZoneChanged;
        public event Action<FootstepSurfaceType> OnSurfaceChanged;

        private void Start()
        {
            if (Managers.Game != null)
                Managers.Game.CurrentRound.OnValueChanged += OnRoundChanged;
        }

        private void OnDestroy()
        {
            if (Managers.Game != null)
                Managers.Game.CurrentRound.OnValueChanged -= OnRoundChanged;
        }

        private void Update()
        {
            if (playerNetwork == null || !playerNetwork.IsOwner) return;

            _timer += Time.deltaTime;
            if (_timer < CheckInterval) return;
            _timer = 0f;

            CheckPosition();
        }

        private void OnRoundChanged(GameDefine.RoundDefine.RoundInfo previous, GameDefine.RoundDefine.RoundInfo current)
        {
            if (playerNetwork == null || !playerNetwork.IsOwner) return;

            CheckPosition();
        }

        private void CheckPosition()
        {
            CheckBgmZone();
            CheckFootstepSurface();
        }

        private void CheckBgmZone()
        {
            if (!GameManager.Instance.IsGameStart.Value) return;
            bool isExplore = Managers.Game.CurrentRound.Value.RoundPhase == GameDefine.RoundDefine.RoundPhase.Explore;

            ZoneType? newZone = null;
            if (isExplore)
            {
                var zoneManager = ZoneManager.Instance;
                if (zoneManager == null) return;
                var executor = zoneManager.GetBgmExecutorByPosition(transform.position);
                newZone = executor?.ZoneType; 
            }

            var newState = new BgmZoneState(isExplore, newZone);
            if (newState == CurrentBgmState) return;

            CurrentBgmState = newState;
            OnBgmZoneChanged?.Invoke(CurrentBgmState);
        }

        private void CheckFootstepSurface()
        {
            var newSurface = GetSurfaceAtCurrentPosition();
            if (newSurface == CurrentSurface) return;

            CurrentSurface = newSurface;
            OnSurfaceChanged?.Invoke(CurrentSurface);
        }

        private FootstepSurfaceType GetSurfaceAtCurrentPosition()
        {
            int count = Physics2D.OverlapPointNonAlloc(transform.position, _overlapBuffer, footstepZoneLayer);
            if (count <= 0) return FootstepSurfaceType.Default;

            var zone = _overlapBuffer[0]?.GetComponent<FootstepZone>();
            return zone?.SurfaceType ?? FootstepSurfaceType.Default;
        }
    }
}