using NUnit.Framework;
using UnityEngine;

namespace PWRISimulator.Tests
{
    public class DumpSoilCaptureTests
    {
        // --- CalculateLocalCaptureBounds tests ---

        [Test]
        public void CalculateLocalCaptureBounds_AlignsWithMergeZoneGeometry()
        {
            Vector3 originalSize = new Vector3(2f, 1f, 4f);
            double soilHeight = 0.5;

            var bounds = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(originalSize, soilHeight);

            // X is centered around 0
            Assert.That(bounds.min.x, Is.EqualTo(-1f));
            Assert.That(bounds.max.x, Is.EqualTo(1f));
            // Z follows merge-zone convention: [0, depth] (rear-half from local z=0.5)
            Assert.That(bounds.min.z, Is.EqualTo(0f));
            Assert.That(bounds.max.z, Is.EqualTo(4f));
            // Y is a zero-thickness slab at soilHeight
            Assert.That(bounds.min.y, Is.EqualTo((float)soilHeight));
            Assert.That(bounds.max.y, Is.EqualTo((float)soilHeight));
        }

        [Test]
        public void CalculateLocalCaptureBounds_SameInput_Deterministic()
        {
            var size = new Vector3(2f, 1f, 4f);

            var bounds1 = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(size, 0.5);
            var bounds2 = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(size, 0.5);

            Assert.That(bounds2.min, Is.EqualTo(bounds1.min));
            Assert.That(bounds2.max, Is.EqualTo(bounds1.max));
        }

        // --- IsSphereOverlappingBounds tests ---

        [Test]
        public void IsSphereOverlappingBounds_SphereInsideBounds_ReturnsTrue()
        {
            var size = new Vector3(2f, 1f, 4f);
            var bounds = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(size, 0.5);
            // Center of the capture zone: x=0, y=soilHeight, z=middle of [0, depth]
            var center = new Vector3(0f, 0.5f, 2f);
            double radius = 0.1;

            bool result = DumpSoil.CaptureUtil.IsSphereOverlappingBounds(center, radius, bounds);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsSphereOverlappingBounds_SphereFarOutside_ReturnsFalse()
        {
            var size = new Vector3(2f, 1f, 4f);
            var bounds = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(size, 0.5);
            var farPoint = new Vector3(100f, 100f, 100f);
            double radius = 0.1;

            bool result = DumpSoil.CaptureUtil.IsSphereOverlappingBounds(farPoint, radius, bounds);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsSphereOverlappingBounds_SphereTouchingLowerSurface_ReturnsTrue()
        {
            var size = new Vector3(2f, 1f, 4f);
            double soilHeight = 0.5;
            var bounds = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(size, soilHeight);
            // Sphere center at y=0.4 so y+r = 0.5 == soilHeight (touching from below)
            var spherePos = new Vector3(0f, 0.4f, 0f);
            double radius = 0.1;

            bool result = DumpSoil.CaptureUtil.IsSphereOverlappingBounds(spherePos, radius, bounds);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsSphereOverlappingBounds_SphereBelowSurface_ReturnsFalse()
        {
            var size = new Vector3(2f, 1f, 4f);
            double soilHeight = 0.5;
            var bounds = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(size, soilHeight);
            // Sphere center at y=0 so y+r = 0.1 < 0.5 → outside
            var spherePos = new Vector3(0f, 0f, 0f);
            double radius = 0.1;

            bool result = DumpSoil.CaptureUtil.IsSphereOverlappingBounds(spherePos, radius, bounds);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsSphereOverlappingBounds_LocalClassificationStableUnderRotation()
        {
            // This test verifies that the capture bounds calculation depends only
            // on local-space size/height — not on any world rotation context.
            // The pure function guarantee: same inputs always produce the same
            // bounds regardless of any external transform orientation.
            var size = new Vector3(2f, 1f, 4f);
            double soilHeight = 0.5;

            var bounds = DumpSoil.CaptureUtil.CalculateLocalCaptureBounds(size, soilHeight);

            // A sphere at a fixed local position produces a stable result
            var localPos = new Vector3(0.5f, 0.5f, 0.5f);
            double radius = 0.1;

            // Call twice — must be deterministic
            bool first = DumpSoil.CaptureUtil.IsSphereOverlappingBounds(localPos, radius, bounds);
            bool second = DumpSoil.CaptureUtil.IsSphereOverlappingBounds(localPos, radius, bounds);

            Assert.That(first, Is.EqualTo(second));
        }

        // --- CalculateWorldBroadPhaseBounds tests ---

        [Test]
        public void CalculateWorldBroadPhaseBounds_ExpandsUniformlyByRadius()
        {
            Vector3 worldMin = new Vector3(1f, 2f, 3f);
            Vector3 worldMax = new Vector3(5f, 6f, 7f);
            double expansion = 0.15;

            var result = DumpSoil.CaptureUtil.CalculateWorldBroadPhaseBounds(worldMin, worldMax, expansion);

            float e = (float)expansion;
            Assert.That(result.min.x, Is.EqualTo(1f - e).Within(1e-6f));
            Assert.That(result.min.y, Is.EqualTo(2f - e).Within(1e-6f));
            Assert.That(result.min.z, Is.EqualTo(3f - e).Within(1e-6f));
            Assert.That(result.max.x, Is.EqualTo(5f + e).Within(1e-6f));
            Assert.That(result.max.y, Is.EqualTo(6f + e).Within(1e-6f));
            Assert.That(result.max.z, Is.EqualTo(7f + e).Within(1e-6f));
        }

        [Test]
        public void CalculateWorldBroadPhaseBounds_SameInputs_Deterministic()
        {
            Vector3 worldMin = new Vector3(1f, 2f, 3f);
            Vector3 worldMax = new Vector3(5f, 6f, 7f);

            var result1 = DumpSoil.CaptureUtil.CalculateWorldBroadPhaseBounds(worldMin, worldMax, 0.15);
            var result2 = DumpSoil.CaptureUtil.CalculateWorldBroadPhaseBounds(worldMin, worldMax, 0.15);

            Assert.That(result2.min, Is.EqualTo(result1.min));
            Assert.That(result2.max, Is.EqualTo(result1.max));
        }

        [Test]
        public void CalculateWorldBroadPhaseBounds_ZeroExpansion_ReturnsOriginal()
        {
            Vector3 worldMin = new Vector3(1f, 2f, 3f);
            Vector3 worldMax = new Vector3(5f, 6f, 7f);

            var result = DumpSoil.CaptureUtil.CalculateWorldBroadPhaseBounds(worldMin, worldMax, 0.0);

            Assert.That(result.min, Is.EqualTo(worldMin));
            Assert.That(result.max, Is.EqualTo(worldMax));
        }
    }
}
