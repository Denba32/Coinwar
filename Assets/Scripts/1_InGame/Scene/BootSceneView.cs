using Cysharp.Threading.Tasks;
using StockGame.Scripts.Manager;
using System.Threading;
using UnityEngine;

namespace StockGame.Scripts.Scenes
{
    public class BootSceneView : SceneBaseView { }

    public class BootSceneController : SceneBaseController<BootSceneView>
    {
        public override UniTask Enter(CancellationToken token)
        {
            return base.Enter(token);
        }
        public override async UniTask Initialize(CancellationToken token)
        {
            Debug.Log("Boot Scene Initialize");
            await base.Initialize(token);
            await NetworkSceneManager.Instance.ChangeScene(Define.SceneEnum.TitleScene, useNetworkSceneManager:false);
        }

        public override UniTask Exit(CancellationToken token)
        {
            return base.Exit(token);
        }
    }
}