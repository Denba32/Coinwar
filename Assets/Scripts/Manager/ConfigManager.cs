using Denba.Common;
using MemoryPack;
using StockGame.Scripts.Datas;
using StockGame.Scripts.Define;
using StockGame.Scripts.Utility;
using System;
using System.IO;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.GraphicsDefine;

namespace StockGame.Scripts.Manager
{
    public sealed class ConfigManager : Singleton<ConfigManager>
    {
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "settings.bin");
        public ConfigData CurrentData { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            Load();
            ApplyAll();
        }

        public void Save()
        {
            try
            {
                var bytes = MemoryPackSerializer.Serialize(CurrentData);
                File.WriteAllBytes(SavePath, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SettingService] 설정 저장 실패: {e}");
            }

        }

        public void Load()
        {
            if (File.Exists(SavePath))
            {
                try
                {
                    var bytes = File.ReadAllBytes(SavePath);
                    CurrentData = MemoryPackSerializer.Deserialize<ConfigData>(bytes) ?? CreateDefaultSetting();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ConfigManager] 설정 로드 실패, 기본값으로 대체: {e}");
                    CurrentData = CreateDefaultSetting();
                }
            }
            else
            {
                CurrentData = CreateDefaultSetting();
            }
        }

        private ConfigData CreateDefaultSetting()
        {
            var setting = new ConfigData();

            var (width, height, refreshRate) = ResolutionUtility.GetNativeResolution();
            setting.ResolutionWidth = width;
            setting.ResolutionHeight = height;
            setting.RefreshRate = refreshRate;

            return setting;
        }

        public void ApplyAll()
        {
            ApplyAudio();
            ApplyGraphics();
        }

        #region Audio
        private void ApplyAudio()
        {
            if (Managers.Sound == null) return;

            Managers.Sound.SetVolume(GameDefine.AudioDefine.BusType.Master, CurrentData.MasterVolume);
            Managers.Sound.SetVolume(GameDefine.AudioDefine.BusType.Bgm, CurrentData.BgmVolume);
            Managers.Sound.SetVolume(GameDefine.AudioDefine.BusType.Sfx, CurrentData.SfxVolume);
        }

        public float GetVolume(GameDefine.AudioDefine.BusType busType) => busType switch
        {
            GameDefine.AudioDefine.BusType.Master => CurrentData.MasterVolume,
            GameDefine.AudioDefine.BusType.Bgm => CurrentData.BgmVolume,
            GameDefine.AudioDefine.BusType.Sfx => CurrentData.SfxVolume,
            _ => 1
        };

        public void SetMasterVolume(float volume)
        {
            CurrentData.MasterVolume = volume;
            Managers.Sound.SetVolume(GameDefine.AudioDefine.BusType.Master, CurrentData.MasterVolume);
        }

        public void SetBgmVolume(float volume)
        {
            CurrentData.BgmVolume = volume;
            Managers.Sound.SetVolume(GameDefine.AudioDefine.BusType.Bgm, CurrentData.BgmVolume);
        }

        public void SetSfxVolume(float volume)
        {
            CurrentData.SfxVolume = volume;
            Managers.Sound.SetVolume(GameDefine.AudioDefine.BusType.Sfx, CurrentData.SfxVolume);
        }

        #endregion Audio

        #region Display
        private void ApplyGraphics()
        {
            ApplyResolution(CurrentData.ResolutionWidth, CurrentData.ResolutionHeight, CurrentData.RefreshRate, CurrentData.WindowMode);
        }

        private void ApplyResolution(int width, int height, int refreshRate, DisplayModeOption windowMode)
        {
            FullScreenMode mode = ToFullScreenMode(windowMode);
            var rate = new RefreshRate { numerator = (uint)refreshRate, denominator = 1 };

            Screen.SetResolution(width, height, mode, rate);
        }

        public void SetResolution(int width, int height, int refreshRate)
        {
            CurrentData.ResolutionWidth = width;
            CurrentData.ResolutionHeight = height;
            CurrentData.RefreshRate = refreshRate;

            ApplyResolution(width, height, refreshRate, CurrentData.WindowMode);
        }

        public void SetWindowMode(DisplayModeOption mode)
        {
            CurrentData.WindowMode = mode;
            ApplyResolution(CurrentData.ResolutionWidth, CurrentData.ResolutionHeight, CurrentData.RefreshRate, mode);
        }

        private static FullScreenMode ToFullScreenMode(DisplayModeOption option) => option switch
        {
            DisplayModeOption.Fullscreen => FullScreenMode.ExclusiveFullScreen,
            DisplayModeOption.BorderlessWindowed => FullScreenMode.FullScreenWindow,
            DisplayModeOption.Windowed => FullScreenMode.Windowed,
            _ => FullScreenMode.FullScreenWindow,
        };

        #endregion Display 

        public override void Dispose()
        {
            base.Dispose();
            Save();
        }
    }
}