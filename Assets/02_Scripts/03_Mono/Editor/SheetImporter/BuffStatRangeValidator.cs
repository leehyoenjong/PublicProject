#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using PublicFramework;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 시트 임포터 검증 룰. BuffData 의 PassiveStat 값이 단위 실수로 보이면 경고로 알린다.
    ///
    /// 단위 규약:
    ///  - Percent 레이어 값 = 비율 (1.0 = +100%, 0.1 = +10%, 0.01 = +1%).
    ///    예) +15% 버프는 0.15 로 적어야 하며, 15 로 적으면 +1500% 가 된다.
    ///  - Multiplicative 레이어 값 = 곱셈 인자 (1.5 = ×1.5, 1.0 = 변화 없음, 0.5 = 절반).
    ///  - Flat 레이어 = 절대 가산값 (도메인 의존) — 범위 검사하지 않는다.
    ///
    /// 임포트 후 자동 호출되어 의심 값을 콘솔 경고 + DisplayDialog 로 "확인 필요" 통지한다.
    /// 에러가 아닌 경고 — 의도적으로 큰 값일 수도 있으므로 임포트를 막지 않는다.
    /// 임계값은 상수라 게임 밸런스에 맞춰 조정한다.
    /// </summary>
    public static class BuffStatRangeValidator
    {
        private const string LOG_PREFIX = "[버프값범위검증]";
        private const string DIALOG_TITLE = "Buff Stat 값 범위 확인";
        private const string DIALOG_OK = "확인";
        private const string MENU_PATH = "PublicFramework/Tools/Validate Buff Stat Ranges";

        // 의심 임계값 (조정 가능)
        private const float PERCENT_ABS_MAX = 3.0f;   // Percent 레이어 |값| 상한 (= ±300%)
        private const float MULT_MAX = 5.0f;          // Multiplicative 레이어 값 상한 (= ×5)

        [MenuItem(MENU_PATH)]
        private static void ValidateFromMenu()
        {
            Validate(null);
        }

        /// <summary>
        /// 대상 폴더(없으면 프로젝트 전역)의 모든 BuffData 를 검사하고 의심 값을 모아 리포트한다.
        /// </summary>
        public static void Validate(string outputFolder)
        {
            List<BuffData> buffs = LoadBuffs(outputFolder);
            if (buffs.Count == 0)
            {
                Debug.Log($"{LOG_PREFIX} 검사 대상 BuffData 가 없음 — 건너뜀");
                return;
            }

            var lines = new List<string>();
            for (int i = 0; i < buffs.Count; i++)
            {
                BuffData buff = buffs[i];
                if (buff.TargetStats == null) continue;
                foreach (PassiveStat stat in buff.TargetStats)
                {
                    if (stat == null) continue;
                    if (!IsSuspicious(stat.Layer, stat.Value, out string detail)) continue;
                    lines.Add($"• {buff.BuffId}: {stat.StatType}/{stat.Layer} = {detail} — 실수인지 확인 필요");
                }
            }

            if (lines.Count == 0)
            {
                Debug.Log($"{LOG_PREFIX} 의심 값 없음 (BuffData {buffs.Count}건 점검)");
                return;
            }

            string body = string.Join("\n", lines);
            Debug.LogWarning($"{LOG_PREFIX} 의심 값 {lines.Count}건 — 단위 실수 가능성, 확인 필요\n{body}");
            EditorUtility.DisplayDialog(DIALOG_TITLE, body, DIALOG_OK);
        }

        /// <summary>
        /// 순수 판정 로직 (테스트 대상). 레이어별 단위 규약 기준으로 의심 값이면 true + 사람이 읽을 detail 을 반환.
        /// Flat 레이어는 절대값(도메인 의존)이라 검사하지 않는다.
        /// </summary>
        public static bool IsSuspicious(StatLayer layer, float value, out string detail)
        {
            switch (layer)
            {
                case StatLayer.Percent:
                    if (Mathf.Abs(value) > PERCENT_ABS_MAX)
                    {
                        string raw = value.ToString("0.###", CultureInfo.InvariantCulture);
                        string pct = (value * 100f).ToString("0.#", CultureInfo.InvariantCulture);
                        string sign = value >= 0f ? "+" : "";
                        detail = $"{raw} (= {sign}{pct}%)";
                        return true;
                    }
                    break;
                case StatLayer.Multiplicative:
                    if (value <= 0f || value > MULT_MAX)
                    {
                        string raw = value.ToString("0.###", CultureInfo.InvariantCulture);
                        detail = $"{raw} (= ×{raw})";
                        return true;
                    }
                    break;
            }
            detail = null;
            return false;
        }

        private static List<BuffData> LoadBuffs(string outputFolder)
        {
            var list = new List<BuffData>();
            string[] guids = string.IsNullOrEmpty(outputFolder)
                ? AssetDatabase.FindAssets($"t:{nameof(BuffData)}")
                : AssetDatabase.FindAssets($"t:{nameof(BuffData)}", new[] { outputFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BuffData data = AssetDatabase.LoadAssetAtPath<BuffData>(path);
                if (data != null) list.Add(data);
            }
            return list;
        }
    }
}
#endif
