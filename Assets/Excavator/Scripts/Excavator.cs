using System;
using UnityEngine;
using UnityEngine.InputSystem;
using AGXUnity;
using AGXUnity.Model;
using AGXUnity.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// 簡単な共通のインタフェースで油圧ショベルのコンストレイントを制御したり、実測したりすることできるようにするクラス。
    /// </summary>
    [RequireComponent(typeof(ExcavationData))]
    public class Excavator : ConstructionMachine
    {
        [Header("Constraint Controls")]
        public ConstraintControl leftSprocket;
        public ConstraintControl rightSprocket;
        public ConstraintControl swing;
        public ConstraintControl boomTilt;
        public ConstraintControl armTilt;
        public ConstraintControl bucketTilt;

        public ExcavationData excavationData { get; private set; }

        protected override bool Initialize()
        {
            bool success = base.Initialize();

            excavationData = GetComponentInChildren<ExcavationData>();

            RegisterConstraintControl(leftSprocket);
            RegisterConstraintControl(rightSprocket);
            RegisterConstraintControl(swing);
            RegisterConstraintControl(boomTilt);
            RegisterConstraintControl(armTilt);
            RegisterConstraintControl(bucketTilt);
            
            return success;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Excavator))]
    public class ExcavatorEditor : ConstructionMachineEditor
    {
        public override void OnInspectorGUI()
        {
            // ConstructionMachineEditorのGUIを表示
            base.OnInspectorGUI();
        }
    }
#endif
}