using StockGame.Scripts.Define;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class TutorialUI : UIBase
    {
        [SerializeField] private Button closeButton;

        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Image tutorialImage;
        [SerializeField] private LocalizeSpriteEvent spriteEvent;
        [SerializeField] private List<Sprite> tutorialList = new();
        private int index;
        private const string TableName = "Tutorial_Asset";

        public int Index
        {
            get => index;
            set
            {
                index = value;
                RefreshSprite();
                prevButton.gameObject.SetActive(index != 0);
                nextButton.gameObject.SetActive(index != tutorialList.Count - 1);
            }
        }
        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
            spriteEvent.OnUpdateAsset.RemoveAllListeners();
            spriteEvent.OnUpdateAsset.AddListener(OnSpriteUpdated);

            Index = 0;
            nextButton?.OnClickAsObservable()?.Subscribe(Next).AddTo(this);
            prevButton?.OnClickAsObservable()?.Subscribe(Prev).AddTo(this);
            closeButton?.OnClickAsObservable()?.Subscribe(_ => Close()).AddTo(this);
        }

        private void Next(Unit _)
        {
            Index = Mathf.Min(index + 1, tutorialList.Count -1);
        }

        private void Prev(Unit _)
        {
            Index = Mathf.Max(index - 1, 0);
        }

        private void RefreshSprite()
        {
            spriteEvent.AssetReference = new LocalizedSprite
            {
                TableReference = TableName,
                TableEntryReference = $"tutorial_{index}"
            };
        }

        private void OnSpriteUpdated(Sprite sprite)
        {
            tutorialImage.sprite = sprite;
        }

        public override void OnDispose()
        {
            base.OnDispose();
            if (spriteEvent != null)
                spriteEvent.OnUpdateAsset.RemoveListener(OnSpriteUpdated);
        }
    }
}