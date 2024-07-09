using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

namespace PWRISimulator.ROS
{
    /// <summary>
    /// 油圧クラス
    /// <param name="mainFluidPressure">メイン油圧(Pa)</param>
    /// <param name="pilotFluidPressure">パイロット油圧(Pa)</param>
    /// 派生クラスで油圧値をセット出来る
    /// </summary>
    public struct FluidPressure{
        public double MainFluidPressure;
        public double PilotFluidPressure;
    }
}
