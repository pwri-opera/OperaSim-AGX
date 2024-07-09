using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEditor.Build.Content;
using UnityEngine;

namespace PWRISimulator
{
    public class ArmAngleToCylinderLengthConvertor : LinkAngleToCylinderLengthConvertor
    {
        [SerializeField]
        public GameObject boomPin;
        public GameObject cylinderRoot;
        public GameObject cylinderBindPoint;
        public GameObject armPin;
        public GameObject bucketPin;

        private float armPinToCylinderRoot = 2.0f; // [m]
        private float armPinToCylinderBindPoint = 0.8f; // [m]
        private float armLength = 2.0f; // [m]
        private float alpha = 0.3f; // [rad]
        private float beta = 0.1f; // [rad]

        protected override void DoStart()
        {
            Vector3 a = boomPin.transform.position - armPin.transform.position;
            Vector3 b = cylinderRoot.transform.position - armPin.transform.position;
            alpha = Mathf.Deg2Rad * Vector3.Angle(a, b);


            Vector3 c = bucketPin.transform.position - cylinderBindPoint.transform.position;
            Vector3 d = armPin.transform.position - cylinderBindPoint.transform.position;
            beta = Mathf.Deg2Rad * Vector3.Angle(c, d);

            armPinToCylinderRoot = b.magnitude;
            armPinToCylinderBindPoint = d.magnitude;
            armLength = (bucketPin.transform.position - armPin.transform.position).magnitude;
        }

        public override float CalculateCylinderRodTelescoping(float _angle)
        {
            return CalculateCylinderLinkLength(_angle) - cylinderLength - cylinderRodDefaultLength;
        }
        protected override float CalculateCylinderLinkLength(float _angle)
        {
            return Mathf.Sqrt(Mathf.Pow(armPinToCylinderRoot, 2.0f) + Mathf.Pow(armPinToCylinderBindPoint, 2.0f) - 2 * armPinToCylinderRoot * armPinToCylinderBindPoint * Mathf.Cos(_angle - alpha + beta));
        }

        public override float CalculateCylinderRodTelescopingVelocity(float _velocity)
        {
            return armPinToCylinderRoot * armPinToCylinderBindPoint * Mathf.Sin(currentLinkAngle - alpha + beta) * _velocity / CalculateCylinderLinkLength(currentLinkAngle);
        }

        public override float CalculateCylinderRodTelescopingForce(float _force)
        {
            //return _force * armLength / armPinToCylinderBindPoint;

            float l = CalculateCylinderLinkLength(currentLinkAngle);
            float epsilon = Mathf.Acos((Mathf.Pow(l, 2.0f) + Mathf.Pow(armPinToCylinderBindPoint, 2.0f) - Mathf.Pow(armPinToCylinderRoot, 2.0f)) / (2 * l * armPinToCylinderBindPoint));
            float delta = Mathf.PI * 0.5f - epsilon;

            return _force * armLength / (armPinToCylinderBindPoint * Mathf.Cos(delta));
        }

        protected override void FixedUpdate()
        {
            currentLinkAngle = joint.GetCurrentAngle() + Mathf.Deg2Rad * (jointInitialAngle);
        }
    }
}
