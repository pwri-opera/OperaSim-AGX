using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using agxPowerLine;
using agx;

namespace PWRISimulator
{
    public class BucketAngleToCylinderLengthConvertor : LinkAngleToCylinderLengthConvertor
    {
        [SerializeField]
        public GameObject bucketPin; // Center of rotation
        public GameObject bucketEdge; 
        public GameObject cylinderRoot;
        public GameObject cylinderBindPoint;
        public GameObject armCylinderBindPoint;
        public GameObject armPin;
        [Tooltip("End point of the bucket side of the H link")]
        public GameObject HLinkRoot; // 
        [Tooltip("End point of the arm side of the I link")]
        public GameObject ILinkRoot; //

        private float bucketPinToHLinkRoot = 0.1f; // [m]       
        private float bucketPinToILinkRoot = 0.1f; // [m]    
        private float HLinkLength = 0.1f; // [m]             
        private float ILinkLength = 0.1f; // [m]             
        private float ILinkRootToCylinderRoot = 0.4f; // [m] 
        private float bucketPinTobucketEdge = 0.75f; // [m] 

        private float alpha = 0.5f; // [rad]
        private float beta = 2.0f; // [rad]
        private float kai = 0.0f; // [rad] 


        protected override void DoStart()
        {
            Vector3 a = bucketEdge.transform.position - bucketPin.transform.position;
            Vector3 b = ILinkRoot.transform.position - bucketPin.transform.position;
            Vector3 c = HLinkRoot.transform.position - bucketPin.transform.position;
            beta = Mathf.Deg2Rad * Vector3.Angle(a, c);
            bucketPinToILinkRoot = b.magnitude;
            bucketPinToHLinkRoot = c.magnitude;

            HLinkLength = (HLinkRoot.transform.position - cylinderBindPoint.transform.position).magnitude;
            ILinkLength = (ILinkRoot.transform.position - cylinderBindPoint.transform.position).magnitude;
            ILinkRootToCylinderRoot = (cylinderRoot.transform.position - ILinkRoot.transform.position).magnitude;
            bucketPinTobucketEdge = (bucketEdge.transform.position - bucketPin.transform.position).magnitude;

            // alpha = Mathf.Deg2Rad * Vector3.Angle(cylinderRoot.transform.position - ILinkRoot.transform.position, armCylinderBindPoint.transform.position - ILinkRoot.transform.position);
            alpha = Mathf.PI - Mathf.Deg2Rad * Vector3.Angle(cylinderRoot.transform.position - ILinkRoot.transform.position, bucketPin.transform.position - ILinkRoot.transform.position);
            kai = Mathf.Deg2Rad * Vector3.Angle( ILinkRoot.transform.position - bucketPin.transform.position, armPin.transform.position - bucketPin.transform.position);

            // ---- Debug.Log 追記 ----
            // Debug.Log($"[DoStart] alpha={alpha:F6}");
            // Debug.Log($"[DoStart] beta={beta:F6}");
            // Debug.Log($"[DoStart] kai={kai:F6}");
            // Debug.Log($"[DoStart] bucketPinToILinkRoot={bucketPinToILinkRoot:F6}, bucketPinToHLinkRoot={bucketPinToHLinkRoot:F6}");
            // Debug.Log($"[DoStart] bucketPinToILinkRoot={bucketPinToILinkRoot:F6}, bucketPinToHLinkRoot={bucketPinToHLinkRoot:F6}");
            // Debug.Log($"[DoStart] HLinkLength={HLinkLength:F6}, ILinkLength={ILinkLength:F6}, ILinkRootToCylinderRoot={ILinkRootToCylinderRoot:F6}, bucketPinTobucketEdge={bucketPinTobucketEdge:F6}");

        }
        public override float CalculateCylinderRodTelescoping(float _angle)
        {
            float linkLen = CalculateCylinderLinkLength(_angle);
            float telescoping = linkLen - cylinderLength - cylinderRodDefaultLength;
            // ---- Debug.Log 追記 ----
            // Debug.Log($"[CalculateCylinderRodTelescoping] _angle(rad)={_angle:F6}");
            // Debug.Log($"[CalculateCylinderRodTelescoping] linkLen={linkLen:F6}, cylinderLength={cylinderLength:F6}, cylinderRodDefaultLength={cylinderRodDefaultLength:F6}");
            // Debug.Log($"[CalculateCylinderRodTelescoping] telescoping={telescoping:F6}");

            return CalculateCylinderLinkLength(_angle) - cylinderLength - cylinderRodDefaultLength;
        }

