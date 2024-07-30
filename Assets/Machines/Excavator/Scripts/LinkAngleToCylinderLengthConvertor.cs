using agxCollide;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using System.Runtime.CompilerServices;

namespace PWRISimulator
{
    public abstract class LinkAngleToCylinderLengthConvertor : MonoBehaviour
    {
        [SerializeField]
        public Constraint joint;
        public float jointInitialAngle = 0.0f; // [degree]
        public float jointMaxAngle = 100.0f;
        public float jointMinAngle = 0.0f; 
        public float cylinderLength = 0.5f; // [rad]

        [HideInInspector]
        public float currentLinkAngle = 0.0f; // [rad]

        protected float cylinderRodDefaultLength = 0.0f;
        protected abstract void DoStart();
        protected abstract float CalculateCylinderLinkLength(float _angle);

        public abstract float CalculateCylinderRodTelescoping(float _angle);
        public abstract float CalculateCylinderRodTelescopingVelocity(float _velocity);
        public abstract float CalculateCylinderRodTelescopingForce(float _force);
        // Start is called before the first frame update

        void Start()
        {
            DoStart();            
            cylinderRodDefaultLength = CalculateCylinderLinkLength(Mathf.Deg2Rad * jointInitialAngle) - cylinderLength;
        }

        public void OnInit()
        {
            agx.RangeReal rangeReal = new agx.RangeReal(Mathf.Deg2Rad * (jointMinAngle - jointInitialAngle), Mathf.Deg2Rad * (jointMaxAngle - jointInitialAngle));
            agx.RangeController rangeController = agx.RangeController.safeCast(joint.GetController<RangeController>()?.Native);
            rangeController.setRange(rangeReal);
            rangeController.setEnable(true);
        }
        protected virtual void FixedUpdate()
        {
            currentLinkAngle = joint.GetCurrentAngle() + Mathf.Deg2Rad * (jointInitialAngle);
        }
    }
}
