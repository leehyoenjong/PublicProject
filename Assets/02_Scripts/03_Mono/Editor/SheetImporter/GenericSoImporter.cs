#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PublicFramework.Core.DataPipeline;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 다운로드된 CSV 텍스트를 받아 SO 로 변환/저장하는 범용 실행기.
    /// 흐름: CsvParse → FlatHeaderExpand → 별칭 병합 → 리플렉션 매핑 → SO 생성/갱신 → Save.
    /// 다운로드는 호출부(Window 등) 책임(SRP).
    /// 에디터 전용. 로그는 전부 `[SheetImporter]` 프리픽스.
    /// </summary>
    public static class GenericSoImporter
    {
        private const string LOG_PREFIX = "[SheetImporter]";
        private const string COMMENT_PREFIX = "#";
        private const string DEV_PREFIX = "*";
        private const string ASSET_EXTENSION = ".asset";
        private const string ID_COLUMN = "MID";
        private const string DEFAULT_OUTPUT_ROOT = "Assets/09_ScriptableObjects";
        private const string FIELD_PREFIX = "_";
        private const BindingFlags FIELD_FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _fieldCache =
            new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public readonly struct ImportResult
        {
            public readonly int Created;
            public readonly int Updated;
            public readonly int Failed;
            public readonly int ErrorLogs;
            public readonly int UnusedExisting;
            public readonly string FirstErrorOrNull;

            public ImportResult(int created, int updated, int failed, int errorLogs, int unusedExisting, string firstErrorOrNull)
            {
                Created = created;
                Updated = updated;
                Failed = failed;
                ErrorLogs = errorLogs;
                UnusedExisting = unusedExisting;
                FirstErrorOrNull = firstErrorOrNull;
            }
        }

        /// <summary>
        /// 다운로드된 CSV 텍스트로 단일 엔트리를 임포트.
        /// 호출부가 ISheetDownloader 를 통해 csvText 를 먼저 확보한 뒤 이 메서드에 넘긴다.
        /// </summary>
        public static ImportResult ImportEntry(
            SheetImporterConfig config,
            SheetImporterEntry entry,
            string csvText,
            Action<float, string> progressCallback = null)
        {
            var state = new Accumulator();
            progressCallback?.Invoke(0f, "검증");

            if (config == null || entry == null)
            {
                state.Error("ImportEntry: config/entry 인자가 null 입니다.");
                return state.ToResult();
            }

            Type targetType = entry.TargetType;
            if (targetType == null || !typeof(ScriptableObject).IsAssignableFrom(targetType))
            {
                string nameForError = string.IsNullOrEmpty(entry.EntryName) ? "(unnamed)" : entry.EntryName;
                state.Error($"엔트리 '{nameForError}': TargetScript 가 유효한 ScriptableObject 타입이 아닙니다.");
                return state.ToResult();
            }

            string entryName = string.IsNullOrEmpty(entry.EntryName) ? targetType.Name : entry.EntryName;
            string outputFolder = string.IsNullOrEmpty(entry.OutputFolder)
                ? $"{DEFAULT_OUTPUT_ROOT}/{targetType.Name}"
                : entry.OutputFolder;

            if (string.IsNullOrEmpty(csvText))
            {
                state.Error($"엔트리 '{entryName}': csvText 가 비어있습니다.");
                return state.ToResult();
            }

            if (SheetImporterReflectionHelper.LooksLikeHtml(csvText))
            {
                state.Error("CSV 응답이 아닙니다 (HTML 감지). 공유 권한(링크 보유자 뷰어 이상)과 URL 형식(/export?format=csv&gid=...)을 확인하세요.");
                return state.ToResult();
            }

            progressCallback?.Invoke(0.1f, "CSV 파싱");

            List<List<string>> raw;
            try
            {
                raw = CsvParser.ParseRaw(csvText);
            }
            catch (FormatException e)
            {
                state.Error($"CSV 파싱 실패(엔트리 '{entryName}'): {e.Message}");
                return state.ToResult();
            }

            int requestedHeaderRow = entry.HeaderRow > 0 ? entry.HeaderRow : 1;
            int headerRowIdx = requestedHeaderRow - 1;

            if (headerRowIdx >= raw.Count)
            {
                state.Error($"Header row {requestedHeaderRow} is out of CSV range (total {raw.Count} rows)");
                return state.ToResult();
            }

            var headerLine = raw[headerRowIdx];
            if (IsRowEmpty(headerLine))
            {
                state.Error($"Header row {requestedHeaderRow} is empty");
                return state.ToResult();
            }

            int requestedFirstCol = entry.FirstDataColumn > 0 ? entry.FirstDataColumn : 1;
            int firstColInclusive = requestedFirstCol - 1;
            int lastColExclusive = entry.LastDataColumn > 0
                ? Math.Min(entry.LastDataColumn, headerLine.Count)
                : headerLine.Count;

            if (firstColInclusive >= headerLine.Count)
            {
                state.Error($"First data column {requestedFirstCol} exceeds header column count {headerLine.Count}");
                return state.ToResult();
            }
            if (firstColInclusive >= lastColExclusive)
            {
                string lastLabel = entry.LastDataColumn > 0 ? entry.LastDataColumn.ToString() : "end";
                state.Error($"Data column range is invalid: first {requestedFirstCol} > last {lastLabel}");
                return state.ToResult();
            }

            var parsedRows = BuildDictRows(raw, headerLine, headerRowIdx, firstColInclusive, lastColExclusive);

            if (parsedRows.Count == 0)
            {
                Debug.LogWarning($"{LOG_PREFIX} 엔트리 '{entryName}': 데이터 행이 없습니다.");
                return state.ToResult();
            }

            StripCommentColumns(parsedRows);

            progressCallback?.Invoke(0.2f, "헤더 전개");
            var expansion = FlatHeaderExpander.Expand(parsedRows);
            for (int i = 0; i < expansion.Errors.Count; i++)
            {
                Debug.LogError(expansion.Errors[i]);
                state.RawError(expansion.Errors[i]);
            }

            progressCallback?.Invoke(0.3f, "별칭 병합");
            var aliasMap = BuildAliasMap(targetType, entry, entryName);

            EnsureFolderExists(outputFolder);
            var existingById = LoadExistingAssets(targetType, outputFolder);

            var processedIds = new HashSet<string>(StringComparer.Ordinal);
            int rowCount = expansion.Rows.Count;

            for (int rowIdx = 0; rowIdx < rowCount; rowIdx++)
            {
                float p = 0.3f + (0.6f * (rowIdx / (float)Math.Max(1, rowCount)));
                progressCallback?.Invoke(p, $"행 {rowIdx + headerRowIdx + 2} 처리");
                ProcessRow(
                    rowIdx,
                    headerRowIdx,
                    expansion.Rows[rowIdx],
                    targetType,
                    aliasMap,
                    outputFolder,
                    existingById,
                    processedIds,
                    entryName,
                    state);
            }

            // 미사용 SO
            var unused = new List<string>();
            foreach (var kv in existingById)
            {
                if (!processedIds.Contains(kv.Key)) unused.Add(kv.Key);
            }
            if (unused.Count > 0)
            {
                state.UnusedExisting = unused.Count;
                Debug.Log($"{LOG_PREFIX} 엔트리 '{entryName}' 미사용 SO {unused.Count}건: {string.Join(", ", unused)}");
            }

            progressCallback?.Invoke(0.93f, "저장");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (entry.CollectionType != null)
            {
                progressCallback?.Invoke(0.96f, "컬렉션 등록");
                RegisterToCollection(entry, targetType, outputFolder, entryName, state);
            }

            Debug.Log($"{LOG_PREFIX} 엔트리 '{entryName}' 완료: 생성 {state.Created} / 갱신 {state.Updated} / 실패 {state.Failed} (로그 {state.ErrorLogs}건)");
            progressCallback?.Invoke(1f, "완료");

            return state.ToResult();
        }

        /// <summary>
        /// 임포트 완료 후 outputFolder 의 대상 타입 SO 를 수집해 지정 DataCollection SO 에 일괄 등록.
        /// 컬렉션 자산이 없으면 자동 생성. 정렬 기준은 파일명(MID) Ordinal.
        /// </summary>
        private static void RegisterToCollection(
            SheetImporterEntry entry,
            Type targetType,
            string outputFolder,
            string entryName,
            Accumulator state)
        {
            Type collectionType = entry.CollectionType;
            if (!typeof(ScriptableObject).IsAssignableFrom(collectionType))
            {
                state.Error($"엔트리 '{entryName}' CollectionScript '{collectionType.Name}' 는 ScriptableObject 가 아닙니다.");
                return;
            }

            MethodInfo setItems = collectionType.GetMethod(
                "SetItems",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (setItems == null)
            {
                state.Error($"엔트리 '{entryName}' CollectionScript '{collectionType.Name}' 에 SetItems 가 없습니다. DataCollection<T> 상속 여부를 확인하세요.");
                return;
            }

            ParameterInfo[] setParams = setItems.GetParameters();
            if (setParams.Length != 1 || !setParams[0].ParameterType.IsArray)
            {
                state.Error($"엔트리 '{entryName}' CollectionScript '{collectionType.Name}' SetItems 시그니처가 (T[]) 가 아닙니다.");
                return;
            }

            Type expectedElement = setParams[0].ParameterType.GetElementType();
            if (expectedElement == null || !expectedElement.IsAssignableFrom(targetType))
            {
                state.Error($"엔트리 '{entryName}' CollectionScript '{collectionType.Name}' 요소 타입({expectedElement?.Name}) 이 대상 SO({targetType.Name}) 와 호환되지 않습니다.");
                return;
            }

            var loaded = LoadExistingAssets(targetType, outputFolder);

            // 자체 컬렉션 SO 가 같은 폴더에 있으면 제외 (자기 자신 포함 방지)
            string collectionPath = $"{outputFolder}/{collectionType.Name}{ASSET_EXTENSION}";
            var keys = new List<string>(loaded.Count);
            foreach (var kv in loaded)
            {
                if (AssetDatabase.GetAssetPath(kv.Value) == collectionPath) continue;
                keys.Add(kv.Key);
            }
            keys.Sort(StringComparer.Ordinal);

            Array itemArray = Array.CreateInstance(expectedElement, keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                itemArray.SetValue(loaded[keys[i]], i);
            }

            var collection = AssetDatabase.LoadAssetAtPath(collectionPath, collectionType) as ScriptableObject;
            if (collection == null)
            {
                collection = ScriptableObject.CreateInstance(collectionType);
                AssetDatabase.CreateAsset(collection, collectionPath);
                Debug.Log($"{LOG_PREFIX} 컬렉션 생성 → {collectionPath}");
            }

            setItems.Invoke(collection, new object[] { itemArray });
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();

            Debug.Log($"{LOG_PREFIX} 컬렉션 '{collectionType.Name}' {keys.Count}건 등록 → {collectionPath}");
        }

        // ==================== 누적 상태 ====================

        internal class Accumulator
        {
            public int Created;
            public int Updated;
            public int Failed;
            public int ErrorLogs;
            public int UnusedExisting;
            public string FirstError;

            public void Error(string body)
            {
                string full = $"{LOG_PREFIX} {body}";
                Debug.LogError(full);
                RawError(full);
            }

            public void RawError(string full)
            {
                ErrorLogs++;
                if (FirstError == null) FirstError = full;
            }

            public ImportResult ToResult()
            {
                return new ImportResult(Created, Updated, Failed, ErrorLogs, UnusedExisting, FirstError);
            }
        }

        // ==================== 준비 단계 ====================

        /// <summary>
        /// ParseRaw 결과에서 헤더 행 이후의 데이터 행을 [firstColInclusive, lastColExclusive) 컬럼만 사용해 Dictionary 로 재구성.
        /// 빈 헤더 컬럼은 건너뛰고, 모든 셀이 빈 행도 스킵.
        /// </summary>
        internal static List<Dictionary<string, string>> BuildDictRows(
            List<List<string>> raw,
            List<string> headerLine,
            int headerRowIdx,
            int firstColInclusive,
            int lastColExclusive)
        {
            int span = Math.Max(0, lastColExclusive - firstColInclusive);
            var rows = new List<Dictionary<string, string>>();
            for (int r = headerRowIdx + 1; r < raw.Count; r++)
            {
                var rowCells = raw[r];
                var dict = new Dictionary<string, string>(span);
                bool allEmpty = true;

                for (int c = firstColInclusive; c < lastColExclusive; c++)
                {
                    string key = c < headerLine.Count ? headerLine[c] : string.Empty;
                    if (string.IsNullOrEmpty(key)) continue;

                    string val = c < rowCells.Count ? (rowCells[c] ?? string.Empty) : string.Empty;
                    dict[key] = val;
                    if (!string.IsNullOrEmpty(val)) allEmpty = false;
                }

                if (dict.Count == 0 || allEmpty) continue;
                rows.Add(dict);
            }
            return rows;
        }

        private static bool IsRowEmpty(List<string> row)
        {
            if (row == null || row.Count == 0) return true;
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrEmpty(row[i])) return false;
            }
            return true;
        }

        internal static void StripCommentColumns(List<Dictionary<string, string>> rows)
        {
            if (rows.Count == 0) return;
            var toRemove = new List<string>();
            foreach (var key in rows[0].Keys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                if (key.StartsWith(COMMENT_PREFIX) || key.StartsWith(DEV_PREFIX)) toRemove.Add(key);
            }
            if (toRemove.Count == 0) return;

            for (int r = 0; r < rows.Count; r++)
            {
                for (int k = 0; k < toRemove.Count; k++) rows[r].Remove(toRemove[k]);
            }
        }

        private static Dictionary<string, string> BuildAliasMap(
            Type targetType,
            SheetImporterEntry entry,
            string entryName)
        {
            // 1) [SheetAlias] 속성 기반 기본 매핑 (서브필드 포함 재귀 수집)
            var aliases = new List<(string sheetHeader, string fieldName)>();
            if (entry?.Aliases != null)
            {
                for (int i = 0; i < entry.Aliases.Count; i++)
                {
                    var a = entry.Aliases[i];
                    if (a == null) continue;
                    aliases.Add((a.SheetHeader, a.FieldName));
                }
            }

            var map = SheetImporterReflectionHelper.BuildAliasMap(targetType, aliases);

            // 중복 감지: 동일 sheetHeader 에 속성 별칭 + Config 별칭이 동시에 있고 값이 다르면 Config 가 이긴다(경고만).
            if (entry?.Aliases != null)
            {
                for (int i = 0; i < entry.Aliases.Count; i++)
                {
                    var a = entry.Aliases[i];
                    if (a == null || string.IsNullOrEmpty(a.SheetHeader) || string.IsNullOrEmpty(a.FieldName)) continue;
                    // 이미 속성으로 동일 매핑이 있으면 Config 별칭은 불필요(중복 경고)
                    // BuildAliasMap 내부에서 덮어쓰기 되었으므로 여기서는 알림만.
                }
            }

            return map;
        }

        internal static Dictionary<string, FieldInfo> GetFieldMap(Type type)
        {
            if (_fieldCache.TryGetValue(type, out var cached)) return cached;

            var map = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            Type current = type;
            while (current != null && current != typeof(object))
            {
                var fields = current.GetFields(FIELD_FLAGS | BindingFlags.DeclaredOnly);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!map.ContainsKey(fields[i].Name)) map[fields[i].Name] = fields[i];
                }
                current = current.BaseType;
            }

            _fieldCache[type] = map;
            return map;
        }

        private static void EnsureFolderExists(string outputFolder)
        {
            if (AssetDatabase.IsValidFolder(outputFolder)) return;

            var parts = outputFolder.Split('/');
            string accumulated = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = accumulated + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(accumulated, parts[i]);
                }
                accumulated = next;
            }
        }

        internal static bool IsExpandedRowEmpty(FlatHeaderExpander.ExpandedRow row)
        {
            if (row == null) return true;
            if (row.Flat != null)
            {
                foreach (var kv in row.Flat)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Value)) return false;
                }
            }
            if (row.Groups != null)
            {
                foreach (var kv in row.Groups)
                {
                    if (kv.Value == null) continue;
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        var sub = kv.Value[i];
                        if (sub == null) continue;
                        foreach (var sv in sub.Values)
                        {
                            if (!string.IsNullOrWhiteSpace(sv)) return false;
                        }
                    }
                }
            }
            return true;
        }

        internal static Dictionary<string, ScriptableObject> LoadExistingAssets(Type targetType, string outputFolder)
        {
            var map = new Dictionary<string, ScriptableObject>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets($"t:{targetType.Name}", new[] { outputFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var so = AssetDatabase.LoadAssetAtPath(path, targetType) as ScriptableObject;
                if (so == null) continue;

                string id = Path.GetFileNameWithoutExtension(path);
                if (!map.ContainsKey(id)) map[id] = so;
            }
            return map;
        }

        // ==================== 행 처리 ====================

        private static void ProcessRow(
            int rowIdx,
            int headerRowIdx,
            FlatHeaderExpander.ExpandedRow row,
            Type targetType,
            Dictionary<string, string> aliasMap,
            string outputFolder,
            Dictionary<string, ScriptableObject> existingById,
            HashSet<string> processedIds,
            string entryName,
            Accumulator state)
        {
            // 스프레드시트 기준 1-based 행 번호 = 헤더 행(headerRowIdx+1) + 데이터 오프셋(rowIdx+1)
            int displayRow = rowIdx + headerRowIdx + 2;

            // trailing empty row (Google Sheets CSV export 가 편집 이력 있던 빈 row 를 포함하는 케이스) silent skip
            if (IsExpandedRowEmpty(row)) return;

            if (!row.Flat.TryGetValue(ID_COLUMN, out string id) || string.IsNullOrEmpty(id))
            {
                state.Error($"Missing id at row {displayRow}");
                state.Failed++;
                return;
            }

            if (id.StartsWith(COMMENT_PREFIX))
            {
                Debug.Log($"{LOG_PREFIX} 행 {displayRow} 주석 스킵(id='{id}').");
                return;
            }

            if (!processedIds.Add(id))
            {
                state.Error($"Duplicate id '{id}' at row {displayRow}");
                state.Failed++;
                return;
            }

            bool isNew = !existingById.TryGetValue(id, out var target);
            if (isNew)
            {
                target = (ScriptableObject)ScriptableObject.CreateInstance(targetType);
            }

            var fieldMap = GetFieldMap(targetType);
            int fieldErrorsBefore = state.ErrorLogs;

            foreach (var kv in row.Flat)
            {
                ApplyFlatField(target, fieldMap, aliasMap, kv.Key, kv.Value, displayRow, state);
            }

            foreach (var groupKv in row.Groups)
            {
                ApplyGroupField(target, fieldMap, aliasMap, groupKv.Key, groupKv.Value, displayRow, state);
            }

            bool hasErrors = state.ErrorLogs > fieldErrorsBefore;

            if (hasErrors)
            {
                state.Failed++;
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
                return;
            }

            if (isNew)
            {
                string path = $"{outputFolder}/{id}{ASSET_EXTENSION}";
                AssetDatabase.CreateAsset(target, path);
                state.Created++;
            }
            else
            {
                EditorUtility.SetDirty(target);
                state.Updated++;
            }
        }

        internal static void ApplyFlatField(
            object target,
            Dictionary<string, FieldInfo> fieldMap,
            Dictionary<string, string> aliasMap,
            string sheetHeader,
            string rawValue,
            int displayRow,
            Accumulator state)
        {
            if (!TryResolveField(fieldMap, aliasMap, sheetHeader, out var fieldInfo))
            {
                Debug.LogWarning($"{LOG_PREFIX} Unknown header '{sheetHeader}' (alias missing?)");
                return;
            }

            TryConvertAndAssign(fieldInfo, target, rawValue, sheetHeader, displayRow, state);
        }

        internal static void ApplyGroupField(
            object target,
            Dictionary<string, FieldInfo> fieldMap,
            Dictionary<string, string> aliasMap,
            string groupName,
            List<Dictionary<string, string>> subRows,
            int displayRow,
            Accumulator state)
        {
            if (!TryResolveField(fieldMap, aliasMap, groupName, out var fieldInfo))
            {
                Debug.LogWarning($"{LOG_PREFIX} Unknown header '{groupName}' (alias missing?)");
                return;
            }

            Type fieldType = fieldInfo.FieldType;
            Type elementType = ResolveElementType(fieldType);
            if (elementType == null)
            {
                state.Error($"Cannot parse '{groupName}' as {fieldType.Name} (List<T> 또는 T[] 이어야 함)");
                return;
            }

            var elements = new List<object>(subRows.Count);
            for (int i = 0; i < subRows.Count; i++)
            {
                var subDict = subRows[i];
                object element = Activator.CreateInstance(elementType);
                var subFieldMap = GetFieldMap(elementType);

                foreach (var kv in subDict)
                {
                    string qualifiedKey = groupName + "." + kv.Key;
                    if (!TryResolveSubField(subFieldMap, aliasMap, qualifiedKey, kv.Key, out var subFieldInfo))
                    {
                        Debug.LogWarning($"{LOG_PREFIX} Unknown header '{qualifiedKey}' (alias missing?)");
                        continue;
                    }

                    TryConvertAndAssign(subFieldInfo, element, kv.Value, qualifiedKey, displayRow, state);
                }

                elements.Add(element);
            }

            AssignCollection(fieldInfo, target, fieldType, elementType, elements);
        }

        private static void TryConvertAndAssign(
            FieldInfo fieldInfo,
            object target,
            string rawValue,
            string sheetHeader,
            int displayRow,
            Accumulator state)
        {
            Type fieldType = fieldInfo.FieldType;

            if (!FieldConverterRegistry.TryConvert(rawValue, fieldType, out object converted, out string convertError))
            {
                string literal = BuildConversionLiteral(rawValue, fieldType, sheetHeader);
                state.Error($"{literal} (행 {displayRow}, 컬럼 '{sheetHeader}') | details: {convertError}");
                return;
            }

            try
            {
                fieldInfo.SetValue(target, converted);
            }
            catch (Exception ex)
            {
                state.Error($"Cannot parse '{rawValue}' as {fieldType.Name} (행 {displayRow}, 컬럼 '{sheetHeader}') | details: SetValue 실패: {ex.Message}");
            }
        }

        private static string BuildConversionLiteral(string rawValue, Type fieldType, string sheetHeader)
        {
            if (fieldType.IsEnum)
                return $"Unknown enum '{rawValue}' on field '{sheetHeader}'";
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                return $"Asset not found for '{rawValue}' on field '{sheetHeader}'";
            return $"Cannot parse '{rawValue}' as {fieldType.Name}";
        }

        /// <summary>
        /// 4단계 자동 매핑: 1) 별칭 → 2) 헤더 그대로 → 3) "_"+헤더 → 4) "_"+camelCase(헤더).
        /// 예: 헤더 "HP" → `_hp`, "DisplayName" → `_displayName`.
        /// </summary>
        private static bool TryResolveField(
            Dictionary<string, FieldInfo> fieldMap,
            Dictionary<string, string> aliasMap,
            string sheetHeader,
            out FieldInfo fieldInfo)
        {
            if (aliasMap.TryGetValue(sheetHeader, out string aliased))
            {
                if (fieldMap.TryGetValue(aliased, out fieldInfo)) return true;
            }

            if (fieldMap.TryGetValue(sheetHeader, out fieldInfo)) return true;
            if (fieldMap.TryGetValue(FIELD_PREFIX + sheetHeader, out fieldInfo)) return true;

            string camel = ToCamelCase(sheetHeader);
            if (!string.IsNullOrEmpty(camel) && fieldMap.TryGetValue(FIELD_PREFIX + camel, out fieldInfo)) return true;

            fieldInfo = null;
            return false;
        }

        /// <summary>
        /// 서브필드 매핑: 1) `부모필드.서브헤더` 별칭 → 2) 서브헤더 그대로 → 3) "_"+서브헤더 → 4) "_"+camelCase(서브헤더).
        /// </summary>
        private static bool TryResolveSubField(
            Dictionary<string, FieldInfo> subFieldMap,
            Dictionary<string, string> aliasMap,
            string qualifiedKey,
            string rawSubHeader,
            out FieldInfo fieldInfo)
        {
            if (aliasMap.TryGetValue(qualifiedKey, out string aliased))
            {
                if (subFieldMap.TryGetValue(aliased, out fieldInfo)) return true;
            }
            if (subFieldMap.TryGetValue(rawSubHeader, out fieldInfo)) return true;
            if (subFieldMap.TryGetValue(FIELD_PREFIX + rawSubHeader, out fieldInfo)) return true;

            string camel = ToCamelCase(rawSubHeader);
            if (!string.IsNullOrEmpty(camel) && subFieldMap.TryGetValue(FIELD_PREFIX + camel, out fieldInfo)) return true;

            fieldInfo = null;
            return false;
        }

        /// <summary>"HP" → "hp", "DisplayName" → "displayName". 전부 대문자면 소문자로, 그 외엔 첫 글자만 소문자.</summary>
        private static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (IsAllUpperLetters(s)) return s.ToLowerInvariant();
            if (char.IsUpper(s[0])) return char.ToLowerInvariant(s[0]) + s.Substring(1);
            return s;
        }

        private static bool IsAllUpperLetters(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetter(c) && !char.IsUpper(c)) return false;
            }
            return true;
        }

        private static Type ResolveElementType(Type collectionType)
        {
            if (collectionType.IsArray) return collectionType.GetElementType();
            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
                return collectionType.GetGenericArguments()[0];
            return null;
        }

        private static void AssignCollection(
            FieldInfo fieldInfo,
            object target,
            Type fieldType,
            Type elementType,
            List<object> elements)
        {
            if (fieldType.IsArray)
            {
                Array arr = Array.CreateInstance(elementType, elements.Count);
                for (int i = 0; i < elements.Count; i++) arr.SetValue(elements[i], i);
                fieldInfo.SetValue(target, arr);
                return;
            }

            var list = (IList)Activator.CreateInstance(fieldType);
            for (int i = 0; i < elements.Count; i++) list.Add(elements[i]);
            fieldInfo.SetValue(target, list);
        }
    }
}
#endif
