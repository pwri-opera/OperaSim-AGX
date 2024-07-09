using System;
using UnityEngine;
using UnityEngine.InputSystem;
using AGXUnity;
using AGXUnity.Model;
using AGXUnity.Utils;
using PWRISimulator.ROS;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator
{
    /// <summary>
    /// 簡単な共通のインタフェースで油圧ショベルのコンストレイントを制御したり、実測したりすることできるようにするクラス。
    /// </summary>
    [RequireComponent(typeof(ExcavationData))]
    public class ExcavatorJoints : ConstructionMachine
    {
        [Header("Constraint Controls")]
        public ActuatorComponent leftSprocket;
        public ActuatorComponent rightSprocket;
        public ActuatorComponent swing;
        public ActuatorComponent boomTilt;
        public ActuatorComponent armTilt;
        public ActuatorComponent bucketTilt;

        public ExcavationData excavationData { get; private set; }

        private ExcavatorInput input;
        protected override bool Initialize()
        {
            bool success = base.Initialize();

            excavationData = GetComponentInChildren<ExcavationData>();

            RegisterConstraintControl(leftSprocket.actuator);
            RegisterConstraintControl(rightSprocket.actuator);
            RegisterConstraintControl(swing.actuator);
            RegisterConstraintControl(boomTilt.actuator);
            RegisterConstraintControl(armTilt.actuator);
            RegisterConstraintControl(bucketTilt.actuator);

            // Constraintから力を取得出来るようにする
            leftSprocket.actuator.constraint.Native.setEnableComputeForces(true);
            rightSprocket.actuator.constraint.Native.setEnableComputeForces(true);
            swing.actuator.constraint.Native.setEnableComputeForces(true);
            boomTilt.actuator.constraint.Native.setEnableComputeForces(true);
            armTilt.actuator.constraint.Native.setEnableComputeForces(true);
            bucketTilt.actuator.constraint.Native.setEnableComputeForces(true);

            input = gameObject.GetComponent<ExcavatorInput>();

            return success;
        }

        protected override void RequestCommands()
        {
            //base.RequestCommands();
            input.SetCommands();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ExcavatorJoints))]
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