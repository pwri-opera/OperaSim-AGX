using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ブルドーザの油圧を保持する
    /// </summary>
    public class BulldozerFluid : MonoBehaviour
    {
        [Header("Hydraulic Actuators")]
        public HydraulicActuator leftSprocketControl;
        public HydraulicActuator rightSprocketControl;
        public HydraulicActuator bladeLiftControl;
        public HydraulicActuator leftbladeAngleControl;
        public HydraulicActuator rightbladeAngleControl;
        public HydraulicActuator bladeTiltControl;

        /// <summary>
        /// <param name="FluidPressures">値を更新するクラス.現在は全て同じ計算で更新する想定で配列にしているが、個別の計算が発生するのであれば分けること</param> 
        /// </summary>
        private FluidPressure[] fluidPressures = new FluidPressure[6];

        // アクセス用プロパティ
        public FluidPressure LiftUp { get=>fluidPressures[0]; }
        public FluidPressure LiftDown { get=>fluidPressures[1]; }
        public FluidPressure TiltForward { get=>fluidPressures[2]; }
        public FluidPressure TiltBackward { get=>fluidPressures[3]; }
        public FluidPressure AngleRight { get=>fluidPressures[4]; }
        public FluidPressure AngleLeft { get=>fluidPressures[5]; }
    }
}
