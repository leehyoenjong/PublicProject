using UnityEditor;
using UnityEngine;

namespace PublicFramework.EditorTools
{
    [CustomPropertyDrawer(typeof(LocalizationKeyAttribute))]
    public class LocalizationKeyDrawer : PropertyDrawer
    {
        private const float IconWidth = 24f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[LocalizationKey] string 전용");
                return;
            }

            Rect fieldRect = new Rect(position.x, position.y, position.width - IconWidth - 2f, position.height);
            Rect iconRect = new Rect(position.xMax - IconWidth, position.y, IconWidth, position.height);

            EditorGUI.PropertyField(fieldRect, property, label);
            EditorGUI.LabelField(iconRect, "🌐");
        }
    }
}
