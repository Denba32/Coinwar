using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Test;
using StockGame.Scripts.UI;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UniRx;
using System;

namespace StockGame.Scripts.Scenes
{
    public sealed class MissionSceneView : SceneBaseView
    {
        [SerializeField] private UI_MissionScene missionUIPrefab;
        [SerializeField] private List<MissionObject> missionObjects;

        public UI_MissionScene MissionUIPrefab => missionUIPrefab;
        public List<MissionObject> MissionObjects => missionObjects;
    }

    public sealed class MissionSceneController : SceneBaseController<MissionSceneView>
    {
        public override UniTask Enter(CancellationToken token)
        {
            return base.Enter(token);
        }
        public override async UniTask Initialize(CancellationToken token)
        {
            try
            {
                await base.Initialize(token);

                // 미션 오브젝트 등록
                // MissionHUD 이벤트 등록
                if (View.MissionUIPrefab != null)
                {
                    var missionUI = await UIManager.Instance.Open(View.MissionUIPrefab, Define.GameDefine.UIDefine.UILayer.SceneUI);
                    TestGameManager.Instance.OnUpdatedPhase.Subscribe(missionUI.OnUpdatedGameFlow).AddTo(_disposables);
                }

                TestGameManager.Instance.GameStart();
            }catch(Exception e)
            {
                Debug.Log(e);
            }

        }

        public override UniTask Exit(CancellationToken token)
        {
            return base.Exit(token);
        }
    }

    public sealed class MissionSceneParam : SceneParam
    {
        public MapData Map { get; }
        public MissionSceneParam(MapData mapData)
        {
            Map = mapData;
        }
    }
}