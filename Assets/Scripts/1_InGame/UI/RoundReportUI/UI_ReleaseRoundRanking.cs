using System.Collections.Generic;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_ReleaseRoundRanking : UIBase
    {
        [SerializeField] private UI_RankingRow rankingRowPrefab;
        [SerializeField] private RectTransform cellContent;
        private List<UI_RankingRow> rankingRows = new();

        public override void Initilaize(int sortOrder, UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
        }

        public void UpdateRanking(List<StockRanking> infos)
        {
            if (infos == null || infos.Count <= 0) return;
            ClearCell();
            foreach (var info in infos)
            {
                var row = Instantiate<UI_RankingRow>(rankingRowPrefab, cellContent);
                // RoundScore: 이번 라운드 추가 점수만 표시
                row?.UpdateData(info.Ranking, info.Nickname, info.GetRankingProfile(), info.Profit);
                rankingRows?.Add(row);
            }
        }

        private void ClearCell()
        {
            if (rankingRows == null || rankingRows.Count <= 0) return;
            foreach (var row in rankingRows)
                Destroy(row.gameObject);
            rankingRows?.Clear();
        }
    }
}