using Cysharp.Threading.Tasks;
using StockGame.Common.Interfaces;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System.Threading;
using UniRx;
using UnityEngine;

namespace StockGame.Scripts.Scenes
{
    public abstract class SceneBaseView : MonoBehaviour
    {
        public SceneEnum sceneEnum = SceneEnum.BootScene;
    }

    public abstract class SceneBaseController<TView> : ISceneController where TView : SceneBaseView
    {
        protected CompositeDisposable _disposables = new();
        protected GameDefine.InputDefine.InputBindingContext _inputBidingContext;
        protected TView View;
        public string BGMPath { get; }

        public SceneBaseController() { }
        public SceneBaseController(string bgmPath = "") { this.BGMPath = bgmPath; }

        public virtual UniTask Enter(CancellationToken token)
        {
            _inputBidingContext = new(typeof(TView).Name);
            Managers.Input.PushBindingContext(_inputBidingContext);
            return UniTask.CompletedTask;
        }

        public virtual UniTask Exit(CancellationToken token)
        {
            Managers.Input.PopBindingContext(_inputBidingContext); 
            return UniTask.CompletedTask;
        }

        public virtual UniTask Initialize(CancellationToken cts)
        {
            ApplySceneBGM(this);
            return UniTask.CompletedTask;
        }

        public void SetView(SceneBaseView view)
        {
            View = (TView)view;
        }

        private void ApplySceneBGM(ISceneController sceneController)
        {
            Managers.Sound.ReplaceBaseBGM(sceneController.BGMPath);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }

    public abstract class SceneParam { }
}