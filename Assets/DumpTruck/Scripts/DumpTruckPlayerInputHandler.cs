using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PWRISimulator
{
    public class DumpTruckPlayerInputHandler : MonoBehaviour
    {
        public DumpTruck dumpTrack;
        public bool printDebugMessages = false;

        private void Start()
        {
            if (dumpTrack != null)
            {
                SetConstraintVelocityControl(dumpTrack.leftSprocket);
                SetConstraintVelocityControl(dumpTrack.rightSprocket);
                SetConstraintVelocityControl(dumpTrack.containerTilt);
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
            SetConstraintControlValue(dumpTrack?.containerTilt, value.Get<float>());
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
