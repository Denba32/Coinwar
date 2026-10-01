using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Players;
using StockGame.Scripts.UI;
using StockGame.Scripts.UI.RoundReport;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Scenes
{
    public class MainSceneView : SceneBaseView
    {
        [SerializeField] private UI_MainView mainUI;
        [SerializeField] private UI_RoundReport roundReport;
        [SerializeField] private List<MissionObject> missionObjects;
        [SerializeField] private GameObject mapObject;

        public List<MissionObject> MissionObjects => missionObjects;
        public UI_MainView MainUI => mainUI;
        public UI_RoundReport RoundReport => roundReport;
        public GameObject MapObject => mapObject;
    }

    public sealed class MainSceneController : SceneBaseController<MainSceneView>
    {
        public MainSceneController() { }

        public MainSceneController(string bgmPath = "") : base(bgmPath) { }

        public override UniTask Enter(CancellationToken token)
        {
            return base.Enter(token);
        }

        public override async UniTask Initialize(CancellationToken token)
        {
            _inputBidingContext.Bind("Escape", () =>
            {
                Debug.Log("Escape!!");
                Managers.UI.Open<UI_OptionView>(GameDefine.UIDefine.UILayer.Popup).Forget();
            })
            .Bind("Map", () => UniTask.Void(async () =>
            {
                var playerObject = NetworkManager.Singleton.LocalClient.PlayerObject;
                var root = playerObject.GetComponent<PlayerNetwork>().Root;
                var minimapUI = await Managers.UI.Open<MiniMapUI>(GameDefine.UIDefine.UILayer.Popup);
                minimapUI?.SetupPlayer(root);
            }));

            // 맵 Zone 등록
            var zones = View.MapObject.GetComponentsInChildren<Zone>();
            ZoneManager.Instance.RegistAll(zones);

            var bgmExecutors = View.MapObject.GetComponentsInChildren<BGMExecutor>();
            ZoneManager.Instance.RegistAllBgmExecutors(bgmExecutors);

            // UI 초기화 — Stock 구독이 먼저 시작되어야 Add 이벤트 정상 수신
            View.MainUI.Initilaize(0, GameDefine.UIDefine.UILayer.SceneUI);
            View.RoundReport.Initilaize(40, GameDefine.UIDefine.UILayer.SceneUI);

            // 미션 오브젝트 등록
            MissionManager.Instance?.Initialize(View.MissionObjects);
            await base.Initialize(token);
        }

        public override UniTask Exit(CancellationToken token)
        {
            ZoneManager.Instance?.ClearAll();
            return base.Exit(token);
        }
    }
}