using StockGame.Common.Interfaces;
using StockGame.Scripts.Define;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;
using UniRx;
using StockGame.Scripts.Manager;

namespace StockGame.Scripts.Scenes
{
    public abstract class SceneBaseView : MonoBehaviour
    {
        public SceneEnum sceneEnum = SceneEnum.BootScene;
    }

    public abstract class SceneBaseController<TView> : ISceneController where TView : SceneBaseView
    {
        protected CompositeDisposable _disposables = new();
        protected TView View;
        protected string bgmPath;

        public SceneBaseController() { }
        public SceneBaseController(string bgmPath = "") { this.bgmPath = bgmPath; }

        public virtual UniTask Enter(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask Exit(CancellationToken token)
        {
            Managers.Sound.StopBgm();
            return UniTask.CompletedTask;
        }

        public virtual UniTask Initialize(CancellationToken cts)
        {
            if (string.IsNullOrEmpty(bgmPath)) return UniTask.CompletedTask;
            Managers.Sound.PlaySound(bgmPath, SoundType.BGM);
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        public void SetView(SceneBaseView view)
        {
            View = (TView)view;
        }
    }

    public abstract class SceneParam { }
}