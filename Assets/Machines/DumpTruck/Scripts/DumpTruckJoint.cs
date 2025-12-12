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
        public bool activateDeadTime = true;
        public double trackDeadTime = 0.0;    
        
        [Header("Constraint Controls")]

        public bool rotateJointEnabled = false;

        public ConstraintControl leftSprocket;
        public ConstraintControl rightSprocket;
        public ConstraintControl dump_joint;

        [ConditionalHide(nameof(rotateJointEnabled), hideCompletely = true)]
        public ConstraintControl rotate_joint;

        private DumpTruckInput input;

        protected override bool Initialize()
        {
            bool success = base.Initialize();

            try
            {
                RegisterConstraintControl(leftSprocket);
                RegisterConstraintControl(rightSprocket);
                RegisterConstraintControl(rotate_joint);
                RegisterConstraintControl(dump_joint);

            leftSprocket.constraint.Native.setEnableComputeForces(true);
            rightSprocket.constraint.Native.setEnableComputeForces(true);
            //rotate_joint.constraint.Native.setEnableComputeForces(true);
            dump_joint.constraint.Native.setEnableComputeForces(true);

            }
            catch { }

            input = gameObject.GetComponent<DumpTruckInput>();


            return success;
        }
        protected override void RequestCommands()
        {
            //base.RequestCommands();
            if (input != null)
            {
                input.SetCommands();
            }
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
