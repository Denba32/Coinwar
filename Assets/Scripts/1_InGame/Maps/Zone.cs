using StockGame.Scripts.Objects;
using System.Collections.Generic;
using UnityEngine;

namespace StockGame.Scripts.Maps
{
}

namespace StockGame.Scripts.Maps
{
    public enum ZoneType
    {
        None,
        ChiefHouse, // 촌장집
        PoliceOffice,     // 경찰서
        PortMarket,      // 항구시장
        Square,             // 광장
        Station,          // 남극기지
        Bank,              // 은행
        Port,            // 항구
        FishingSpot,       // 낚시터
        BaseCamp,     // 베이스캠프
        Bunker             // 벙커
    }
    public class Zone : MonoBehaviour
    {
        [Header("스폰 위치")]
        [SerializeField] private List<Transform> teleportPoints;
        [Header("입구")]
        [SerializeField] private Transform entrance;
        [Header("바리게이트")]
        [SerializeField] private Barricade barricade;
        [SerializeField] private BrokerObject brokerObject;
        public ZoneType ZoneType;

        public Transform GetTeleportPoint()
        {
            if (teleportPoints == null || teleportPoints.Count <= 0) return null;
            var random = Random.Range(0, teleportPoints.Count);
            return teleportPoints[random];
        }

        public BrokerObject GetBrokerObject => brokerObject;
        public Transform GetEntrance => entrance;
        public Barricade GetBarricade => barricade;
        public string GetZoneKey() => ZoneType.ToString();
        public string GetZoneName() => ZoneType switch
        {
            ZoneType.ChiefHouse => "촌장집",
            ZoneType.PoliceOffice => "경찰서",
            ZoneType.PortMarket => "항구 시장",
            ZoneType.Square => "광장",
            ZoneType.Station => "남극 기지",
            ZoneType.Bank => "은행",
            ZoneType.Port => "항구",
            ZoneType.FishingSpot => "낚시터",
            ZoneType.BaseCamp => "베이스 캠프",
            ZoneType.Bunker => "벙커",
            _ => string.Empty
        };
    }
}