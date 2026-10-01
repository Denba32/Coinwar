using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.UI
{
    public class UI_JobGuideView : UIBase
    {
        [SerializeField] private BaseButton closeButton;
        public BaseButton CloseButton => closeButton;
        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_JobGuideView, UI_JobGuidePresenter>();
        }
    }

    public class UI_JobGuidePresenter : UIPresenter<UI_JobGuideView>
    {
        protected override void OnBind()
        {
            base.OnBind();
            View.CloseButton.OnClickAsObservableFirst().Subscribe(OnClose).AddTo(this);
        }

        private void OnClose(Unit _)
        {
            View.Close();
        }
    }
}