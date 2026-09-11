using NUnit.Framework;
using UnityEngine;

namespace PWRISimulator.Tests.Editor
{
    public class DumpTerrainFactoryTests
    {
        [Test]
        public void SampleHeightFromMainTerrain_ReturnsHeightAtWorldPosition()
        {
            // Create a fake main terrain heightmap: 5x5, flat at 0.5 normalized height
            float[,] mainHeights = new float[5, 5];
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                    mainHeights[x, y] = 0.5f;

            // Main terrain: 100m x 100m, origin at (0,0), height scale 50m
            Vector3 mainTerrainSize = new Vector3(100, 50, 100);
            Vector3 mainTerrainPos = Vector3.zero;

            // Sample at world (50, 50) — center of terrain
            float h = DumpTerrainFactory.SampleHeightFromMainTerrain(
                mainHeights, 5, mainTerrainSize, mainTerrainPos,
                new Vector2(50f, 50f));
            Assert.AreEqual(0.5f, h, 0.001f, "Center sample should be 0.5");
        }

        [Test]
        public void SampleHeightFromMainTerrain_ClampsOutOfBounds()
        {
            float[,] mainHeights = new float[3, 3];
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    mainHeights[x, y] = 0.3f;

            Vector3 mainTerrainSize = new Vector3(10, 5, 10);
            Vector3 mainTerrainPos = Vector3.zero;

            // Sample outside terrain bounds — should clamp to edge
            float h = DumpTerrainFactory.SampleHeightFromMainTerrain(
                mainHeights, 3, mainTerrainSize, mainTerrainPos,
                new Vector2(-5f, -5f));
            Assert.AreEqual(0.3f, h, 0.001f, "Out-of-bounds sample should clamp to edge value");
        }

        [Test]
        public void BuildDumpHeightmap_ProducesCorrectResolution()
        {
            float[,] mainHeights = new float[5, 5];
            for (int y = 0; y < 5; y++)
                for (int x = 0; x < 5; x++)
                    mainHeights[x, y] = 0.4f;

            Vector3 mainTerrainSize = new Vector3(100, 50, 100);
            Vector3 mainTerrainPos = Vector3.zero;
            Vector3 dumpTerrainWorldMin = new Vector3(40, 0, 40);
            Vector3 dumpTerrainSize = new Vector3(12, 6, 7);
            int dumpResolution = 65;

            float[,] dumpHeights = DumpTerrainFactory.BuildDumpHeightmap(
                mainHeights, 5, mainTerrainSize, mainTerrainPos,
                dumpTerrainWorldMin, dumpTerrainSize, dumpResolution);

            Assert.AreEqual(dumpResolution, dumpHeights.GetLength(0));
            Assert.AreEqual(dumpResolution, dumpHeights.GetLength(1));
            // All samples should be 0.4f since main terrain is flat
            Assert.AreEqual(0.4f, dumpHeights[0, 0], 0.01f);
            Assert.AreEqual(0.4f, dumpHeights[dumpResolution - 1, dumpResolution - 1], 0.01f);
        }
    }
}
