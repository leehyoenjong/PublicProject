#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// SO 필드 수집 + 자동 매핑(GenericSoImporter 4단계 규칙) 시뮬레이션 + 별칭 분석.
    /// GenericSoImporter 내부 규칙과 동일 의미를 재현하지만, 에디터 UI(분석/프리뷰)용 공유 헬퍼로 분리한다.
    /// </summary>
    public static class SheetImporterReflectionHelper
    {
        private const string FIELD_PREFIX = "_";

        // -------- HTML 응답 감지 --------

        /// <summary>
        /// Google Sheets 가 /edit URL 에서 CSV 대신 HTML 을 돌려주는 경우 등을 조기 감지.
        /// 앞쪽 공백만 무시하고 `<!DOCTYPE` / `<html` / `<?xml` 접두인지만 확인. 대소문자 무시.
        /// </summary>
        public static bool LooksLikeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var trimmed = text.TrimStart();
            return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
        }


        // -------- SO 필드 수집 --------

        /// <summary>
        /// 대상 SO 타입의 직렬화 대상 필드 이름 리스트를 상속 체인 포함으로 수집.
        /// 규칙: public (NonSerialized 없음) 또는 non-public + [SerializeField].
        /// </summary>
        public static List<string> CollectSerializedFieldNames(Type targetType)
        {
            var result = new List<string>();
            if (targetType == null) return result;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var t = targetType;
            while (t != null && t != typeof(UnityEngine.Object) && t != typeof(object))
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    if (!IsUnitySerialized(f)) continue;
                    if (seen.Add(f.Name)) result.Add(f.Name);
                }
                t = t.BaseType;
            }
            return result;
        }

        private static bool IsUnitySerialized(FieldInfo f)
        {
            if (f == null) return false;
            if (f.IsStatic) return false;
            if (f.IsLiteral) return false;
            if (f.GetCustomAttribute<NonSerializedAttribute>() != null) return false;
            if (f.IsPublic) return true;
            return f.GetCustomAttribute<UnityEngine.SerializeField>() != null;
        }

        // -------- 자동 매핑 4단계 --------

        /// <summary>
        /// 헤더 → SO 필드명 해결. 4단계 우선순위:
        /// 1) 별칭 매핑, 2) 헤더 그대로, 3) "_"+헤더, 4) "_"+ToCamelCase(헤더).
        /// </summary>
        public static bool TryResolveField(
            HashSet<string> fieldNames,
            Dictionary<string, string> aliasMap,
            string sheetHeader,
            out string resolvedFieldName)
        {
            resolvedFieldName = null;
            if (fieldNames == null || string.IsNullOrEmpty(sheetHeader)) return false;

            if (aliasMap != null && aliasMap.TryGetValue(sheetHeader, out string aliased))
            {
                if (fieldNames.Contains(aliased))
                {
                    resolvedFieldName = aliased;
                    return true;
                }
            }

            if (fieldNames.Contains(sheetHeader))
            {
                resolvedFieldName = sheetHeader;
                return true;
            }

            string withPrefix = FIELD_PREFIX + sheetHeader;
            if (fieldNames.Contains(withPrefix))
            {
                resolvedFieldName = withPrefix;
                return true;
            }

            string camel = ToCamelCase(sheetHeader);
            if (!string.IsNullOrEmpty(camel))
            {
                string prefixed = FIELD_PREFIX + camel;
                if (fieldNames.Contains(prefixed))
                {
                    resolvedFieldName = prefixed;
                    return true;
                }
            }

            return false;
        }

        /// <summary>"HP" → "hp", "DisplayName" → "displayName". 전부 대문자면 소문자로, 그 외엔 첫 글자만 소문자.</summary>
        public static string ToCamelCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            bool allUpper = true;
            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsUpper(s[i]) && !char.IsDigit(s[i])) { allUpper = false; break; }
            }
            if (allUpper) return s.ToLowerInvariant();
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        // -------- 별칭 병합 (엔트리 단일 스코프) --------

        /// <summary>
        /// Config 별칭만 수집. 우선순위가 가장 높음.
        /// </summary>
        public static Dictionary<string, string> BuildAliasMap(
            IEnumerable<(string sheetHeader, string fieldName)> entryAliases)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (entryAliases == null) return map;

            foreach (var a in entryAliases)
            {
                if (string.IsNullOrEmpty(a.sheetHeader)) continue;
                map[a.sheetHeader] = a.fieldName;
            }
            return map;
        }

        /// <summary>
        /// `[SheetAlias]` 속성 기반 별칭 + Config 별칭을 합성. Config 가 우선.
        /// 서브타입(배열/리스트 요소 또는 직렬화 클래스)에 대해서도 `parent.alias` 형태로 재귀 수집.
        /// </summary>
        public static Dictionary<string, string> BuildAliasMap(
            Type targetType,
            IEnumerable<(string sheetHeader, string fieldName)> entryAliases)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            CollectAttributeAliases(targetType, null, map, new HashSet<Type>());

            if (entryAliases != null)
            {
                foreach (var a in entryAliases)
                {
                    if (string.IsNullOrEmpty(a.sheetHeader) || string.IsNullOrEmpty(a.fieldName)) continue;
                    map[a.sheetHeader] = a.fieldName;
                }
            }
            return map;
        }

        /// <summary>
        /// 타입 트리를 순회하며 `[SheetAlias]` 속성을 "sheetHeader → fieldName" 맵에 수집.
        /// 서브필드는 부모 필드의 시트 이름(`[SheetAlias]` 첫 별칭 또는 필드명에서 "_" 제거)을 prefix 로 사용.
        /// </summary>
        private static void CollectAttributeAliases(
            Type type,
            string parentPrefix,
            Dictionary<string, string> map,
            HashSet<Type> visited)
        {
            if (type == null) return;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)
                && type != typeof(UnityEngine.ScriptableObject)
                && !type.IsSubclassOf(typeof(UnityEngine.ScriptableObject))) return;
            if (!visited.Add(type)) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            Type t = type;
            while (t != null && t != typeof(UnityEngine.Object) && t != typeof(object))
            {
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    if (!IsUnitySerialized(f)) continue;

                    var attr = f.GetCustomAttribute<SheetAliasAttribute>();
                    if (attr != null && attr.Aliases != null)
                    {
                        for (int ai = 0; ai < attr.Aliases.Length; ai++)
                        {
                            string alias = attr.Aliases[ai];
                            if (string.IsNullOrEmpty(alias)) continue;
                            string qualified = string.IsNullOrEmpty(parentPrefix) ? alias : $"{parentPrefix}.{alias}";
                            if (!map.ContainsKey(qualified)) map[qualified] = f.Name;
                        }
                    }

                    Type sub = ResolveRecursiveType(f.FieldType);
                    if (sub != null && sub != typeof(string))
                    {
                        string sheetName = (attr != null && attr.Aliases != null && attr.Aliases.Length > 0 && !string.IsNullOrEmpty(attr.Aliases[0]))
                            ? attr.Aliases[0]
                            : StripLeadingUnderscore(f.Name);
                        string nextPrefix = string.IsNullOrEmpty(parentPrefix) ? sheetName : $"{parentPrefix}.{sheetName}";
                        CollectAttributeAliases(sub, nextPrefix, map, visited);
                    }
                }
                t = t.BaseType;
            }
        }

        private static Type ResolveRecursiveType(Type fieldType)
        {
            if (fieldType == null) return null;
            if (fieldType.IsArray) return fieldType.GetElementType();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return fieldType.GetGenericArguments()[0];
            if (fieldType.IsClass && !fieldType.IsPrimitive && fieldType != typeof(string)
                && !typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                return fieldType;
            return null;
        }

        private static string StripLeadingUnderscore(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return name[0] == '_' ? name.Substring(1) : name;
        }

        // -------- 별칭 분석 --------

        public readonly struct AliasAnalysis
        {
            public readonly List<string> UnmappedFields;      // SO 필드인데 어떤 헤더로도 도달 불가(단, 헤더 목록이 있는 경우에 한해 정확)
            public readonly List<AliasEntryRef> InvalidAliases; // fieldName 이 SO 에 없음
            public readonly List<AliasEntryRef> UnusedAliases;  // 자동 매핑으로 이미 해결되는데 별칭으로도 등록됨

            public AliasAnalysis(
                List<string> unmapped,
                List<AliasEntryRef> invalid,
                List<AliasEntryRef> unused)
            {
                UnmappedFields = unmapped;
                InvalidAliases = invalid;
                UnusedAliases = unused;
            }
        }

        public readonly struct AliasEntryRef
        {
            public readonly string SheetHeader;
            public readonly string FieldName;
            public AliasEntryRef(string header, string field)
            {
                SheetHeader = header;
                FieldName = field;
            }
        }

        /// <summary>
        /// 헤더 목록이 없을 때 호출하는 분석.
        /// - InvalidAliases: 별칭 fieldName 이 SO 필드에 없음
        /// - UnusedAliases: 별칭 sheetHeader 로부터 자동 매핑(2~4단계) 만으로도 동일 fieldName 에 도달 가능 → 중복 등록
        /// - UnmappedFields: 헤더를 모르면 정확한 계산 불가 → 빈 리스트
        /// </summary>
        public static AliasAnalysis AnalyzeAliases(
            Type targetType,
            IEnumerable<(string sheetHeader, string fieldName)> entryAliases)
        {
            var invalid = new List<AliasEntryRef>();
            var unused = new List<AliasEntryRef>();
            var fieldNameList = CollectSerializedFieldNames(targetType);
            var fieldNameSet = new HashSet<string>(fieldNameList, StringComparer.Ordinal);

            InspectAliases(entryAliases, fieldNameSet, invalid, unused);

            return new AliasAnalysis(new List<string>(), invalid, unused);
        }

        private static void InspectAliases(
            IEnumerable<(string sheetHeader, string fieldName)> src,
            HashSet<string> fieldNames,
            List<AliasEntryRef> invalid,
            List<AliasEntryRef> unused)
        {
            if (src == null) return;
            foreach (var a in src)
            {
                if (string.IsNullOrEmpty(a.sheetHeader) || string.IsNullOrEmpty(a.fieldName)) continue;

                if (!fieldNames.Contains(a.fieldName))
                {
                    invalid.Add(new AliasEntryRef(a.sheetHeader, a.fieldName));
                    continue;
                }

                // 별칭이 없었다면 자동 매핑(2~4)만으로도 도달하는가?
                if (TryAutoOnly(fieldNames, a.sheetHeader, out string autoResolved)
                    && string.Equals(autoResolved, a.fieldName, StringComparison.Ordinal))
                {
                    unused.Add(new AliasEntryRef(a.sheetHeader, a.fieldName));
                }
            }
        }

        /// <summary>1단계(별칭)를 제외한 2~4단계로만 해결 시도.</summary>
        private static bool TryAutoOnly(HashSet<string> fieldNames, string sheetHeader, out string resolved)
        {
            resolved = null;
            if (fieldNames.Contains(sheetHeader)) { resolved = sheetHeader; return true; }
            string withPrefix = FIELD_PREFIX + sheetHeader;
            if (fieldNames.Contains(withPrefix)) { resolved = withPrefix; return true; }
            string camel = ToCamelCase(sheetHeader);
            if (!string.IsNullOrEmpty(camel))
            {
                string prefixed = FIELD_PREFIX + camel;
                if (fieldNames.Contains(prefixed)) { resolved = prefixed; return true; }
            }
            return false;
        }

        // -------- 헤더 매칭 프리뷰 --------

        public enum HeaderMatchStatus
        {
            AutoMatched,       // 자동 매핑(2~4) 으로 해결
            AliasMatched,      // 별칭(1) 으로 해결
            NeedsAlias,        // SO 필드에는 있지만 매핑 불가 — 별칭 제안 가능
            Unknown,           // SO 에 해당 필드 없음 — 무시될 헤더
        }

        public readonly struct HeaderMatch
        {
            public readonly string SheetHeader;
            public readonly string ResolvedFieldName;
            public readonly HeaderMatchStatus Status;

            public HeaderMatch(string header, string resolved, HeaderMatchStatus status)
            {
                SheetHeader = header;
                ResolvedFieldName = resolved;
                Status = status;
            }
        }

        public static List<HeaderMatch> BuildHeaderMatches(
            Type targetType,
            IEnumerable<string> headers,
            Dictionary<string, string> aliasMap)
        {
            var result = new List<HeaderMatch>();
            if (headers == null) return result;

            var fieldNameList = CollectSerializedFieldNames(targetType);
            var fieldNameSet = new HashSet<string>(fieldNameList, StringComparer.Ordinal);

            // 속성 기반 별칭은 자동 매칭으로 분류. Config 별칭은 기존대로 AliasMatched.
            var attributeAliases = new Dictionary<string, string>(StringComparer.Ordinal);
            CollectAttributeAliases(targetType, null, attributeAliases, new HashSet<Type>());

            foreach (var h in headers)
            {
                if (string.IsNullOrEmpty(h)) continue;

                bool viaConfigAlias = aliasMap != null && aliasMap.TryGetValue(h, out string aliased) && fieldNameSet.Contains(aliased);
                if (viaConfigAlias)
                {
                    result.Add(new HeaderMatch(h, aliasMap[h], HeaderMatchStatus.AliasMatched));
                    continue;
                }

                if (attributeAliases.TryGetValue(h, out string byAttr) && fieldNameSet.Contains(byAttr))
                {
                    result.Add(new HeaderMatch(h, byAttr, HeaderMatchStatus.AutoMatched));
                    continue;
                }

                if (TryAutoOnly(fieldNameSet, h, out string auto))
                {
                    result.Add(new HeaderMatch(h, auto, HeaderMatchStatus.AutoMatched));
                    continue;
                }

                // 매칭 실패 — SO 에 아예 없는지, 그냥 별칭 등록이 필요한지는 UI 가 판단 (여기선 "NeedsAlias" 로 통일, SO 에 후보 없음은 "Unknown")
                if (fieldNameSet.Count == 0)
                {
                    result.Add(new HeaderMatch(h, null, HeaderMatchStatus.Unknown));
                }
                else
                {
                    result.Add(new HeaderMatch(h, null, HeaderMatchStatus.NeedsAlias));
                }
            }
            return result;
        }
    }
}
#endif
