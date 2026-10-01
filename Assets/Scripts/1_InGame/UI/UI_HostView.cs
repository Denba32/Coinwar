using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Scenes;
using StockGame.Scripts.Utility;
using System;
using System.Threading;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class UI_HostView : UIBase
    {
        [SerializeField] private TMP_InputField nicknameInputField;
        [SerializeField] private TMP_InputField maxPlayerInputField;

        [SerializeField] private Button enterButton;
        [SerializeField] private Button closeButton;

        public TMP_InputField NickNameInputField => nicknameInputField;
        public TMP_InputField MaxPlayerInputField => maxPlayerInputField;

        public Button EnterButton => enterButton;
        public Button CloseButton => closeButton;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_HostView, UI_HostPresenter>();
        }
    }

    public class UI_HostPresenter : UIPresenter<UI_HostView>
    {
        private int maxPlayer = 8;
        private string userName;

        protected override void OnBind()
        {
            base.OnBind();
            var token = Managers.Token.GetToken(this);
            View.EnterButton.OnClickAsObservableFirst().Subscribe(_ => CreateLobby(token).Forget()).AddTo(this);
            View.CloseButton.OnClickAsObservableFirst().Subscribe(_ => View.Close()).AddTo(this);
        }

        private async UniTask CreateLobby(CancellationToken token)
        {
            try
            {
                Managers.Input.StopUIInput();
                var table = LocalizationSettings.StringDatabase.GetTable("Alert_String");

                if (string.IsNullOrEmpty(View.MaxPlayerInputField.text) || string.IsNullOrEmpty(View.NickNameInputField.text))
                {
                    var localizedString = table.GetEntry("empty_nickname_player_number").GetLocalizedString();
                    var alert = await UIManager.Instance.Open<UI_AlertView>(GameDefine.UIDefine.UILayer.Popup);
                    alert?.SetAlert(localizedString);
                    return;
                }

                maxPlayer = int.Parse(View.MaxPlayerInputField.text);
                userName = View.NickNameInputField.text;
                View.EnterButton.interactable = false;

                if (maxPlayer < 2 || maxPlayer > 8)
                {
                    var localizedString = table.GetEntry("player_count_range_invalid").GetLocalizedString();
                    var alert = await UIManager.Instance.Open<UI_AlertView>(GameDefine.UIDefine.UILayer.Popup);
                    alert?.SetAlert(localizedString);
                    View.EnterButton.interactable = true;
                    return;
                }
                var result = await Managers.Multiplay.CreateRelayServer(maxPlayer, userName);
                if (!result) View.EnterButton.interactable = true;
                var joinCode = Managers.Multiplay.JoinCode;
                LobbyManager.Instance.SubmitPlayerDataRpc(userName);
                var param = new LobbySceneParam(playerName: userName, joinCode: joinCode);
                await NetworkSceneManager.Instance.ChangeScene(SceneEnum.LobbyScene, nextParam: param, useNetworkSceneManager: true, fadeTime: 2f, token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message); Debug.LogWarning($"{ex.Message}\n" +
                $"{ex.StackTrace}\n" +
                $"{ex.InnerException}\n" +
                $"{ex.Source}");
                Managers.Token.Cancel(this);
            }
            finally
            {
                Managers.Input.StartUIInput();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            Managers.Token.CancelAll(this);
        }
    }
}