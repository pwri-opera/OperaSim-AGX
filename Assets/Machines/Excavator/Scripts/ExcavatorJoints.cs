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
    // public class TrackComponent
    // {
    //     [Min(0)]
    //     public double trackDeadTime = 0.0;        

    //     [Header("Sprockets")]
    //     public ActuatorComponent leftSprocket;
    //     public ActuatorComponent rightSprocket;

    //     public void applySetting()
    //     {
    //         if (leftSprocket == null || rightSprocket == null) return; // Component が指定されていない場合は処理をスキップ

    //         leftSprocket.actuator.DeadTime = trackDeadTime;
    //         rightSprocket.actuator.DeadTime = trackDeadTime;
    //     }
    // }

    /// <summary>
    /// 簡単な共通のインタフェースで油圧ショベルのコンストレイントを制御したり、実測したりすることできるようにするクラス。
    /// </summary>
    [RequireComponent(typeof(ExcavationData))]
    public class ExcavatorJoints : ConstructionMachine
    {
        /// <summary>
        /// むだ時間の再現 有効:True / 無効:False
        /// </summary>
        public bool activateDeadTime = true;
        public double trackDeadTime = 0.0;    

        [Header("Constraint Controls")]

        // public TrackComponent trackModule;   
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

            // leftSprocket.actuator.deadTime = trackDeadTime;
            // rightSprocket.actuator.deadTime = trackDeadTime;
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

            boomTilt.convertor.OnInit();
            armTilt.convertor.OnInit();
            bucketTilt.convertor.OnInit();

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