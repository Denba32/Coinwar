using Cysharp.Threading.Tasks;
using Denba.Common;
using FMOD.Studio;
using FMODUnity;
using StockGame.Scripts.Define;
using StockGame.Scripts.Maps;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace StockGame.Scripts.Manager
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
        #region CONSTANTS
        private const string MIXER_PATH = "Mixer/MainMixer";
        #endregion CONSTANTS

        private Stack<(EventInstance Instance, string Path)> bgmStack = new();
        private readonly Dictionary<string, EventInstance> _activeSfxInstances = new();

        private EventInstance _currentEvent;

        private Bus _masterBus;
        private Bus _bgmBus;
        private Bus _sfxBus;

        private bool _hasBaseBgm = false;
        private string _currentBaseBgmPath;

        private Bus MasterBus => RuntimeManager.GetBus(GameDefine.ResourceDefine.FMODBus.MASTER);
        private Bus BgmBus => RuntimeManager.GetBus(GameDefine.ResourceDefine.FMODBus.BGM);
        private Bus SfxBus => RuntimeManager.GetBus(GameDefine.ResourceDefine.FMODBus.SFX);

        public override void Initialize()
        {
            base.Initialize();
            _masterBus = MasterBus;
            _bgmBus = BgmBus;
            _sfxBus = SfxBus;
            var token = Managers.Token.GetToken(this, nameof(InitAsync));
            InitAsync(token).Forget();
        }

        private async UniTask InitAsync(CancellationToken token)
        {
            await UniTask.Yield(cancellationToken: token);
        }

        public void ReplaceBaseBGM(string bgmPath)
        {
            bool hasBgm = !string.IsNullOrEmpty(bgmPath);

            if (hasBgm && _hasBaseBgm && _currentBaseBgmPath == bgmPath) return;

            ClearStack();

            _hasBaseBgm = hasBgm;
            _currentBaseBgmPath = bgmPath;

            if (!hasBgm) return; // BGM이 없는 씬 → 무음

            _currentEvent = RuntimeManager.CreateInstance(bgmPath);
            _currentEvent.start();
            bgmStack.Push((_currentEvent, bgmPath));
        }

        public void PushBGM(string bgmPath, bool isStopImmediatly = false)
        {
            if (bgmStack.Count > 0)
            {
                var (currentInstance, currentPath) = bgmStack.Peek();

                bool isSameAndAlive = currentPath == bgmPath && currentInstance.isValid();
                if (isSameAndAlive)
                {
                    return;
                }

                if (currentInstance.isValid())
                {
                    currentInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    //currentInstance.setPaused(true);
                }

                if (currentPath == bgmPath && !currentInstance.isValid())
                {
                    bgmStack.Pop();
                }
            }

            if (string.IsNullOrEmpty(bgmPath))
            {
                Debug.Log("BGMPath 가 읎다");
                bgmStack.Push((default, bgmPath));
                return;
            }

            var instance = RuntimeManager.CreateInstance(bgmPath);
            instance.start();
            bgmStack.Push((instance, bgmPath));
        }

        public void PushBGM(ZoneType zone)
        {
            var path = GetBGMByZoneType(zone);
            Debug.Log($"{zone.ToString()}: {path}");
            PushBGM(path);
        }

        private string GetBGMByZoneType(ZoneType zone) => zone switch
        {
            ZoneType.None => string.Empty,
            ZoneType.FishingSpot => GameDefine.ResourceDefine.FMODEvent.BGM103,
            ZoneType.Port => GameDefine.ResourceDefine.FMODEvent.BGM103,
            ZoneType.Bunker => GameDefine.ResourceDefine.FMODEvent.BGM102,
            ZoneType.PoliceOffice => string.Empty,
            ZoneType.Bank => string.Empty,
            ZoneType.Station => string.Empty,
            ZoneType.MayorGarden => string.Empty,
            _ => GameDefine.ResourceDefine.FMODEvent.BGM101,
        };


        public void PopBGM()
        {
            if (bgmStack.Count == 0) return;

            if (bgmStack.Count == 1)
            {
                var (lastInstance, lastPath) = bgmStack.Pop();
                if (lastInstance.isValid())
                {
                    lastInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    lastInstance.release();
                }
                bgmStack.Push((default, lastPath));
                return;
            }

            var (topInstance, _) = bgmStack.Pop();
            if (topInstance.isValid())
            {
                topInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                topInstance.release();
            }

            if (bgmStack.Count > 0)
            {
                var (newTopInstance, _) = bgmStack.Peek();
                if (newTopInstance.isValid())
                {
                    newTopInstance.setPaused(false);
                }
            }
        }

        public void PlaySfx(string eventName) => RuntimeManager.PlayOneShot(eventName);

        public void ClearStack()
        {
            while (bgmStack.Count > 0)
            {
                var (instance, _) = bgmStack.Pop();
                if (instance.isValid())
                {
                    instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    instance.release();
                }
            }

            _hasBaseBgm = false;
        }

        /// <summary>
        /// 동일 eventName의 사운드가 이미 재생 중이면 무시하고, 아니면 새로 재생합니다.
        /// 여러 오브젝트가 같은 사운드를 공유해야 할 때(예: 동일 종류의 인터랙터 다수) 사용합니다.
        /// </summary>
        public void PlaySfxIfNotPlaying(string eventName)
        {
            if (IsSfxPlaying(eventName)) return;

            var instance = RuntimeManager.CreateInstance(eventName);
            instance.start();
            instance.release(); // 재생이 끝나면 FMOD가 알아서 정리됨. 핸들 자체는 isValid()로 계속 추적 가능
            _activeSfxInstances[eventName] = instance;
        }

        public bool IsSfxPlaying(string eventName)
        {
            if (!_activeSfxInstances.TryGetValue(eventName, out var instance)) return false;
            if (!instance.isValid()) return false;

            instance.getPlaybackState(out var state);
            return state == PLAYBACK_STATE.PLAYING || state == PLAYBACK_STATE.STARTING;
        }

        #region Volume Control Method

        public float GetVolume(GameDefine.AudioDefine.BusType busType)
        {
            float volume = 0;
            switch (busType)
            {
                case GameDefine.AudioDefine.BusType.Master:
                    _masterBus.getVolume(out volume);
                    break;
                case GameDefine.AudioDefine.BusType.Bgm:
                    _bgmBus.getVolume(out volume);
                    break;
                case GameDefine.AudioDefine.BusType.Sfx:
                    _sfxBus.getVolume(out volume);
                    break;
            }
            return volume;
        }

        public void SetVolume(GameDefine.AudioDefine.BusType busType, float volume)
        {
            var isMute = volume <= 0;
            switch (busType)
            {
                case GameDefine.AudioDefine.BusType.Master:
                    {
                        _masterBus.setMute(isMute);
                        _masterBus.setVolume(volume);
                        break;
                    }
                case GameDefine.AudioDefine.BusType.Bgm:
                    {
                        _bgmBus.setMute(isMute);
                        _bgmBus.setVolume(volume);
                        break;
                    }
                case GameDefine.AudioDefine.BusType.Sfx:
                    {
                        _sfxBus.setMute(isMute);
                        _sfxBus.setVolume(volume);
                        break;
                    }
                default: break;
            }
        }

        #endregion Volume Control Method
    }
}