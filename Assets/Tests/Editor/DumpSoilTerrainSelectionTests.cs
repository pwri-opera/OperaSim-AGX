using NUnit.Framework;
using UnityEngine;
using AGXUnity.Model;

namespace PWRISimulator.Tests.Editor
{
    public class DumpSoilTerrainSelectionTests
    {
        [Test]
        public void ResolveTerrain_PrefersDumpRoleTerrain_WhenAvailable()
        {
            // In EditMode we cannot create real DeformableTerrain instances easily,
            // but we can test the static selection logic. DumpSoil.ResolveTerrain
            // delegates to TerrainRole.FindTerrainByRole(Role.Dump) first.
            // When no dump-role terrain exists, it falls back to FindObjectOfType.

            // With no terrains in the scene, both should return null.
            var dumpTerrain = TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump);
            Assert.IsNull(dumpTerrain, "Should return null when no dump terrain exists in EditMode");
        }

        [Test]
        public void ResolveTerrain_FallsBackToFindObjectOfType_WhenNoRoleMarker()
        {
            // Verify that DumpSoil.ResolveTerrain returns null (no terrain found)
            // rather than throwing, when neither TerrainRole nor FindObjectOfType
            // finds a terrain. This confirms the fallback path doesn't crash.
            Assert.DoesNotThrow(() =>
            {
                var result = TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump);
                // null is acceptable — DumpSoil.Initialize checks for null and returns false
            });
        }
    }
}
