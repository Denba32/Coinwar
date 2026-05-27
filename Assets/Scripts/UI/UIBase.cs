using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StockGame.Scripts.Base;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StockGame.Scripts.UI
{
    public abstract class UIBase : MonoBehaviour, IDisposable
    {
        protected Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();
        protected IDisposable presenter;

        [SerializeField] protected Canvas canvas;
        [SerializeField] protected CanvasScaler scaler;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected GameDefine.UIDefine.UILayer uiLayer;

        private bool isDisposed = false;
        public bool isInitialized = false;

        public virtual void OnOpen(params object[] args) { }
        public virtual UniTask OnClose(params object[] args) { return UniTask.CompletedTask; }

        public virtual void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            canvas.sortingOrder = sortOrder;
            this.uiLayer = uiLayer;
        }

#if UNITY_EDITOR
        [ContextMenu(nameof(AutoBindComponent))]
        private void AutoBindComponent()
        {
            canvas = GetComponent<Canvas>();
            scaler = GetComponent<CanvasScaler>();
            canvasGroup = GetComponent<CanvasGroup>();
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        [ContextMenu("Auto Binding")]
        protected void Bind()
        {
            var fields = GetType().GetFields();

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject))
                {
                    var obj = Util.FindChild<Transform>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(Button))
                {
                    var obj = Util.FindChild<Button>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(Image))
                {
                    var obj = Util.FindChild<Image>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(TMP_Text))
                {
                    var obj = Util.FindChild<TMP_Text>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(Slider))
                {
                    var obj = Util.FindChild<Slider>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(TMP_Dropdown))
                {
                    var obj = Util.FindChild<TMP_Dropdown>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(Toggle))
                {
                    var obj = Util.FindChild<Toggle>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(ScrollRect))
                {
                    var obj = Util.FindChild<ScrollRect>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(Scrollbar))
                {
                    var obj = Util.FindChild<Scrollbar>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(Transform))
                {
                    var obj = Util.FindChild<Transform>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(RectTransform))
                {
                    var obj = Util.FindChild<RectTransform>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
                else if (field.FieldType == typeof(TMP_InputField))
                {
                    var obj = Util.FindChild<TMP_InputField>(gameObject, field.Name, true);
                    field.SetValue(this, obj);
                }
            }
        }
#endif
        public TPresenter Bind<TView, TPresenter>() where TView : UIBase where TPresenter : UIPresenter<TView>, new()
        {
            var p = new TPresenter();
            p.Initialize(this as TView, new System.Threading.CancellationTokenSource());
            presenter = p;
            return p;
        }

        protected void Bind<T>(Type type) where T : UnityEngine.Object
        {
            string[] names = Enum.GetNames(type);
            UnityEngine.Object[] objs = new UnityEngine.Object[names.Length];

            _objects.Add(type, objs);

            for (int i = 0; i < names.Length; i++)
            {
                if (typeof(T) == typeof(GameObject))
                    objs[i] = Util.FindChild(gameObject, names[i], true);
                else
                    objs[i] = Util.FindChild<T>(gameObject, names[i], true);

                if (objs[i] == null)
                    Debug.Log($"Failed to Bind ({names[i]})");
            }
        }

        protected T Get<T>(int idx) where T : UnityEngine.Object
        {
            UnityEngine.Object[] objs = null;

            if (_objects.TryGetValue(typeof(T), out objs) == false)
                return null;

            return objs[idx] as T;
        }

        protected Button GetButton(int idx) => Get<Button>(idx);
        protected TMP_Text GetTMPText(int idx) => Get<TMP_Text>(idx);
        protected Image GetImage(int idx) => Get<Image>(idx);
        protected GameObject GetObject(int idx) => Get<GameObject>(idx);
        protected Slider GetSlider(int idx) => Get<Slider>(idx);
        protected Toggle GetToggle(int idx) => Get<Toggle>(idx);
        protected Dropdown GetDropdown(int idx) => Get<Dropdown>(idx);
        protected Outline GetOutline(int idx) => Get<Outline>(idx);
        protected RectTransform GetRectTransform(int idx) => Get<RectTransform>(idx);
        protected TMP_InputField GetTMPInputField(int idx) => Get<TMP_InputField>(idx);

        public void Close()
        {
            Dispose();
            UIManager.Instance.Close(this).Forget();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public virtual void Show()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public virtual void Hide()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public int GetOrder() => canvas?.sortingOrder ?? 0;

        public virtual void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            presenter?.Dispose();

            var objs = _objects.Values;
            if (objs != null)
            {
                foreach (var objList in objs)
                {
                    foreach (var obj in objList)
                    {
                        Destroy(obj);
                    }
                }
            }
            _objects?.Clear();
            _objects = null;
        }
    }
}