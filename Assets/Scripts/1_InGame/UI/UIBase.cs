using Cysharp.Threading.Tasks;
using DG.Tweening;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public abstract class UIBase : MonoBehaviour, IDisposable
    {
        protected IDisposable presenter;

        [SerializeField] protected Canvas canvas;
        [SerializeField] protected CanvasScaler scaler;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected GameDefine.UIDefine.UILayer uiLayer;

        [SerializeField] private bool isNoneCloseBinding = false;
        private bool isDisposed = false;
        protected bool isClosing = false;
        public bool isInitialized = false;

        protected GameDefine.InputDefine.InputBindingContext inputBindingContext;
        public void ActiveCanvas(bool isActive) => canvas.enabled = isActive;

        public virtual void OnOpen(params object[] args) { }
        public virtual UniTask OnClose(params object[] args) { isClosing = true; return UniTask.CompletedTask; }

        public virtual void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            canvas.sortingOrder = sortOrder;
            this.uiLayer = uiLayer;

            if (uiLayer != GameDefine.UIDefine.UILayer.Popup || isNoneCloseBinding) return;
            inputBindingContext = new(gameObject.name);
            inputBindingContext.Bind("Escape", () =>
            {
                Managers.Sound?.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX251);
                Close();
            });
            Managers.Input.PushBindingContext(inputBindingContext);
        }

        public TPresenter Bind<TView, TPresenter>() where TView : UIBase where TPresenter : UIPresenter<TView>, new()
        {
            var p = new TPresenter();
            p.Initialize(this as TView, new System.Threading.CancellationTokenSource());
            presenter = p;
            return p;
        }

        public virtual void Close()
        {
            Dispose();
            UIManager.Instance?.Close(this).Forget();
        }

        public virtual void Show()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public async UniTask ShowAsync(float duration, bool isInteractable = true)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
            var token = Managers.Token.GetToken(this);
            await canvasGroup.DOFade(1f, duration).SetEase(Ease.InQuad).SetLink(gameObject).ToUniTask(cancellationToken: token);
        }

        public virtual void Hide()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public async UniTask HideAsync(float duration, bool isInteractable = true, CancellationToken token = default)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, this.GetCancellationTokenOnDestroy());
            await canvasGroup.DOFade(0f, duration).SetEase(Ease.InQuad).SetLink(gameObject).ToUniTask(cancellationToken: linkedToken.Token);
        }

        public int GetOrder()
        {
            if (this == null || canvas == null) return 0; // Fake Null 체크
            return canvas.sortingOrder;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            var input = Managers.Input;
            if(input != null) input?.PopBindingContext(inputBindingContext); // 등록했던 바인딩 해제

            Managers.Token.Cancel(this);
            presenter?.Dispose();
            OnDispose();
        }

        public virtual void OnDispose() { }
    }
}