using Cysharp.Threading.Tasks;
using StockGame.Scripts.Base;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using static StockGame.Scripts.Define.GameDefine.GraphicsDefine;

namespace StockGame.Scripts.UI
{
    public class UI_OptionView : UIBase
    {
        [Header("Audio")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Graphics")]
        [SerializeField] private TMP_Dropdown resolutionDropDown;
        [SerializeField] private TMP_Dropdown screenModeDropDown;

        [Header("Languages")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Exit Button")]
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_Text exitButtonText;

        [SerializeField] private Button okButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        public Slider MasterSlider => masterSlider;
        public Slider BgmSlider => bgmSlider;
        public Slider SfxSlider => sfxSlider;
        public Button CloseButton => closeButton;
        public Button OKButton => okButton;
        public Button ResetButton => resetButton;
        public Button ExitButton => exitButton;
        public TMP_Text ExitButtonText => exitButtonText;
        public TMP_Dropdown ResolutionDropdown => resolutionDropDown;
        public TMP_Dropdown ScreenModeDropDown => screenModeDropDown;
        public TMP_Dropdown LanguageDropDown => languageDropdown;

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            Bind<UI_OptionView, UI_OptionViewPresenter>();
        }

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
        }
    }

    public sealed class UI_OptionViewPresenter : UIPresenter<UI_OptionView>
    {
        private const int FixedRefreshRate = 60;
        private readonly LocalizedString _fullscreenLabel = new("Option_String", "resolution_fullscreen");
        private readonly LocalizedString _windowedLabel = new("Option_String", "resolution_windowed");
        private readonly LocalizedString _englishLabel = new("Option_String", "language_en");
        private readonly LocalizedString _koreanLabel = new("Option_String", "language_kr");
        private Dictionary<string, LocalizedString> _languageLabelMap;

        private List<Locale> _availableLocales;

        private List<ResolutionOption> _resolutionOptions;
        private bool _isInitializing;

        protected override void OnBind()
        {
            base.OnBind();


        }

        protected override async UniTask OnBindAsnyc()
        {
            if (Managers.Config.CurrentData == null)
            {
                Debug.Log("ConfigData가 없음");
                Close(Unit.Default);
                return;
            }

            if (Managers.NetworkScene.CurrentSceneId == SceneEnum.TitleScene)
            {
                View.ExitButton.interactable = false;
            }
            else if (Managers.NetworkScene.CurrentSceneId == SceneEnum.LobbyScene || Managers.NetworkScene.CurrentSceneId == SceneEnum.MainScene)
            {
                View.ExitButton.interactable = true;
            }

            // ── Audio ──
            View.MasterSlider.value = Managers.Config.GetVolume(GameDefine.AudioDefine.BusType.Master);
            View.BgmSlider.value = Managers.Config.GetVolume(GameDefine.AudioDefine.BusType.Bgm);
            View.SfxSlider.value = Managers.Config.GetVolume(GameDefine.AudioDefine.BusType.Sfx);

            View.MasterSlider.OnValueChangedAsObservable().Subscribe(SetMasterVolume).AddTo(this);
            View.BgmSlider.OnValueChangedAsObservable().Subscribe(SetBgmVolume).AddTo(this);
            View.SfxSlider.OnValueChangedAsObservable().Subscribe(SetSFXVolume).AddTo(this);

            // ── Graphics ──
            BuildResolutionOptions();
            await BuildScreenModeOptions();

            View.ResolutionDropdown.onValueChanged.AsObservable().Subscribe(OnResolutionChanged).AddTo(this);
            View.ScreenModeDropDown.onValueChanged.AsObservable().Subscribe(OnScreenModeChanged).AddTo(this);

            // ── Languages ──
            await BuildLanguageOptions();
            View.LanguageDropDown.onValueChanged.AsObservable().Subscribe(OnLanguageChanged).AddTo(this);

            // ── Buttons ──
            View.OKButton.OnClickAsObservableFirst().Subscribe(Accept).AddTo(this);
            View.ResetButton.OnClickAsObservableFirst().Subscribe(Reset).AddTo(this);
            View.CloseButton.OnClickAsObservableFirst().Subscribe(Close).AddTo(this);

            View.ExitButton.OnClickAsObservableFirst().Subscribe(Exit).AddTo(this);

            SyncFromCurrentSetting();

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }


        #region Audio
        private void SetMasterVolume(float volume) => Managers.Config.SetMasterVolume(volume);
        private void SetBgmVolume(float volume) => Managers.Config.SetBgmVolume(volume);
        private void SetSFXVolume(float volume) => Managers.Config.SetSfxVolume(volume);
        #endregion Audio

        #region Graphics

