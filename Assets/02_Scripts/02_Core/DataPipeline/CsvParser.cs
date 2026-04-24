using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PublicFramework.Core.DataPipeline
{
    /// <summary>
    /// RFC 4180 기반 CSV 파서. Unity 의존 없음(순수 C#).
    /// 따옴표 감싸기, 이스케이프(""), 필드 내 콤마/개행 허용.
    /// 첫 행을 헤더로 사용하는 Parse와 원시 2D 를 반환하는 ParseRaw 제공.
    /// </summary>
    public static class CsvParser
    {
        private const char FIELD_DELIMITER = ',';
        private const char QUOTE = '"';
        private const char CR = '\r';
        private const char LF = '\n';

        /// <summary>
        /// 헤더 기반 파싱. 1행을 헤더로 사용하여 각 데이터 행을 Dictionary 로 변환.
        /// 빈 입력/헤더만 있는 입력/따옴표 불일치는 예외.
        /// </summary>
        public static List<Dictionary<string, string>> Parse(string csvText)
        {
            var raw = ParseRaw(csvText);
            var headers = raw[0];
            if (headers.Count == 0)
                throw new FormatException("[CsvParser] 헤더 행이 비어있습니다.");

            var result = new List<Dictionary<string, string>>();

            for (int r = 1; r < raw.Count; r++)
            {
                var row = raw[r];
                if (IsEmptyRow(row)) continue;

                var dict = new Dictionary<string, string>(headers.Count);
                for (int c = 0; c < headers.Count; c++)
                {
                    string key = headers[c] ?? string.Empty;
                    if (string.IsNullOrEmpty(key)) continue;

                    string value = c < row.Count ? row[c] : string.Empty;
                    dict[key] = value ?? string.Empty;
                }
                result.Add(dict);
            }

            return result;
        }

        /// <summary>
        /// 원시 2D 파싱. 각 행은 셀 리스트로 반환.
        /// null/빈 문자열/개행만/따옴표 불일치 입력은 FormatException.
        /// </summary>
        public static List<List<string>> ParseRaw(string csvText)
        {
            if (string.IsNullOrEmpty(csvText))
                throw new FormatException("[CsvParser] 입력 CSV 가 비어있습니다.");

            var rows = new List<List<string>>();

            using var reader = new StringReader(csvText);
            var field = new StringBuilder();
            var row = new List<string>();

            bool inQuotes = false;
            bool fieldStarted = false;
            int lineNumber = 1;
            int quoteStartLine = 0;

            int read;
            while ((read = reader.Read()) != -1)
            {
                char ch = (char)read;

                if (inQuotes)
                {
                    if (ch == QUOTE)
                    {
                        int next = reader.Peek();
                        if (next == QUOTE)
                        {
                            field.Append(QUOTE);
                            reader.Read();
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        if (ch == LF) lineNumber++;
                        field.Append(ch);
                    }
                    continue;
                }

                if (ch == QUOTE)
                {
                    if (fieldStarted && field.Length > 0)
                        throw new FormatException($"[CsvParser] 비인용 필드 내 따옴표(line {lineNumber}).");

                    inQuotes = true;
                    fieldStarted = true;
                    quoteStartLine = lineNumber;
                    continue;
                }

                if (ch == FIELD_DELIMITER)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    fieldStarted = false;
                    continue;
                }

                if (ch == CR)
                {
                    if (reader.Peek() == LF) reader.Read();
                    row.Add(field.ToString());
                    field.Clear();
                    fieldStarted = false;
                    rows.Add(row);
                    row = new List<string>();
                    lineNumber++;
                    continue;
                }

                if (ch == LF)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    fieldStarted = false;
                    rows.Add(row);
                    row = new List<string>();
                    lineNumber++;
                    continue;
                }

                field.Append(ch);
                fieldStarted = true;
            }

            if (inQuotes)
                throw new FormatException($"[CsvParser] 따옴표가 닫히지 않았습니다(시작 line {quoteStartLine}).");

            if (field.Length > 0 || row.Count > 0 || fieldStarted)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            TrimTrailingEmptyRows(rows);

            if (rows.Count == 0)
                throw new FormatException("[CsvParser] 유효한 데이터 행이 없습니다(개행만 입력됨).");

            return rows;
        }

        private static bool IsEmptyRow(List<string> row)
        {
            if (row == null || row.Count == 0) return true;
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrEmpty(row[i])) return false;
            }
            return true;
        }

        private static void TrimTrailingEmptyRows(List<List<string>> rows)
        {
            while (rows.Count > 0 && IsEmptyRow(rows[rows.Count - 1]))
            {
                rows.RemoveAt(rows.Count - 1);
            }
        }
    }
}
