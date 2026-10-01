using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class NewsUI : UIBase
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button submitButton;

        public const float NewsDisplayDuration = 5f;
        public const float NewsFadeOutDuration = 1f;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            closeButton.OnClickAsObservableFirst().Subscribe(_ => Close()).AddTo(this);
            submitButton.OnClickAsObservableFirst().Subscribe(Submit).AddTo(this);
        }

        public override void Close()
        {
            base.Close();
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX251);
        }

        private void Submit(Unit _)
        {
            Managers.Game.RegistNewsRpc(inputField.text);
            Close();
        }

        public void ShowNews(string text)
        {
            inputField.readOnly = true;
            submitButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(false);
            inputField.text = string.IsNullOrEmpty(text) ? " " : text;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}