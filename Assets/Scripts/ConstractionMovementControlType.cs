using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator.ROS
{
    /// <summary>
    ///  建設機械モデルの走行操作方法
    ///  いずれかの方法で前進、後退、旋回する
    ///  <param name="ActuatorCommand">
    ///  関節を操作する。操作入力値は速度、位置、力の3種類があり、どれを使うかControlTypeで指定する</param>
    ///  <param name="TwistCommand">具体的な速度、角速度を指定する</param>
    ///  <param name="VolumeCommand">forwardVolume, turnVolumeをそれぞれ-1.0~1.0で指定して操作する</param>
    /// </summary>
    public enum ConstractionMovementControlType
    {
        ActuatorCommand,
        TwistCommand,
        VolumeCommand
    };
}
