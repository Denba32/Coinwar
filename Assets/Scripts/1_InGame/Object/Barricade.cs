using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Objects
{
    public class Barricade : NetworkBehaviour
    {
        private NetworkVariable<bool> globalActive = new NetworkVariable<bool>(true);
        private CancellationTokenSource _timerCts;
        private GameManager _subscribedGameManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log("OnNetworkSpawn : Barricade");

            globalActive.OnValueChanged -= OnChangedActive;
            globalActive.OnValueChanged += OnChangedActive;
            gameObject.SetActive(globalActive.Value);

            _subscribedGameManager = Managers.Game;
            if (_subscribedGameManager != null)
            {
                _subscribedGameManager.CurrentRound.OnValueChanged -= OnRoundChanged;
                _subscribedGameManager.CurrentRound.OnValueChanged += OnRoundChanged;
            }

            if (!IsServer) return;
            globalActive.Value = false;
        }

        private void OnRoundChanged(GameDefine.RoundDefine.RoundInfo previousValue, GameDefine.RoundDefine.RoundInfo newValue)
        {
            if (this == null || !IsSpawned) return;
            if (newValue.RoundPhase == GameDefine.RoundDefine.RoundPhase.Explore) return;
            if (!IsServer) return;
            globalActive.Value = false;
        }

        private void OnChangedActive(bool previousValue, bool newValue)
        {
            gameObject.SetActive(newValue);
            if (!IsServer) return;

            // 이전 타이머 항상 취소 후 정리
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;

            if (!newValue) return;

            // 활성화됐을 때만 새 타이머 시작
            _timerCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            RunDeactivateTimerAsync(_timerCts.Token).Forget();
        }

        private async UniTaskVoid RunDeactivateTimerAsync(CancellationToken token)
        {
            try
            {
                await UniTask.WaitForSeconds(45f, cancellationToken: token);
                globalActive.Value = false;
            }
            catch (OperationCanceledException) { }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Unsubscribe();

            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;
        }

        public override void OnDestroy()
        {
            Unsubscribe();

            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;

            base.OnDestroy();
        }

        private void Unsubscribe()
        {
            globalActive.OnValueChanged -= OnChangedActive;

            if (_subscribedGameManager != null && _subscribedGameManager.CurrentRound != null)
                _subscribedGameManager.CurrentRound.OnValueChanged -= OnRoundChanged;
            _subscribedGameManager = null;
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void SetActiveGlobalRpc(bool isActive)
        {
            if (!IsServer) return;
            globalActive.Value = isActive;
        }
    }
}