using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace PWRISimulator
{
    public class BladeTiltToCylinderLengthConvertor : LinkAngleToCylinderLengthConvertor
    {
        [SerializeField]
        public GameObject centerOfRotation;
        public GameObject cylinderRoot;
        public GameObject cylinderBindPoint;

        public GameObject angleCylinderBindPointLeft;
        public GameObject angleCylinderBindPointRight;

        private float centerOfRotToCylinderRoot = 0.5f;
        private float rotationRadius = 1.0f;

        // Start is called before the first frame update
        protected override void DoStart()
        {
            centerOfRotToCylinderRoot = (centerOfRotation.transform.position - cylinderRoot.transform.position).magnitude;
            rotationRadius = (centerOfRotation.transform.position - cylinderBindPoint.transform.position).magnitude;
            cylinderRodDefaultLength = (cylinderBindPoint.transform.position - cylinderRoot.transform.position).magnitude - cylinderLength;
        }
        public override float CalculateCylinderRodTelescoping(float _angle)
        {
            return CalculateCylinderLinkLength(_angle) - cylinderLength - cylinderRodDefaultLength;
        }

        protected override float CalculateCylinderLinkLength(float _angle)
        {
            return -centerOfRotToCylinderRoot * Mathf.Sin(_angle) + Mathf.Sqrt(Mathf.Pow(centerOfRotToCylinderRoot, 2.0f) * Mathf.Pow(Mathf.Sin(_angle), 2.0f) - Mathf.Pow(centerOfRotToCylinderRoot, 2.0f) + Mathf.Pow(rotationRadius, 2.0f));
        }

        public override float CalculateCylinderRodTelescopingVelocity(float _velocity)
        {
            float term1 = -centerOfRotToCylinderRoot * Mathf.Cos(currentLinkAngle);
            //float term2 = (Mathf.Pow(centerOfRotToCylinderRoot, 2.0f) * Mathf.Cos(currentLinkAngle) * Mathf.Sin(currentLinkAngle)) / Mathf.Sqrt(Mathf.Pow(centerOfRotToCylinderRoot, 2.0f) - Mathf.Pow(centerOfRotToCylinderRoot * Mathf.Cos(currentLinkAngle),2.0f));
            float term2 = (Mathf.Pow(centerOfRotToCylinderRoot, 2.0f) * Mathf.Cos(currentLinkAngle) * Mathf.Sin(currentLinkAngle)) / 
                Mathf.Sqrt(Mathf.Pow(centerOfRotToCylinderRoot * Mathf.Sin(currentLinkAngle), 2.0f) - (Mathf.Pow(centerOfRotToCylinderRoot, 2.0f) - Mathf.Pow(rotationRadius, 2.0f)) );

            return (term1 + term2) * _velocity;
        }

        public override float CalculateCylinderRodTelescopingForce(float _force)
        {
            //return _force * rotationRadius / CalculateCylinderLinkLength(currentLinkAngle);

            float len = CalculateCylinderLinkLength(currentLinkAngle);
            float alpha = Mathf.Acos( (Mathf.Pow(rotationRadius, 2.0f) + Mathf.Pow(len, 2.0f) - Mathf.Pow(centerOfRotToCylinderRoot, 2.0f)) / (2 * rotationRadius * len));
            float beta = Mathf.PI * 0.5f - alpha;

            return _force / Mathf.Cos(beta);
        }
        protected override void FixedUpdate()
        {
            Vector3 a = angleCylinderBindPointRight.transform.position - angleCylinderBindPointLeft.transform.position;
            Vector3 b = new Vector3(angleCylinderBindPointRight.transform.position.x, angleCylinderBindPointLeft.transform.position.y, angleCylinderBindPointRight.transform.position.z) - angleCylinderBindPointLeft.transform.position;

            currentLinkAngle = Mathf.Deg2Rad * Vector3.Angle(a, b);
        }
    }
}
