using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.UI;
using System.Threading;
using UnityEngine;

namespace StockGame.Scripts.Scenes
{
    public class TitleSceneView : SceneBaseView
    {
        [SerializeField] private TitleMenuView titleMenu;
        public TitleMenuView TitleMenu => titleMenu;
    }

    public sealed class TitleSceneController : SceneBaseController<TitleSceneView>
    {
        public TitleSceneController(){ }

        public TitleSceneController(string bgmPath = "") : base(bgmPath){ }

        public override UniTask Enter(CancellationToken token)
        {
            return base.Enter(token);
        }

        public override async UniTask Initialize(CancellationToken token)
        {
            Managers.UI.RegisterUI(View.TitleMenu, Define.GameDefine.UIDefine.UILayer.SceneUI);
            View.TitleMenu.Bind<TitleMenuView, TitleMenuPresenter>();
            Managers.Sound.PlayBgm("BGM001");
            await base.Initialize(token);
        }
        public override UniTask Exit(CancellationToken token)
        {
            return base.Exit(token);
        }
    }
}