using UnityEngine;
using Object = UnityEngine.Object;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace StockGame.Editor.Scripts.DataDownloaders
{
    [CreateAssetMenu(fileName = "SheetConfig", menuName = "SpreadSheet/SheetConfig")]
    public class SheetConfig : ScriptableObject
    {
        [Tooltip("Google SpreadSheet의 시트 GID\nex) 0, 123456789")]
        public string sheetId;

        [Tooltip("다운로드할 셀 범위 (A1 표기법)\nex) A1:Z100  /  비워두면 시트 전체 다운로드")]
        public string range;

        [Tooltip("다운로드 파일 포맷")]
        public DownloadFormat downloadFormat = DownloadFormat.TSV;

        [Tooltip("저장될 파일 이름 (확장자 제외)\nex) ItemData, StageData")]
        public string fileName = "DownloadedData";

        [Tooltip("저장될 폴더를 Project 창에서 드래그하여 지정하세요.")]
        public Object saveFolder;

        // ── Helpers ──────────────────────────────────────────────────────────────

        public string GetFileExtension()
            => downloadFormat == DownloadFormat.TSV ? ".tsv" : ".csv";

        public string GetFormatParam()
            => downloadFormat == DownloadFormat.TSV ? "tsv" : "csv";

#if UNITY_EDITOR
        /// <summary>
        /// saveFolder 오브젝트로부터 AssetDatabase를 통해 절대 경로를 반환합니다.
        /// </summary>
        public string GetFullSavePath()
        {
            if (saveFolder == null)
            {
                Debug.LogError($"[SheetConfig] '{name}' 의 saveFolder가 비어있습니다.");
                return string.Empty;
            }

            string assetPath = AssetDatabase.GetAssetPath(saveFolder);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"[SheetConfig] '{name}' 의 saveFolder 경로를 AssetDatabase에서 찾을 수 없습니다.");
                return string.Empty;
            }

            string absoluteFolderPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", assetPath));

            return System.IO.Path.Combine(absoluteFolderPath, fileName + GetFileExtension());
        }

        /// <summary>Inspector / EditorWindow 표시용 (Assets/... 형태)</summary>
        public string GetAssetRelativeSavePath()
        {
            if (saveFolder == null) return "(폴더 미지정)";
            string assetPath = AssetDatabase.GetAssetPath(saveFolder);
            return string.IsNullOrEmpty(assetPath)
                ? "(경로 없음)"
                : $"{assetPath}/{fileName}{GetFileExtension()}";
        }
#endif
    }
}