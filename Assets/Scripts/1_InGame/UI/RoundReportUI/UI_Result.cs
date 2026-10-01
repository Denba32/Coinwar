using StockGame.Scripts.Define;
using System.Collections.Generic;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;

namespace StockGame.Scripts.UI.RoundReport
{
    public class UI_Result : UIBase
    {
        [SerializeField] private UI_ResultRow resultRowPrefab;
        [SerializeField] private RectTransform cellContent;
        private List<UI_ResultRow> rankingRows = new();

        public override void Initilaize(int sortOrder, GameDefine.UIDefine.UILayer uiLayer)
        {
            base.Initilaize(sortOrder, uiLayer);
        }

        public void UpdateRanking(List<StockRanking> infos)
        {
            if (infos == null || infos.Count <= 0)
            {
                Debug.Log("처리 에러 발생! 인원이 없습니다!");
                return;
            }

            ClearCell();
            foreach (var info in infos)
            {
                var row = Instantiate<UI_ResultRow>(resultRowPrefab, cellContent);
                row?.UpdateData(info.Ranking, info.Nickname, info.GetRankingProfile(), info.Profit);
                rankingRows?.Add(row);
            }
        }

        private void ClearCell()
        {
            if (rankingRows == null || rankingRows.Count <= 0) return;
            foreach (var row in rankingRows)
            {
                Destroy(row.gameObject);
            }
            rankingRows?.Clear();
        }
    }
}