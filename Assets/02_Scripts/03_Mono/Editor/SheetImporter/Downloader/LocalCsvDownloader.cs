#if UNITY_EDITOR
using System;
using System.IO;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 로컬 CSV 파일 다운로더. 프로젝트 상대("Assets/..." / "Packages/...") 또는 절대 경로 지원.
    /// 개발/QA 단계의 오프라인 테스트용.
    /// </summary>
    public class LocalCsvDownloader : ISheetDownloader
    {
        public bool CanHandle(string source)
        {
            if (string.IsNullOrEmpty(source)) return false;
            if (source.StartsWith("Assets/")) return true;
            if (source.StartsWith("Packages/")) return true;
            return Path.IsPathRooted(source);
        }

        public bool TryDownload(string source, out string csvText, out string error)
        {
            csvText = null;
            error = null;

            if (string.IsNullOrEmpty(source))
            {
                error = "[SheetImporter] Source 가 비어있습니다(LocalCsvDownloader).";
                return false;
            }

            if (!File.Exists(source))
            {
                error = $"[SheetImporter] 로컬 CSV 파일을 찾을 수 없습니다: {source}";
                return false;
            }

            try
            {
                csvText = File.ReadAllText(source);
                return true;
            }
            catch (Exception ex)
            {
                error = $"[SheetImporter] 로컬 CSV 읽기 실패 ({source}): {ex.Message}";
                return false;
            }
        }
    }
}
#endif
