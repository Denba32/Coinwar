using UnityEngine;
using System.Collections.Generic;


#if UNITY_EDITOR
#endif

namespace StockGame.Editor.Scripts.DataDownloaders
{
    [CreateAssetMenu(fileName = "DataDownloaderConfig", menuName = "SpreadSheet/DataDownloaderConfig")]
    public class DataDownloaderConfig : ScriptableObject
    {
        [Header("SpreadSheet Settings")]
        [Tooltip("Google SpreadSheet Export Base URL\nex) https://docs.google.com/spreadsheets/d/{spreadsheetId}/export")]
        public string spreadSheetUrl;

        [Header("Sheet List")]
        [Tooltip("다운로드할 시트 설정 목록")]
        public List<SheetConfig> sheets = new List<SheetConfig>();

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 특정 SheetConfig의 다운로드 URL을 조합하여 반환합니다.
        /// 형식: {baseUrl}?format={tsv|csv}&gid={sheetId}&range={range}
        /// </summary>
        public string BuildDownloadUrl(SheetConfig sheet)
        {
            if (string.IsNullOrEmpty(spreadSheetUrl))
            {
                Debug.LogError($"[DataDownloaderConfig] '{name}' 의 SpreadSheet URL이 비어있습니다.");
                return string.Empty;
            }

            if (sheet == null)
            {
                Debug.LogError($"[DataDownloaderConfig] '{name}' 에 전달된 SheetConfig가 null입니다.");
                return string.Empty;
            }

            string url = spreadSheetUrl.TrimEnd('/');
            url += $"?format={sheet.GetFormatParam()}";

            if (!string.IsNullOrEmpty(sheet.sheetId))
                url += $"&gid={sheet.sheetId}";

            if (!string.IsNullOrEmpty(sheet.range))
                url += $"&range={sheet.range}";

            return url;
        }
    }
    public enum DownloadFormat
    {
        TSV,
        CSV
    }
}