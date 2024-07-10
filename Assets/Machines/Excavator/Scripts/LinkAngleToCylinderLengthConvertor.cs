using agxCollide;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

namespace PWRISimulator
{
    public abstract class LinkAngleToCylinderLengthConvertor : MonoBehaviour
    {
        [SerializeField]
        public Constraint joint;
        public float jointInitialAngle = 0.0f; // [degree]
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
        protected virtual void FixedUpdate()
        {
            currentLinkAngle = joint.GetCurrentAngle() + Mathf.Deg2Rad * (jointInitialAngle);
        }
    }
}
