using UnityEngine;
using UnityEditor;

namespace SmartSettings
    {
    [CustomPropertyDrawer(typeof(Settings), true)]
    public class SettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true
            );

            float y = position.y + EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                SerializedProperty prop = property.Copy();
                SerializedProperty end = prop.GetEndProperty();

                prop.NextVisible(true);

                EditorGUI.BeginChangeCheck(); 

                while (!SerializedProperty.EqualContents(prop, end))
                {
                    float height = EditorGUI.GetPropertyHeight(prop, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, true);
                    y += height + EditorGUIUtility.standardVerticalSpacing;
                    prop.NextVisible(false);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    
                    object targetObject = fieldInfo.GetValue(property.serializedObject.targetObject);
                    if (targetObject is Settings settings) settings.InvokeChange();
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded)
                return height;

            SerializedProperty prop = property.Copy();
            SerializedProperty end = prop.GetEndProperty();

            prop.NextVisible(true);
            while (!SerializedProperty.EqualContents(prop, end))
            {
                height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
                prop.NextVisible(false);
            }

            return height;
        }
    }
}