        protected override float CalculateCylinderLinkLength(float _angle)
        {
            // jointMaxAngle <->jointMinAngle の間で各リンク角度を制限
            if (Mathf.Deg2Rad * jointMaxAngle < _angle)
                _angle = Mathf.Deg2Rad * jointMaxAngle;
                
            else if (Mathf.Deg2Rad * jointMinAngle > _angle)
                _angle = Mathf.Deg2Rad * jointMinAngle;
                
            float gamma = beta - (_angle - kai); 
            float delta = Mathf.PI - gamma;
            float diagonal = Mathf.Sqrt( Mathf.Pow(bucketPinToILinkRoot, 2.0f) + Mathf.Pow(bucketPinToHLinkRoot, 2.0f) - 2 * bucketPinToILinkRoot * bucketPinToHLinkRoot * Mathf.Cos(delta));
            float eta = Mathf.Acos((Mathf.Pow(ILinkLength, 2.0f) + Mathf.Pow(diagonal, 2.0f) - Mathf.Pow(HLinkLength, 2.0f)) / (2 * ILinkLength * diagonal));
            float omega = Mathf.Acos( (Mathf.Pow(bucketPinToILinkRoot, 2.0f) + Mathf.Pow(diagonal, 2.0f) - Mathf.Pow(bucketPinToHLinkRoot, 2.0f)) / (2 * bucketPinToILinkRoot * diagonal) );
            float lamda = delta < Mathf.PI? eta + omega: eta - omega;
            float psi = Mathf.PI - alpha - lamda;
            
            // ---- Debug.Log 追記 ----
            // Debug.Log($"[CalculateCylinderLinkLength] gameObject={name}");
            // Debug.Log($"[CalculateCylinderLinkLength] _angle(rad)={_angle:F6}, beta(rad)={beta:F6}, alpha(rad)={alpha:F6}");
            // Debug.Log($"[CalculateCylinderLinkLength] gamma={gamma:F6}, delta={delta:F6}, diagonal={diagonal:F6}");
            // Debug.Log($"[CalculateCylinderLinkLength] eta={eta:F6}, omega={omega:F6}, lamda={lamda:F6}, psi={psi:F6}");
            // Debug.Log($"[CalculateCylinderLinkLength] ILinkRootToCylinderRoot={ILinkRootToCylinderRoot:F6}, ILinkLength={ILinkLength:F6}");

            return Mathf.Sqrt( Mathf.Pow(ILinkRootToCylinderRoot, 2.0f) + Mathf.Pow(ILinkLength, 2.0f) - 2 * ILinkRootToCylinderRoot * ILinkLength * Mathf.Cos(psi)); 
        }

        public override float CalculateCylinderRodTelescopingVelocity(float _velocity)
        {
            // パターン1　バケットピンを中心とした回転運動に近似
            return bucketPinToHLinkRoot * _velocity;

            //// パターン2　移送法
            //// 瞬間中心を求める
            //Vector3 localBucketPin = armCylinderBindPoint.transform.InverseTransformPoint(bucketPin.transform.position);
            //Vector3 localHLinkRoot = armCylinderBindPoint.transform.InverseTransformPoint(HLinkRoot.transform.position);
            //Vector3 localcylinderBindPoint = armCylinderBindPoint.transform.InverseTransformPoint(cylinderBindPoint.transform.position);
            //Vector3 localILinkRoot = armCylinderBindPoint.transform.InverseTransformPoint(ILinkRoot.transform.position);

            //Vector2 localBucketPin_2d = new Vector2(localBucketPin.z, localBucketPin.y);
            //Vector2 localHLinkRoot_2d = new Vector2(localHLinkRoot.z, localHLinkRoot.y);
            //Vector2 localcylinderBindPoint_2d = new Vector2(localcylinderBindPoint.z, localcylinderBindPoint.y);
            //Vector2 localILinkRoot_2d = new Vector2(localILinkRoot.z, localILinkRoot.y);

            //// Step1.直線の方程式を求める y = ax + b
            //float a1 = (localcylinderBindPoint_2d.y - localILinkRoot_2d.y) / (localcylinderBindPoint_2d.x - localILinkRoot_2d.x);
            //float b1 = a1 * localcylinderBindPoint_2d.x - localcylinderBindPoint_2d.y;

            //float a2 = (localHLinkRoot_2d.y - localBucketPin_2d.y) / (localHLinkRoot_2d.x - localBucketPin_2d.x);
            //float b2 = a2 * localHLinkRoot_2d.x - localHLinkRoot_2d.y;

            //// Step2.直線の交点を求める. 交点 = 瞬間中心
            //float x = (b2 -b1) / (a1 - a2);
            //float y = x * a1 + b1;
            //Vector2 instantRotCentre = new Vector2(x, y);

            //// Step3.速度を求めたい点と瞬間中心との距離を計算
            //float dist1 = Vector2.Distance(instantRotCentre, localcylinderBindPoint_2d);
            //float dist2 = Vector2.Distance(instantRotCentre, localHLinkRoot_2d);

            //// Step4.バケットの回転角速度より、Hリンクの接続点の速度を算出
            //float hLinkRootVelocity = _velocity * bucketPinToHLinkRoot;

            //// Step5.シリンダ接続点の速度は瞬間中心からの距離の比となる
            //return hLinkRootVelocity * (dist1 / dist2);
        }

        public override float CalculateCylinderRodTelescopingForce(float _force)
        {
            float alpha_2 = Mathf.Deg2Rad * Vector3.Angle(HLinkRoot.transform.position - bucketPin.transform.position, cylinderBindPoint.transform.position - bucketPin.transform.position);
            float alpha_3 = Mathf.Deg2Rad * Vector3.Angle(bucketPin.transform.position - cylinderBindPoint.transform.position, ILinkRoot.transform.position - cylinderBindPoint.transform.position);
            float alpha_4 = Mathf.Deg2Rad * Vector3.Angle(bucketPin.transform.position - cylinderBindPoint.transform.position, HLinkRoot.transform.position - cylinderBindPoint.transform.position);

            float gamma = Mathf.Deg2Rad * Vector3.Angle(ILinkRoot.transform.position - cylinderBindPoint.transform.position, cylinderRoot.transform.position - cylinderBindPoint.transform.position);
            float required_torque = ((ILinkLength * Mathf.Sin(alpha_3 + alpha_4)) / (bucketPinToHLinkRoot * Mathf.Sin(alpha_2 + alpha_4))) * _force * bucketPinTobucketEdge;
            return (required_torque / ILinkLength) / Mathf.Sin(gamma);
        }


    }
}
