#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PublicFramework.Core.DataPipeline;
using UnityEditor;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 부모 GenericSO 엔트리에 연결된 ChildTableLink 하나를 처리.
    /// 자식 시트의 행을 부모 SO 의 배열/리스트 필드 요소로 주입한다.
    /// - 부모 타입/폴더는 호출자가 parentEntry 로 직접 전달
    /// - 자식 시트의 parentIdHeader 로 그룹화 후 orderByHeader 로 정렬
    /// - GenericSoImporter 의 필드 매핑 로직(ApplyFlatField/ApplyGroupField) 재사용
    /// </summary>
    public static class ChildTableImporter
    {
        private const string LOG_PREFIX = "[SheetImporter]";
        private const string DEFAULT_OUTPUT_ROOT = "Assets/09_ScriptableObjects";
        private const BindingFlags FIELD_FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static GenericSoImporter.ImportResult ImportLink(
            SheetImporterEntry parentEntry,
            ChildTableLink link,
            string csvText,
            Action<float, string> progressCallback = null)
        {
            var state = new GenericSoImporter.Accumulator();
            progressCallback?.Invoke(0f, "검증");

            if (parentEntry == null || link == null)
            {
                state.Error("ChildTable: parentEntry/link 인자가 null 입니다.");
                return state.ToResult();
            }

            string parentLabel = string.IsNullOrEmpty(parentEntry.EntryName) ? "(unnamed)" : parentEntry.EntryName;
            string linkLabel = string.IsNullOrEmpty(link.ParentFieldName) ? "(unnamed-field)" : link.ParentFieldName;

            if (string.IsNullOrEmpty(link.ParentFieldName))
            {
                state.Error($"[{parentLabel}] 자식 링크: ParentFieldName 이 비어있습니다.");
                return state.ToResult();
            }
            if (string.IsNullOrEmpty(csvText))
            {
                state.Error($"[{parentLabel}.{linkLabel}] csvText 가 비어있습니다.");
                return state.ToResult();
            }

            Type parentType = parentEntry.TargetType;
            if (parentType == null || !typeof(ScriptableObject).IsAssignableFrom(parentType))
            {
                state.Error($"[{parentLabel}] 부모 엔트리의 TargetScript 가 유효한 ScriptableObject 가 아닙니다.");
                return state.ToResult();
            }

            string parentFolder = string.IsNullOrEmpty(parentEntry.OutputFolder)
                ? $"{DEFAULT_OUTPUT_ROOT}/{parentType.Name}"
                : parentEntry.OutputFolder;

            var parents = GenericSoImporter.LoadExistingAssets(parentType, parentFolder);
            if (parents.Count == 0)
            {
                state.Error($"[{parentLabel}.{linkLabel}] 부모 폴더 '{parentFolder}' 에 {parentType.Name} 에셋이 없습니다. 부모 엔트리가 먼저 성공해야 합니다.");
                return state.ToResult();
            }

            FieldInfo parentField = parentType.GetField(link.ParentFieldName, FIELD_FLAGS);
            if (parentField == null)
            {
                state.Error($"[{parentLabel}.{linkLabel}] 부모 타입 '{parentType.Name}' 에 필드 '{link.ParentFieldName}' 가 없습니다.");
                return state.ToResult();
            }

            Type elementType = ResolveElementType(parentField.FieldType);
            if (elementType == null)
            {
                state.Error($"[{parentLabel}.{linkLabel}] 필드 '{link.ParentFieldName}' 가 배열/리스트 타입이 아닙니다.");
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
                state.Error($"CSV 파싱 실패([{parentLabel}.{linkLabel}]): {e.Message}");
                return state.ToResult();
            }

            int requestedHeaderRow = link.HeaderRow > 0 ? link.HeaderRow : 1;
            int headerRowIdx = requestedHeaderRow - 1;
            if (headerRowIdx >= raw.Count)
            {
                state.Error($"Header row {requestedHeaderRow} is out of CSV range (total {raw.Count} rows)");
                return state.ToResult();
            }

            var headerLine = raw[headerRowIdx];
            int firstCol = link.FirstDataColumn > 0 ? link.FirstDataColumn - 1 : 0;
            int lastColExcl = link.LastDataColumn > 0
                ? Math.Min(link.LastDataColumn, headerLine.Count)
                : headerLine.Count;

            var parsedRows = GenericSoImporter.BuildDictRows(raw, headerLine, headerRowIdx, firstCol, lastColExcl);
            GenericSoImporter.StripCommentColumns(parsedRows);

            progressCallback?.Invoke(0.3f, "헤더 전개");
            var expansion = FlatHeaderExpander.Expand(parsedRows);
            for (int i = 0; i < expansion.Errors.Count; i++)
            {
                Debug.LogError(expansion.Errors[i]);
                state.RawError(expansion.Errors[i]);
            }

            string parentIdHeader = string.IsNullOrEmpty(link.ParentIdHeader) ? "parentId" : link.ParentIdHeader;
            string orderByHeader = string.IsNullOrEmpty(link.OrderByHeader) ? "order" : link.OrderByHeader;

            var aliases = new List<(string sheetHeader, string fieldName)>();
            if (link.Aliases != null)
            {
                foreach (var a in link.Aliases)
                {
                    if (a == null) continue;
                    aliases.Add((a.SheetHeader, a.FieldName));
                }
            }
            var aliasMap = SheetImporterReflectionHelper.BuildAliasMap(elementType, aliases);
            var elementFieldMap = GenericSoImporter.GetFieldMap(elementType);

            var groups = new Dictionary<string, List<ElementBuild>>(StringComparer.Ordinal);
            int rowCount = expansion.Rows.Count;

            progressCallback?.Invoke(0.5f, "요소 생성");

            for (int rowIdx = 0; rowIdx < rowCount; rowIdx++)
            {
                var expanded = expansion.Rows[rowIdx];
                if (GenericSoImporter.IsExpandedRowEmpty(expanded)) continue;

                int displayRow = rowIdx + headerRowIdx + 2;

                if (!expanded.Flat.TryGetValue(parentIdHeader, out string parentId) || string.IsNullOrEmpty(parentId))
                {
                    state.Error($"Missing parentId at row {displayRow}");
                    state.Failed++;
                    continue;
                }

                int order = 0;
                if (expanded.Flat.TryGetValue(orderByHeader, out string orderStr) && !string.IsNullOrEmpty(orderStr))
                {
                    int.TryParse(orderStr, out order);
                }

                object element = Activator.CreateInstance(elementType);
                int errorsBefore = state.ErrorLogs;

                foreach (var kv in expanded.Flat)
                {
                    if (kv.Key == parentIdHeader || kv.Key == orderByHeader) continue;
                    GenericSoImporter.ApplyFlatField(element, elementFieldMap, aliasMap, kv.Key, kv.Value, displayRow, state);
                }

                foreach (var groupKv in expanded.Groups)
                {
                    GenericSoImporter.ApplyGroupField(element, elementFieldMap, aliasMap, groupKv.Key, groupKv.Value, displayRow, state);
                }

                if (state.ErrorLogs > errorsBefore)
                {
                    state.Failed++;
                    continue;
                }

                if (!groups.TryGetValue(parentId, out var list))
                {
                    list = new List<ElementBuild>();
                    groups[parentId] = list;
                }
                list.Add(new ElementBuild(order, element));
            }

            progressCallback?.Invoke(0.85f, "부모 주입");
            int applied = 0;
            foreach (var kv in groups)
            {
                if (!parents.TryGetValue(kv.Key, out var parentSo))
                {
                    state.Error($"[{parentLabel}.{linkLabel}] parentId '{kv.Key}' 에 해당하는 부모 SO 를 찾을 수 없습니다 (경로: {parentFolder}).");
                    continue;
                }

                kv.Value.Sort((a, b) => a.Order.CompareTo(b.Order));
                Array arr = Array.CreateInstance(elementType, kv.Value.Count);
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    arr.SetValue(kv.Value[i].Element, i);
                }

                if (parentField.FieldType.IsArray)
                {
                    parentField.SetValue(parentSo, arr);
                }
                else
                {
                    var list = (IList)Activator.CreateInstance(parentField.FieldType);
                    for (int i = 0; i < kv.Value.Count; i++) list.Add(kv.Value[i].Element);
                    parentField.SetValue(parentSo, list);
                }

                EditorUtility.SetDirty(parentSo);
                applied++;
            }

            state.Updated = applied;

            AssetDatabase.SaveAssets();
            progressCallback?.Invoke(1f, "완료");

            Debug.Log($"{LOG_PREFIX} 자식 '{parentLabel}.{linkLabel}' 완료: 부모 {applied}건 주입 / 실패 {state.Failed}");

            return state.ToResult();
        }

        private static Type ResolveElementType(Type collectionType)
        {
            if (collectionType == null) return null;
            if (collectionType.IsArray) return collectionType.GetElementType();
            if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
                return collectionType.GetGenericArguments()[0];
            return null;
        }

        private readonly struct ElementBuild
        {
            public readonly int Order;
            public readonly object Element;
            public ElementBuild(int order, object element)
            {
                Order = order;
                Element = element;
            }
        }
    }
}
#endif

