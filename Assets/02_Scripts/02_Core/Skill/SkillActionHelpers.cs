using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>스킬 액션 공용 파싱 유틸.</summary>
    public static class SkillActionHelpers
    {
        private static readonly Regex VEC3_PATTERN = new Regex(
            @"\{\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*,\s*(-?\d+(?:\.\d+)?)\s*\}",
            RegexOptions.Compiled);

        /// <summary>`{x,y,z}` 포맷을 Vector3 로 파싱. 실패 시 Vector3.zero.</summary>
        public static Vector3 ParseVector3(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Vector3.zero;
            var m = VEC3_PATTERN.Match(raw);
            if (!m.Success) return Vector3.zero;

            float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
            float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
            float.TryParse(m.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 런타임에 BuffData 를 조회. SkillSystem 이 IBuffDataProvider 를 주입하거나,
        /// 에디터-로드된 BuffDataCollection 에서 MID 로 찾는다. MVP 는 Resources 검색 없이 런타임 인덱스 사용.
        /// </summary>
        public static BuffData FindBuffData(string buffId)
        {
            if (string.IsNullOrEmpty(buffId)) return null;
            return BuffDataIndex.Get(buffId);
        }
    }

    /// <summary>
    /// BuffData 런타임 조회 인덱스. 부팅 시 BuffDataCollection 을 Build 로 등록한 뒤, MID 로 조회.
    /// </summary>
    public static class BuffDataIndex
    {
        private static readonly System.Collections.Generic.Dictionary<string, BuffData> _map
            = new System.Collections.Generic.Dictionary<string, BuffData>();

        public static void Build(System.Collections.Generic.IEnumerable<BuffData> items)
        {
            _map.Clear();
            if (items == null) return;
            foreach (BuffData d in items)
            {
                if (d == null || string.IsNullOrEmpty(d.BuffId)) continue;
                _map[d.BuffId] = d;
            }
        }

        public static BuffData Get(string buffId)
        {
            return _map.TryGetValue(buffId, out BuffData d) ? d : null;
        }
    }
}
