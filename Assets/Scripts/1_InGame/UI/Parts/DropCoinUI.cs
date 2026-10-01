using Unity.VisualScripting;
using UnityEngine;

namespace StockGame.Scripts.UI
{
    public class DropCoinUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform coin;
        [SerializeField] private RectTransform respawnPoint;
        [SerializeField] private float fallingSpeed = 1.0f;

        private RectTransform[] spawnPoints;
        int index;

        private void Start()
        {
            spawnPoints = root.GetComponentsInChildren<RectTransform>();
        }

        private void Update()
        {
            if (coin == null) return;
            var position = coin.anchoredPosition;
            position.y -= fallingSpeed * Time.deltaTime;
            coin.anchoredPosition = position;

            if (coin.transform.position.y < respawnPoint.transform.position.y)
                SpawnNext();
        }

        private void SpawnNext()
        {
            index++;
            var idx = index % 5;
            var spawnPoint = spawnPoints[idx];
            coin.position= spawnPoint.position;
        }
    }
}