using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class UI_StockTimer : UIBase
    {
        [SerializeField] private TMP_Text timerText = null;
        [SerializeField] private TMP_Text skipPlayerDisplayText = null;
        [SerializeField] private Button skipButton = null;
        public IObservable<Unit> OnClickAsObservable => skipButton.OnClickAsObservable();

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Managers.Game.OnSkip.Subscribe(SetSkipCount).AddTo(this);
        }

        public void OnTimerValueChanged(int newValue)
        {
            if (Managers.Game.CurrentRound.Value.RoundPhase == GameDefine.RoundDefine.RoundPhase.Explore) return;

            int totalSeconds = newValue;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        public void ActivateSkipButton()
        {
            skipButton.interactable = true;
            SetSkipCount();
        }

        private void SetSkipCount(int count = 0)
        {
            var allPlayerCount = Managers.Game.JoinInfos.Count;
            if(skipPlayerDisplayText != null)
                skipPlayerDisplayText.text = $"{count} / {allPlayerCount}";
        }

        public void SetSkipInteract(bool isActive) => skipButton.interactable = isActive;
        public void OnClickSkipButton()
        {
            if (NetworkManager.Singleton.LocalClient != null)
            {
                Debug.Log("Skip");
                skipButton.interactable = false;
                GameManager.Instance.SendSkipVoteServerRpc();
            }
        }
    }
}