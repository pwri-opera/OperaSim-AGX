using NUnit.Framework;
using UnityEngine;
using AGXUnity.Model;

namespace PWRISimulator.Tests.Editor
{
    public class TerrainRoleTests
    {
        [Test]
        public void FindTerrainByRole_ReturnsNull_WhenNoTerrainHasRole()
        {
            // FindTerrainByRole scans active DeformableTerrains. In EditMode without
            // a scene loaded, there are none, so it should return null.
            var result = TerrainRole.FindTerrainByRole(TerrainRole.Role.Dump);
            Assert.IsNull(result);
        }

        [Test]
        public void Role_DefaultsToExcavation()
        {
            var go = new GameObject("TestTerrainRole");
            var role = go.AddComponent<TerrainRole>();
            Assert.AreEqual(TerrainRole.Role.Excavation, role.role);
            Object.DestroyImmediate(go);
        }
    }
}
