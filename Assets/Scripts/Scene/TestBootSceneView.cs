using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Maps;
using StockGame.Scripts.Test;
using System.Threading;

namespace StockGame.Scripts.Scenes
{
    public sealed class TestBootSceneView : SceneBaseView { }
    public sealed class TestBootSceneController : SceneBaseController<TestBootSceneView>
    {
        private MapDataSO mapData;

        public override async UniTask Enter(CancellationToken token)
        {
            await base.Enter(token);
            mapData = await ResourceManager.Instance.LoadAsync<MapDataSO>(path:"Maps/MAP001", Define.ResourceDirectory.Datas).AttachExternalCancellation(token);
        }

        public override UniTask Exit(CancellationToken token)
        {
            return base.Exit(token);
        }

        public override async UniTask Initialize(CancellationToken token)
        {
            await base.Initialize(token);

            var map = new MapData(mapData);
            var nextParam = new MissionSceneParam(map);
        }
    }
}