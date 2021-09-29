using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class |
     AttributeTargets.Struct, Inherited = true)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool wasEnabled = GUI.enabled;
            try
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label);
            }
            finally
            {
                GUI.enabled = wasEnabled;
            }
        }
    }
#endif
}
