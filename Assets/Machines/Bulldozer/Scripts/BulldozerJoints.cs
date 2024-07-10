using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using AGXUnity.Model;
using AGXUnity.Utils;
using agx;
using PWRISimulator.ROS;

namespace PWRISimulator
{
    public class BulldozerJoints : ConstructionMachine
    {
        [Header("Constraint Controls")]
        public ConstraintControl leftSprocket;
        public ConstraintControl rightSprocket;
        public ConstraintControl bladeLift;
        public ConstraintControl bladeTilt;
        public ConstraintControl bladeAngleRight;
        public ConstraintControl bladeAngleLeft;

        private BulldozerInput input;
        protected override bool Initialize()
        {
            bool success = base.Initialize();

            //excavationData = GetComponentInChildren<ExcavationData>();

            RegisterConstraintControl(leftSprocket);
            RegisterConstraintControl(rightSprocket);
            RegisterConstraintControl(bladeLift);
            RegisterConstraintControl(bladeTilt);
            RegisterConstraintControl(bladeAngleRight);
            RegisterConstraintControl(bladeAngleLeft);


            leftSprocket.constraint.Native.setEnableComputeForces(true);
            rightSprocket.constraint.Native.setEnableComputeForces(true);
            bladeLift.constraint.Native.setEnableComputeForces(true);
            bladeTilt.constraint.Native.setEnableComputeForces(true);
            bladeAngleRight.constraint.Native.setEnableComputeForces(true);
            bladeAngleLeft.constraint.Native.setEnableComputeForces(true);

            input = gameObject.GetComponent<BulldozerInput>();

            return success;
        }

        protected override void RequestCommands()
        {
            //base.RequestCommands();
            input.SetCommands();
        }
    }
}
