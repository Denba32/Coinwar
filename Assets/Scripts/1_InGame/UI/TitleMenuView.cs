using StockGame.Scripts.Manager;
using UnityEngine.UI;
using UnityEngine;
using StockGame.Scripts.Base;
using Cysharp.Threading.Tasks;
using UniRx;
using StockGame.Scripts.Utility;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StockGame.Scripts.UI
{
    public class TitleMenuView : UIBase
    {
        [SerializeField] private Button enterHostbutton;
        [SerializeField] private Button enterLocalButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button gameQuitButton;
        [SerializeField] private Image localLabelImage;
        [SerializeField] private DropCoinUI dropCoin;
        public Button EnterHostbutton => enterHostbutton;
        public Button EnterClientButton => enterLocalButton;
        public Button OptionButton => optionButton;
        public Button GameQuitButton => gameQuitButton;
        public DropCoinUI DropCoin => dropCoin;
    }

    public class TitleMenuPresenter : UIPresenter<TitleMenuView>
    {
        protected override void OnBind()
        {
            base.OnBind();
            View.EnterHostbutton?.OnClickAsObservableFirst().Subscribe(_ => EnterHost().Forget()).AddTo(this);
            View.EnterClientButton?.OnClickAsObservableFirst().Subscribe(_ => EnterClient().Forget()).AddTo(this);
            View.OptionButton?.OnClickAsObservableFirst().Subscribe(_ => ActiveOption().Forget()).AddTo(this);
            View.GameQuitButton?.OnClickAsObservableFirst().Subscribe(ExitGame).AddTo(this);
        }

        protected override UniTask OnBindAsnyc()
        {
            return base.OnBindAsnyc();
        }

        private async UniTask EnterHost()
        {
            await Managers.UI.Open<UI_HostView>(Define.GameDefine.UIDefine.UILayer.Popup);
        }

        private async UniTask EnterClient()
        {
            await Managers.UI.Open<UI_ClientView>(Define.GameDefine.UIDefine.UILayer.Popup);
        }

        private async UniTask ActiveOption()
        {
            await Managers.UI.Open<UI_OptionView>(Define.GameDefine.UIDefine.UILayer.Popup);
        }

        private void ExitGame(Unit _)
        {

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
Application.Quit();
#endif
        }
    }
}