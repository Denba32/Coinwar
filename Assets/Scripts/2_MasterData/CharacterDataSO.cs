using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace StockGame.Scripts.Datas
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObject/CharacterDataSO")]
    public class CharacterDataSO : ScriptableObject
    {
        [Space(1)]
        [Header("Character Information")]
        public SpriteLibraryAsset libaryAsset = null;
        public Sprite thumbnail = null;

        [Space(1)]
        [Header("Scan")]
        public float scanCenterPivot;

        [ContextMenu(nameof(SetScanCenterPivot))]
        public void SetScanCenterPivot()
        {
            var categoryNames = libaryAsset.GetCategoryLabelNames("Idle");
            if (categoryNames == null || categoryNames.Count() == 0) return;
            foreach(var categoryName in categoryNames)
            {
                var sprite = libaryAsset.GetSprite("Idle", categoryName);
                if(sprite != null)
                {
                    thumbnail = sprite;
                    break;
                }
            }
        }
    }
}