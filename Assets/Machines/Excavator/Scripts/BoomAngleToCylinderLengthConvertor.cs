//using System.Collections;
//using System.Collections.Generic;
using AGXUnity;
using UnityEngine;


namespace PWRISimulator
{

    /// <summary>
    /// This Class converts target angle or angular velocity of boom link to its cylinder telescoping or telescoping velocity.
    /// </summary>
    public class BoomAngleToCylinderLengthConvertor : LinkAngleToCylinderLengthConvertor
    {

        [SerializeField]
        public GameObject boomPin;
        public GameObject cylinderRoot;
        public GameObject cylinderBindPoint;
        public GameObject armPin;

        private float boomPinToCylinderRoot = 0.2f; // [m]
        private float boomPinToCylinderBindPoint = 0.8f; // [m]
        private float boomPinToArmPin = 2.0f; // [m]
        private float alpha = 0.3f; // [rad]
        private float beta = 0.3f; // [rad]
        // Start is called before the first frame update
        protected override void DoStart()
        {
            Vector3 a = cylinderRoot.transform.position - boomPin.transform.position;
            Vector3 b = cylinderBindPoint.transform.position - boomPin.transform.position;
            Vector3 c = armPin.transform.position - cylinderBindPoint.transform.position;
            Vector3 d = armPin.transform.position - boomPin.transform.position;
            Vector3 e = new Vector3(boomPin.transform.position.x, boomPin.transform.position.y, armPin.transform.position.z) - boomPin.transform.position;

            boomPinToCylinderRoot = a.magnitude;
            boomPinToCylinderBindPoint = b.magnitude;
            boomPinToArmPin = d.magnitude;

            alpha = Vector3.Angle(a, e) * Mathf.Deg2Rad;
            beta = Vector3.Angle(b, d) * Mathf.Deg2Rad;
        }
        public override float CalculateCylinderRodTelescoping(float _angle)
        {
            return CalculateCylinderLinkLength(_angle) - cylinderLength - cylinderRodDefaultLength;
        }

        protected override float CalculateCylinderLinkLength(float _angle)
        {
            // jointMaxAngle <->jointMinAngle の間で各リンク角度を制限
            if (Mathf.Deg2Rad * jointMaxAngle < _angle)
                _angle = Mathf.Deg2Rad * jointMaxAngle;
                
            else if (Mathf.Deg2Rad * jointMinAngle > _angle)
                _angle = Mathf.Deg2Rad * jointMinAngle;
            return Mathf.Sqrt(Mathf.Pow(boomPinToCylinderBindPoint, 2.0f) + Mathf.Pow(boomPinToCylinderRoot, 2.0f) - 2 * boomPinToCylinderBindPoint * boomPinToCylinderRoot * Mathf.Cos(alpha + beta - _angle));
        }

        public override float CalculateCylinderRodTelescopingVelocity(float _velocity)
        {
            return  -boomPinToCylinderBindPoint * boomPinToCylinderRoot * Mathf.Sin(alpha + beta - currentLinkAngle) * _velocity / CalculateCylinderLinkLength(currentLinkAngle);  
        }

        public override float CalculateCylinderRodTelescopingForce(float _force)
        {
            float l = CalculateCylinderLinkLength(currentLinkAngle);
            float gamma = Mathf.Acos((Mathf.Pow(boomPinToCylinderBindPoint, 2.0f) + Mathf.Pow(l, 2.0f) - Mathf.Pow(boomPinToCylinderRoot, 2.0f)) / (2 * boomPinToCylinderBindPoint * l));
            float delta = Mathf.PI * 0.5f - gamma;
            
            return _force * boomPinToArmPin / (boomPinToCylinderBindPoint * Mathf.Cos(delta));
        }
    }
}
