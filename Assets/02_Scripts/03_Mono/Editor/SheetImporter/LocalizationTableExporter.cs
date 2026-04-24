#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using PublicFramework.Core.DataPipeline;
using UnityEditor;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// Localization 모드 전용 내보내기.
    /// 시트(MID + 언어열) → 언어별 LocalizationTable.asset 생성/갱신.
    /// GenericSoImporter.ImportResult 를 재사용해 Window 요약 표시와 정합을 유지.
    /// </summary>
    public static class LocalizationTableExporter
    {
        private const string LOG_PREFIX = "[SheetImporter]";
        private const string DEFAULT_OUTPUT_FOLDER = "Assets/09_ScriptableObjects/LocalizationTable";
        private const string ASSET_EXTENSION = ".asset";
        private const string DEFAULT_KEY_HEADER = "MID";
        private const string ASSETS_ROOT = "Assets";

        public static GenericSoImporter.ImportResult ExportEntry(
            SheetImporterConfig config,
            SheetImporterEntry entry,
            string csvText,
            Action<float, string> progressCallback = null)
        {
            int created = 0;
            int updated = 0;
            int failed = 0;
            int errorLogs = 0;
            string firstError = null;

            void Error(string body)
            {
                string full = $"{LOG_PREFIX} {body}";
                Debug.LogError(full);
                errorLogs++;
                if (firstError == null) firstError = full;
            }

            progressCallback?.Invoke(0f, "검증");

            if (config == null || entry == null)
            {
                Error("ExportEntry: config/entry 인자가 null 입니다.");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            string entryName = string.IsNullOrEmpty(entry.EntryName) ? "Localization" : entry.EntryName;

            if (string.IsNullOrEmpty(csvText))
            {
                Error($"엔트리 '{entryName}': csvText 가 비어있습니다.");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            if (SheetImporterReflectionHelper.LooksLikeHtml(csvText))
            {
                Error("CSV 응답이 아닙니다 (HTML 감지). 공유 권한과 URL 형식(/export?format=csv&gid=...)을 확인하세요.");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            progressCallback?.Invoke(0.1f, "CSV 파싱");

            List<List<string>> raw;
            try
            {
                raw = CsvParser.ParseRaw(csvText);
            }
            catch (FormatException e)
            {
                Error($"CSV 파싱 실패(엔트리 '{entryName}'): {e.Message}");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            int headerRow = entry.HeaderRow > 0 ? entry.HeaderRow : 1;
            int headerRowIdx = headerRow - 1;
            if (headerRowIdx >= raw.Count)
            {
                Error($"Header row {headerRow} is out of CSV range (total {raw.Count} rows)");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            var headerLine = raw[headerRowIdx];
            string keyHeader = string.IsNullOrEmpty(entry.KeyHeader) ? DEFAULT_KEY_HEADER : entry.KeyHeader;

            int firstCol = entry.FirstDataColumn > 0 ? entry.FirstDataColumn - 1 : 0;
            int lastColExcl = entry.LastDataColumn > 0
                ? Math.Min(entry.LastDataColumn, headerLine.Count)
                : headerLine.Count;

            if (firstCol >= lastColExcl)
            {
                Error($"열 범위가 유효하지 않습니다: first={entry.FirstDataColumn}, last={entry.LastDataColumn}");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            int keyColIdx = -1;
            var languageCols = new List<(int ColIdx, LanguageCode Lang)>();
            for (int c = firstCol; c < lastColExcl; c++)
            {
                string h = (headerLine[c] ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(h)) continue;

                if (h.Equals(keyHeader, StringComparison.OrdinalIgnoreCase))
                {
                    keyColIdx = c;
                    continue;
                }

                if (Enum.TryParse<LanguageCode>(h, true, out LanguageCode lang))
                {
                    languageCols.Add((c, lang));
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX} 알 수 없는 언어 헤더 '{h}' — 스킵");
                }
            }

            if (keyColIdx < 0)
            {
                Error($"키 헤더 '{keyHeader}' 를 찾을 수 없습니다.");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            if (languageCols.Count == 0)
            {
                Error("인식 가능한 언어 헤더가 없습니다 (예: ko, en, ja).");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            progressCallback?.Invoke(0.3f, "데이터 수집");

            var tables = new Dictionary<LanguageCode, List<LocalizationEntry>>();
            foreach (var col in languageCols)
            {
                tables[col.Lang] = new List<LocalizationEntry>();
            }

            for (int r = headerRowIdx + 1; r < raw.Count; r++)
            {
                var row = raw[r];
                if (row.Count <= keyColIdx) continue;

                string keyRaw = (row[keyColIdx] ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(keyRaw)) continue;
                if (keyRaw.StartsWith("#")) continue;

                if (!int.TryParse(keyRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int key))
                {
                    Debug.LogWarning($"{LOG_PREFIX} {r + 1}행 정수 키 파싱 실패: '{keyRaw}' — 행 스킵");
                    failed++;
                    continue;
                }

                foreach (var col in languageCols)
                {
                    string val = col.ColIdx < row.Count ? (row[col.ColIdx] ?? string.Empty) : string.Empty;
                    tables[col.Lang].Add(new LocalizationEntry(key, val));
                }
            }

            progressCallback?.Invoke(0.6f, "SO 갱신");

            string outputFolder = string.IsNullOrEmpty(entry.OutputFolder) ? DEFAULT_OUTPUT_FOLDER : entry.OutputFolder;
            if (!EnsureFolder(outputFolder))
            {
                Error($"출력 폴더를 생성할 수 없습니다: {outputFolder}");
                return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
            }

            foreach (var kvp in tables)
            {
                string assetPath = $"{outputFolder}/{kvp.Key}{ASSET_EXTENSION}";
                LocalizationEntry[] entries = kvp.Value.ToArray();

                var existing = AssetDatabase.LoadAssetAtPath<LocalizationTable>(assetPath);
                if (existing != null)
                {
                    existing.SetData(kvp.Key, entries);
                    EditorUtility.SetDirty(existing);
                    updated++;
                    Debug.Log($"{LOG_PREFIX} 로컬라이제이션 '{kvp.Key}' {entries.Length}행 갱신 → {assetPath}");
                }
                else
                {
                    var table = ScriptableObject.CreateInstance<LocalizationTable>();
                    table.SetData(kvp.Key, entries);
                    AssetDatabase.CreateAsset(table, assetPath);
                    created++;
                    Debug.Log($"{LOG_PREFIX} 로컬라이제이션 '{kvp.Key}' {entries.Length}행 생성 → {assetPath}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            progressCallback?.Invoke(1f, "완료");

            return new GenericSoImporter.ImportResult(created, updated, failed, errorLogs, 0, firstError);
        }

        /// <summary>Assets/ 하위 경로를 한 단계씩 확인하며 없으면 생성.</summary>
        private static bool EnsureFolder(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (!assetPath.StartsWith(ASSETS_ROOT)) return false;
            if (AssetDatabase.IsValidFolder(assetPath)) return true;

            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    string guid = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(guid)) return false;
                }
                current = next;
            }
            return AssetDatabase.IsValidFolder(assetPath);
        }
    }
}
#endif
