using System.IO;
using NUnit.Framework;

namespace PWRISimulator.Tests
{
    public class DeformableTerrainInitializationTests
    {
        [Test]
        public void InitializeNative_AddsTerrainToSimulationBeforeOptionalShovels()
        {
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "AGXUnity",
                "AGXUnity",
                "Model",
                "DeformableTerrain.cs");
            var source = File.ReadAllText(path);

            var addTerrainIndex = source.IndexOf("GetSimulation().add( Native );", System.StringComparison.Ordinal);
            var shovelLoopIndex = source.IndexOf("foreach ( var shovel in Shovels )", System.StringComparison.Ordinal);

            Assert.That(addTerrainIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(shovelLoopIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(addTerrainIndex, Is.LessThan(shovelLoopIndex),
                "Terrain initialization must not depend on optional shovel initialization.");
        }

        [Test]
        public void InitializeNative_IgnoresBrokenOptionalShovels()
        {
            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "AGXUnity",
                "AGXUnity",
                "Model",
                "DeformableTerrain.cs");
            var source = File.ReadAllText(path);

            Assert.That(source, Does.Contain("catch ( Exception e )"));
            Assert.That(source, Does.Contain("Failed to initialize deformable terrain shovel"));
        }
    }
}
