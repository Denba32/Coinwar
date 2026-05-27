using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Datas;
using StockGame.Utility;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace StockGame.Scripts.Manager
{
    public class Managers : MonoSingleton<Managers>
    {
        #region Default System
        public static InputManager Input => InputManager.Instance;
        public static UIManager UI => UIManager.Instance;
        public static SoundManager Sound => SoundManager.Instance;
        public static MultiplayManager Multiplay => MultiplayManager.Instance;
        public static FadeManager Fade => FadeManager.Instance;
        public static NetworkSceneManager NetworkScene => NetworkSceneManager.Instance;
        public static ResourceManager Resource => ResourceManager.Instance;
        public static MasterDataManager Master => MasterDataManager.Instance;
        public static CameraManager Camera => CameraManager.Instance;
        public static CancellationTokenManager Token => CancellationTokenManager.Instance;
        #endregion

        #region Game System
        public static GameManager Game => GameManager.Instance;
        public static JobManager Job => JobManager.Instance;
        public static MissionManager Mission => MissionManager.Instance;
        public static StockManager Stock => StockManager.Instance;
        public static LobbyManager Lobby => LobbyManager.Instance;
        public static SpawnManager Spawn => SpawnManager.Instance;
        #endregion Game System

        #region Config
        private static ConfigData configData;
        public static ConfigData ConfigData => configData;

        [SerializeField] private ResourcePathConfigSO resourcePath;
        public ResourcePathConfigSO ResourcePath => resourcePath;
        #endregion Config

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
            Token.Initialize();
            InitAsync(Token.GetToken(this, nameof(Initialize))).Forget();
        }

        private async UniTask InitAsync(CancellationToken token)
        {
            if(token.IsCancellationRequested) return;
            Application.targetFrameRate = 60;
            try
            {
                UI.transform.SetParent(transform);
                Sound.transform.SetParent(transform);
                Multiplay.transform.SetParent(transform);
                Input.transform.SetParent(transform);
                Fade.transform.SetParent(transform);
                NetworkScene.transform.SetParent(transform);

                await Resource.InitAsync(token);
                Master.Initialize();
                LoadConfig();

                UI.Initialize();
                Sound.Initialize();
                Multiplay.Initialize();
                Fade.Initialize();
                Input.Initialize();
                Camera.Initialize();

                await Game.Initialize();
                await Job.Initialize();
                await Mission.Initialize();
                await Stock.Initialize();
                await Lobby.Initialize();
                await Spawn.Initialize();
                
                await NetworkScene.Initialize();
            }
            catch (Exception e)
            {
                Debug.LogError($"InitAsync 실패: {e.Message}\n{e.StackTrace}");
                throw;
            }

            Debug.Log("초기화 완료");
        }

        public override void Clear()
        {
            base.Clear();
            UI.Clear();
            Sound.Clear();
        }

        void OnApplicationQuit()
        {
            if (configData != null)
                SaveConfig();
        }

        void OnDestroy()
        {
            Token?.CancelAll(this);
        }

        public void SaveConfig()
        {
            ConfigData.ToJson("Config/config.json");
        }

        public void LoadConfig()
        {
            string path = Path.Combine(Application.persistentDataPath, "Config/config.json");
            if (!File.Exists(path))
            {
                configData = new ConfigData();
                configData?.Initialize();
                SaveConfig();
            }
            else
            {
                configData = File.ReadAllText(path).FromJson<ConfigData>();
            }
        }
    }
}