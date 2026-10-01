using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.MissionDefine;

namespace StockGame.Scripts.UI
{
    public sealed class MiniMapUI : UIBase
    {
        [SerializeField] private RectTransform pageRoot;
        [SerializeField] private Button closeButton;

        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Image pageImage;

        [SerializeField] private CanvasGroup page1Group;
        [SerializeField] private CanvasGroup page2Group;

        [SerializeField] private List<Sprite> miniMapSprites;

        [SerializeField] private RectTransform playerMarker;
        [SerializeField] private RectTransform brokerMarker1;
        [SerializeField] private RectTransform brokerMarker2;


        public RectTransform PageRoot => pageRoot;
        public Button CloseButton => closeButton;
        public Button PrevButton => prevButton;
        public Button NextButton => nextButton;
        public Image PageImage => pageImage;
        public CanvasGroup Page1Group => page1Group;
        public CanvasGroup Page2Group => page2Group;
        public List<Sprite> MiniMapSprites => miniMapSprites;
        public RectTransform PlayerMarker => playerMarker;
        public RectTransform BrokerMarker1 => brokerMarker1;
        public RectTransform BrokerMarker2 => brokerMarker2;

        public Transform Player;
        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            inputBindingContext.Bind("Map", Close);
            Bind<MiniMapUI, MiniMapUIPresenter>();
        }

