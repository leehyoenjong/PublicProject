#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// HTTP(S) URL 에서 CSV 다운로드. Google Sheets 편집 URL 은 /export?format=csv 로 자동 정규화.
    /// 에디터 동기 실행을 위해 SendWebRequest 완료까지 스레드 슬립으로 폴링.
    /// </summary>
    public class UrlSheetDownloader : ISheetDownloader
    {
        private const int POLL_INTERVAL_MS = 10;
        private const int TIMEOUT_SEC = 30;
        private const string DEFAULT_GID = "0";
        private const string EXPORT_PATH_TOKEN = "/export?";

        private static readonly Regex GOOGLE_SHEETS_EDIT_PATTERN = new Regex(
            @"^https?://docs\.google\.com/spreadsheets/d/([a-zA-Z0-9_\-]+)/(?:edit|view|htmlview)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex GID_PATTERN = new Regex(
            @"[?#&]gid=(\d+)",
            RegexOptions.Compiled);

        public bool CanHandle(string source)
        {
            if (string.IsNullOrEmpty(source)) return false;
            return source.StartsWith("http://") || source.StartsWith("https://");
        }

        public bool TryDownload(string source, out string csvText, out string error)
        {
            csvText = null;
            error = null;

            if (string.IsNullOrEmpty(source))
            {
                error = "[SheetImporter] Source 가 비어있습니다(UrlSheetDownloader).";
                return false;
            }

            string effectiveUrl = NormalizeToCsvExport(source);
            if (!string.Equals(effectiveUrl, source, StringComparison.Ordinal))
            {
                Debug.Log($"[SheetImporter] Source URL 자동 변환: {source} → {effectiveUrl}");
            }

            UnityWebRequest req = null;
            try
            {
                req = UnityWebRequest.Get(effectiveUrl);
                req.timeout = TIMEOUT_SEC;
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    Thread.Sleep(POLL_INTERVAL_MS);
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    error = $"[SheetImporter] CSV 다운로드 실패 ({effectiveUrl}): {req.error}";
                    return false;
                }

                csvText = req.downloadHandler != null ? req.downloadHandler.text : null;
                if (string.IsNullOrEmpty(csvText))
                {
                    error = $"[SheetImporter] 다운로드된 CSV 가 비어있습니다 ({effectiveUrl}).";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = $"[SheetImporter] CSV 다운로드 예외 ({effectiveUrl}): {ex.Message}";
                return false;
            }
            finally
            {
                req?.Dispose();
            }
        }

        /// <summary>
        /// Google Sheets 편집/보기 URL 을 /export?format=csv 로 정규화.
        /// 이미 export 형식이거나 매칭 실패하면 원본 그대로 반환.
        /// </summary>
        public static string NormalizeToCsvExport(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            if (source.IndexOf(EXPORT_PATH_TOKEN, StringComparison.OrdinalIgnoreCase) >= 0) return source;

            var match = GOOGLE_SHEETS_EDIT_PATTERN.Match(source);
            if (!match.Success) return source;

            string sheetId = match.Groups[1].Value;
            string gid = DEFAULT_GID;
            var gidMatch = GID_PATTERN.Match(source);
            if (gidMatch.Success) gid = gidMatch.Groups[1].Value;

            return $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";
        }
    }
}
#endif
