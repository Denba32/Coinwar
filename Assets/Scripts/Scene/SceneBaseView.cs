using Cysharp.Threading.Tasks;
using FMODUnity;
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
        protected TView View;
        public string BGMPath { get; }

        public SceneBaseController() { }
        public SceneBaseController(string bgmPath = "") { this.BGMPath = bgmPath; }

        public virtual UniTask Enter(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask Exit(CancellationToken token)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask Initialize(CancellationToken cts)
        {
            ApplySceneBGM(this);
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

        private void ApplySceneBGM(ISceneController sceneController)
        {
            Managers.Sound.ReplaceBaseBGM(sceneController.BGMPath);
        }
    }

    public abstract class SceneParam { }
}