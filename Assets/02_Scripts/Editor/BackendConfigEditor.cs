#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// BackendConfig 인스펙터 확장. 읽기 전용 경고만 표시 — 자동 수정 금지.
    /// </summary>
    [CustomEditor(typeof(BackendConfig))]
    public class BackendConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var config = target as BackendConfig;
            if (config == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("검증", EditorStyles.boldLabel);

            if (string.IsNullOrEmpty(config.AppVersion))
                EditorGUILayout.HelpBox("AppVersion 이 비어있습니다.", MessageType.Warning);

            if (config.DefaultTimeoutSec <= 0)
                EditorGUILayout.HelpBox("DefaultTimeoutSec 은 1 이상이어야 합니다.", MessageType.Warning);

            ValidateLeaderboardBindings(config.LeaderboardUuids);
            ValidateFlexibleTableBindings(config.FlexibleTableNames);
        }

        private static void ValidateLeaderboardBindings(IReadOnlyList<LeaderboardBinding> bindings)
        {
            if (bindings == null || bindings.Count == 0) return;

            var seenKeys = new HashSet<LeaderboardKey>();
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b == null) continue;

                if (!seenKeys.Add(b.Key))
                    EditorGUILayout.HelpBox($"LeaderboardBinding[{i}]: Key '{b.Key}' 중복.", MessageType.Warning);

                if (string.IsNullOrEmpty(b.Uuid))
                    EditorGUILayout.HelpBox($"LeaderboardBinding[{i}] ({b.Key}): Uuid 가 비어있습니다.", MessageType.Warning);
            }
        }

        private static void ValidateFlexibleTableBindings(IReadOnlyList<FlexibleTableBinding> bindings)
        {
            if (bindings == null || bindings.Count == 0) return;

            var seenKeys = new HashSet<FlexibleTableKey>();
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b == null) continue;

                if (!seenKeys.Add(b.Key))
                    EditorGUILayout.HelpBox($"FlexibleTableBinding[{i}]: Key '{b.Key}' 중복.", MessageType.Warning);

                if (string.IsNullOrEmpty(b.TableName))
                    EditorGUILayout.HelpBox($"FlexibleTableBinding[{i}] ({b.Key}): TableName 이 비어있습니다.", MessageType.Warning);
            }
        }
    }
}
#endif
