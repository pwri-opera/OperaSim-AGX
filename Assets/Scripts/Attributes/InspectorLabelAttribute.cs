using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// Inspectorに表示されるプロパティ名をオーバライドするAttribute。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class |
        AttributeTargets.Struct, Inherited = true)]
    public class InspectorLabelAttribute : PropertyAttribute
    {
        public string label = "";
        
        public InspectorLabelAttribute(string label)
        {
            this.label = label;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InspectorLabelAttribute))]
    public class InspectorLabelPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = new GUIContent((attribute as InspectorLabelAttribute).label);
            EditorGUI.PropertyField(position, property, label);
        }
    }
#endif
}
