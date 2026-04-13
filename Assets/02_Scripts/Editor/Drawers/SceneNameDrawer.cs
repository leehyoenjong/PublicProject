using UnityEditor;
using UnityEngine;

namespace PublicFramework.EditorTools
{
    [CustomPropertyDrawer(typeof(SceneNameAttribute))]
    public class SceneNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[SceneName] string 필드에만 사용");
                return;
            }

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            string[] names = new string[scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
            {
                names[i] = System.IO.Path.GetFileNameWithoutExtension(scenes[i].path);
            }

            int currentIndex = System.Array.IndexOf(names, property.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, names);
            if (names.Length > 0)
            {
                property.stringValue = names[newIndex];
            }
        }
    }
}
