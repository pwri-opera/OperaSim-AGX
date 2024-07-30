using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// ダンプトラックの油圧を保持する
    /// 現時点では出力は行っていない
    /// </summary>
    public class DumpTruckFluid : MonoBehaviour
    {
        [Header("Hydraulic Actuators")]
        public HydraulicActuator leftSprocketControl;
        public HydraulicActuator rightSprocketControl;
        public HydraulicActuator vesselControl;

        /// <summary>
        /// <param name="FluidPressures">値を更新するクラス.現在は全て同じ計算で更新する想定で配列にしているが、個別の計算が発生するのであれば分けること</param> 
        /// </summary>
        private FluidPressure[] fluidPressures;
    }
}
