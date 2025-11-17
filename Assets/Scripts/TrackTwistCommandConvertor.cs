using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using System;
using AGXUnity.Model;
using UnityEngine.InputSystem.LowLevel;
using RosMessageTypes.Geometry;
using agxDriveTrain;
using UnityEditor.Rendering;

// 車体の並進速度を調整するためのモード一覧
public enum ProjectionMode
{
    Radial,         // 原点方向へ等比縮小
    RadialRatio     // 
}

namespace PWRISimulator
{
    public class TrackTwistCommandConvertor : MonoBehaviour
    {
        public GameObject trackLink;
        public GameObject leftTrack;
        public GameObject rightTrack;

        public PIDController speedController;
        public PIDController angularSpeedController;

        [Tooltip("cmd_velコマンドで指定可能な最大速度(m/s)")]
        public double maxLinearVelocity = 3.00;  // unit is m/sec

        [Tooltip("cmd_velコマンドで指定可能な最大角速度(度/s)")]
        public double maxAngularVelocity = Math.PI * 2.0 * 5.0 / 360.0;  // unit is rad/sec

        [Tooltip("VW挙動調整モードを使うか？\n（VW挙動調整モード：cmd_vel (vw入力) が与えられた際，vw組合せ時の出力を制限するモード")]
        public bool EnableVWBehaviorMode = false;

        [ConditionalHide("EnableVWBehaviorMode", true)]
        [Tooltip("車体並進速度v と 車体旋回速度ω　が同時に与えられた際に，出力値を抑えるパラメータ: 値が小さい程，v，w 同時出力時の速度が制限される")]
        public double VWDecelFactor = 1.0;

        [ConditionalHide("EnableVWBehaviorMode", true)]
        [Tooltip("車体並進速度v と 車体旋回速度ω の縮小配分を決める重み（0＝v優先でωを多く削る、1＝ω優先でvを多く削る）")]
        public double VWRatioFactor = 0.9;

        private double leftSprocketRadius = 0.25;
        private double rightSprocketRadius = 0.25;
        private double trackWidth = 2.0;

        private Vector3 lastPosition;
        private Vector3 lastRotation;

        public double sprocketSpeed_L { get; private set; }
        public double sprocketSpeed_R { get; private set; }



        private void Start()
        {

            AGXUnity.Model.Track leftTrackModel = leftTrack.GetComponentInChildren<AGXUnity.Model.Track>();
            AGXUnity.Model.Track rightTrackModel = rightTrack.GetComponentInChildren<AGXUnity.Model.Track>();

            if (leftTrackModel == null || leftTrackModel == null)
            {
                Debug.LogWarning("Track GameObject not Assigned.");
            }
            else
            {
                AGXUnity.Model.TrackWheel left_sp = null;
                AGXUnity.Model.TrackWheel right_sp = null;
                // get sprockets
                for (int i = 0; i < leftTrackModel.Wheels.Length; i++)
                {
                    if (leftTrackModel.Wheels[i].Model == TrackWheelModel.Sprocket)
                    {
                        left_sp = leftTrackModel.Wheels[i];
                        leftSprocketRadius = left_sp.Radius;
                    }
                }

                for (int i = 0; i < rightTrackModel.Wheels.Length; i++)
                {
                    if (rightTrackModel.Wheels[i].Model == TrackWheelModel.Sprocket)
                    {
                        right_sp = rightTrackModel.Wheels[i];
                        rightSprocketRadius = right_sp.Radius;
                    }
                }

                if (left_sp != null && right_sp != null)
                {
                    trackWidth = (left_sp.transform.position - right_sp.transform.position).magnitude;
                }
                else
                {
                    Debug.LogWarning("Could not find sprocket(s).");
                }
            }

            lastPosition = trackLink.transform.position;
            lastRotation = trackLink.transform.rotation.eulerAngles;

        }

        private void FixedUpdate()
        {

        }

        public void SetCommand(Vector3Msg cmd_linear, Vector3Msg cmd_angular)
        {
            // Feedback Control
            //double dt = Time.deltaTime;
            //Vector3 currentRotation = trackLink.transform.rotation.eulerAngles;

            //double currentSpeed = (-trackLink.transform.InverseTransformPoint(lastPosition).z) / dt;
            //double currentRotSpeed = (currentRotation - lastRotation).y * Mathf.Deg2Rad / dt;

            //lastPosition = trackLink.transform.position;
            //lastRotation = currentRotation;

            //double out_speed = speedController.Calculate(cmd_linear.x, currentSpeed, dt);
            //double out_omega = angularSpeedController.Calculate(cmd_angular.z, currentRotSpeed, dt);

            //sprocketSpeed_L = (out_speed - trackWidth * 0.5 * out_omega) / leftSprocketRadius;
            //sprocketSpeed_R = (out_speed + trackWidth * 0.5 * out_omega) / rightSprocketRadius;

            double linear=0, angular=0;

            // 車体の最大設定速度，旋回速度を超えた値を制限
            linear = Math.Min(cmd_linear.x, maxLinearVelocity);
            linear = Math.Max(linear, -maxLinearVelocity);
            angular = Math.Min(cmd_angular.z, maxAngularVelocity);
            angular = Math.Max(angular, -maxAngularVelocity); 

            if (EnableVWBehaviorMode)
                (linear, angular) = CommandLinearAngularVelocityVWBehaviorMode (linear, angular);

            sprocketSpeed_L = (linear - trackWidth * 0.5 * angular) / (leftSprocketRadius + 0.07);
            sprocketSpeed_R = (linear + trackWidth * 0.5 * angular) / (rightSprocketRadius + 0.07);


        }

