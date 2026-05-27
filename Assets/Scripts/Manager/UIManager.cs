using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.UI;
using StockGame.Scripts.UI.Missions;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine;

namespace StockGame.Scripts.Manager
{
    public class UIManager : MonoSingleton<UIManager>
    {
        private Dictionary<UIDefine.UILayer, LinkedList<UIBase>> UIDict = new();
        private UIRoot root = null;
        
        public IDragItem CurrentDragItem { get; private set; }
        public IPickedItem CurrentPickedItem { get; private set; }

        private void Update()
        {
            if (CurrentPickedItem == null) return;
            CurrentPickedItem?.Move(Input.mousePosition);
        }

        public void RegisterUI<T>(T target, UIDefine.UILayer uiLayer) where T : UIBase
        {
            int order = 0;
            if (!UIDict.ContainsKey(uiLayer))
            {
                UIDict?.Add(uiLayer, new LinkedList<UIBase>());
                order = (int)uiLayer;
            }
            var list = UIDict[uiLayer];

            int currentOrder;

            if (list.Count == 0)
            {
                // 첫 UI면 레이어 기준값
                currentOrder = (int)uiLayer;
            }
            else
            {
                var last = list.Last.Value;
                currentOrder = last.GetOrder() + 1;
            }
            target?.Initilaize(currentOrder, uiLayer);
            UIDict[uiLayer].AddLast(target);
        }
        public async UniTask<T> Open<T>(T target, UIDefine.UILayer uiLayer, CancellationToken token = default, params object[] param) where T : UIBase
        {
            if (root == null) CreateRoot();

            var ui = Instantiate(target);

            int order = 0;
            if (!UIDict.ContainsKey(uiLayer))
            {
                UIDict?.Add(uiLayer, new LinkedList<UIBase>());
                order = (int)uiLayer;
            }

            ui.transform.SetParent(root.transform);
            ui.name = target.name;
            ui.gameObject.SetActive(true);

            var list = UIDict[uiLayer];

            int currentOrder;

            if (list.Count == 0)
            {
                // 첫 UI면 레이어 기준값
                currentOrder = (int)uiLayer;
            }
            else
            {
                var last = list.Last.Value;
                currentOrder = last.GetOrder() + 1;
            }

            ui.Initilaize(currentOrder, uiLayer);
            ui.OnOpen(param);
            UIDict[uiLayer].AddLast(ui);

            return await UniTask.FromResult<T>(ui);
        }

        public async UniTask<T> Open<T>(UIDefine.UILayer uiLayer, string path = "", params object[] param) where T : UIBase
        {
            if (root == null) CreateRoot();

            string _path = string.IsNullOrEmpty(path) ? $"UI/{typeof(T).Name}" : $"UI/{path}";
            T ui = ResourceManager.Instance.Instantiate<T>(_path);

            int order = 0;
            if (!UIDict.ContainsKey(uiLayer))
            {
                UIDict?.Add(uiLayer, new LinkedList<UIBase>());
                order = (int)uiLayer;
            }

            ui.transform.SetParent(root.transform);
            ui.name = typeof(T).Name;
            ui.gameObject.SetActive(true);

            var list = UIDict[uiLayer];

            int currentOrder;

            if (list.Count == 0)
            {
                // 첫 UI면 레이어 기준값
                currentOrder = (int)uiLayer;
            }
            else
            {
                var last = list.Last.Value;
                currentOrder = last.GetOrder() + 1;
            }

            ui.Initilaize(currentOrder, uiLayer);
            ui.OnOpen(param);
            UIDict[uiLayer].AddLast(ui);
            return await UniTask.FromResult<T>(ui);
        }

        private void CreateRoot(string rootName = "")
        {
            if (root != null) return;
            root = ResourceManager.Instance.Instantiate<UIRoot>($"UI/{nameof(UIRoot)}");
            if (root == null) return;
            root.transform.SetParent(transform);
        }

        public async UniTask Close(UIBase ui)
        {
            if (ui == null) return;

            foreach (var pair in UIDict)
            {
                var list = pair.Value;
                var node = list.Find(ui);

                if (node != null)
                {
                    list.Remove(node);

                    if (list.Count == 0)
                        UIDict.Remove(pair.Key);

                    break;
                }
            }

            if (ui.gameObject != null)
            {
                await ui.OnClose();
                Destroy(ui.gameObject);
            }           
        }

        public void SetDrag(IDragItem dragItem) => CurrentDragItem = dragItem;
        public void Pick(IPickedItem pickItem) => CurrentPickedItem = pickItem;
        public override void Clear()
        {
            base.Clear();

            var closeList = new List<UIBase>();

            foreach (var pair in UIDict)
            {
                foreach (var ui in pair.Value)
                {
                    if (ui != null)
                        closeList.Add(ui);
                }
            }

            foreach (var ui in closeList)
            {
                Close(ui).Forget();
            }

            UIDict.Clear();

            CurrentDragItem = null;
            CurrentPickedItem = null;
        }

        public async UniTask ClearWithoutTransition()
        {
            var closeList = new List<UIBase>();

            foreach (var pair in UIDict)
            {
                var layer = pair.Key;

                if (layer == UIDefine.UILayer.Transition || layer == UIDefine.UILayer.SceneUI)
                    continue;

                foreach (var ui in pair.Value)
                {
                    if (ui != null)
                        closeList.Add(ui);
                }
            }

            foreach (var ui in closeList)
            {
                await Close(ui);
            }
        }

        void OnDestroy()
        {
            root = null;
        }
    }
}