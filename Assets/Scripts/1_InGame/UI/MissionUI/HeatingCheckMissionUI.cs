using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class HeatingCheckMissionUI : MissionUIBase
    {
        private const string CelsiusSymbol = "กษ";
        private List<int> inductionTemperature = new List<int>()
        {
            20, 40, 60, 80, 100
        };

        private int targetTemperature;

        [SerializeField] private List<Sprite> hotPlateSprites = new();
        [SerializeField] private Image hotPlateImage;

        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button lockButton;

        [SerializeField] private TMP_Text temperatureText;

        private CancellationTokenSource cts;

        private int index = -1;
        public int Index
        {
            get => index;
            set
            {
                if (!hotPlateImage.gameObject.activeSelf)
                    hotPlateImage.gameObject.SetActive(true);

                index = Mathf.Clamp(value, 0, hotPlateSprites.Count - 1);
                hotPlateImage.sprite = hotPlateSprites[index];
            }
        }

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            temperatureText.text = string.Empty;
            hotPlateImage.gameObject.SetActive(false);
            hotPlateImage.sprite = null;

            minusButton.OnClickAsObservable().Subscribe(Minus).AddTo(disposables);
            plusButton.OnClickAsObservable().Subscribe(Plus).AddTo(disposables);
            lockButton.OnClickAsObservable().Subscribe(LockTemperature).AddTo(disposables);

            InitAsync().Forget();
        }

        private async UniTask InitAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var shuffleTemperature = inductionTemperature.ToList();
            shuffleTemperature.Shuffle();

            await UniTask.WaitForSeconds(0.5f, cancellationToken: cts.Token);
            targetTemperature = shuffleTemperature[0];
            SetTemperature(targetTemperature);
        }

        private void SetTemperature(int temperature)
        {
            temperatureText.text = $"{temperature}{CelsiusSymbol}";
        }

        private void Minus(Unit _)
        {
            if (hotPlateImage == null) return;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX222);
            Index--;
        }

        private void Plus(Unit _)
        {
            if (hotPlateImage == null) return;
            Managers.Sound.PlaySfx(GameDefine.ResourceDefine.FMODEvent.SFX221);
            Index++;
        }

        private void LockTemperature(Unit _)
        {
            var targetIndex = inductionTemperature.IndexOf(targetTemperature);
            if (targetIndex == index)
            {
                ShowSuccess().Forget();
            }
            else
            {
                ShowFailed().Forget();
            }
        }

        public override void OnDispose()
        {
            base.OnDispose();
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }
    }
}