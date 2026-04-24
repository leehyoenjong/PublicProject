#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 엔트리의 TargetType 을 리플렉션해 다른 SO 참조를 감지하고 엔트리 간 실행 순서를 해결한다.
    /// - 구체 ScriptableObject 타입 필드: 자동 감지
    /// - 베이스 ScriptableObject 타입 필드: DependsOnEntry Attribute 로 힌트
    /// - 배열/리스트 요소 타입도 재귀 검사
    /// 순환 의존은 경고 로그만 찍고 DFS 방문 순서에 따라 건너뛴다.
    /// </summary>
    public static class EntryDependencyResolver
    {
        private const string LOG_PREFIX = "[SheetImporter]";
        private const BindingFlags FIELD_FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>전체 엔트리를 Topological sort 로 정렬한 인덱스 리스트.</summary>
        public static List<int> ResolveOrder(IReadOnlyList<SheetImporterEntry> entries)
        {
            var result = new List<int>();
            if (entries == null || entries.Count == 0) return result;

            var typeToIndex = BuildTypeToIndex(entries);
            var visited = new HashSet<int>();
            var onStack = new HashSet<int>();
            bool cycleDetected = false;

            for (int i = 0; i < entries.Count; i++)
            {
                Visit(i, entries, typeToIndex, visited, onStack, result, ref cycleDetected);
            }

            if (cycleDetected)
            {
                Debug.LogWarning($"{LOG_PREFIX} 엔트리 간 순환 의존 감지. 순환 구간은 원본 순서로 폴백되었습니다.");
            }

            return result;
        }

        /// <summary>단건 대상 엔트리의 의존 체인(자기 자신 포함)을 실행 순서로 반환. 마지막 요소가 대상.</summary>
        public static List<int> ResolveChain(int targetIndex, IReadOnlyList<SheetImporterEntry> entries)
        {
            var result = new List<int>();
            if (entries == null || targetIndex < 0 || targetIndex >= entries.Count) return result;

            var typeToIndex = BuildTypeToIndex(entries);
            var visited = new HashSet<int>();
            var onStack = new HashSet<int>();
            bool cycleDetected = false;

            Visit(targetIndex, entries, typeToIndex, visited, onStack, result, ref cycleDetected);

            if (cycleDetected)
            {
                Debug.LogWarning($"{LOG_PREFIX} 엔트리 간 순환 의존 감지 (단건 체인).");
            }

            return result;
        }

        private static Dictionary<Type, int> BuildTypeToIndex(IReadOnlyList<SheetImporterEntry> entries)
        {
            var map = new Dictionary<Type, int>();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                var t = e.TargetType;
                if (t == null) continue;
                if (!map.ContainsKey(t)) map[t] = i;
            }
            return map;
        }

        private static void Visit(
            int idx,
            IReadOnlyList<SheetImporterEntry> entries,
            Dictionary<Type, int> typeToIndex,
            HashSet<int> visited,
            HashSet<int> onStack,
            List<int> result,
            ref bool cycleDetected)
        {
            if (visited.Contains(idx)) return;
            if (onStack.Contains(idx)) { cycleDetected = true; return; }
            onStack.Add(idx);

            var deps = CollectDependencies(idx, entries, typeToIndex);
            foreach (var d in deps)
            {
                Visit(d, entries, typeToIndex, visited, onStack, result, ref cycleDetected);
            }

            onStack.Remove(idx);
            visited.Add(idx);
            result.Add(idx);
        }

        private static HashSet<int> CollectDependencies(int idx, IReadOnlyList<SheetImporterEntry> entries, Dictionary<Type, int> typeToIndex)
        {
            var deps = new HashSet<int>();
            var entry = entries[idx];
            if (entry == null) return deps;
            var type = entry.TargetType;
            if (type == null) return deps;

            foreach (var field in type.GetFields(FIELD_FLAGS))
            {
                CollectFromField(field, deps, typeToIndex, idx);
            }

            return deps;
        }

        private static void CollectFromField(FieldInfo field, HashSet<int> deps, Dictionary<Type, int> typeToIndex, int selfIdx)
        {
            var attr = field.GetCustomAttribute<DependsOnEntryAttribute>();
            if (attr != null && attr.TargetTypes != null)
            {
                foreach (var t in attr.TargetTypes)
                {
                    if (t == null) continue;
                    if (typeToIndex.TryGetValue(t, out int hintIdx) && hintIdx != selfIdx)
                    {
                        deps.Add(hintIdx);
                    }
                }
            }

            var elementType = ResolveElementType(field.FieldType);
            if (elementType == null) return;
            if (elementType == typeof(ScriptableObject)) return; // 베이스 타입은 Attribute 전용
            if (!typeof(ScriptableObject).IsAssignableFrom(elementType)) return;

            if (typeToIndex.TryGetValue(elementType, out int autoIdx) && autoIdx != selfIdx)
            {
                deps.Add(autoIdx);
            }
        }

        private static Type ResolveElementType(Type fieldType)
        {
            if (fieldType == null) return null;
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return fieldType.GetGenericArguments()[0];
            }
            return fieldType;
        }
    }
}
#endif
