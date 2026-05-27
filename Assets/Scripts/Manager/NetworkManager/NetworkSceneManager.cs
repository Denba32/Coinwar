using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Common.Interfaces;
using StockGame.Scripts.Define;
using StockGame.Scripts.Scenes;
using StockGame.Scripts.UI;
using StockGame.Utility;
using System;
using System.Threading;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StockGame.Scripts.Manager
{
    public class NetworkSceneManager : NetworkSingleton<NetworkSceneManager>
    {
        public SceneBaseView CurrentSceneView { get; private set; }
        public ISceneController CurrentSceneController { get; private set; }
        public SceneEnum CurrentSceneId { get; private set; }
        public SceneParam CurrentSceneParam { get; private set; }

        bool IsNetworkSceneManagementEnabled =>
            NetworkManager != null &&
            NetworkManager.SceneManager != null &&
            NetworkManager.NetworkConfig.EnableSceneManagement;

        bool m_IsInitialized;
        bool m_IsNetworkSceneActive;

        private UI_Loading loadingUI = null;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log($"[NetworkSceneManager] OnNetworkSpawn");

            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnServerStarted += OnServerStarted;

            NetworkManager.OnServerStopped += OnNetworkingSessionEnded;
            NetworkManager.OnClientStopped += OnNetworkingSessionEnded;

            SceneManager.sceneLoaded += OnSceneLoaded;
            if (IsServer) return;
            var spawnToken = Managers.Token.GetToken(this, nameof(HandleCurrentScene));
            HandleCurrentScene(spawnToken).Forget();
        }

        public override async UniTask Initialize()
        {
            Debug.Log("NetworkSceneManager InitAsync");
            loadingUI = await UIManager.Instance.Open<UI_Loading>(GameDefine.UIDefine.UILayer.Transition);
            var activatedScene = SceneManager.GetActiveScene();
            var rootObjs = activatedScene.GetRootGameObjects();
            if (rootObjs.Length == 0) return;
            foreach (var obj in rootObjs)
            {
                var view = obj.GetComponent<SceneBaseView>();
                if (view is not null)
                {
                    CurrentSceneView = view;
                    break;
                }
            }

            if (CurrentSceneView != null)
            {
                var changeToken = Managers.Token.GetToken(this, nameof(ChangeScene));
                CurrentSceneId = CurrentSceneView.sceneEnum;
                Debug.Log($"Find Scene View : {CurrentSceneView.name}, SceneId: {CurrentSceneId}");

                CurrentSceneController = GameDefine.SceneDefine.CreateSceneController(CurrentSceneView.sceneEnum);

                try
                {
                    if (CurrentSceneController != null)
                    {
                        await CurrentSceneController.Enter(changeToken);
                        await CurrentSceneController.Initialize(changeToken);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{ex.Message}" +
                        $"{ex.StackTrace}" +
                        $"{ex.InnerException}" +
                        $"{ex.Source}");
                    Managers.Token.Cancel(this, nameof(ChangeScene));
                }
            }
        }

        private void OnServerStarted()
        {
            Debug.Log("[NetworkSceneManager] Server Started");
            OnNetworkingSessionStarted();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId != NetworkManager.LocalClientId) return;

            Debug.Log("[NetworkSceneManager] Client Connected");
            OnNetworkingSessionStarted();
        }


        public async UniTask ChangeScene(SceneEnum nextSceneId, SceneParam nextParam = null, bool useNetworkSceneManager = false, float fadeTime = 1.0f, CancellationToken token = default)
        {
            if (loadingUI == null)
                loadingUI = await UIManager.Instance.Open<UI_Loading>(GameDefine.UIDefine.UILayer.Transition);

            loadingUI?.Show();

            CurrentSceneId = nextSceneId;
            CurrentSceneParam = nextParam;

            try
            {
                if (useNetworkSceneManager)
                    await ChangeSceneNetwork(nextSceneId, fadeTime, token);
                else
                    await ChangeSceneLocal(nextSceneId, fadeTime, token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex.Message); Debug.LogWarning($"{ex.Message}" +
                $"{ex.StackTrace}" +
                $"{ex.InnerException}" +
                $"{ex.Source}");
            }
        }

        private async UniTask ChangeSceneNetwork(SceneEnum nextSceneId, float fadeTime, CancellationToken token)
        {
            if (!NetworkManager.IsServer)
            {
                Debug.LogError("ChangeSceneNetwork는 Host만 호출 가능합니다.");
                return;
            }

            if (CurrentSceneController != null) await CurrentSceneController.Exit(token);

            var sceneName = nextSceneId.GetSceneName();
            NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private async UniTask ChangeSceneLocal(SceneEnum nextSceneId, float fadeTime, CancellationToken token)
        {
            await UIManager.Instance.ClearWithoutTransition();

            if (loadingUI == null)
                loadingUI = await UIManager.Instance.Open<UI_Loading>(GameDefine.UIDefine.UILayer.Transition);
            loadingUI.gameObject?.SetActive(true);

            var prevScene = SceneManager.GetActiveScene();
            var sceneName = nextSceneId.GetSceneName();

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await op.ToUniTask(cancellationToken: token);

            var nextScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(nextScene);

            if (CurrentSceneController != null) await CurrentSceneController.Exit(token);
            if (prevScene.IsValid()) await SceneManager.UnloadSceneAsync(prevScene).ToUniTask(cancellationToken: token);
            await InitializeSceneController(nextSceneId, nextScene, token: token);

            await UniTask.NextFrame();
            loadingUI?.Hide();
            await FadeManager.Instance.FadeOut(fadeTime, DG.Tweening.Ease.OutQuad);
        }

        private async UniTask InitializeSceneController(SceneEnum sceneId, Scene scene, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            FindSceneController(sceneId, scene);

            if (CurrentSceneController == null)
            {
                Debug.LogError("SceneController not found");
                return;
            }

            if (CurrentSceneController is ISceneController c)
                c?.SetView(CurrentSceneView);

            await CurrentSceneController.Enter(token);
            await CurrentSceneController.Initialize(token);

            // 네트워크 씬일 때만 서버에 초기화 완료 신호 전송
            if (m_IsNetworkSceneActive && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && sceneId == SceneEnum.MainScene)
            {
                Debug.Log($"[NetworkSceneManager] 클라이언트 초기화 완료 → Ready 신호 전송 (씬: {sceneId})");
                GameManager.Instance.NotifyClientReadyServerRpc();
            }
        }

        private void FindSceneController(SceneEnum nextSceneId, Scene nextScene)
        {
            CurrentSceneView = null;

            var rootObjects = nextScene.GetRootGameObjects();
            if (rootObjects == null || rootObjects.Length == 0) return;

            foreach (var obj in rootObjects)
            {
                var component = obj.GetComponent<SceneBaseView>();
                if (component is not null)
                {
                    CurrentSceneView = component;
                    break;
                }
            }

            if (CurrentSceneView == null)
            {
                Debug.LogError($"{nextSceneId}의 SceneView를 찾을 수 없음");
                return;
            }

            CurrentSceneController = GameDefine.SceneDefine.CreateSceneController(nextSceneId);
        }

        public T GetSceneParam<T>() where T : SceneParam
        {
            return CurrentSceneParam as T;
        }

        public string GetSceneName(SceneEnum scene) => scene.ToString();

        #region SESSION_EVENT

        void OnNetworkingSessionStarted()
        {
            if (!m_IsInitialized)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    m_IsNetworkSceneActive = true;
                    NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
                    NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
                }
                m_IsInitialized = true;
            }
        }

        void OnNetworkingSessionEnded(bool unused)
        {
            if (m_IsInitialized)
            {
                if (IsNetworkSceneManagementEnabled)
                {
                    m_IsNetworkSceneActive = false;
                    NetworkManager.SceneManager.OnSceneEvent -= OnSceneEvent;
                }
                m_IsInitialized = false;
            }
        }

        #endregion

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!m_IsNetworkSceneActive)
                return;
            OnSceneLoadedAsync(scene, mode).Forget();
        }

        private async UniTask OnSceneLoadedAsync(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (loadingUI == null)
                    loadingUI = await Managers.UI.Open<UI_Loading>(GameDefine.UIDefine.UILayer.Transition);
                loadingUI?.Show();
                if (!scene.name.Equals(CurrentSceneId.GetSceneName()))
                {
                    if (Enum.TryParse<SceneEnum>(scene.name, out var parsedEnum))
                        CurrentSceneId = parsedEnum;
                    else
                        return;
                }

                var prevController = CurrentSceneController;
                CurrentSceneController = null;

                var sceneLoadedToken = Managers.Token.GetToken(this, nameof(OnSceneLoaded));
                if (prevController != null)
                    await prevController.Exit(sceneLoadedToken);
                await InitializeSceneController(CurrentSceneId, scene, sceneLoadedToken);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}" +
                    $"{ex.StackTrace}" +
                    $"{ex.InnerException}" +
                    $"{ex.Source}");
            }
        }

        void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    loadingUI?.Show();
                    CurrentSceneId = GameDefine.SceneDefine.GetSceneIdByName(sceneEvent.SceneName);
                    break;

                case SceneEventType.LoadEventCompleted:
                    if (CurrentSceneId != SceneEnum.MainScene)
                    {
                        Managers.UI.ClearWithoutTransition().Forget();
                        loadingUI?.Hide();
                        FadeManager.Instance.FadeOut(1.0f, DG.Tweening.Ease.OutQuad).Forget();
                    }
                    break;
            }
        }

        private async UniTask HandleCurrentScene(CancellationToken token)
        {
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                Debug.Log($"[NetworkSceneManager] HandleCurrentScene - {activeScene.name}");

                if (Enum.TryParse<SceneEnum>(activeScene.name, out var sceneEnum))
                    CurrentSceneId = sceneEnum;
                else
                    return;

                CurrentSceneController?.Exit(token);
                FindSceneController(CurrentSceneId, activeScene);

                await InitializeSceneController(CurrentSceneId, activeScene, token);
                FadeManager.Instance.FadeOut(1.0f, DG.Tweening.Ease.OutQuad).SuppressCancellationThrow().Forget();
                UIManager.Instance.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex.Message}" +
                    $"{ex.StackTrace}" +
                    $"{ex.InnerException}" +
                    $"{ex.Source}");
                Managers.Token.Cancel(this, nameof(HandleCurrentScene));
            }
        }

        public override void OnDestroy()
        {
            Managers.Token.CancelAll(this);

            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (NetworkManager != null)
            {
                NetworkManager.OnServerStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnClientStarted -= OnNetworkingSessionStarted;
                NetworkManager.OnServerStopped -= OnNetworkingSessionEnded;
                NetworkManager.OnClientStopped -= OnNetworkingSessionEnded;
            }

            base.OnDestroy();
        }
    }
}