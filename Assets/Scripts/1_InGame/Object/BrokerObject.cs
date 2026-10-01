using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.UI;
using System;
using UnityEngine;

namespace StockGame.Scripts.Objects
{
    public sealed class BrokerObject : ObjectInteractor
    {
        private Action onClose = null;
        private bool isSubscribed = false;

        public Collider2D Col;
        public void Initialize()
        {
            isInteracting = false;
            locked = false;
            onClose = OnClose;

            if (!isSubscribed)
            {
                GameManager.Instance.CurrentRound.OnValueChanged += OnRoundChanged;
                isSubscribed = true;
            }
        }

        private void OnRoundChanged(GameDefine.RoundDefine.RoundInfo previousValue, GameDefine.RoundDefine.RoundInfo newValue)
        {
            if (newValue.RoundPhase != GameDefine.RoundDefine.RoundPhase.Explore)
            {
                gameObject.SetActive(false); // Destroy(gameObject) 대신 - 씬 고정 오브젝트라 파괴하면 다음 라운드에 못 씀
            }
        }

        public override void OnInteract(InteractorContext ctx)
        {
            base.OnInteract(ctx);
            var localPlayer = Managers.Game.GetLocalPlayerInfo();

            if (localPlayer.Coin < 1)
            {
                isInteracting = false;
                return;
            }
            Managers.UI.Open<UI_BrokerShop>(GameDefine.UIDefine.UILayer.Popup, path:string.Empty, onClose).Forget();
        }

        private void OnClose()
        {
            isInteracting = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            var gameManager = Managers.Game;
            if (gameManager == null) return;
            GameManager.Instance.CurrentRound.OnValueChanged -= OnRoundChanged;
            isSubscribed = false;
        }
    }
}