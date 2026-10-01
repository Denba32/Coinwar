using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StockGame.Scripts.UI
{
    public class MiniMapMarker : MonoBehaviour
    {
        public enum MiniMapMarkerType
        {
            NormalMission,
            JobMission,
        }

        [SerializeField] private MiniMapMarkerType markerType;
        [SerializeField] private Image markerImage;
        [SerializeField] private TMP_Text markerCountText;
        [SerializeField] private List<Sprite> markerSprites;
        public void SetMarker(MiniMapMarkerType type, int count)
        {
            markerType = type;
            markerCountText.gameObject.SetActive(false);
            markerImage.sprite = markerSprites[(int)markerType];
            if (count > 1)
            {
                markerCountText.gameObject.SetActive(true);
                markerCountText.text = $"x{count}";
            }
        }
    }
}