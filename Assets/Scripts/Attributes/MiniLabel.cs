using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    public class MiniLabel : PropertyAttribute
    {
        public string text = "";

        public MiniLabel(string text)
        {
            this.text = text;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MiniLabel))]
    public class MiniLabelDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            return base.GetHeight();
        }

        public override void OnGUI(Rect position)
        {
            EditorGUI.LabelField(position, (attribute as MiniLabel).text, EditorStyles.centeredGreyMiniLabel);
        }
    }
#endif
}
