using TRPG_Project.Scripts.Common;
using Cysharp.Threading.Tasks;
using StockGame.Scripts.UI;
using System.Threading;

namespace StockGame.Scripts.Base
{
    public class UIPresenter<TView> : DisposableContainer where TView : UIBase
    {
        protected CancellationTokenSource cts;
        private TView view;
        public TView View => view;

        public void Initialize(TView view, CancellationTokenSource cts)
        {
            this.view = view;
            this.cts = cts;
            OnBind();
        }

        protected virtual void OnBind()
        {
            OnBindAsnyc().Forget();
        }

        protected virtual UniTask OnBindAsnyc() { return UniTask.CompletedTask; }

        public override void Dispose()
        {
            base.Dispose();
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}