using StockGame.Scripts.Base;
using StockGame.Scripts.Define;

namespace StockGame.Scripts.UI
{
    public class UI_Loading : UIBase
    {
        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_Loading, UI_LoadingPresenter>();
        }
    }

    public class UI_LoadingPresenter : UIPresenter<UI_Loading>
    {
        protected override void OnBind()
        {
            base.OnBind();
        }
    }
}