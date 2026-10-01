using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Chat
{
    public class LobbyChatPanel : MonoBehaviour
    {
        private readonly CompositeDisposable _disposables = new();

        [SerializeField] private RectTransform chatRect;
        [SerializeField] private RectTransform chatPanelRect;

        [SerializeField] private ScrollRect scrollRectChat;
        [SerializeField] private TMP_InputField chatInputField;
        [SerializeField] private CanvasGroup chatIconCanvasGroup;
        [SerializeField] private CanvasGroup chatCanvasGroup;

        [SerializeField] private BaseButton sendChatButton;
        [SerializeField] private BaseButton closeChatButton;
        [SerializeField] private BaseButton openChatButton;

        [SerializeField] private ChatMessageUI chatMessagePrefab;

        private ChatManager Chat => ChatManager.Instance;

        private bool isChatOpened;

        private bool IsInputFocused =>
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == chatInputField.gameObject;

        public void Initialize()
        {
            ChatManager.Instance.SetChatPanelContent(scrollRectChat.content);

            // 버튼
            closeChatButton.OnClickAsObservableFirst()?.Subscribe(CloseChat).AddTo(_disposables);
            openChatButton?.OnClickAsObservableFirst()?.Subscribe(OpenChat).AddTo(_disposables);

            InputManager.Instance.OnSubmit.Subscribe(HandleSubmit).AddTo(_disposables);

            // TMP 이벤트
            chatInputField.OnSelectAsObservable().Subscribe(OnInputSelected).AddTo(_disposables);
            chatInputField.OnDeselectAsObservable().Subscribe(OnInputDeselected).AddTo(_disposables);
            chatInputField.OnEndEditAsObservable().Subscribe(OnEndEdit).AddTo(_disposables);

            // 채팅 수신 구독
            Chat.OnMessageReceived
                .Subscribe(AppendChatMessage)
                .AddTo(_disposables);

            CloseChat(Unit.Default);
        }

        private void OpenChat(Unit _)
        {
            chatCanvasGroup.alpha = 1f;
            chatCanvasGroup.blocksRaycasts = true;
            chatCanvasGroup.interactable = true;
            isChatOpened = true;

            chatIconCanvasGroup.alpha = 0f;
            chatIconCanvasGroup.blocksRaycasts = false;
            chatIconCanvasGroup.interactable = false;
        }

        private void CloseChat(Unit _)
        {
            chatCanvasGroup.alpha = 0f;
            chatCanvasGroup.blocksRaycasts = false;
            chatCanvasGroup.interactable = false;
            isChatOpened = false;

            chatIconCanvasGroup.alpha = 1f;
            chatIconCanvasGroup.blocksRaycasts = true;
            chatIconCanvasGroup.interactable = true;
        }

        private void HandleSubmit(Unit _)
        {
            if (!isChatOpened) return;

            if (!IsInputFocused)
                FocusInput();
        }

        private void FocusInput()
        {
            if (chatInputField == null) return;
            if (!IsInputFocused)
            {
                chatInputField.ActivateInputField();
                chatInputField.MoveTextEnd(false);
            }
        }

        private void UnfocusInput()
        {
            Observable.NextFrame()
                .Subscribe(_ =>
                {
                    if (EventSystem.current == null) return;
                    EventSystem.current.SetSelectedGameObject(null);
                })
                .AddTo(this);
        }

        private void OnInputSelected(BaseEventData eventData)
        {
            InputManager.Instance.StopPlayerInput();
        }

        private void OnInputDeselected(BaseEventData eventData)
        {
            InputManager.Instance.StartPlayerInput();
        }

        private void OnEndEdit(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                UnfocusInput();
                return;
            }

            SendChat(message);
            chatInputField.ActivateInputField();
        }

        private void SendChat(string text)
        {
            chatInputField.text = string.Empty;

            var localClientId = Chat.NetworkManager.LocalClientId;
            Chat.RequestMessageRpc(localClientId, text);
        }

        private void AppendChatMessage(ChatLog log)
        {
            var item = Instantiate(chatMessagePrefab, scrollRectChat.content);
            item.SetData(log);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scrollRectChat.verticalNormalizedPosition = 0f;
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}