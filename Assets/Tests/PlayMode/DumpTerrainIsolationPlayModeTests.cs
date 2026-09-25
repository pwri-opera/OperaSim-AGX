using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AGXUnity;
using AGXUnity.Model;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PWRISimulator.Tests.PlayMode
{
    public class DumpTerrainIsolationPlayModeTests
    {
        private const string SceneName = "GameScene";
        private Type _terrainRoleType;
        private FieldInfo _actionModeField;
        private int _originalActionMode;
        private float _originalTimeScale;
        private float _originalFixedDeltaTime;
        private Scene _scene;
        private Simulation _simulation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _originalTimeScale = Time.timeScale;
            _originalFixedDeltaTime = Time.fixedDeltaTime;
            _terrainRoleType = Type.GetType("PWRISimulator.TerrainRole, Assembly-CSharp");
            Assert.That(_terrainRoleType, Is.Not.Null);
            var globals = Type.GetType("PWRISimulator.GlobalVariables, Assembly-CSharp");
            Assert.That(globals, Is.Not.Null);
            _actionModeField = globals.GetField("ActionMode", BindingFlags.Public | BindingFlags.Static);
            Assert.That(_actionModeField, Is.Not.Null);
            _originalActionMode = (int)_actionModeField.GetValue(null);
            _actionModeField.SetValue(null, -1);

            // Always reload, including when the previous test left GameScene active.
            // Pause on sceneLoaded, before Start/FixedUpdate can advance native physics.
            SceneManager.sceneLoaded += PauseLoadedScene;
            try
            {
                SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= PauseLoadedScene;
            }

            Assert.That(_simulation, Is.Not.Null, "GameScene must contain an AGX simulation.");
            _simulation.AutoSteppingMode = Simulation.AutoSteppingModes.Disabled;
            Assert.That(NativeOf(_simulation), Is.Not.Null, "AGX simulation must initialize.");
        }

        private void PauseLoadedScene(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName)
                return;
            _scene = scene;
            _simulation = UnityEngine.Object.FindObjectOfType<Simulation>();
            if (_simulation != null)
                _simulation.AutoSteppingMode = Simulation.AutoSteppingModes.Disabled;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= PauseLoadedScene;
            // Native particle deletion is unsafe here. Normal scene teardown owns
            // destruction of the simulation and all particles seeded by this test.
            try
            {
                if (_scene.IsValid() && _scene.isLoaded)
                {
                    var emptyScene = SceneManager.CreateScene("DumpTerrainIsolationCleanup");
                    SceneManager.SetActiveScene(emptyScene);
                    yield return SceneManager.UnloadSceneAsync(_scene);
                }
            }
            finally
            {
                if (_actionModeField != null)
                    _actionModeField.SetValue(null, _originalActionMode);
                Time.timeScale = _originalTimeScale;
                Time.fixedDeltaTime = _originalFixedDeltaTime;
                _simulation = null;
                _scene = default(Scene);
            }
        }

        [Test]
        public void DumpSurface_MatchesMainGround_AndNativeGridMatchesVisibleGrid()
        {
            var dump = FindTerrainByRole("Dump");
            var excavation = FindTerrainByRole("Excavation");
            var marker = GameObject.Find("Dump_frame");
            Assert.That(marker, Is.Not.Null, "GameScene must provide the dump destination marker.");
            var data = dump.TerrainData;
            var center = dump.transform.position + new Vector3(
                data.size.x * 0.5f, data.GetInterpolatedHeight(0.5f, 0.5f), data.size.z * 0.5f);
            var native = NativeOf(dump);
            var nativeSize = Invoke(native, "getSize");
            double nativeSpacing = Convert.ToDouble(Invoke(native, "getElementSize"));

            Assert.That(center.x, Is.EqualTo(marker.transform.position.x).Within(0.01f),
                "The visible dump center must match the destination X.");
            Assert.That(center.z, Is.EqualTo(marker.transform.position.z).Within(0.01f),
                "The visible dump center must match the destination Z.");
            Assert.That(data.heightmapScale.x, Is.EqualTo(data.heightmapScale.z).Within(0.00001f),
                "AGX uses one element spacing for both horizontal axes.");
            Assert.That(Convert.ToInt32(Invoke(native, "getResolutionX")), Is.EqualTo(data.heightmapResolution));
            Assert.That(Convert.ToInt32(Invoke(native, "getResolutionY")), Is.EqualTo(data.heightmapResolution));
            Assert.That(nativeSpacing, Is.EqualTo(data.heightmapScale.x).Within(0.00001));
            Assert.That(nativeSpacing, Is.EqualTo(data.heightmapScale.z).Within(0.00001));
            Assert.That(Coordinate(nativeSize, "x"), Is.EqualTo(data.size.x).Within(0.001));
            Assert.That(Coordinate(nativeSize, "y"), Is.EqualTo(data.size.z).Within(0.001),
                "Native collision/deposition bounds must not extend beyond the visible dump terrain.");

            // Marker Y is not a terrain elevation. Check the center and every
            // corner/edge midpoint against the actual surrounding ground.
            int last = data.heightmapResolution - 1;
            foreach (int z in new[] { 0, last / 2, last })
            {
                foreach (int x in new[] { 0, last / 2, last })
                {
                    var point = dump.transform.position + new Vector3(
                        x * data.heightmapScale.x, 0f, z * data.heightmapScale.z);
                    float ground = excavation.Terrain.SampleHeight(point) + excavation.transform.position.y;
                    float visible = data.GetHeight(x, z) + dump.transform.position.y;
                    float nativeHeight = dump.GetHeight(x, z) + dump.MaximumDepth + dump.transform.position.y;
                    Assert.That(visible, Is.EqualTo(ground).Within(0.005f),
                        $"Visible dump surface must join main ground at ({point.x}, {point.z}).");
                    Assert.That(nativeHeight, Is.EqualTo(ground).Within(0.005f),
                        $"Native dump surface must join main ground at ({point.x}, {point.z}).");
                }
            }
        }

        [Test]
        public void OneStep_PreservesExcavationAndDumpParticles_ButDeletesParticlesOutsideMainTerrain()
        {
            var excavation = FindTerrainByRole("Excavation");
            var dump = FindTerrainByRole("Dump");
            var soil = Invoke(NativeOf(excavation), "getSoilSimulationInterface");
            var marker = GameObject.Find("Dump_frame");
            Assert.That(marker, Is.Not.Null);

            // In the air, away from truck capture and terrain conversion: only
            // boundary deletion should affect these particles during one 20 ms step.
            var excavationPosition = new Vector3(20f, 15f, 10f);
            var dumpPosition = marker.transform.position + Vector3.up * 6f;
            var outsidePosition = new Vector3(
                Mathf.Max(excavation.transform.position.x + excavation.TerrainData.size.x,
                    dump.transform.position.x + dump.TerrainData.size.x) + 100f,
                15f,
                Mathf.Max(excavation.transform.position.z + excavation.TerrainData.size.z,
                    dump.transform.position.z + dump.TerrainData.size.z) + 100f);
            var before = ParticlePositions(soil);
            Assert.That(CountNear(before, excavationPosition), Is.Zero);
            Assert.That(CountNear(before, dumpPosition), Is.Zero);
            Assert.That(CountNear(before, outsidePosition), Is.Zero);

            CreateParticle(soil, excavationPosition);
            CreateParticle(soil, dumpPosition);
            CreateParticle(soil, outsidePosition);
            var seeded = ParticlePositions(soil);
            Assert.That(CountNear(seeded, excavationPosition), Is.EqualTo(1));
            Assert.That(CountNear(seeded, dumpPosition), Is.EqualTo(1));
            Assert.That(CountNear(seeded, outsidePosition), Is.EqualTo(1));

            var nativeSimulation = NativeOf(_simulation);
            double timeBefore = Convert.ToDouble(Invoke(nativeSimulation, "getTimeStamp"));
            _simulation.DoStep();
            double timeAfter = Convert.ToDouble(Invoke(nativeSimulation, "getTimeStamp"));
            var after = ParticlePositions(soil);

            Assert.That(timeAfter - timeBefore, Is.EqualTo(_simulation.TimeStep).Within(0.000001),
                "The probe must advance exactly one AGX step without a frame yield.");
            Assert.That(CountNear(after, excavationPosition), Is.EqualTo(1),
                "The small dump boundary must not delete excavation particles in the shared particle system.");
            Assert.That(CountNear(after, dumpPosition), Is.EqualTo(1),
                "Particles over the dump must remain available for deposition.");
            Assert.That(CountNear(after, outsidePosition), Is.Zero,
                "Disabling dump deletion must not disable the main terrain's outer boundary deletion.");
        }

        private DeformableTerrain FindTerrainByRole(string roleName)
        {
            var roleType = _terrainRoleType.GetNestedType("Role");
            Assert.That(roleType, Is.Not.Null);
            var method = _terrainRoleType.GetMethod("FindTerrainByRole", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            var terrain = method.Invoke(null, new[] { Enum.Parse(roleType, roleName) }) as DeformableTerrain;
            Assert.That(terrain, Is.Not.Null, $"GameScene must initialize its {roleName} terrain.");
            Assert.That(NativeOf(terrain), Is.Not.Null, $"{roleName} terrain native initialization failed.");
            return terrain;
        }

        // agxDotNet and Assembly-CSharp are deliberately not asmdef dependencies.
        private static object NativeOf(Component component)
        {
            var property = component.GetType().GetProperty("Native");
            Assert.That(property, Is.Not.Null);
            return property.GetValue(component);
        }

        private static object Invoke(object target, string name)
        {
            Assert.That(target, Is.Not.Null);
            var method = target.GetType().GetMethod(name, Type.EmptyTypes);
            Assert.That(method, Is.Not.Null, $"{target.GetType().Name}.{name}() must exist.");
            return method.Invoke(target, null);
        }

        private static double Coordinate(object vector, string name)
        {
            return Convert.ToDouble(vector.GetType().GetProperty(name).GetValue(vector));
        }

        private static void CreateParticle(object soil, Vector3 position)
        {
            var method = soil.GetType().GetMethods().Single(m =>
                m.Name == "createSoilParticle" && m.GetParameters().Length == 3 &&
                m.GetParameters()[0].ParameterType == typeof(double));
            var vectorType = method.GetParameters()[1].ParameterType;
            var constructor = vectorType.GetConstructor(new[] { typeof(double), typeof(double), typeof(double) });
            Assert.That(constructor, Is.Not.Null);
            // Unity -> AGX handedness conversion negates world X.
            var nativePosition = constructor.Invoke(new object[] { -(double)position.x, (double)position.y, (double)position.z });
            var velocity = constructor.Invoke(new object[] { 0.0, 0.0, 0.0 });
            method.Invoke(soil, new[] { (object)0.1, nativePosition, velocity });
        }

        private static List<Vector3> ParticlePositions(object soil)
        {
            var particles = Invoke(soil, "getSoilParticles");
            int count = Convert.ToInt32(Invoke(particles, "size"));
            var at = particles.GetType().GetMethod("at", new[] { typeof(uint) });
            Assert.That(at, Is.Not.Null);
            var positions = new List<Vector3>(count);
            for (uint i = 0; i < count; i++)
            {
                var particle = at.Invoke(particles, new object[] { i });
                var position = Invoke(particle, "position");
                positions.Add(new Vector3(-(float)Coordinate(position, "x"),
                    (float)Coordinate(position, "y"), (float)Coordinate(position, "z")));
            }
            return positions;
        }

        private static int CountNear(List<Vector3> positions, Vector3 expected)
        {
            return positions.Count(position => (position - expected).sqrMagnitude < 0.25f);
        }
    }
}
