using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PWRISimulator.ROS
{
    public class DumpTruckJoint : ConstructionMachine
    {
        [Header("Constraint Controls")]
        public ConstraintControl leftSprocket;
        public ConstraintControl rightSprocket;
        public ConstraintControl rotate_joint;
        public ConstraintControl dump_joint;

        private DumpTruckInput input;

        protected override bool Initialize()
        {
            bool success = base.Initialize();

            RegisterConstraintControl(leftSprocket);
            RegisterConstraintControl(rightSprocket);
            RegisterConstraintControl(rotate_joint);
            RegisterConstraintControl(dump_joint);

            leftSprocket.constraint.Native.setEnableComputeForces(true);
            rightSprocket.constraint.Native.setEnableComputeForces(true);
            //rotate_joint.constraint.Native.setEnableComputeForces(true);
            dump_joint.constraint.Native.setEnableComputeForces(true);

            input = gameObject.GetComponent<DumpTruckInput>();

            return success;
        }
        protected override void RequestCommands()
        {
            //base.RequestCommands();
            input.SetCommands();
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(DumpTruckJoint))]
    public class DumpTruckEditor : ConstructionMachineEditor
    {
        public override void OnInspectorGUI()
        {
            // ConstructionMachineEditor��GUI��\��
            base.OnInspectorGUI();
        }
    }
#endif
}
