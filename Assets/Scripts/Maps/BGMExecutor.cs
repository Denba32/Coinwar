using UnityEngine;

namespace StockGame.Scripts.Maps
{
    public class BGMExecutor : MonoBehaviour
    {
        [SerializeField] private ZoneType zoneType;
        [SerializeField] private Collider2D col;

        public ZoneType ZoneType => zoneType;
        public Collider2D Collider => col;
    }
}