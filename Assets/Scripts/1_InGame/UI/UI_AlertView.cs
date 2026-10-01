using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Utility;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
namespace StockGame.Scripts.UI
{
    public class UI_AlertView : UIBase
    {
        [SerializeField] private Button agreeButton;
        [SerializeField] private TMP_Text alertInfoText;

        public Button AgreeButton => agreeButton;
        public TMP_Text AlertInfoText => alertInfoText;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_AlertView, UI_AlertPresenter>();
        }
        public void SetAlert(string message)
        {
            alertInfoText.text = message;
        }
    }

    public sealed class UI_AlertPresenter : UIPresenter<UI_AlertView>
    {
        protected override void OnBind()
        {
            base.OnBind();
            View.AgreeButton?.OnClickAsObservableFirst().Subscribe(Agree).AddTo(this);
            View.AlertInfoText.text = string.Empty;
        }

        private void Agree(Unit _)
        {
            View.Close();
        }
    }
}