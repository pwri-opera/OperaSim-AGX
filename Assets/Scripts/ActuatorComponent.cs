using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using System;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 関節とアクチュエータのセット
    /// </summary>
    [Serializable]
    public class ActuatorComponent// : MonoBehaviour
    {
        // 関節
        [SerializeField] public Constraint joint;
        // コンバータ(関節への動作指令をアクチュエータの出力に変換
        [SerializeField] public LinkAngleToCylinderLengthConvertor convertor;
        // アクチュエータ(直動アクチュエータなど)
        [SerializeField] public ConstraintControl actuator;
        // 油圧
        [SerializeField] public HydraulicActuator hydraulicActuator;

        public double JointCurrentPosition
        {
            get 
            {
                double tmpPosition = joint == null ? 0.0 : joint.GetCurrentAngle();
                tmpPosition += convertor == null ? 0.0 : convertor.jointInitialAngle * Mathf.Deg2Rad;
                return tmpPosition; 
            }
        }

        public double JointCurrentSpeed
        {
            get
            {
                return joint == null ? 0.0 : joint.GetCurrentSpeed();
            }
        }

        public double JointCurrentForce
        {
            get
            {
                return joint == null ? 0.0 : joint.Native.getCurrentForce(1);
            }
        }
    }
}
