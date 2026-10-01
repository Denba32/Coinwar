using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class UI_ClientView : UIBase
    {
        [SerializeField] private TMP_InputField codeInputField;
        [SerializeField] private TMP_InputField nicknameInputField;

        [SerializeField] private Button enterButton;
        [SerializeField] private Button closeButton;

        public TMP_InputField CodeInputField => codeInputField;
        public TMP_InputField NicknameInputField => nicknameInputField;
        public Button EnterButton => enterButton;
        public Button CloseButton => closeButton;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_ClientView, UI_ClientPresenter>();
        }
    }

    public sealed class UI_ClientPresenter : UIPresenter<UI_ClientView>
    {
        private string code;
        private string nickname;

        protected override void OnBind()
        {
            base.OnBind();
            View.EnterButton.OnClickAsObservableFirst().Subscribe(_ => EnterToServer().Forget()).AddTo(this);
            View.CloseButton.OnClickAsObservableFirst().Subscribe(_ => View.Close()).AddTo(this);
        }

        private async UniTask EnterToServer()
        {
            try
            {
                Managers.Input.StopUIInput();
                var loadingUI = await UIManager.Instance.Open<UI_Loading>(GameDefine.UIDefine.UILayer.Popup);

                nickname = View.NicknameInputField.text;
                code = View.CodeInputField.text;
                if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(code))
                {
                    var table = LocalizationSettings.StringDatabase.GetTable("Alert_String");
                    var localizedString = table.GetEntry("empty_nickname_code").GetLocalizedString();
                    await UIManager.Instance.Close(loadingUI);
                    var alert = await UIManager.Instance.Open<UI_AlertView>(Define.GameDefine.UIDefine.UILayer.Popup);
                    alert?.SetAlert(localizedString);
                    return;
                }

                View.EnterButton.interactable = false;
                var result = await MultiplayManager.Instance.JoinToRelayServer(code, nickname);
                if (!result)
                {
                    View.EnterButton.interactable = true;
                    await UIManager.Instance.Close(loadingUI);
                    return;
                }
                LobbyManager.Instance.SubmitPlayerDataRpc(nickname);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message); Debug.LogWarning($"{ex.Message}" +
                $"{ex.StackTrace}" +
                $"{ex.InnerException}" +
                $"{ex.Source}");
                Managers.Token.CancelAll(this);
            }
            finally
            {
                Managers.Input.StartUIInput();
            }
        }
    }
}