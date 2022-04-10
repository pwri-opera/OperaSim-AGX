using UnityEngine;
using AGXUnity;
using AGXUnity.Utils;

namespace PWRISimulator
{
    public static class AgxUtil
    {
        public static void ToAgxMinMax(Bounds bounds, out agx.Vec3 min, out agx.Vec3 max)
        {
            min = bounds.min.ToHandedVec3();
            max = bounds.max.ToHandedVec3();
            double minX = min.x;
            min.x = max.x;
            max.x = minX;
        }
        
        public static agx.AffineMatrix4x4 GetRelativeAgxTransform(Transform parentWorld, Transform childWorld)
        {
            agx.AffineMatrix4x4 parentTransform = new agx.AffineMatrix4x4(
                parentWorld.rotation.ToHandedQuat(), parentWorld.position.ToHandedVec3());

            agx.AffineMatrix4x4 childTransform = new agx.AffineMatrix4x4(
                childWorld.rotation.ToHandedQuat(), childWorld.position.ToHandedVec3());

            return childTransform * parentTransform.inverse();
        }
    }
}
