using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PublicFramework.EditorTools
{
    /// <summary>
    /// Play 버튼을 어떤 씬에서 누르든 항상 00_Loading 씬부터 시작하도록 강제.
    /// EditorPref 로 토글 가능. 메뉴: Tools/PublicFramework/PlayMode 시작 씬 강제.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartSceneSetup
    {
        private const string PREF_KEY = "PublicFramework.ForceLoadingSceneOnPlay";
        private const string MENU_PATH = "Tools/PublicFramework/PlayMode 시작 씬 강제";
        private const string LOADING_SCENE_PATH = "Assets/00_Scenes/00_Loading.unity";

        static PlayModeStartSceneSetup()
        {
            EditorApplication.delayCall += Apply;
        }

        private static bool IsEnabled => EditorPrefs.GetBool(PREF_KEY, true);

        private static void Apply()
        {
            if (!IsEnabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LOADING_SCENE_PATH);
            if (scene == null)
            {
                Debug.LogWarning($"[PlayModeStartSceneSetup] '{LOADING_SCENE_PATH}' 못 찾음 — 비활성화");
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            EditorSceneManager.playModeStartScene = scene;
        }

        [MenuItem(MENU_PATH)]
        private static void Toggle()
        {
            bool next = !IsEnabled;
            EditorPrefs.SetBool(PREF_KEY, next);
            Apply();
            Debug.Log($"[PlayModeStartSceneSetup] {(next ? "활성화 — 항상 00_Loading 부터 시작" : "비활성화 — 현재 씬에서 그대로 시작")}");
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MENU_PATH, IsEnabled);
            return true;
        }
    }
}
