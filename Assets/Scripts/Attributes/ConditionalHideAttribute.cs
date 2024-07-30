using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// 他のbool型のfieldによって、Inspectorにプロパティを無効化／隠すAttribute。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class |
     AttributeTargets.Struct, Inherited = true)]
    public class ConditionalHideAttribute : PropertyAttribute
    {
        // bool要件のfieldの名前。
        public string conditionalSourceField = "";

        // 無効化する代わり(false)に、隠す(true）かどうかbool。 
        public bool hideCompletely = false;

        // bool要件がtrueなのに、プロパティが編集抱きなくてプロパティを表示するだけ。
        public bool alwaysReadOnly = false;
        
        public ConditionalHideAttribute(string conditionalSourceField, bool hideCompletely = false,
            bool alwaysReadOnly = false)
        {
            this.conditionalSourceField = conditionalSourceField;
            this.hideCompletely = hideCompletely;
            this.alwaysReadOnly = alwaysReadOnly;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
    public class ConditionalHidePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            bool wasEnabled = GUI.enabled;
            try
            {
                GUI.enabled = enabled && !condHAtt.alwaysReadOnly;
                if (!condHAtt.hideCompletely || enabled)
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
            ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
            bool enabled = GetConditionalHideAttributeResult(condHAtt, property);

            if (!condHAtt.hideCompletely || enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }

        bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
        {
            // 有効化／無効化したいプロパティのパス
            string propertyPath = property.propertyPath;
            // 有効化の条件を表すプロパティのパス
            string conditionPath = propertyPath.Replace(property.name, condHAtt.conditionalSourceField);
            // 有効化の条件の値
            SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(conditionPath);

            if (sourcePropertyValue != null)
            {
                return sourcePropertyValue.boolValue;
            }
            else
            {
                Debug.LogWarning("Attempting to use a ConditionalHideAttribute but no matching SourcePropertyValue " +
                                 "found in object: " + condHAtt.conditionalSourceField);
                return true;
            }
        }
    }
#endif
}
