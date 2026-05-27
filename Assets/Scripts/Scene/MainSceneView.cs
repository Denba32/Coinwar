using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using StockGame.Scripts.UI;
using StockGame.Scripts.UI.RoundReport;
using System.Collections.Generic;
using System.Threading;
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
        public override UniTask Enter(CancellationToken token)
        {
            return base.Enter(token);
        }

        public override async UniTask Initialize(CancellationToken token)
        {
            // 맵 Zone 등록
            var zones = View.MapObject.GetComponentsInChildren<Zone>();
            ZoneManager.Instance.RegistAll(zones);

            // UI 초기화 — Stock 구독이 먼저 시작되어야 Add 이벤트 정상 수신
            View.MainUI.Initilaize(0, Define.GameDefine.UIDefine.UILayer.SceneUI);
            View.RoundReport.Initilaize(50, Define.GameDefine.UIDefine.UILayer.Popup);

            // 미션 오브젝트 등록
            MissionManager.Instance?.Initialize(View.MissionObjects);
            await base.Initialize(token);
        }

        public override UniTask Exit(CancellationToken token)
        {
            return base.Exit(token);
        }
    }
}