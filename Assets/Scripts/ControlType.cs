using System;
using UnityEngine;

namespace PWRISimulator
{
    /// <summary>
    /// 制御の指令値の種類。
    /// </summary>
    [Serializable]
    public enum ControlType
    {
        /// <summary>
        /// 位置／角度でConstraintを制御する
        /// </summary>
        Position,

        /// <summary>
        /// 速度／加速度でConstraintを制御する
        /// </summary>
        Speed,

        /// <summary>
        /// 力／トルクでConstraintを制御する
        /// </summary>
        Force
    };
}
