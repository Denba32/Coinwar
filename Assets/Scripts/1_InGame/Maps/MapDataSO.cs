using static StockGame.Scripts.Define.GameDefine.MissionDefine;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace StockGame.Scripts.Maps
{
    [CreateAssetMenu(fileName = "MapDataSO", menuName ="ScriptableObject/Maps/MapDataSO", order =0)]
    public sealed class MapDataSO : ScriptableObject
    {
        [Header("Game Round")]
        public int MaxRound;

        [Header("주식 확인 시간")]
        public int StockCheckTime;
        [Header("파밍 시간")]
        public int ExploreTime;
        [Header("주식 구매 및 판매 시간")]
        public int StockPurchaseAndSellTime;
        [Header("순위 확인 시간")]
        public int ReleaseRankingTime;
        [Header("최종 결과 발표 시간")]
        public int ResultTime;

        [Header("제공되는 미션 수")]
        public int MissionCount;
    }

    public sealed class MapData : IDisposable
    {
        private List<Mission> missions = new();
        public List<Mission> Missions => missions;

        private int maxRound;

        private int stockCheckTime;
        private int exploreTime;
        private int stockPurchaseAndSellTime;
        private int releaseRankingTime;
        private int resultTime;

        private int missionCount;

        public int MaxRound 
        {
            get => maxRound;
            set
            {
                maxRound = value;
                maxRound = Mathf.Clamp(maxRound, 3, 10);
            }
        }
        public int StockCheckTime { get => stockCheckTime; set => stockCheckTime = value; }
        public int ExploreTime { get => exploreTime; set => exploreTime = value; }
        public int StockPurchaseAndSellTime { get => stockPurchaseAndSellTime; set => stockPurchaseAndSellTime = value; }
        public int ReleaseRankingTime { get => releaseRankingTime; set => releaseRankingTime = value; }
        public int ResultTime { get => resultTime; set => resultTime = value; }
        public int MissionCount
        {
            get => missionCount;
            set
            {
                missionCount = Mathf.Clamp(value, 5, 10);
            }
        }

        public MapData(MapDataSO mapDataSO)
        {
            MaxRound = mapDataSO.MaxRound;
            stockCheckTime = mapDataSO.StockCheckTime;
            ExploreTime = mapDataSO.ExploreTime;
            StockPurchaseAndSellTime = mapDataSO.StockPurchaseAndSellTime;
            ReleaseRankingTime = mapDataSO.ReleaseRankingTime;
            ResultTime = mapDataSO.ResultTime;
            MissionCount = mapDataSO.MissionCount;
        }

        public void Dispose()
        {
            if (missions == null) return;
            foreach(var mission in missions)
            {
                mission?.Dispose(); 
            }
            missions?.Clear();
            missions = null;
        }
    }
}