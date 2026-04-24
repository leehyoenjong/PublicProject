#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// IFieldConverter 등록소. 기본 컨버터는 정적 초기화 시 자동 등록.
    /// 외부에서 Register 로 확장 가능(OCP). 타입별 조회 결과는 Dictionary 캐시.
    /// </summary>
    public static class FieldConverterRegistry
    {
        private static readonly List<IFieldConverter> _converters = new List<IFieldConverter>();
        private static readonly Dictionary<Type, IFieldConverter> _typedOverrides = new Dictionary<Type, IFieldConverter>();
        private static readonly Dictionary<Type, IFieldConverter> _cache = new Dictionary<Type, IFieldConverter>();
        private static bool _initialized;

        static FieldConverterRegistry()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// 타입별 오버라이드 등록. 해당 Type 에 대해서는 CanConvert 체인 대신 이 컨버터를 우선 사용.
        /// 동일 타입 재등록 시 덮어쓴다.
        /// </summary>
        public static void Register(Type targetType, IFieldConverter converter)
        {
            if (targetType == null || converter == null) return;
            EnsureInitialized();
            _typedOverrides[targetType] = converter;
            _cache.Remove(targetType);
        }

        /// <summary>
        /// CanConvert 기반 체인에 등록. 나중 등록이 우선(Insert(0) 이므로 커스텀이 기본보다 앞에 삽입됨).
        /// 범용 카테고리 컨버터(커스텀 Enum 처리 등) 추가 시 사용.
        /// </summary>
        public static void Register(IFieldConverter converter)
        {
            if (converter == null) return;
            EnsureInitialized();
            _converters.Insert(0, converter);
            _cache.Clear();
        }

        /// <summary>대상 타입에 매칭되는 컨버터. 없으면 null. 결과는 캐시됨.</summary>
        public static IFieldConverter GetConverterFor(Type targetType)
        {
            if (targetType == null) return null;
            EnsureInitialized();

            if (_typedOverrides.TryGetValue(targetType, out var typed)) return typed;
            if (_cache.TryGetValue(targetType, out var cached)) return cached;

            for (int i = 0; i < _converters.Count; i++)
            {
                if (_converters[i].CanConvert(targetType))
                {
                    _cache[targetType] = _converters[i];
                    return _converters[i];
                }
            }

            _cache[targetType] = null;
            return null;
        }

        /// <summary>
        /// 최상위 변환 진입점. 빈 입력은 해당 타입 기본값으로 성공 반환(기획 컨벤션).
        /// </summary>
        public static bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            error = null;

            if (targetType == null)
            {
                value = null;
                error = "[FieldConverter] targetType 이 null 입니다.";
                return false;
            }

            if (string.IsNullOrEmpty(raw))
            {
                value = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                return true;
            }

            var converter = GetConverterFor(targetType);
            if (converter == null)
            {
                value = null;
                error = FormatError(raw, targetType, "등록된 컨버터가 없습니다.");
                return false;
            }

            return converter.TryConvert(raw, targetType, out value, out error);
        }

        /// <summary>테스트/리셋 용도 — 레지스트리 상태를 기본값으로 복구.</summary>
        public static void ResetToDefaults()
        {
            _converters.Clear();
            _typedOverrides.Clear();
            _cache.Clear();
            _initialized = false;
            EnsureInitialized();
        }

        internal static string FormatError(string raw, Type type, string reason)
        {
            string typeName = type != null ? type.Name : "null";
            return $"[FieldConverter] 원본값 '{raw}'을 {typeName} 으로 변환 실패: {reason}";
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            // 등록 순서: 구체 타입 → 카테고리 타입(Enum/Array/List) → Unity Object → JSON 폴백.
            // GetConverterFor 는 CanConvert 가 true 인 첫 컨버터를 반환.
            _converters.Add(new StringConverter());
            _converters.Add(new IntConverter());
            _converters.Add(new LongConverter());
            _converters.Add(new FloatConverter());
            _converters.Add(new DoubleConverter());
            _converters.Add(new BoolConverter());
            _converters.Add(new Vector2Converter());
            _converters.Add(new Vector3Converter());
            _converters.Add(new ColorConverter());
            _converters.Add(new EnumConverter());
            _converters.Add(new RewardListConverter());
            _converters.Add(new PassiveStatListConverter());
            _converters.Add(new ArrayConverter());
            _converters.Add(new ListConverter());
            _converters.Add(new AssetReferenceConverter());
            _converters.Add(new NestedJsonConverter());
        }
    }
}
#endif
