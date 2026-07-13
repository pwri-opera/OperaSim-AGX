using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PWRISimulator.ROS
{
    public class DumpTruckPlayerInputHandler : MonoBehaviour
    {
        public DumpTruckJoint dumpTrack;
        public bool printDebugMessages = false;
        private DumpTruckInput dumpTruckInput;

        private void Start()
        {
            dumpTruckInput = GetComponent<DumpTruckInput>();

            if (dumpTrack != null)
            {
                SetConstraintVelocityControl(dumpTrack.leftSprocket);
                SetConstraintVelocityControl(dumpTrack.rightSprocket);
                // SetConstraintVelocityControl(dumpTrack.containerTilt);
                SetConstraintVelocityControl(dumpTrack.dump_joint);
            }
        }
        
        public void OnLeftSprocket(InputValue value)
        {
            SetConstraintControlValue(dumpTrack?.leftSprocket, value.Get<float>());
        }

        public void OnRightSprocket(InputValue value)
        {
            SetConstraintControlValue(dumpTrack?.rightSprocket, value.Get<float>());
        }
        
        public void OnContainerTilt(InputValue value)
        {
            double speed = value.Get<float>();
            if (dumpTruckInput != null)
                dumpTruckInput.ApplyManualDumpSpeed(speed);
            else
                SetConstraintControlValue(dumpTrack?.dump_joint, speed);
        }
        
        protected void SetConstraintControlValue(ConstraintControl constraintControl, double value)
        {
            if (constraintControl != null)
            {
                if (printDebugMessages)
                    Debug.Log($"{constraintControl.constraint.name} input value = {value}");

                constraintControl.controlValue = value;
            }
        }
        
        protected void SetConstraintVelocityControl(ConstraintControl constraintControl)
        {
            if (constraintControl != null)
                constraintControl.controlType = ControlType.Speed;
        }
    }
}
