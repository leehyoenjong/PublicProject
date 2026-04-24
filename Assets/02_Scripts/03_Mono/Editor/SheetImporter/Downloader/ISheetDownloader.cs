#if UNITY_EDITOR
namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// CSV 소스 다운로더. DIP — 임포터는 이 인터페이스에 의존, 구현은 URL/로컬 등으로 교체 가능.
    /// 에디터 타임 동기 API.
    /// </summary>
    public interface ISheetDownloader
    {
        /// <summary>이 다운로더가 해당 source 문자열을 처리할 수 있는지.</summary>
        bool CanHandle(string source);

        /// <summary>
        /// source 에서 CSV 텍스트를 가져온다. 성공 시 true + csvText 설정,
        /// 실패 시 false + error 메시지 설정.
        /// </summary>
        bool TryDownload(string source, out string csvText, out string error);
    }
}
#endif
