using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

namespace PWRISimulator
{
    public class BladeAngleToCylinderLengthConvertor : LinkAngleToCylinderLengthConvertor
        {
        [SerializeField]
        public GameObject centerOfRotation;
        public GameObject cylinderBindPointLeft;
        public GameObject cylinderBindPointRight;
        public GameObject cylinderRootLeft;
        public GameObject cylinderRootRight;

        private float rotationRadius = 1.0f;
        private float cylinderRootToCenterOfRotation = 1.0f;
        // Start is called before the first frame update
        protected override void DoStart()
        {
            rotationRadius = (cylinderBindPointLeft.transform.position - cylinderBindPointRight.transform.position).magnitude * 0.5f;
            cylinderRootToCenterOfRotation = ((cylinderRootLeft.transform.position + cylinderRootRight.transform.position) * 0.5f - centerOfRotation.transform.position).magnitude;
        }
        public override float CalculateCylinderRodTelescoping(float _angle)
        {
            return rotationRadius * Mathf.Sin(_angle);
        }

        protected override float CalculateCylinderLinkLength(float _angle)
        {
            return cylinderRootToCenterOfRotation + rotationRadius * Mathf.Sin(_angle);
        }

        public override float CalculateCylinderRodTelescopingVelocity(float _velocity)
        {
            return rotationRadius * Mathf.Cos(currentLinkAngle) * _velocity;
        }

        public override float CalculateCylinderRodTelescopingForce(float _force)
        {
            return _force * 0.5f;
        }

        protected override void FixedUpdate()
        {
            Vector3 bladeDirection = cylinderBindPointLeft.transform.position - cylinderBindPointRight.transform.position;
            Vector3 center = (cylinderRootLeft.transform.position + cylinderRootRight.transform.position) * 0.5f - centerOfRotation.transform.position;
            currentLinkAngle = Mathf.Deg2Rad * (-Vector3.Angle(bladeDirection, center) + 90.0f);
        }
    }
}