        private void BuildResolutionOptions()
        {
            _resolutionOptions = ResolutionUtility.GetResolutionsMatchingNativeAspect();

            View.ResolutionDropdown.ClearOptions();
            View.ResolutionDropdown.AddOptions(_resolutionOptions.Select(r => r.ToString()).ToList());
        }
        private void OnLocaleChanged(Locale _)
        {
            BuildScreenModeOptions().Forget();
            BuildLanguageOptions().Forget();
        }
        private async UniTask BuildScreenModeOptions()
        {
            int selectedIndex = View.ScreenModeDropDown.value; // 선택값 보존

            var fullscreen = await _fullscreenLabel.GetLocalizedStringAsync().ToUniTask();
            var windowed = await _windowedLabel.GetLocalizedStringAsync().ToUniTask();

            View.ScreenModeDropDown.ClearOptions();
            View.ScreenModeDropDown.AddOptions(new List<string> { fullscreen, windowed });
            View.ScreenModeDropDown.SetValueWithoutNotify(selectedIndex);
        }

        private void SyncFromCurrentSetting()
        {
            _isInitializing = true;

            var data = Managers.Config.CurrentData;

            int resIndex = _resolutionOptions.FindIndex(r => r.Width == data.ResolutionWidth && r.Height == data.ResolutionHeight);
            if (resIndex < 0) resIndex = 0;
            View.ResolutionDropdown.SetValueWithoutNotify(resIndex);

            int modeIndex = (int)data.WindowMode;
            View.ScreenModeDropDown.SetValueWithoutNotify(modeIndex);

            if (_availableLocales != null)
            {
                // ConfigData가 아니라 지금 실제로 적용되어 있는 SelectedLocale에서 직접 읽는다
                int langIndex = _availableLocales.FindIndex(l => l.Identifier == LocalizationSettings.SelectedLocale?.Identifier);
                if (langIndex < 0) langIndex = 0;
                View.LanguageDropDown.SetValueWithoutNotify(langIndex);
            }

            _isInitializing = false;
        }

        private void OnResolutionChanged(int index)
        {
            if (_isInitializing) return;

            var selected = _resolutionOptions[index];
            Managers.Config.SetResolution(selected.Width, selected.Height, FixedRefreshRate);
        }

        private void OnScreenModeChanged(int index)
        {
            if (_isInitializing) return;

            Managers.Config.SetWindowMode((DisplayModeOption)index);
        }

        #endregion Graphics



        #region Languages
        private void OnLanguageChanged(int index)
        {
            if (_isInitializing) return;
            if (_availableLocales == null || index < 0 || index >= _availableLocales.Count) return;

            Managers.Config.SetLanguage(_availableLocales[index]);
        }
        private async UniTask BuildLanguageOptions()
        {
            await LocalizationSettings.InitializationOperation.Task;

            _availableLocales = LocalizationSettings.AvailableLocales.Locales.ToList();
            _languageLabelMap ??= new Dictionary<string, LocalizedString>
            {
                { "en", _englishLabel },
                { "ko", _koreanLabel },
            };

            int selectedIndex = View.LanguageDropDown.value;

            var labels = new List<string>(_availableLocales.Count);
            foreach (var locale in _availableLocales)
            {
                if (_languageLabelMap.TryGetValue(locale.Identifier.Code, out var localized))
                    labels.Add(await localized.GetLocalizedStringAsync().ToUniTask());
                else
                    labels.Add(locale.LocaleName);
            }

            View.LanguageDropDown.ClearOptions();
            View.LanguageDropDown.AddOptions(labels);
            View.LanguageDropDown.SetValueWithoutNotify(selectedIndex);
        }

        private void ResetLanguageToInitial()
        {
            var systemSelector = new SystemLocaleSelector();
            var systemLocale = systemSelector.GetStartupLocale(LocalizationSettings.AvailableLocales);

            if (systemLocale != null)
                Managers.Config.SetLanguage(systemLocale);
        }
        #endregion Languages

        #region Buttons

        private void Accept(Unit _)
        {
            Managers.Config.Save();
            Close(Unit.Default);
        }

        private void Reset(Unit _)
        {
            Managers.Config.SetMasterVolume(1f);
            Managers.Config.SetBgmVolume(1f);
            Managers.Config.SetSfxVolume(1f);

            View.MasterSlider.SetValueWithoutNotify(1f);
            View.BgmSlider.SetValueWithoutNotify(1f);
            View.SfxSlider.SetValueWithoutNotify(1f);

            var (width, height, _) = ResolutionUtility.GetNativeResolution();
            Managers.Config.SetResolution(width, height, FixedRefreshRate);
            Managers.Config.SetWindowMode(DisplayModeOption.Fullscreen);
            ResetLanguageToInitial();
            SyncFromCurrentSetting();
        }
        private void Exit(Unit unit)
        {
            Managers.Game.ForceStopGame();
            Managers.Multiplay.Shutdown();
            Managers.NetworkScene.ChangeScene(SceneEnum.TitleScene).Forget();
        }

        private void Close(Unit _)
        {
            View.Close();
        }

        #endregion Buttons

        public override void Dispose()
        {
            base.Dispose();
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }
    }
}