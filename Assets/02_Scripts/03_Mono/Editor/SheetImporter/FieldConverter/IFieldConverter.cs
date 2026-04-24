#if UNITY_EDITOR
using System;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// CSV 셀 문자열 → 지정 타입 변환 규약. LSP 통일: 모든 구현체가 동일 시그니처.
    /// CanConvert 는 등록 시 Type 매칭 용도(성능), TryConvert 는 실제 변환.
    /// 실패 시 value 는 null 또는 default, error 에 원인 기술.
    /// </summary>
    public interface IFieldConverter
    {
        /// <summary>이 컨버터가 targetType 을 처리할 수 있는지.</summary>
        bool CanConvert(Type targetType);

        /// <summary>
        /// 문자열을 targetType 으로 변환. 성공 시 true.
        /// 빈 입력은 Registry 가 상위에서 기본값 처리하므로 여기 도달 시 raw 는 비어있지 않다고 가정.
        /// </summary>
        bool TryConvert(string raw, Type targetType, out object value, out string error);
    }
}
#endif
