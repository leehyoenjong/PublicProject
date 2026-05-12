#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using PublicFramework;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 시트 임포터 검증 룰 (γ).
    /// 한 SkillData 안에 여러 ApplyBuff 액션이 있을 때 참조하는 BuffData.Duration 이 서로 다르면
    /// 같은 스킬 시전이 만든 버프 아이콘들의 만료 타이밍이 시각적으로 어긋난다.
    /// 임포트 후 자동 호출되어 위반을 콘솔 에러 + EditorUtility.DisplayDialog 로 알린다.
    /// </summary>
    public static class SkillBuffDurationValidator
    {
        private const string LOG_PREFIX = "[버프듀레이션검증]";
        private const string DIALOG_TITLE = "Skill ↔ Buff Duration 검증";
        private const string DIALOG_OK = "확인";
        private const string MENU_PATH = "PublicFramework/Tools/Validate Skill ↔ Buff Duration";

        [MenuItem(MENU_PATH)]
        private static void ValidateFromMenu()
        {
            Validate(null);
        }

        /// <summary>
        /// 대상 폴더 내 모든 SkillData 를 검사하고 위반 케이스를 발견하면 한 번에 모아 리포트한다.
        /// outputFolder 가 비어있으면 프로젝트 전역에서 t:SkillData 를 검색한다.
        /// </summary>
        public static void Validate(string outputFolder)
        {
            Dictionary<string, BuffData> buffById = BuildBuffIndex();
            List<SkillData> skills = LoadSkills(outputFolder);

            if (skills.Count == 0)
            {
                Debug.Log($"{LOG_PREFIX} 검사 대상 SkillData 가 없음 — 건너뜀");
                return;
            }

            var violations = new List<Violation>();
            for (int i = 0; i < skills.Count; i++)
            {
                Violation v = AnalyzeSkill(skills[i], buffById);
                if (v != null) violations.Add(v);
            }

            if (violations.Count == 0)
            {
                Debug.Log($"{LOG_PREFIX} 위반 없음 (SkillData {skills.Count}건 점검)");
                return;
            }

            string body = BuildReport(violations);
            Debug.LogError($"{LOG_PREFIX} 위반 {violations.Count}건 감지\n{body}");
            EditorUtility.DisplayDialog(DIALOG_TITLE, body, DIALOG_OK);
        }

        private static Dictionary<string, BuffData> BuildBuffIndex()
        {
            var map = new Dictionary<string, BuffData>(System.StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BuffData)}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                BuffData data = AssetDatabase.LoadAssetAtPath<BuffData>(path);
                if (data == null) continue;
                if (string.IsNullOrEmpty(data.BuffId)) continue;
                map[data.BuffId] = data;
            }
            return map;
        }

        private static List<SkillData> LoadSkills(string outputFolder)
        {
            var list = new List<SkillData>();
            string[] guids = string.IsNullOrEmpty(outputFolder)
                ? AssetDatabase.FindAssets($"t:{nameof(SkillData)}")
                : AssetDatabase.FindAssets($"t:{nameof(SkillData)}", new[] { outputFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
                if (data != null) list.Add(data);
            }
            return list;
        }

        private static Violation AnalyzeSkill(SkillData skill, Dictionary<string, BuffData> buffById)
        {
            if (skill == null || skill.Actions == null) return null;

            List<BuffRef> refs = null;
            for (int i = 0; i < skill.Actions.Count; i++)
            {
                SkillActionEntry entry = skill.Actions[i];
                if (entry == null) continue;
                if (entry.ActionType != SkillActionType.ApplyBuff) continue;

                string buffId = entry.Param1;
                if (string.IsNullOrEmpty(buffId)) continue;
                if (!buffById.TryGetValue(buffId, out BuffData data)) continue;

                if (refs == null) refs = new List<BuffRef>();
                refs.Add(new BuffRef { BuffId = buffId, Duration = data.Duration });
            }

            if (refs == null || refs.Count < 2) return null;

            float first = refs[0].Duration;
            bool mismatch = false;
            for (int i = 1; i < refs.Count; i++)
            {
                if (!Mathf.Approximately(refs[i].Duration, first))
                {
                    mismatch = true;
                    break;
                }
            }
            if (!mismatch) return null;

            return new Violation { SkillId = skill.SkillId, Refs = refs };
        }

        private static string BuildReport(List<Violation> violations)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < violations.Count; i++)
            {
                Violation v = violations[i];
                sb.Append("• ").Append(v.SkillId).Append(": ");
                for (int j = 0; j < v.Refs.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    BuffRef r = v.Refs[j];
                    sb.Append(r.BuffId).Append('(').Append(r.Duration.ToString("0.##")).Append("s)");
                }
                if (i < violations.Count - 1) sb.Append('\n');
            }
            return sb.ToString();
        }

        private class Violation
        {
            public string SkillId;
            public List<BuffRef> Refs;
        }

        private struct BuffRef
        {
            public string BuffId;
            public float Duration;
        }
    }
}
#endif
