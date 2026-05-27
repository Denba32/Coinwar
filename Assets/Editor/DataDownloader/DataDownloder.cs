using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace StockGame.Editor.Scripts.DataDownloaders
{
    public class SpreadSheetDownloaderWindow : EditorWindow
    {

        private DataDownloaderConfig _config;

        private readonly Dictionary<SheetConfig, bool> _checkMap = new Dictionary<SheetConfig, bool>();

        private Vector2 _scrollPos;
        private bool _isDownloading;


        [MenuItem("Tools/SpreadSheet/Download Manager")]
        public static void OpenWindow()
        {
            var window = GetWindow<SpreadSheetDownloaderWindow>("SpreadSheet Downloader");
            window.minSize = new Vector2(520, 400);
            window.Show();
        }


        private void OnEnable()
        {
            // 마지막으로 사용한 Config를 EditorPrefs에서 복원
            string savedGuid = EditorPrefs.GetString("SpreadSheetDownloader_ConfigGuid", string.Empty);
            if (!string.IsNullOrEmpty(savedGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(savedGuid);
                if (!string.IsNullOrEmpty(path))
                    SetConfig(AssetDatabase.LoadAssetAtPath<DataDownloaderConfig>(path));
            }
        }


        private void OnGUI()
        {
            GUILayout.Space(8);
            DrawConfigSelector();
            GUILayout.Space(6);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("DataDownloaderConfig 에셋을 위 슬롯에 연결해주세요.", MessageType.Info);
                return;
            }

            DrawSheetList();
            GUILayout.Space(6);
            DrawBottomButtons();

            if (_isDownloading)
            {
                GUILayout.Space(4);
                EditorGUILayout.HelpBox("다운로드 중...", MessageType.None);
            }
        }


        private void DrawConfigSelector()
        {
            EditorGUILayout.LabelField("SpreadSheet Download Manager", EditorStyles.boldLabel);
            GUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Config", GUILayout.Width(50));

                var prev = _config;
                var next = (DataDownloaderConfig)EditorGUILayout.ObjectField(
                    _config, typeof(DataDownloaderConfig), false);

                if (next != prev)
                    SetConfig(next);
            }

            if (_config != null)
            {
                EditorGUILayout.LabelField(
                    $"URL: {(string.IsNullOrEmpty(_config.spreadSheetUrl) ? "(미입력)" : _config.spreadSheetUrl)}",
                    EditorStyles.miniLabel);
            }
        }


        private void DrawSheetList()
        {
            if (_config.sheets == null || _config.sheets.Count == 0)
            {
                EditorGUILayout.HelpBox("Config에 SheetConfig가 없습니다.\nInspector에서 sheets 리스트를 채워주세요.", MessageType.Warning);
                return;
            }

            // 전체 선택 / 해제
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("다운로드할 시트 선택", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("전체 선택", EditorStyles.miniButton, GUILayout.Width(60)))
                    SetAllChecks(true);
                if (GUILayout.Button("전체 해제", EditorStyles.miniButton, GUILayout.Width(60)))
                    SetAllChecks(false);
            }

            GUILayout.Space(2);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _config.sheets.Count; i++)
            {
                var sheet = _config.sheets[i];
                if (sheet == null)
                {
                    EditorGUILayout.HelpBox($"[{i}] SheetConfig가 null입니다.", MessageType.Warning);
                    continue;
                }

                if (!_checkMap.ContainsKey(sheet))
                    _checkMap[sheet] = false;

                DrawSheetRow(sheet, i);
                GUILayout.Space(3);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSheetRow(SheetConfig sheet, int index)
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                // 체크박스
                _checkMap[sheet] = EditorGUILayout.Toggle(_checkMap[sheet], GUILayout.Width(18));

                // 이름
                EditorGUILayout.LabelField($"[{index}]  {sheet.name}", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                // 즉시 다운로드 버튼
                GUI.enabled = !_isDownloading;
                if (GUILayout.Button("다운로드", EditorStyles.miniButton, GUILayout.Width(65)))
                    _ = DownloadSheetsAsync(new List<SheetConfig> { sheet });
                GUI.enabled = true;
            }

            // 상세 정보
            string savePath = sheet.GetAssetRelativeSavePath();
            string gid = string.IsNullOrEmpty(sheet.sheetId) ? "(전체)" : sheet.sheetId;
            string range = string.IsNullOrEmpty(sheet.range) ? "(전체)" : sheet.range;

            EditorGUILayout.LabelField(
                $"GID: {gid}   Range: {range}   Format: {sheet.downloadFormat}   저장: {savePath}",
                EditorStyles.miniLabel);
        }


        private void DrawBottomButtons()
        {
            int checkedCount = CountChecked();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !_isDownloading && checkedCount > 0;
                if (GUILayout.Button($"▼  선택 다운로드  ({checkedCount}개)", GUILayout.Height(30)))
                    _ = DownloadCheckedAsync();
                GUI.enabled = true;
            }
        }


        private void SetConfig(DataDownloaderConfig config)
        {
            _config = config;
            _checkMap.Clear();

            if (config != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(config));
                EditorPrefs.SetString("SpreadSheetDownloader_ConfigGuid", guid);

                if (config.sheets != null)
                    foreach (var sheet in config.sheets)
                        if (sheet != null)
                            _checkMap[sheet] = false;
            }
            else
            {
                EditorPrefs.DeleteKey("SpreadSheetDownloader_ConfigGuid");
            }

            Repaint();
        }

        private void SetAllChecks(bool value)
        {
            if (_config?.sheets == null) return;
            foreach (var sheet in _config.sheets)
                if (sheet != null && _checkMap.ContainsKey(sheet))
                    _checkMap[sheet] = value;
            Repaint();
        }

        private int CountChecked()
        {
            int count = 0;
            foreach (var kv in _checkMap)
                if (kv.Value) count++;
            return count;
        }


        private async Task DownloadCheckedAsync()
        {
            var targets = new List<SheetConfig>();
            foreach (var kv in _checkMap)
                if (kv.Value) targets.Add(kv.Key);

            await DownloadSheetsAsync(targets);
        }

        private async Task DownloadSheetsAsync(List<SheetConfig> targets)
        {
            if (targets == null || targets.Count == 0) return;

            _isDownloading = true;
            Repaint();

            int success = 0, fail = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                var sheet = targets[i];

                EditorUtility.DisplayProgressBar(
                    "SpreadSheet Downloader",
                    $"다운로드 중: {sheet.name}  ({i + 1} / {targets.Count})",
                    (float)(i + 1) / targets.Count);

                bool ok = await DownloadSheetAsync(_config, sheet);
                if (ok) success++; else fail++;
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            _isDownloading = false;
            Repaint();

            string msg = targets.Count == 1
                ? (success == 1
                    ? $"[{targets[0].name}] 다운로드 완료!\n{targets[0].GetAssetRelativeSavePath()}"
                    : $"[{targets[0].name}] 다운로드 실패.")
                : $"성공: {success}개  /  실패: {fail}개";

            EditorUtility.DisplayDialog(
                targets.Count == 1 ? (success == 1 ? "완료" : "실패") : "일괄 다운로드 완료",
                msg, "확인");
        }

        private static async UniTask<bool> DownloadSheetAsync(DataDownloaderConfig config, SheetConfig sheet)
        {
            string url = config.BuildDownloadUrl(sheet);
            if (string.IsNullOrEmpty(url)) return false;

            string fullPath = sheet.GetFullSavePath();
            if (string.IsNullOrEmpty(fullPath)) return false;

            try
            {
                using var client = new HttpClient();
                client.Timeout = System.TimeSpan.FromSeconds(30);

                Debug.Log($"[SpreadSheetDownloader] 다운로드 시작: {sheet.name}\nURL: {url}");

                string content = await client.GetStringAsync(url);

                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                await File.WriteAllTextAsync(fullPath, content, System.Text.Encoding.UTF8);

                Debug.Log($"[SpreadSheetDownloader] 저장 완료: {fullPath}");
                return true;
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"[SpreadSheetDownloader] HTTP 오류 [{sheet.name}]: {e.Message}");
                EditorUtility.DisplayDialog("다운로드 실패", $"[{sheet.name}]\nHTTP 오류: {e.Message}", "확인");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SpreadSheetDownloader] 오류 [{sheet.name}]: {e.Message}");
                EditorUtility.DisplayDialog("다운로드 실패", $"[{sheet.name}]\n{e.Message}", "확인");
                return false;
            }
        }
    }
}