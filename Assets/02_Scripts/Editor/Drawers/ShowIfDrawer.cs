using UnityEditor;
using UnityEngine;

namespace PublicFramework.EditorTools
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ShouldShow(property)) return;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!ShouldShow(property)) return 0f;
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool ShouldShow(SerializedProperty property)
        {
            ShowIfAttribute attr = (ShowIfAttribute)attribute;
            string path = property.propertyPath;
            int lastDot = path.LastIndexOf('.');
            string parentPath = lastDot >= 0 ? path.Substring(0, lastDot) : string.Empty;
            string conditionPath = string.IsNullOrEmpty(parentPath)
                ? attr.ConditionFieldName
                : parentPath + "." + attr.ConditionFieldName;

            SerializedProperty cond = property.serializedObject.FindProperty(conditionPath);
            if (cond == null || cond.propertyType != SerializedPropertyType.Boolean) return true;

            bool value = cond.boolValue;
            return attr.Invert ? !value : value;
        }
    }
}
