using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AGXUnity;
using AGXUnity.Model;

namespace PWRISimulator.Tests.PlayMode
{
    public class DumpTerrainIsolationPlayModeTests
    {
        [UnityTest]
        public IEnumerator DumpTerrain_ExistsAndHasCorrectRole()
        {
            yield return null; // Wait one frame for Awake/Start

            var dumpTerrain = TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump);
            Assert.IsNotNull(dumpTerrain, "Dump terrain should be created by DumpTerrainFactory");
            Assert.AreEqual("Terrain_Dump", dumpTerrain.gameObject.name);
        }

        [UnityTest]
        public IEnumerator DumpParticles_DoNotModifyExcavationTerrainHeights()
        {
            // 1. Find terrains
            var dumpTerrain = TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump);
            var excTerrain = TerrainRole.FindTerrainByRole(TerrainRole.Role.Excavation);
            if (excTerrain == null)
                excTerrain = FindObjectOfType<DeformableTerrain>();

            Assert.IsNotNull(dumpTerrain, "Dump terrain should exist after DumpTerrainFactory runs");
            Assert.IsNotNull(excTerrain, "Excavation terrain should exist");

            // 2. Record excavation terrain heights at excavation area
            TerrainData excData = excTerrain.TerrainData;
            int excRes = excData.heightmapResolution;
            // Excavation area center at world (20, 10), terrain at (0, -6, 0), size ~200m
            int excCenterX = Mathf.RoundToInt(20f / excData.size.x * (excRes - 1));
            int excCenterY = Mathf.RoundToInt(10f / excData.size.z * (excRes - 1));
            int patchRadius = 10; // sample a 21x21 patch around excavation center
            int patchSize = patchRadius * 2 + 1;
            float[,] excHeightsBefore = excData.GetHeights(
                excCenterX - patchRadius, excCenterY - patchRadius,
                patchSize, patchSize);

            // 3. Simulate for a few seconds to let any physics settle
            yield return new WaitForSeconds(3.0f);

            // 4. Check excavation terrain heights are unchanged
            float[,] excHeightsAfter = excData.GetHeights(
                excCenterX - patchRadius, excCenterY - patchRadius,
                patchSize, patchSize);

            bool heightsChanged = false;
            for (int y = 0; y < patchSize && !heightsChanged; y++)
            {
                for (int x = 0; x < patchSize && !heightsChanged; x++)
                {
                    if (Mathf.Abs(excHeightsBefore[x, y] - excHeightsAfter[x, y]) > 0.0001f)
                    {
                        heightsChanged = true;
                    }
                }
            }

            Assert.IsFalse(heightsChanged,
                "Excavation terrain heights changed during simulation. " +
                "Dump particles may be affecting the excavation terrain.");
        }
    }
}
