using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PWRISimulator
{
    public static class MathUtil
    {
        public static double Lerp(double A, double B, double t)
        {
            return A * (1.0 - t) + B * t;
        }

        public static double Lerpf(float A, float B, float t)
        {
            return A * (1.0f - t) + B * t;
        }

        public static Bounds TransformBounds(Transform transform, Bounds localBounds)
        {
            var center = transform.TransformPoint(localBounds.center);

            var extents = localBounds.extents;
            var axisX = transform.TransformVector(extents.x, 0, 0);
            var axisY = transform.TransformVector(0, extents.y, 0);
            var axisZ = transform.TransformVector(0, 0, extents.z);

            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }
    }
}
