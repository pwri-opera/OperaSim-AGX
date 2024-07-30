using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator
{
    public class BladeLiftToCylinderLengthConvertor : LinkAngleToCylinderLengthConvertor
    {
        [SerializeField]
        public GameObject centerOfRotation;
        public GameObject cylinderRoot;
        public GameObject cylinderBindPoint;
        public GameObject blade;

        private float cylinderBindPointToCenterOfRotaion = 1.0f;
        private float centerOfRotationToBlade = 1.5f;
        private float centerOfRotationToCylinderRoot = 0.5f; 

        private float alpha;
        private float beta;
        // Start is called before the first frame update
        protected override void DoStart()
        {
            Vector3 a = cylinderBindPoint.transform.position - centerOfRotation.transform.position;
            Vector3 b = blade.transform.position - centerOfRotation.transform.position;
            Vector3 c = cylinderRoot.transform.position - centerOfRotation.transform.position;

            cylinderBindPointToCenterOfRotaion = a.magnitude;
            centerOfRotationToBlade = b.magnitude;
            centerOfRotationToCylinderRoot = c.magnitude;

            // 回転中心から見たシリンダ根元の位置ベクトル
            Vector3 d = centerOfRotation.transform.InverseTransformPoint(cylinderRoot.transform.position);

            alpha = Mathf.Atan2(Mathf.Abs(d.y), Mathf.Abs(d.z));
            beta = Mathf.Deg2Rad * Vector3.Angle(a, b);
        }

        public override float CalculateCylinderRodTelescoping(float _angle)
        {
            return CalculateCylinderLinkLength(_angle) - cylinderLength - cylinderRodDefaultLength;
        }

        protected override float CalculateCylinderLinkLength(float _angle)
        {
            float gamma = Mathf.PI - alpha - beta + _angle;
            return Mathf.Sqrt(Mathf.Pow(cylinderBindPointToCenterOfRotaion, 2.0f) + Mathf.Pow(centerOfRotationToCylinderRoot, 2.0f) - 2 * cylinderBindPointToCenterOfRotaion * centerOfRotationToCylinderRoot * Mathf.Cos(gamma));
        }

        public override float CalculateCylinderRodTelescopingVelocity(float _velocity)
        {
            float gamma = Mathf.PI - alpha - beta + currentLinkAngle;
            return (cylinderBindPointToCenterOfRotaion * centerOfRotationToCylinderRoot * Mathf.Sin(gamma) * _velocity) / CalculateCylinderLinkLength(currentLinkAngle);
        }

        public override float CalculateCylinderRodTelescopingForce(float _force)
        {
            float len = CalculateCylinderLinkLength(currentLinkAngle);
            float delta = Mathf.Acos((Mathf.Pow(cylinderBindPointToCenterOfRotaion, 2.0f) + Mathf.Pow(len, 2.0f) - Mathf.Pow(centerOfRotationToCylinderRoot, 2.0f)) / (2 * cylinderBindPointToCenterOfRotaion * len));
            return _force * cylinderBindPointToCenterOfRotaion / (centerOfRotationToBlade * Mathf.Cos(Mathf.PI * 0.5f - delta));
        }
    }
}
