using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using System;
using PWRISimulator.ROS;
using AGXUnity.Utils;
using System.Runtime.InteropServices;
using agxControl;

namespace PWRISimulator
{
    /// <summary>
    /// 油圧アクチュエータクラス
    /// </summary>
    [Serializable]
    public class HydraulicActuator
    {
        /// <summary>
        /// 油圧アクチュエータの駆動タイプ
        /// </summary>
        public enum ActuatorType
        {
            /// <summary>
            /// 直線移動タイプ。油圧シリンダ
            /// </summary>
            Linear,
            /// <summary>
            /// 回転運動タイプ。油圧モータ
            /// </summary>
            Rotation
        }
        /// <summary>
        /// アクチュエータの駆動方向
        /// シリンダ駆動の場合、伸びる方向と縮む方向で計算方法が異なるため使用
        /// </summary>
        private enum DirectionType
        {
            Forward,
            Backward
        }

        public Constraint constraint = null;
        public ActuatorType actuatorType = ActuatorType.Linear;

        /// <summary>
        /// シリンダの直径[mm]
        /// </summary>
        [ConditionalHide_Enum("actuatorType", (int)ActuatorType.Linear)]
        public double cylinderDiameter = 0.1f;

        /// <summary>
        /// シリンダロッドの直径[mm]
        /// </summary>
        [ConditionalHide_Enum("actuatorType", (int)ActuatorType.Linear)]
        public double rodDiameter = 0.05f;
        
        /// <summary>
        /// シリンダの数
        /// ブームは2本のシリンダが同期して駆動するため
        /// numOfCylinders = 2
        /// </summary>
        [ConditionalHide_Enum("actuatorType", (int)ActuatorType.Linear)]
        public int numOfCylinders = 1;

        /// <summary>
        /// 押しのけ容積[cm^2/rev]
        /// </summary>
        [ConditionalHide_Enum("actuatorType", (int)ActuatorType.Rotation)]
        public double displacementsVolume = 70.0f;
        
        /// <summary>
        /// モータのトルク効率
        /// </summary>
        [ConditionalHide_Enum("actuatorType", (int)ActuatorType.Rotation)]
        public double torqueEfficient = 0.8f;

        /// <summary>
        /// メイン圧力とパイロット圧力の比率
        /// パイロット圧力 = scaleFactor * メイン圧力
        /// として計算
        /// </summary>
        [Tooltip("Ratio of Main Pressure and Pilot Pressure ")]
        public double scaleFactor = 0.2;

        //private double lastPosition = 0.0;
        private DirectionType direction = DirectionType.Forward;

        private Vector3 lastForce = Vector3.zero;
        private Vector3 lastTorque = Vector3.zero;

        private void EstimateDirection()
        {
            agx.Vec3 force = new agx.Vec3(0, 0, 0);
            agx.Vec3 torque = new agx.Vec3(0, 0, 0);
            if ((constraint != null) && (constraint.Native.getLastLocalForce(0, ref force, ref torque)))
            {
                switch (actuatorType)
                {
                    case ActuatorType.Linear:
                        lastForce = force.ToHandedVector3();
                        direction = lastForce.z < 0 ? DirectionType.Backward : DirectionType.Forward;
                        break;
                    case ActuatorType.Rotation:
                        lastTorque = torque.ToHandedVector3();

                        break;
                }
            }
        }

        public FluidPressure GetUpperPressure()
        {
            EstimateDirection();
            FluidPressure fp = new FluidPressure();

            if (direction == DirectionType.Backward)
            {
                fp.MainFluidPressure = 0.0f;
                fp.PilotFluidPressure = 0.0f;
                return fp;
            }
            agx.Vec3 force = new agx.Vec3(0, 0, 0);
            agx.Vec3 torque = new agx.Vec3(0, 0, 0);

            if ((constraint != null) && (constraint.Native.getLastForce(0, ref force, ref torque))) {
                switch (actuatorType)
                {
                    case ActuatorType.Linear:
                        fp.MainFluidPressure = Mathf.Abs(lastForce.z) * 4 / (System.Math.Pow(cylinderDiameter, 2) * System.Math.PI) / numOfCylinders;
                        fp.PilotFluidPressure = fp.MainFluidPressure * scaleFactor;
                        break;

                    case ActuatorType.Rotation:
                        double pressureDiff = 2 * System.Math.PI * torque.ToHandedVector3().magnitude / (displacementsVolume * torqueEfficient);
                        fp.MainFluidPressure = pressureDiff;
                        fp.PilotFluidPressure = fp.MainFluidPressure * scaleFactor;
                        break;
                }
            }

            return fp; 
        }

        public FluidPressure GetLowerPressure()
        {
            EstimateDirection();
            FluidPressure fp =  new FluidPressure();

            if (direction == DirectionType.Forward)
            {
                fp.MainFluidPressure = 0.0f;
                fp.PilotFluidPressure = 0.0f;
                return fp;
            }

            agx.Vec3 force = new agx.Vec3(0, 0, 0);
            agx.Vec3 torque = new agx.Vec3(0, 0, 0);

            if ((constraint != null) && (constraint.Native.getLastForce(0, ref force, ref torque)))
            {
                switch (actuatorType)
                {
                    case ActuatorType.Linear: 
                        fp.MainFluidPressure = Mathf.Abs(lastForce.z) * 4 / ((System.Math.Pow(cylinderDiameter, 2) - System.Math.Pow(rodDiameter, 2)) * System.Math.PI) / numOfCylinders;
                        fp.PilotFluidPressure = fp.MainFluidPressure * scaleFactor;
                        break;

                    case ActuatorType.Rotation:
                        double pressureDiff = 2 * System.Math.PI * torque.ToHandedVector3().magnitude / (displacementsVolume * torqueEfficient);
                        fp.MainFluidPressure = pressureDiff;
                        fp.PilotFluidPressure = fp.MainFluidPressure * scaleFactor;
                        break;
                }
            }
            return fp;
        }
    }
}
