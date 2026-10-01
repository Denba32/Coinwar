using StockGame.Scripts.Manager;
using StockGame.Scripts.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI.Missions
{
    public class PuzzleMissionUI : DragAndDropMissionUI
    {
        [SerializeField] private List<RectTransform> puzzlePieceSpawnPosition;
        [SerializeField] private List<UIDragItem> dragItems;

        [SerializeField] private Dictionary<int, List<Sprite>> puzzleDatas = new();
        [SerializeField] private List<Sprite> completedImages = new();
        [SerializeField] private Image completedImage;
        [SerializeField] private bool isRandomPuzzle = false;

        public override void OnOpen(params object[] args)
        {
            base.OnOpen(args);
            LoadPuzzleSprites();
            SetPuzzleImage();
            SufflePiecePosition();
        }

        private void LoadPuzzleSprites()
        {
            puzzleDatas.Clear();

            var puzzlePaths = new[]
            {
                "Mission/Puzzle/IMG882",
                "Mission/Puzzle/IMG883",
            };

            for (int i = 0; i < puzzlePaths.Length; i++)
            {
                var sprites = Managers.Resource.LoadAll<Sprite>(puzzlePaths[i], Define.ResourceDirectory.Images);
                if (sprites == null || sprites.Count == 0) continue;
                puzzleDatas[i] = new List<Sprite>(sprites);
            }
        }

        private void SetPuzzleImage()
        {
            if (!isRandomPuzzle) return;

            var keys = new List<int>(puzzleDatas.Keys);
            if (keys.Count == 0) return;

            var randomIndex = Random.Range(0, keys.Count);
            var randomKey = keys[randomIndex];

            if (!puzzleDatas.TryGetValue(randomKey, out var spriteList)) return;

            completedImage.sprite = completedImages[randomIndex];
            for (int i = 0; i < spriteList.Count; i++)
                dragItems[i].ChangeImage(spriteList[i]);
        }

        private void SufflePiecePosition()
        {
            if (puzzlePieceSpawnPosition.Count == 0 || dragItems.Count == 0 || puzzlePieceSpawnPosition.Count != dragItems.Count) return;
            puzzlePieceSpawnPosition.Shuffle();
            dragItems.Shuffle();

            for (int i = 0; i < puzzlePieceSpawnPosition.Count; i++)
            {
                dragItems[i].Rect.anchoredPosition = puzzlePieceSpawnPosition[i].anchoredPosition;
            }
        }
    }
}