        public void SetCommand(double cmd_linear, double cmd_angular)
        {
            double linear, angular;

            // 車体の最大設定速度，旋回速度を超えた値を制限
            linear = Math.Min(cmd_linear, maxLinearVelocity);
            linear = Math.Max(cmd_linear, -maxLinearVelocity);
            angular = Math.Min(cmd_angular, maxAngularVelocity);
            angular = Math.Max(cmd_angular, -maxAngularVelocity); 

            if (EnableVWBehaviorMode)
                (linear, angular) = CommandLinearAngularVelocityVWBehaviorMode (linear, angular);

            sprocketSpeed_L = (linear - trackWidth * 0.5 * angular) / (leftSprocketRadius + 0.07);
            sprocketSpeed_R = (linear + trackWidth * 0.5 * angular) / (rightSprocketRadius + 0.07);
        }


        private  (double, double) CommandLinearAngularVelocityVWBehaviorMode(double cmdLinearVel, double cmdAngularVel)
        {
            double p = VWDecelFactor;
            double ratio = VWRatioFactor;

            // 1. 可行域判定
            double g = Math.Pow(
                        Math.Pow(Math.Abs(cmdLinearVel) / maxLinearVelocity, p) +
                        Math.Pow(Math.Abs(cmdAngularVel) / maxAngularVelocity, p),
                        1.0 / p);

            double v_out = cmdLinearVel;
            double w_out = cmdAngularVel;

            ProjectionMode projMode = ProjectionMode.RadialRatio;

            if (g > 1.0)        // ===== 投影が必要 =====
            {
                switch (projMode)
                {
                    // --- 原点に向け等比縮小 (Radial) -----------------
                    case ProjectionMode.Radial:
                        double s = 1.0 / g;
                        v_out *= s;
                        w_out *= s;
                        break;

                    case ProjectionMode.RadialRatio:
                        (v_out, w_out) = ProjectByRatioScale(
                            cmdLinearVel, cmdAngularVel,
                            maxLinearVelocity, maxAngularVelocity,
                            p, ratio);
                        break;
                }
            }
            return (v_out, w_out);
        }

        private static (double v_out, double w_out) ProjectByRatioScale(
            double v_in, double w_in, double v_max, double w_max, double p, double ratio)
        {
            // 入力の正規化
            double V = Math.Abs(v_in) / v_max;
            double W = Math.Abs(w_in) / w_max;

            // 端点は数値的に不安定なので少しだけ離す
            double alpha = Math.Clamp(ratio, 0.0, 1.0);
            const double EPS = 1e-6;
            alpha = Math.Clamp(alpha, EPS, 1.0 - EPS);

            double ap = alpha * p;
            double bp = (1.0 - alpha) * p;

            double Va = Math.Pow(V, p);
            double Wb = Math.Pow(W, p);

            // f(s) = Va*s^ap + Wb*s^bp - 1 = 0 を s∈(0,1] で解く（2分探索）
            Func<double, double> f = s => Va * Math.Pow(s, ap) + Wb * Math.Pow(s, bp) - 1.0;

            double sL = 0.0; // f(sL) < 0
            double sR = 1.0; // f(sR) >= 0
            for (int i = 0; i < 50; i++)
            {
                double sM = 0.5 * (sL + sR);
                double fM = f(sM);
                if (fM < 0.0) sL = sM; else sR = sM;
            }
            double s = 0.5 * (sL + sR);

            double scaleV = Math.Pow(s, alpha);
            double scaleW = Math.Pow(s, 1.0 - alpha);

            double v_out = Math.Sign(v_in) * scaleV * Math.Abs(v_in);
            double w_out = Math.Sign(w_in) * scaleW * Math.Abs(w_in);

            // 浮動誤差の安全クリップ
            v_out = Math.Clamp(v_out, -v_max, v_max);
            w_out = Math.Clamp(w_out, -w_max, w_max);
            return (v_out, w_out);
        }
    }
}
