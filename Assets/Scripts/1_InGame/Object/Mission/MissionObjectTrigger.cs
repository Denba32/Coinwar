using UnityEditor;
using UnityEngine;

namespace StockGame.Scripts.Objects.Missions
{
    public class MissionObjectTrigger : MonoBehaviour
    {
        public Sprite enterSprite;
        public Sprite exitSprite;

        private SpriteRenderer targetRenderer;

        private void Awake()
        {
            // 부모에서 SpriteRenderer 자동 탐색
            targetRenderer = GetComponentInParent<SpriteRenderer>();
        }

        //private void OnTriggerEnter2D(Collider2D other)
        //{
        //    if (other.CompareTag("Player"))
        //    {
        //        targetRenderer.sprite = enterSprite;
        //    }
        //}

        //private void OnTriggerExit2D(Collider2D other)
        //{
        //    if (other.CompareTag("Player"))
        //    {
        //        targetRenderer.sprite = exitSprite;
        //    }
        //}


#if UNITY_EDITOR

        [ContextMenu(nameof(AutoInsertImage))]
        public void AutoInsertImage()
        {
            if(targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            var sprite = targetRenderer.sprite;
            var spriteName = sprite.name;
            Debug.Log(spriteName);

            var path = AssetDatabase.GetAssetPath(sprite);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path);

            Debug.Log("Path " + path);

            var index = spriteName.LastIndexOf("_");
            var length = spriteName.Length;
            var number = spriteName.Substring(index + 1, length - index -1);
            if(int.TryParse(number, out var parsedNumber))
            {
                var targetNumber = parsedNumber + 1;
                var targetName = spriteName.Substring(0, index) + "_" + targetNumber;
                Debug.Log($"Target Name : {targetName}");
                var lastIndex = path.LastIndexOf('_');
                var lastPath = path.Substring(0, lastIndex);
                Debug.Log(lastPath);
                var targetPath = path.Replace(".png", "") + "_" + targetNumber + ".png";
                Debug.Log(targetPath);


                foreach (var s in sprites)
                {
                    if (s is Sprite sp && sp.name == targetName)
                    {
                        enterSprite = sp;
                        break;
                    }
                }
                exitSprite = targetRenderer.sprite;
            }
            if(gameObject.GetComponent<Rigidbody2D>() == null)
            {
                var rigid = gameObject.AddComponent<Rigidbody2D>();
                rigid.bodyType = RigidbodyType2D.Static;
            }

        }
#endif
    }

}