using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.UI.Chat;
using StockGame.Scripts.Utility;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class UI_LobbyView : MonoBehaviour
    {
        private CompositeDisposable _disposables = new();
        [Space(10)]
        [Header("RoomInformation")]
        [SerializeField] private LobbyRoomInformationPanel lobbyRoomInformationPanel;

        [Space(10)]
        [Header("Chat")]
        [SerializeField] private LobbyChatPanel lobbyChatPanel;

        [Space(10)]
        [Header("Player Information")]
        [SerializeField] private LobbyPlayerPanel lobbyPlayerInfo;

        [Space(10)]
        [Header("Buttons")]
        [SerializeField] private BaseButton playButton;
        [SerializeField] private BaseButton jobDescriptionButton;
        [SerializeField] private BaseButton optionButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private BaseButton exitButton;

        public void Initialize()
        {
            lobbyRoomInformationPanel?.Initialize();
            lobbyChatPanel?.Initialize();
            lobbyPlayerInfo?.Initialize();

            playButton.OnClickAsObservableFirst().Subscribe(_ => StartGame().Forget()).AddTo(_disposables);
            jobDescriptionButton.OnClickAsObservableFirst().Subscribe(_ => OpenJobDescription().Forget()).AddTo(_disposables);
            optionButton.OnClickAsObservableFirst().Subscribe(_ => OpenOption().Forget()).AddTo(_disposables);
            tutorialButton.OnClickAsObservableFirst().Subscribe(_ => OpenTutorialUI()).AddTo(_disposables);
            exitButton.OnClickAsObservableFirst().Subscribe(Exit).AddTo(_disposables);
        }

        public void ActiveGameStart(bool isActive)
        {
            playButton.gameObject.SetActive(isActive);
        }

        private void OpenTutorialUI()
        {
            Managers.UI.Open<TutorialUI>(Define.GameDefine.UIDefine.UILayer.Popup).Forget();
        }

        private async UniTask OpenJobDescription()
        {
            await Managers.UI.Open<UI_JobGuideView>(Define.GameDefine.UIDefine.UILayer.Popup);
        }

        private async UniTask OpenOption()
        {
            await Managers.UI.Open<UI_OptionView>(Define.GameDefine.UIDefine.UILayer.Popup);
        }

        private async UniTask StartGame()
        {
            var table = LocalizationSettings.StringDatabase.GetTable("Alert_String");

            if (!NetworkManager.Singleton.IsServer) return;
#if !UNITY_EDITOR
            if(LobbyManager.Instance.PlayerDataList.Count < 2) // 이 부분도 로컬라이제이션이 필요
            {
                var popup = await Managers.UI.Open<UI_AlertView>(Define.GameDefine.UIDefine.UILayer.Popup);
                var localizedString = table.GetEntry("min_player_count_required").GetLocalizedString();
                popup.SetAlert(localizedString);
                return;
            }
#endif
            if (LobbyManager.Instance.PlayerDataList.Count > MultiplayManager.Instance.MaxUserCount)
            {
                var popup = await Managers.UI.Open<UI_AlertView>(Define.GameDefine.UIDefine.UILayer.Popup);
                var localizedString = table.GetEntry("lobby_user_count_exceeded").GetLocalizedString();
                popup.SetAlert(localizedString);
                return;
            }
            playButton.interactable = false;
            var token = Managers.Token.GetToken(this);
            Managers.Game.GameStart();
            await Managers.NetworkScene.ChangeScene(Define.SceneEnum.MainScene, useNetworkSceneManager: true, token: token);
        }

        private void Exit(Unit _)
        {
            Managers.Multiplay.Shutdown();
            Managers.NetworkScene.ChangeScene(SceneEnum.TitleScene).Forget();
        }

        private void OnDestroy()
        {
            Managers.Token.Cancel(this);
        }
    }
}