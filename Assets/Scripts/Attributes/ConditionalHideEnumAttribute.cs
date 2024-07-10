using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class |
     AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHide_EnumAttribute : PropertyAttribute
    {
        // Field name of enum
        public string conditionalSourceField = "";

        // Index of enum want to be enabled
        public int enumInex = 0;

        public ConditionalHide_EnumAttribute(string conditionalSourceField, int enumInex = 0)
        {
            this.conditionalSourceField = conditionalSourceField;
            this.enumInex = enumInex;
        }
    }
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ConditionalHide_EnumAttribute))]
    public class ConditionalHidePropertyDrawer_Enum : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalHide_EnumAttribute condHAtt = (ConditionalHide_EnumAttribute)attribute;
            int enabledIndex = GetConditionalHideAttributeResult(condHAtt, property);

            bool wasEnabled = GUI.enabled;
            try
            {
                GUI.enabled = enabledIndex == condHAtt.enumInex;
                if (condHAtt.enumInex == enabledIndex)
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
            finally
            {
                GUI.enabled = wasEnabled;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ConditionalHide_EnumAttribute condHAtt = (ConditionalHide_EnumAttribute)attribute;
            int enabledIndex = GetConditionalHideAttributeResult(condHAtt, property);

            if (condHAtt.enumInex == enabledIndex)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }

        int GetConditionalHideAttributeResult(ConditionalHide_EnumAttribute condHAtt, SerializedProperty property)
        {
            // 有効化／無効化したいプロパティのパス
            string propertyPath = property.propertyPath;
            // 有効化の条件を表すプロパティのパス
            string conditionPath = propertyPath.Replace(property.name, condHAtt.conditionalSourceField);
            // 有効化の条件の値
            SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);

            if (sourcePropertyValue != null)
            {
                return sourcePropertyValue.enumValueIndex;
            }
            else
            {
                Debug.LogWarning("Attempting to use a ConditionalHideAttribute but no matching SourcePropertyValue " +
                                 "found in object: " + condHAtt.conditionalSourceField);
                return 0;
            }
        }
    }
#endif
}
