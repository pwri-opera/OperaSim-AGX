using System.Collections;
using System.Linq;
using System.Reflection;
using AGXUnity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PWRISimulator.Tests
{
    /// <summary>
    /// PlayMode integration tests verifying the full particle-to-solid
    /// conversion pipeline in DumpSoil:
    ///
    ///   1. DumpSoil.Initialize() creates soilMassBody (solid rigid body)
    ///   2. AGX soil particles spawned directly over the dump truck merge
    ///      zone are captured by ProcessCaptureStep (removed from AGX
    ///      particle simulation, mass accumulated into soilMass,
    ///      soilMassBody mass updated)
    ///
    /// Instead of driving the excavator to dig (which requires complex
    /// ROS joint command plumbing), this test spawns soil particles
    /// directly via terrain.Native.getSoilSimulationInterface()
    /// .createSoilParticle() at positions inside the dump truck's merge
    /// zone.  This isolates the particle-to-solid conversion logic from
    /// the excavator control system.
    ///
    /// Access strategy
    /// ---------------
    ///   DumpSoil, GlobalVariables → reflection
    ///   (test assembly does not reference Assembly-CSharp)
    ///   AGXUnity.RigidBody        → direct reference
    ///   agx.SoilSimulationInterface → reflection (avoids agxDotNet ref)
    /// </summary>
    public class DumpSoilParticleToSolidTests
    {
        private const string SceneName = "GameScene";

        /// <summary>Seconds to wait after starting simulation before testing.</summary>
        private const float SettleTime = 3.0f;

        /// <summary>Seconds to wait for particle capture after spawning.</summary>
        private const float CaptureTime = 3.0f;

        /// <summary>Number of particles to spawn over the merge zone.</summary>
        private const int SpawnCount = 50;

        // ActionMode values (matches ControlPhysics.Update logic)
        private const int ActionModeSimulation = 3;
        private const int ActionModeIdle = -1;

        private const BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PublicInstance =
            BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PublicStatic =
            BindingFlags.Public | BindingFlags.Static;

        // Reflection handles
        private System.Type _globalVariablesType;
        private FieldInfo _actionModeField;
        private int _originalActionMode;

        private System.Type _dumpSoilType;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // --- GlobalVariables ---
            _globalVariablesType = System.Type.GetType(
                "PWRISimulator.GlobalVariables, Assembly-CSharp");
            Assert.That(_globalVariablesType, Is.Not.Null,
                "GlobalVariables not found in Assembly-CSharp.");

            _actionModeField = _globalVariablesType.GetField(
                "ActionMode", PublicStatic);
            Assert.That(_actionModeField, Is.Not.Null,
                "GlobalVariables.ActionMode field not found.");
            _originalActionMode = (int)_actionModeField.GetValue(null);

            // --- DumpSoil type ---
            _dumpSoilType = System.Type.GetType(
                "PWRISimulator.DumpSoil, Assembly-CSharp");
            Assert.That(_dumpSoilType, Is.Not.Null,
                "DumpSoil type not found in Assembly-CSharp.");

            // --- Load GameScene ---
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;
            }

            // Start with physics paused.
            _actionModeField.SetValue(null, ActionModeIdle);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_actionModeField != null)
                _actionModeField.SetValue(null, _originalActionMode);
            yield return null;
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                              //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Find the DumpSoil component in the scene via reflection.
        /// </summary>
        private object FindDumpSoil()
        {
            var dumpSoilObj = UnityEngine.Object.FindObjectOfType(
                _dumpSoilType);
            Assert.That(dumpSoilObj, Is.Not.Null,
                "DumpSoil component not found in GameScene.");
            return dumpSoilObj;
        }

        /// <summary>
        /// Get the soilMass property value from a DumpSoil instance.
        /// </summary>
        private double GetSoilMass(object dumpSoil)
        {
            var prop = _dumpSoilType.GetProperty("soilMass");
            Assert.That(prop, Is.Not.Null,
                "DumpSoil.soilMass property not found.");
            return (double)prop.GetValue(dumpSoil);
        }

        /// <summary>
        /// Get the isRuntimeReady field value from a DumpSoil instance.
        /// </summary>
        private bool GetIsRuntimeReady(object dumpSoil)
        {
            var field = _dumpSoilType.GetField("isRuntimeReady", PrivateInstance);
            Assert.That(field, Is.Not.Null,
                "DumpSoil.isRuntimeReady field not found.");
            return (bool)field.GetValue(dumpSoil);
        }

        /// <summary>
        /// Get the soilMassBody field (RigidBody) from a DumpSoil instance.
        /// </summary>
        private RigidBody GetSoilMassBody(object dumpSoil)
        {
            var field = _dumpSoilType.GetField("soilMassBody",
                PrivateInstance);
            Assert.That(field, Is.Not.Null,
                "DumpSoil.soilMassBody field not found.");
            return field.GetValue(dumpSoil) as RigidBody;
        }

        /// <summary>
        /// Get the terrain field from a DumpSoil instance as Component.
        /// </summary>
        private Component GetTerrain(object dumpSoil)
        {
            var field = _dumpSoilType.GetField("terrain", PublicInstance);
            Assert.That(field, Is.Not.Null,
                "DumpSoil.terrain field not found.");
            return field.GetValue(dumpSoil) as Component;
        }

        /// <summary>
        /// Get the terrainNative field (agx.Terrain) from a DumpSoil instance
        /// via reflection, since we can't reference agxDotNet directly.
        /// </summary>
        private object GetTerrainNative(object dumpSoil)
        {
            var field = _dumpSoilType.GetField("terrainNative", PrivateInstance);
            Assert.That(field, Is.Not.Null,
                "DumpSoil.terrainNative field not found.");
            return field.GetValue(dumpSoil);
        }

        /// <summary>
        /// Get the mergeZoneOriginalSize property — the local scale of the
        /// DumpSoil transform, which defines the merge zone dimensions.
        /// </summary>
        private Vector3 GetMergeZoneOriginalSize(object dumpSoil)
        {
            // mergeZoneOriginalSize is a property that returns transform.localScale
            var prop = _dumpSoilType.GetProperty(
                "mergeZoneOriginalSize",
                PrivateInstance | PublicInstance);
            if (prop != null)
                return (Vector3)prop.GetValue(dumpSoil);
            // Fallback: just use transform.localScale
            var comp = dumpSoil as Component;
            return comp.transform.localScale;
        }

        /// <summary>
        /// Find a property declared exactly on the given type (not inherited)
        /// to avoid AmbiguousMatchException when a property name exists in
        /// both a base class and a derived class.
        /// </summary>
        private static PropertyInfo FindDeclaredProperty(
            System.Type type, string name)
        {
            return type.GetProperty(name,
                BindingFlags.DeclaredOnly |
                BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Get the mass value from a RigidBody's MassProperties.Mass.Value
        /// chain, using reflection to avoid agxDotNet assembly reference.
        /// </summary>
        private float GetRigidBodyMass(RigidBody body)
        {
            // MassProperties property
            var massPropsProp = FindDeclaredProperty(
                body.GetType(), "MassProperties") ??
                body.GetType().GetProperty("MassProperties");
            Assert.That(massPropsProp, Is.Not.Null,
                "RigidBody.MassProperties property not found.");
            object massProps = massPropsProp.GetValue(body);

            // Mass property (RangeReal or similar)
            var massProp = FindDeclaredProperty(
                massProps.GetType(), "Mass") ??
                massProps.GetType().GetProperty("Mass");
            Assert.That(massProp, Is.Not.Null,
                "MassProperties.Mass property not found.");
            object massObj = massProp.GetValue(massProps);

            // Value property
            var valueProp = FindDeclaredProperty(
                massObj.GetType(), "Value") ??
                massObj.GetType().GetProperty("Value");
            Assert.That(valueProp, Is.Not.Null,
                "Mass.Value property not found.");
            return (float)valueProp.GetValue(massObj);
        }

        /// <summary>
        /// Spawn soil particles directly over the dump truck merge zone
        /// using the AGX terrain's soil simulation interface.
        ///
        /// Uses reflection to call:
        ///   terrainNative.getSoilSimulationInterface().createSoilParticle(
        ///     radius, position, velocity)
        ///
        /// Particles are spawned at the center of the merge zone, slightly
        /// above the soil height so they fall into the capture bounds.
        /// </summary>
        private int SpawnParticlesOverMergeZone(object dumpSoil, int count)
        {
            object terrainNative = GetTerrainNative(dumpSoil);
            Assert.That(terrainNative, Is.Not.Null,
                "terrainNative is null — DumpSoil not fully initialized.");

            // Get soil simulation interface via reflection
            // Use exact binding to avoid AmbiguousMatchException
            var getSoilSimMethod = null as MethodInfo;
            foreach (var m in terrainNative.GetType().GetMethods())
            {
                if (m.Name == "getSoilSimulationInterface" &&
                    m.GetParameters().Length == 0)
                {
                    getSoilSimMethod = m;
                    break;
                }
            }
            Assert.That(getSoilSimMethod, Is.Not.Null,
                "getSoilSimulationInterface() method not found on terrain.");
            object soilSim = getSoilSimMethod.Invoke(terrainNative, null);
            Assert.That(soilSim, Is.Not.Null,
                "SoilSimulationInterface is null.");

            // Get createSoilParticle method — use GetMethods and filter to
            // avoid AmbiguousMatchException from overloaded versions.
            var createParticleMethod = null as MethodInfo;
            foreach (var m in soilSim.GetType().GetMethods())
            {
                if (m.Name != "createSoilParticle")
                    continue;
                var ps = m.GetParameters();
                // We want the overload: (double radius, Vec3 pos, Vec3 vel)
                if (ps.Length == 3 &&
                    ps[0].ParameterType == typeof(double))
                {
                    createParticleMethod = m;
                    break;
                }
            }
            Assert.That(createParticleMethod, Is.Not.Null,
                "createSoilParticle(double, Vec3, Vec3) method not found " +
                "on SoilSimulationInterface.");

            // Get the DumpSoil transform to find the merge zone center
            var comp = dumpSoil as Component;
            Vector3 mergeZoneCenter = comp.transform.position;
            Vector3 mergeZoneSize = GetMergeZoneOriginalSize(dumpSoil);

            // Spawn particles in a grid pattern within the merge zone.
            // The capture bounds in local space are:
            //   X: [-halfWidth, halfWidth]  (centered on transform)
            //   Y: [0, 0]                   (zero-thickness slab at bed surface)
            //   Z: [0, depth]               (starts at transform, extends forward)
            // So in world space, particles should be:
            //   X: center.x ± halfWidth
            //   Y: center.y (bed surface)
            //   Z: center.z to center.z + depth (NOT centered — starts at origin)
            int spawned = 0;
            double radius = 0.05; // 5cm radius — typical soil particle
            float halfWidth = mergeZoneSize.x * 0.4f;
            float depthFraction = 0.8f; // stay within [0.1, 0.9] of depth
            float spawnY = mergeZoneCenter.y;

            int gridSide = Mathf.CeilToInt(Mathf.Sqrt(count));
            for (int i = 0; i < count; i++)
            {
                int gx = i % gridSide;
                int gz = i / gridSide;
                float offsetX = (gx / (float)(gridSide - 1) - 0.5f) * 2f * halfWidth;
                // Z starts at the DumpSoil origin and extends forward (0 to depth)
                float offsetZ = (gz / (float)(gridSide - 1)) * mergeZoneSize.z * depthFraction;

                // Get the Vec3 type from the method's parameter signature
                // (avoids needing to reference agxDotNet assembly directly)
                var vec3Type = createParticleMethod.GetParameters()[1].ParameterType;

                float x = mergeZoneCenter.x + offsetX;
                float y = spawnY;
                float z = mergeZoneCenter.z + offsetZ;

                // Convert Unity world position to AGX world position.
                // ToHandedVec3 flips X: agx.Vec3(-x, y, z)
                var vec3Constructor = vec3Type.GetConstructor(
                    new[] { typeof(double), typeof(double), typeof(double) });
                Assert.That(vec3Constructor, Is.Not.Null,
                    "agx.Vec3(double,double,double) constructor not found.");
                object posVec = vec3Constructor.Invoke(
                    new object[] { (double)-x, (double)y, (double)z });
                // Zero velocity — particles stay at spawn position for capture
                object velVec = vec3Constructor.Invoke(
                    new object[] { 0.0, 0.0, 0.0 });

                try
                {
                    createParticleMethod.Invoke(soilSim,
                        new object[] { radius, posVec, velVec });
                    spawned++;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning(
                        $"[DumpSoilParticleToSolidTests] Failed to spawn " +
                        $"particle {i} at ({x},{y},{z}): {e.InnerException?.Message ?? e.Message}");
                }
            }

            return spawned;
        }

        // ------------------------------------------------------------------ //
        // Tests                                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that DumpSoil.Initialize() succeeds in GameScene:
        ///   - isRuntimeReady becomes true
        ///   - soilMassBody (solid rigid body) is created
        ///   - terrain reference is set
        ///
        /// This is a prerequisite for particle-to-solid conversion —
        /// without a soilMassBody, captured particle mass has nowhere
        /// to go.
        /// </summary>
        [UnityTest]
        public IEnumerator DumpSoil_InitializesAndCreatesSoilMassBody()
        {
            // Start simulation so Initialize() runs
            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            object dumpSoil = FindDumpSoil();

            // --- Assert: isRuntimeReady ---
            bool isReady = GetIsRuntimeReady(dumpSoil);
            Assert.That(isReady, Is.True,
                "DumpSoil.isRuntimeReady should be true after Initialize() " +
                "completes with terrain and containerBody references.");

            // --- Assert: terrain is set ---
            Component terrain = GetTerrain(dumpSoil);
            Assert.That(terrain, Is.Not.Null,
                "DumpSoil.terrain should be assigned after Initialize().");

            // Check terrain.Native via reflection (avoids agxDotNet assembly ref)
            var terrainNativeProp = FindDeclaredProperty(
                terrain.GetType(), "Native") ??
                terrain.GetType().GetProperty("Native");
            Assert.That(terrainNativeProp, Is.Not.Null,
                "DeformableTerrain.Native property not found.");
            object terrainNative = terrainNativeProp.GetValue(terrain);
            Assert.That(terrainNative, Is.Not.Null,
                "DeformableTerrain.Native should be initialized.");

            // --- Assert: soilMassBody is created ---
            RigidBody soilMassBody = GetSoilMassBody(dumpSoil);
            Assert.That(soilMassBody, Is.Not.Null,
                "DumpSoil.soilMassBody should be created by " +
                "CreateSoilMassBody() during Initialize(). " +
                "This rigid body represents the solid mass of captured " +
                "soil particles — without it, particle-to-solid conversion " +
                "cannot occur.");

            // --- Assert: soilMassBody is initialized ---
            var nativeProp = FindDeclaredProperty(
                soilMassBody.GetType(), "Native") ??
                soilMassBody.GetType().GetProperty("Native");
            Assert.That(nativeProp, Is.Not.Null,
                "RigidBody.Native property not found.");
            object nativeBody = nativeProp.GetValue(soilMassBody);
            Assert.That(nativeBody, Is.Not.Null,
                "soilMassBody.Native should be initialized (AGX rigid body " +
                "created in simulation).");

            // Stop simulation
            _actionModeField.SetValue(null, ActionModeIdle);
            yield return null;
        }

        /// <summary>
        /// Verifies the full particle-to-solid conversion pipeline:
        ///
        ///   1. Start simulation
        ///   2. Spawn AGX soil particles directly over the dump truck
        ///      merge zone (bypassing the excavator)
        ///   3. Wait for particles to fall into the merge zone and be
        ///      captured by ProcessCaptureStep
        ///   4. Assert soilMass > 0 (particles converted to solid mass)
        ///   5. Assert soilMassBody mass reflects captured particles
        ///
        /// This test proves that AGX soil particles are captured by the
        /// dump truck's merge zone and converted into solid rigid body
        /// mass — the core particle-to-solid conversion.
        /// </summary>
        [UnityTest]
        public IEnumerator DumpSoil_ParticlesConvertedToSolid_WhenSpawnedOverMergeZone()
        {
            // Start simulation
            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            object dumpSoil = FindDumpSoil();

            // Verify DumpSoil is initialized
            Assert.That(GetIsRuntimeReady(dumpSoil), Is.True,
                "DumpSoil must be initialized before spawning particles.");

            // Record initial soilMass (should be 0 — no particles captured yet)
            double soilMassBefore = GetSoilMass(dumpSoil);
            Debug.Log(
                $"[DumpSoilParticleToSolidTests] soilMass before spawn: {soilMassBefore}");

            // Spawn particles directly over the merge zone
            int spawned = SpawnParticlesOverMergeZone(dumpSoil, SpawnCount);
            Debug.Log(
                $"[DumpSoilParticleToSolidTests] Spawned {spawned}/{SpawnCount} " +
                $"particles over merge zone.");
            Assert.That(spawned, Is.GreaterThan(0),
                "Failed to spawn any soil particles. " +
                "createSoilParticle() may not be available.");

            // --- Diagnostic: check captureStepExecutionCount ---
            var captureCountField = _dumpSoilType.GetField("captureStepExecutionCount",
                BindingFlags.NonPublic | BindingFlags.Instance);
            int captureCountBefore = captureCountField != null
                ? (int)captureCountField.GetValue(dumpSoil) : -1;
            Debug.Log(
                $"[DumpSoilParticleToSolidTests] captureStepExecutionCount " +
                $"before wait: {captureCountBefore}");

            // Wait for particles to be captured by ProcessCaptureStep
            yield return new WaitForSeconds(CaptureTime);

            int captureCountAfter = captureCountField != null
                ? (int)captureCountField.GetValue(dumpSoil) : -1;
            Debug.Log(
                $"[DumpSoilParticleToSolidTests] captureStepExecutionCount " +
                $"after wait: {captureCountAfter}");

            // --- Assert: soilMass increased ---
            double soilMassAfter = GetSoilMass(dumpSoil);
            Debug.Log(
                $"[DumpSoilParticleToSolidTests] soilMass after capture: {soilMassAfter}");

            Assert.That(soilMassAfter, Is.GreaterThan(0.0),
                "soilMass should be > 0 after soil particles are spawned " +
                "over the merge zone and captured. A value of 0 means no " +
                "soil particles were converted to solid mass — the " +
                "particle-to-solid conversion pipeline is broken.");

            // --- Assert: soilMassBody mass reflects captured particles ---
            RigidBody soilMassBody = GetSoilMassBody(dumpSoil);
            Assert.That(soilMassBody, Is.Not.Null);
            // Access MassProperties.Mass.Value via reflection to avoid
            // agxDotNet assembly reference issues. Use GetProperty with
            // exact declared type to avoid AmbiguousMatchException.
            float bodyMass = GetRigidBodyMass(soilMassBody);
            Debug.Log(
                $"[DumpSoilParticleToSolidTests] soilMassBody mass: {bodyMass}");

            Assert.That(bodyMass, Is.GreaterThan(1.0f),
                "soilMassBody mass should be > 1.0 (the minimum default) " +
                "after particles are captured. The mass should reflect " +
                "the accumulated soilMass from captured particles.");

            // Stop simulation
            _actionModeField.SetValue(null, ActionModeIdle);
            yield return null;
        }
    }
}
