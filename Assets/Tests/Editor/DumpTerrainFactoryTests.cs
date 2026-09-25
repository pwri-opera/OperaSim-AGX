using NUnit.Framework;
using UnityEngine;

namespace PWRISimulator.Tests.Editor
{
    public class DumpTerrainFactoryTests
    {
        [Test]
        public void SampleHeightFromMainTerrain_InterpolatesAsymmetricHeightsInZxOrder()
        {
            // TerrainData.GetHeights returns rows in Z and columns in X.
            float[,] mainHeights =
            {
                { 0.1f, 0.2f, 0.6f },
                { 0.3f, 0.8f, 0.9f },
                { 0.4f, 0.5f, 1.0f }
            };

            // Grid coordinates (0.25, 0.75), with nonzero origin and unequal X/Z scales.
            float height = DumpTerrainFactory.SampleHeightFromMainTerrain(
                mainHeights, 3, new Vector3(8f, 20f, 12f), new Vector3(10f, 7f, -20f),
                new Vector2(11f, -15.5f));

            Assert.AreEqual(0.35f, height, 0.00001f);
        }

        [Test]
        public void SampleHeightFromMainTerrain_ClampsOutsideBoundsAndInterpolatesAlongEdges()
        {
            float[,] mainHeights =
            {
                { 0.1f, 0.2f, 0.6f },
                { 0.3f, 0.8f, 0.9f },
                { 0.4f, 0.5f, 1.0f }
            };
            Vector3 size = new Vector3(8f, 20f, 12f);
            Vector3 position = new Vector3(10f, 7f, -20f);

            Assert.AreEqual(0.25f, DumpTerrainFactory.SampleHeightFromMainTerrain(
                mainHeights, 3, size, position, new Vector2(-100f, -15.5f)), 0.00001f,
                "Below minimum X, interpolate along the minimum-X edge.");
            Assert.AreEqual(0.425f, DumpTerrainFactory.SampleHeightFromMainTerrain(
                mainHeights, 3, size, position, new Vector2(11f, 100f)), 0.00001f,
                "Above maximum Z, interpolate along the maximum-Z edge.");
            Assert.AreEqual(0.6f, DumpTerrainFactory.SampleHeightFromMainTerrain(
                mainHeights, 3, size, position, new Vector2(100f, -100f)), 0.00001f,
                "Beyond maximum X and minimum Z, use their shared corner.");
        }

        [Test]
        public void BuildDumpHeightmap_PreservesWorldHeightAcrossDifferentBasesAndScales()
        {
            float[,] mainHeights =
            {
                { 0.1f, 0.2f, 0.3f },
                { 0.4f, 0.5f, 0.6f },
                { 0.7f, 0.8f, 0.9f }
            };
            Vector3 mainSize = new Vector3(8f, 20f, 12f);
            Vector3 mainPosition = new Vector3(10f, 7f, -20f);
            Vector3 dumpPosition = new Vector3(12f, -3f, -18.5f);
            Vector3 dumpSize = new Vector3(4f, 40f, 3f);

            float[,] dumpHeights = DumpTerrainFactory.BuildDumpHeightmap(
                mainHeights, 3, mainSize, mainPosition, dumpPosition, dumpSize, 3);

            // Compare rendered world heights, independently of either terrain's normalization.
            Assert.AreEqual(11.5f, dumpPosition.y + dumpHeights[0, 0] * dumpSize.y, 0.0001f);
            Assert.AreEqual(13.5f, dumpPosition.y + dumpHeights[0, 2] * dumpSize.y, 0.0001f,
                "Changing the column must follow world X.");
            Assert.AreEqual(14.5f, dumpPosition.y + dumpHeights[2, 0] * dumpSize.y, 0.0001f,
                "Changing the row must follow world Z.");
        }
    }
}
