// Copyright 2021 VMC Motion Technologies Co., Ltd.

namespace VMT.Extensions
{
    /// <summary>
    /// 満足なToStringメソッドを提供していない型にカスタムのToStringメソッドを提供するスタティッククラス。
    /// </summary>
    public static partial class ToStringExtensions
    {
        /// <summary>
        /// agx.SPDMatrix3x3のデータをstringに出力する。
        /// </summary>
        public static string ToString(this agx.SPDMatrix3x3 matrix, string rowSeparator = " ")
        {
            return string.Format("[{0:0.0000}, {1:0.0000}, {2:0.0000}]{3}" +
                                 "[{4:0.0000}, {5:0.0000}, {6:0.0000}]{7}" +
                                 "[{8:0.0000}, {9:0.0000}, {10:0.0000}]",
                                 matrix.at(0, 0), matrix.at(0, 1), matrix.at(0, 2), rowSeparator,
                                 matrix.at(1, 0), matrix.at(1, 1), matrix.at(1, 2), rowSeparator,
                                 matrix.at(2, 0), matrix.at(2, 1), matrix.at(2, 2));
        }

        public static string ToStr(this agx.Vec3 v)
        {
            return string.Format("[{0:0.0000}, {1:0.0000}, {2:0.0000}]", v.x, v.y, v.z);
        }
    }
}