        public override void Close()
        {
            base.Close();
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX251);
        }

        public override void OnDispose()
        {
            base.OnDispose();
        }
        public void SetupPlayer(Transform player) => Player = player;
    }

    public sealed class MiniMapUIPresenter : UIPresenter<MiniMapUI>
    {
        private Dictionary<ZoneType, MiniMapZone> zones;
        private Collider2D pageOneZone;
        private Collider2D pageTwoZone;

        private Collider2D activatedBrokerOne;
        private Collider2D activatedBrokerTwo;

        int index = 0;

        private int Index
        {
            get => index;
            set
            {
                index = value;
                View.PrevButton.gameObject.SetActive(index != 0);
                View.NextButton.gameObject.SetActive(index != 1);
            }
        }
        protected override void OnBind()
        {
            base.OnBind();
            Index = 0;
            pageOneZone = ZoneManager.Instance.MiniMapPageOneZone;
            pageTwoZone = ZoneManager.Instance.MiniMapPageTwoZone;

            zones = new Dictionary<ZoneType, MiniMapZone>();
            var miniMapZones = View.PageRoot.GetComponentsInChildren<MiniMapZone>();
            foreach (var zone in miniMapZones)
                zones[zone.zoneType] = zone;

            SetupMissions(
                Managers.Mission.GetLocalPlayerNormalMission(),
                (miniMapZone, list) => miniMapZone.SetProgressNormalMission(list)
            );

            SetupMissions(
                Managers.Mission.GetLocalPlayerJobMission(),
                (miniMapZone, list) => miniMapZone.SetProgressJobMission(list),
                mission => mission is JobMission jm && jm.Condition.MissionActionType == MissionActionType.CompleteInteraction
            );

            View.CloseButton.OnClickAsObservable().Subscribe(OnClose).AddTo(this);
            View.PrevButton.OnClickAsObservable().Subscribe(OnPrev).AddTo(this);
            View.NextButton.OnClickAsObservable().Subscribe(OnNext).AddTo(this);

            Observable.EveryLateUpdate().Subscribe(_ =>
            {
                FindActivatedBroker();
                UpdatePlayerPosition();
                UpdateBrokerOnePosition();
                UpdateBrokerTwoPosition();
            }).AddTo(this);
        }

        private void FindActivatedBroker()
        {
            if (activatedBrokerOne && activatedBrokerTwo) return;
            var zoneList = ZoneManager.Instance.ZoneDict.Values;
            if (zoneList == null) return;
            foreach(var zone in zoneList)
            {
                if (zone.GetBrokerObject == null) continue;
                var broker = zone.GetBrokerObject;
                if (activatedBrokerOne == null)
                    activatedBrokerOne = broker.gameObject.activeSelf ? broker.Col : null;
                else if(activatedBrokerTwo == null)
                    activatedBrokerTwo = broker.gameObject.activeSelf ? broker.Col : null;
            }
        }

        private void OnNext(Unit _)
        {
            var count = View.MiniMapSprites.Count -1;
            if (index == count) return;
            Index++;
            
            var sprite = View.MiniMapSprites[index];
            View.PageImage.sprite = sprite;
            View.Page1Group.alpha = 0;
            View.Page2Group.alpha = 1;
        }

        private void OnPrev(Unit _)
        {
            if (index == 0) return;
            Index--;

            var sprite = View.MiniMapSprites[index];
            View.PageImage.sprite = sprite;
            View.Page1Group.alpha = 1;
            View.Page2Group.alpha = 0;
        }

        private void OnClose(Unit _)
        {
            View.Close();
        }

        /// <summary>
        /// 미션 목록을 Zone 기준으로 묶어서 MiniMapZone에 전달
        /// </summary>
        /// <param name="missions">표시할 미션 목록</param>
        /// <param name="onApply">MiniMapZone에 미션 목록 전달하는 콜백</param>
        /// <param name="filter">표시 조건 필터 (null이면 전체)</param>
        private void SetupMissions(
            List<Mission> missions,
            System.Action<MiniMapZone, List<Mission>> onApply,
            System.Func<Mission, bool> filter = null)
        {
            if (missions == null) return;

            var missionByZone = new Dictionary<Zone, List<Mission>>();
            foreach (var mission in missions)
            {
                if (mission.IsCompleted) continue;
                if (filter != null && !filter(mission)) continue;

                var missionZones = ZoneManager.Instance.GetZoneByMissionObjects(mission);
                if (missionZones == null) continue;

                foreach (var zone in missionZones)
                {
                    if (zone == null) continue;
                    if (!missionByZone.TryGetValue(zone, out var list))
                    {
                        list = new List<Mission>();
                        missionByZone[zone] = list;
                    }
                    list.Add(mission);
                }
            }

            foreach (var kvp in missionByZone)
            {
                if (!zones.TryGetValue(kvp.Key.ZoneType, out var miniMapZone)) continue;
                onApply(miniMapZone, kvp.Value);
            }
        }

        private void UpdatePlayerPosition()
        {
            if (View.Player == null || View.PlayerMarker == null || pageOneZone == null || pageTwoZone == null) return;
            var currentZone = index == 0 ? pageOneZone : pageTwoZone;
            if (currentZone == null) return;

            Bounds b = currentZone.bounds;

            // Mathf.InverseLerp는 결과를 0~1로 clamp하기 때문에 범위 밖 판정에 못 씀.
            // 직접 나눗셈으로 clamp 안 된 비율을 계산.
            float normX = (View.Player.position.x - b.min.x) / (b.max.x - b.min.x);
            float normY = (View.Player.position.y - b.min.y) / (b.max.y - b.min.y);

            bool isInsideZone = normX >= 0f && normX <= 1f && normY >= 0f && normY <= 1f;

            if (!isInsideZone)
            {
                if (View.PlayerMarker.gameObject.activeSelf)
                    View.PlayerMarker.gameObject.SetActive(false);
                return;
            }

            if (!View.PlayerMarker.gameObject.activeSelf)
                View.PlayerMarker.gameObject.SetActive(true);

            float mapW = View.PageRoot.rect.width;
            float mapH = View.PageRoot.rect.height;

            View.PlayerMarker.anchoredPosition = new Vector2(
                (normX - 0.5f) * mapW,
                (normY - 0.5f) * mapH
            );
        }

        private void UpdateBrokerOnePosition()
        {
            if (activatedBrokerOne == null || View.BrokerMarker1 == null || pageOneZone == null || pageTwoZone == null)
            {
                if (View.BrokerMarker1.gameObject.activeSelf)
                    View.BrokerMarker1.gameObject.SetActive(false);
                return;
            }
            var currentZone = index == 0 ? pageOneZone : pageTwoZone;
            if (currentZone == null) return;

            Bounds b = currentZone.bounds;

            float normX = (activatedBrokerOne.transform.position.x - b.min.x) / (b.max.x - b.min.x);
            float normY = (activatedBrokerOne.transform.position.y - b.min.y) / (b.max.y - b.min.y);

            bool isInsideZone = normX >= 0f && normX <= 1f && normY >= 0f && normY <= 1f;

            if (!isInsideZone)
            {
                if (View.BrokerMarker1.gameObject.activeSelf)
                    View.BrokerMarker1.gameObject.SetActive(false);
                return;
            }

            if (!View.BrokerMarker1.gameObject.activeSelf)
                View.BrokerMarker1.gameObject.SetActive(true);

            float mapW = View.PageRoot.rect.width;
            float mapH = View.PageRoot.rect.height;

            View.BrokerMarker1.anchoredPosition = new Vector2(
                (normX - 0.5f) * mapW,
                (normY - 0.5f) * mapH
            );
        }

        private void UpdateBrokerTwoPosition()
         {
            if (activatedBrokerTwo == null || View.BrokerMarker2 == null || pageOneZone == null || pageTwoZone == null)
            {
                if (View.BrokerMarker2.gameObject.activeSelf)
                    View.BrokerMarker2.gameObject.SetActive(false);
                return;
            }
            var currentZone = index == 0 ? pageOneZone : pageTwoZone;
            if (currentZone == null) return;

            Bounds b = currentZone.bounds;

            float normX = (activatedBrokerTwo.transform.position.x - b.min.x) / (b.max.x - b.min.x);
            float normY = (activatedBrokerTwo.transform.position.y - b.min.y) / (b.max.y - b.min.y);

            bool isInsideZone = normX >= 0f && normX <= 1f && normY >= 0f && normY <= 1f;

            if (!isInsideZone)
            {
                if (View.BrokerMarker2.gameObject.activeSelf)
                    View.BrokerMarker2.gameObject.SetActive(false);
                return;
            }

            if (!View.BrokerMarker2.gameObject.activeSelf)
                View.BrokerMarker2.gameObject.SetActive(true);

            float mapW = View.PageRoot.rect.width;
            float mapH = View.PageRoot.rect.height;

            View.BrokerMarker2.anchoredPosition = new Vector2(
                (normX - 0.5f) * mapW,
                (normY - 0.5f) * mapH
            );
        }
    }
}