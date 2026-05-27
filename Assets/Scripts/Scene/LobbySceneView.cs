using Cysharp.Threading.Tasks;
using StockGame.Scripts.Players;
using StockGame.Scripts.UI;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

namespace StockGame.Scripts.Scenes
{
    public class LobbySceneView : SceneBaseView
    {
        [SerializeField] private UI_LobbyView lobbyView;
        [SerializeField] private Collider2D confiner2D;
        public UI_LobbyView LobbyView => lobbyView;
        public Collider2D Confiner2D => confiner2D;
    }

    public sealed class LobbySceneController : SceneBaseController<LobbySceneView>
    {
        public override UniTask Enter(CancellationToken token)
        {
            return base.Enter(token);
        }
        public override UniTask Initialize(CancellationToken token)
        {
            Debug.Log("Lobby Scene Controller Initialize");
            View.LobbyView?.Initialize();
            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
                View.LobbyView.ActiveGameStart(false);

            ResetLocalPlayerPosition();
            return base.Initialize(token);
        }

        private void ResetLocalPlayerPosition()
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient?.PlayerObject == null) return;

            var player = localClient.PlayerObject
                .GetComponentInChildren<PlayerNetwork>();
            player.ResetPosition();
        }
        public override UniTask Exit(CancellationToken token)
        {
            return base.Exit(token);
        }
    }

    public sealed class LobbySceneParam : SceneParam
    {
        public string PlayerName { get; }
        public string JoinCode { get; }

        public LobbySceneParam(string playerName = "", string joinCode = "")
        {
            PlayerName = playerName;
            JoinCode = joinCode;
        }
    }
}
