using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PublicFramework.Core.DataPipeline
{
    /// <summary>
    /// 평탄화된 CSV 헤더(`name[index].subfield`)를 그룹으로 재구성한다.
    /// Unity 의존 0(CsvParser와 동일 계층). 에러는 LogError 대신 ExpansionResult.Errors 로 반환,
    /// 호출부(ImportService)가 실제 로그를 출력한다(SRP).
    ///
    /// 예) 헤더 `conditions[0].type, conditions[0].value` →
    ///     Groups[\"conditions\"] = [ {type, value}, ... ]
    /// </summary>
    public static class FlatHeaderExpander
    {
        private static readonly Regex GROUP_PATTERN = new Regex(
            @"^([a-zA-Z_][a-zA-Z0-9_]*)\[(\d+)\]\.([a-zA-Z_][a-zA-Z0-9_]*)$",
            RegexOptions.Compiled);

        /// <summary>
        /// 확장된 한 행. Flat 은 단순 컬럼, Groups 는 `name[n].sub` 헤더들이 그룹으로 묶인 결과.
        /// 그룹 내 인덱스 N 의 모든 서브필드가 빈 셀이면 해당 인덱스는 누락(스킵).
        /// </summary>
        public class ExpandedRow
        {
            public Dictionary<string, string> Flat = new Dictionary<string, string>();
            public Dictionary<string, List<Dictionary<string, string>>> Groups = new Dictionary<string, List<Dictionary<string, string>>>();
        }

        /// <summary>
        /// 확장 결과. Rows 는 파싱된 행 개수만큼, Errors 는 유효하지 않은 헤더에 대한
        /// 포맷 완료된 에러 메시지 목록(호출부에서 LogError 로 그대로 출력).
        /// </summary>
        public class ExpansionResult
        {
            public List<ExpandedRow> Rows = new List<ExpandedRow>();
            public List<string> Errors = new List<string>();
        }

        /// <summary>
        /// CsvParser.Parse 결과를 받아 평탄화 헤더를 그룹으로 재구성.
        /// 모든 행은 첫 행과 동일한 헤더 셋을 공유한다고 가정한다(CsvParser 계약).
        /// </summary>
        public static ExpansionResult Expand(List<Dictionary<string, string>> parsedRows)
        {
            var result = new ExpansionResult();
            if (parsedRows == null || parsedRows.Count == 0) return result;

            var headers = new List<string>(parsedRows[0].Keys);

            var flatHeaders = new List<string>();
            var groupHeaders = new Dictionary<string, List<GroupHeader>>();
            var reportedInvalid = new HashSet<string>();

            for (int i = 0; i < headers.Count; i++)
            {
                string header = headers[i];
                if (string.IsNullOrEmpty(header)) continue;

                if (header.IndexOf('[') < 0)
                {
                    flatHeaders.Add(header);
                    continue;
                }

                var match = GROUP_PATTERN.Match(header);
                if (!match.Success)
                {
                    if (reportedInvalid.Add(header))
                    {
                        result.Errors.Add(
                            $"[SheetImporter] Invalid flat header '{header}' — ignored. Expected pattern: name[index].subfield");
                    }
                    continue;
                }

                string groupName = match.Groups[1].Value;
                int index = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                string sub = match.Groups[3].Value;

                if (!groupHeaders.TryGetValue(groupName, out var list))
                {
                    list = new List<GroupHeader>();
                    groupHeaders[groupName] = list;
                }
                list.Add(new GroupHeader(header, index, sub));
            }

            for (int r = 0; r < parsedRows.Count; r++)
            {
                var row = parsedRows[r];
                var expanded = new ExpandedRow();

                for (int f = 0; f < flatHeaders.Count; f++)
                {
                    string key = flatHeaders[f];
                    expanded.Flat[key] = row.TryGetValue(key, out var v) ? (v ?? string.Empty) : string.Empty;
                }

                foreach (var kv in groupHeaders)
                {
                    string groupName = kv.Key;
                    var entries = kv.Value;

                    var indexSet = new SortedSet<int>();
                    for (int e = 0; e < entries.Count; e++) indexSet.Add(entries[e].Index);

                    var indexedList = new List<Dictionary<string, string>>();
                    foreach (int idx in indexSet)
                    {
                        var subDict = new Dictionary<string, string>();
                        bool allEmpty = true;

                        for (int e = 0; e < entries.Count; e++)
                        {
                            var eh = entries[e];
                            if (eh.Index != idx) continue;

                            string raw = row.TryGetValue(eh.Header, out var rv) ? (rv ?? string.Empty) : string.Empty;
                            subDict[eh.Sub] = raw;
                            if (!string.IsNullOrEmpty(raw)) allEmpty = false;
                        }

                        if (!allEmpty) indexedList.Add(subDict);
                    }

                    expanded.Groups[groupName] = indexedList;
                }

                result.Rows.Add(expanded);
            }

            return result;
        }

        private readonly struct GroupHeader
        {
            public readonly string Header;
            public readonly int Index;
            public readonly string Sub;

            public GroupHeader(string header, int index, string sub)
            {
                Header = header;
                Index = index;
                Sub = sub;
            }
        }
    }
}
