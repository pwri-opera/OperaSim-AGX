using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PWRISimulator.Tests
{
    /// <summary>
    /// PlayMode integration tests verifying the dump truck vessel (dump bed)
    /// up/down control paths end-to-end in GameScene:
    ///
    ///   1. ManualDumpSpeed_MovesJointUpAndDown
    ///      - Calls DumpTruckInput.ApplyManualDumpSpeed (public API)
    ///      - Verifies the dump joint angle increases with positive speed
    ///        and decreases with negative speed, then holds at zero.
    ///
    ///   2. RosPositionCommand_MovesJointToTarget
    ///      - Sets RotDumpSubscriber.DumpCmd with control_type=Position
    ///      - Lets the automatic OnPreStepForward → SetCommands() loop run
    ///      - Verifies the joint converges to the target angle (within _eps)
    ///        via the computeAngularVelocity → Speed conversion path.
    ///
    ///   3. DumpUp_UnderLoad_FromParticleCapture
    ///      - Spawns AGX soil particles over the merge zone (reuses the
    ///        pattern from DumpSoilParticleToSolidTests)
    ///      - Waits for soilMass > threshold (particles converted to solid)
    ///      - Commands dump up via ApplyManualDumpSpeed
    ///      - Verifies the joint angle increases despite the load, proving
    ///        the controlMaxForce (600000) is sufficient under real load.
    ///
    /// Access strategy
    /// ---------------
    ///   DumpTruckInput, DumpTruckJoint, DumpTruckDumpSubscriber,
    ///   DumpSoil, GlobalVariables, ConstraintControl,
    ///   JointCmdMsg → reflection (test assembly does not reference
    ///   Assembly-CSharp)
    /// </summary>
    public class DumpTruckControlPlayModeTests
    {
        private const string SceneName = "GameScene";

        /// <summary>Seconds to wait after starting simulation before testing.</summary>
        private const float SettleTime = 3.0f;

        /// <summary>Seconds to wait for joint movement after commanding.</summary>
        private const float MoveTime = 2.0f;

        /// <summary>Number of particles to spawn over the merge zone for load test.</summary>
        private const int SpawnCount = 50;

        /// <summary>Minimum soilMass [kg] required before attempting to lift under load.</summary>
        private const double MinLoadSoilMass = 10.0;

        // ActionMode values (matches ControlPhysics.Update logic)
        private const int ActionModeSimulation = 3;
        private const int ActionModeIdle = -1;

        // JointCmdMsg.control_type values
        private const byte PositionControl = 0;

        private const BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PublicInstance =
            BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PublicStatic =
            BindingFlags.Public | BindingFlags.Static;

        // Reflection handles — Assembly-CSharp
        private System.Type _globalVariablesType;
        private FieldInfo _actionModeField;
        private int _originalActionMode;

        private System.Type _dumpTruckInputType;
        private System.Type _dumpTruckJointType;
        private System.Type _dumpSubscriberType;
        private System.Type _dumpSoilType;
        private System.Type _constraintControlType;

        // Reflection handles — RosMessages
        private System.Type _jointCmdMsgType;

        // Cached method/field handles
        private MethodInfo _applyManualDumpSpeedMethod;
        private FieldInfo _rotDumpSubscriberField;
        private FieldInfo _dumpCmdBackingField;
        private FieldInfo _dumpCmdJointNameField;
        private FieldInfo _dumpCmdControlTypeField;
        private FieldInfo _dumpCmdPositionField;
        private FieldInfo _dumpCmdVelocityField;

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

            // --- DumpTruckInput ---
            _dumpTruckInputType = System.Type.GetType(
                "PWRISimulator.ROS.DumpTruckInput, Assembly-CSharp");
            Assert.That(_dumpTruckInputType, Is.Not.Null,
                "DumpTruckInput not found in Assembly-CSharp.");

            // --- DumpTruckJoint ---
            _dumpTruckJointType = System.Type.GetType(
                "PWRISimulator.ROS.DumpTruckJoint, Assembly-CSharp");
            Assert.That(_dumpTruckJointType, Is.Not.Null,
                "DumpTruckJoint not found in Assembly-CSharp.");

            // --- DumpTruckDumpSubscriber ---
            _dumpSubscriberType = System.Type.GetType(
                "PWRISimulator.ROS.DumpTruckDumpSubscriber, Assembly-CSharp");
            Assert.That(_dumpSubscriberType, Is.Not.Null,
                "DumpTruckDumpSubscriber not found in Assembly-CSharp.");

            // --- DumpSoil ---
            _dumpSoilType = System.Type.GetType(
                "PWRISimulator.DumpSoil, Assembly-CSharp");
            Assert.That(_dumpSoilType, Is.Not.Null,
                "DumpSoil not found in Assembly-CSharp.");

            // --- ConstraintControl ---
            _constraintControlType = System.Type.GetType(
                "PWRISimulator.ConstraintControl, Assembly-CSharp");
            Assert.That(_constraintControlType, Is.Not.Null,
                "ConstraintControl not found in Assembly-CSharp.");

            // --- JointCmdMsg (in Assembly-CSharp — no separate asmdef for RosMessages) ---
            _jointCmdMsgType = System.Type.GetType(
                "RosMessageTypes.Com3.JointCmdMsg, Assembly-CSharp");
            if (_jointCmdMsgType == null)
            {
                // Fallback: search all loaded assemblies
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    _jointCmdMsgType = asm.GetType("RosMessageTypes.Com3.JointCmdMsg");
                    if (_jointCmdMsgType != null)
                        break;
                }
            }
            Assert.That(_jointCmdMsgType, Is.Not.Null,
                "JointCmdMsg not found in any loaded assembly.");

            // --- Cache DumpTruckInput handles ---
            _applyManualDumpSpeedMethod = _dumpTruckInputType.GetMethod(
                "ApplyManualDumpSpeed", PublicInstance);
            Assert.That(_applyManualDumpSpeedMethod, Is.Not.Null,
                "DumpTruckInput.ApplyManualDumpSpeed method not found.");

            _rotDumpSubscriberField = _dumpTruckInputType.GetField(
                "RotDumpSubscriber", PublicInstance);
            Assert.That(_rotDumpSubscriberField, Is.Not.Null,
                "DumpTruckInput.RotDumpSubscriber field not found.");

            // --- Cache DumpTruckDumpSubscriber.DumpCmd handles ---
            // DumpCmd has a public getter but private setter, so we write
            // via the backing field.
            var dumpCmdProperty = _dumpSubscriberType.GetProperty("DumpCmd");
            Assert.That(dumpCmdProperty, Is.Not.Null,
                "DumpTruckDumpSubscriber.DumpCmd property not found.");

            _dumpCmdBackingField = _dumpSubscriberType.GetField(
                "dumpCmd", PrivateInstance);
            Assert.That(_dumpCmdBackingField, Is.Not.Null,
                "DumpTruckDumpSubscriber.dumpCmd backing field not found.");

            _dumpCmdJointNameField = _jointCmdMsgType.GetField("joint_name");
            _dumpCmdControlTypeField = _jointCmdMsgType.GetField("control_type");
            _dumpCmdPositionField = _jointCmdMsgType.GetField("position");
            _dumpCmdVelocityField = _jointCmdMsgType.GetField("velocity");
            Assert.That(_dumpCmdJointNameField, Is.Not.Null,
                "JointCmdMsg.joint_name field not found.");
            Assert.That(_dumpCmdControlTypeField, Is.Not.Null,
                "JointCmdMsg.control_type field not found.");
            Assert.That(_dumpCmdPositionField, Is.Not.Null,
                "JointCmdMsg.position field not found.");
            Assert.That(_dumpCmdVelocityField, Is.Not.Null,
                "JointCmdMsg.velocity field not found.");

            // --- Load GameScene ---
            // The AGX terrain may log known non-fatal errors during scene
            // initialization (EntryPointNotFoundException for Shovel SWIG,
            // "Initialize call when object is being initialized", etc.).
            // These only appear in environments with an AGX native library
            // mismatch — the terrain continues without the shovel and the
            // dump truck control does not depend on it.
            //
            // We install a dynamic log handler that catches these known
            // errors via LogAssert.Expect AS they occur, so the tests pass
            // both with and without the errors. This avoids the strict
            // "must appear exactly once" semantics of pre-registering
            // LogAssert.Expect calls.
            Application.logMessageReceived += OnAgxKnownError;

            if (SceneManager.GetActiveScene().name != SceneName)
            {
                SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;
            }
            else
            {
                yield return null;
            }

            Application.logMessageReceived -= OnAgxKnownError;

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
        /// Dynamic log handler that catches known AGX non-fatal errors
        /// via LogAssert.Expect as they occur. This allows tests to pass
        /// both in environments where the errors appear (AGX native
        /// mismatch) and where they don't (proper AGX install).
        /// </summary>
        private void OnAgxKnownError(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
                return;

            // Known non-fatal AGX errors during terrain init
            if (condition.Contains("CSharp_agxTerrain_new_Shovel__SWIG_1") ||
                condition.Contains("Initialize call when object is being initialized") ||
                condition.Contains("DllNotFoundException: libdl.so"))
            {
                LogAssert.Expect(LogType.Exception, condition);
            }
        }

        /// <summary>
        /// Find the DumpTruckInput component in the scene via reflection.
        /// </summary>
        private object FindDumpTruckInput()
        {
            var inputObj = UnityEngine.Object.FindObjectOfType(_dumpTruckInputType);
            Assert.That(inputObj, Is.Not.Null,
                "DumpTruckInput component not found in GameScene.");
            return inputObj;
        }

        /// <summary>
        /// Find the DumpTruckJoint component in the scene via reflection.
        /// </summary>
        private object FindDumpTruckJoint()
        {
            var jointObj = UnityEngine.Object.FindObjectOfType(_dumpTruckJointType);
            Assert.That(jointObj, Is.Not.Null,
                "DumpTruckJoint component not found in GameScene.");
            return jointObj;
        }

        /// <summary>
        /// Find the DumpSoil component in the scene via reflection.
        /// </summary>
        private object FindDumpSoil()
        {
            var dumpSoilObj = UnityEngine.Object.FindObjectOfType(_dumpSoilType);
            Assert.That(dumpSoilObj, Is.Not.Null,
                "DumpSoil component not found in GameScene.");
            return dumpSoilObj;
        }

        /// <summary>
        /// Get the dump_joint ConstraintControl from a DumpTruckJoint instance.
        /// </summary>
        private object GetDumpJoint(object dumpTruckJoint)
        {
            var field = _dumpTruckJointType.GetField("dump_joint", PublicInstance);
            Assert.That(field, Is.Not.Null,
                "DumpTruckJoint.dump_joint field not found.");
            return field.GetValue(dumpTruckJoint);
        }

        /// <summary>
        /// Get CurrentPosition from a ConstraintControl (reads nativeConstraint.getAngle()).
        /// </summary>
        private double GetJointCurrentPosition(object constraintControl)
        {
            var prop = _constraintControlType.GetProperty("CurrentPosition");
            Assert.That(prop, Is.Not.Null,
                "ConstraintControl.CurrentPosition property not found.");
            return (double)prop.GetValue(constraintControl);
        }

        /// <summary>
        /// Get the RotDumpSubscriber from a DumpTruckInput instance.
        /// </summary>
        private object GetRotDumpSubscriber(object dumpTruckInput)
        {
            return _rotDumpSubscriberField.GetValue(dumpTruckInput);
        }

        /// <summary>
        /// Set the DumpCmd on a DumpTruckDumpSubscriber instance.
        /// Uses the backing field because the property setter is private.
        /// </summary>
        private void SetDumpCmd(object dumpSubscriber, object dumpCmd)
        {
            _dumpCmdBackingField.SetValue(dumpSubscriber, dumpCmd);
        }

        /// <summary>
        /// Create a new JointCmdMsg with the given control_type and arrays.
        /// Uses reflection since the test assembly doesn't reference RosMessages.
        /// </summary>
        private object CreateJointCmdMsg(byte controlType, string[] jointNames,
            double[] position, double[] velocity, double[] effort)
        {
            // Use the (string[], double[], double[], double[]) constructor
            var ctor = _jointCmdMsgType.GetConstructor(
                new[] { typeof(string[]), typeof(double[]), typeof(double[]), typeof(double[]) });
            Assert.That(ctor, Is.Not.Null,
                "JointCmdMsg(string[], double[], double[], double[]) constructor not found.");
            object msg = ctor.Invoke(new object[] { jointNames, position, velocity, effort });

            // Set control_type
            _dumpCmdControlTypeField.SetValue(msg, controlType);
            return msg;
        }

        /// <summary>
        /// Call DumpTruckInput.ApplyManualDumpSpeed(double speed) via reflection.
        /// </summary>
        private void ApplyManualDumpSpeed(object dumpTruckInput, double speed)
        {
            _applyManualDumpSpeedMethod.Invoke(dumpTruckInput, new object[] { speed });
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
            var prop = _dumpSoilType.GetProperty(
                "mergeZoneOriginalSize",
                PrivateInstance | PublicInstance);
            if (prop != null)
                return (Vector3)prop.GetValue(dumpSoil);
            var comp = dumpSoil as Component;
            return comp.transform.localScale;
        }

        /// <summary>
        /// Spawn soil particles directly over the dump truck merge zone
        /// using the AGX terrain's soil simulation interface.
        ///
        /// Reuses the same reflection pattern as DumpSoilParticleToSolidTests.
        /// </summary>
        private int SpawnParticlesOverMergeZone(object dumpSoil, int count)
        {
            object terrainNative = GetTerrainNative(dumpSoil);
            Assert.That(terrainNative, Is.Not.Null,
                "terrainNative is null — DumpSoil not fully initialized.");

            // Get soil simulation interface via reflection
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

            // Get createSoilParticle method
            var createParticleMethod = null as MethodInfo;
            foreach (var m in soilSim.GetType().GetMethods())
            {
                if (m.Name != "createSoilParticle")
                    continue;
                var ps = m.GetParameters();
                if (ps.Length == 3 && ps[0].ParameterType == typeof(double))
                {
                    createParticleMethod = m;
                    break;
                }
            }
            Assert.That(createParticleMethod, Is.Not.Null,
                "createSoilParticle(double, Vec3, Vec3) method not found.");

            var comp = dumpSoil as Component;
            Vector3 mergeZoneCenter = comp.transform.position;
            Vector3 mergeZoneSize = GetMergeZoneOriginalSize(dumpSoil);

            int spawned = 0;
            double radius = 0.05;
            float halfWidth = mergeZoneSize.x * 0.4f;
            float depthFraction = 0.8f;
            float spawnY = mergeZoneCenter.y;

            int gridSide = Mathf.CeilToInt(Mathf.Sqrt(count));
            for (int i = 0; i < count; i++)
            {
                int gx = i % gridSide;
                int gz = i / gridSide;
                float offsetX = (gx / (float)(gridSide - 1) - 0.5f) * 2f * halfWidth;
                float offsetZ = (gz / (float)(gridSide - 1)) * mergeZoneSize.z * depthFraction;

                var vec3Type = createParticleMethod.GetParameters()[1].ParameterType;
                var vec3Constructor = vec3Type.GetConstructor(
                    new[] { typeof(double), typeof(double), typeof(double) });
                Assert.That(vec3Constructor, Is.Not.Null,
                    "agx.Vec3(double,double,double) constructor not found.");

                float x = mergeZoneCenter.x + offsetX;
                float y = spawnY;
                float z = mergeZoneCenter.z + offsetZ;

                object posVec = vec3Constructor.Invoke(
                    new object[] { (double)-x, (double)y, (double)z });
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
                        $"[DumpTruckControlPlayModeTests] Failed to spawn " +
                        $"particle {i} at ({x},{y},{z}): {e.InnerException?.Message ?? e.Message}");
                }
            }

            return spawned;
        }

        // ------------------------------------------------------------------ //
        // Tests                                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies the manual dump speed control path:
        ///
        ///   1. Start simulation
        ///   2. Record initial dump joint angle
        ///   3. Command positive speed (dump up) via ApplyManualDumpSpeed
        ///   4. Wait MoveTime, verify angle increased
        ///   5. Command negative speed (dump down)
        ///   6. Wait MoveTime, verify angle decreased
        ///   7. Command zero speed (stop)
        ///   8. Wait, verify angle is stable (not drifting)
        ///
        /// This validates: ApplyDumpCommand(Speed) → ReapplyControlValue →
        /// setLockedAtZeroSpeed → native TargetSpeedController.
        /// </summary>
        [UnityTest]
        public IEnumerator ManualDumpSpeed_MovesJointUpAndDown()
        {
            // Start simulation
            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            object input = FindDumpTruckInput();
            object joint = FindDumpTruckJoint();
            object dumpJoint = GetDumpJoint(joint);

            // Record initial position
            double initialAngle = GetJointCurrentPosition(dumpJoint);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] Initial dump angle: {initialAngle:F6}");

            // --- Dump Up ---
            ApplyManualDumpSpeed(input, 0.3); // positive speed = up
            yield return new WaitForSeconds(MoveTime);

            double upAngle = GetJointCurrentPosition(dumpJoint);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] After up command: {upAngle:F6}");
            Assert.That(upAngle, Is.GreaterThan(initialAngle),
                "Dump joint angle should increase after a positive speed command. " +
                "If it doesn't, ApplyManualDumpSpeed → ApplyDumpCommand(Speed) → " +
                "ReapplyControlValue may not be reaching the native controller.");

            // --- Dump Down ---
            ApplyManualDumpSpeed(input, -0.3); // negative speed = down
            yield return new WaitForSeconds(MoveTime);

            double downAngle = GetJointCurrentPosition(dumpJoint);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] After down command: {downAngle:F6}");
            Assert.That(downAngle, Is.LessThan(upAngle),
                "Dump joint angle should decrease after a negative speed command.");

            // --- Stop ---
            ApplyManualDumpSpeed(input, 0.0);
            yield return new WaitForSeconds(1.0f);

            double stopAngle1 = GetJointCurrentPosition(dumpJoint);
            yield return new WaitForSeconds(1.0f);
            double stopAngle2 = GetJointCurrentPosition(dumpJoint);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] After stop: {stopAngle1:F6} → {stopAngle2:F6}");

            // The joint should be stable (not drifting more than a small tolerance).
            // Note: with setLockedAtZeroSpeed, the controller locks at current position,
            // so drift should be minimal. Allow 0.01 rad tolerance for physics noise.
            Assert.That(System.Math.Abs(stopAngle2 - stopAngle1), Is.LessThan(0.01),
                "Dump joint should be stable after zero-speed command. " +
                "setLockedAtZeroSpeed should lock the constraint at its current position.");

            // Stop simulation
            _actionModeField.SetValue(null, ActionModeIdle);
            yield return null;
        }

        /// <summary>
        /// Verifies the ROS position command control path:
        ///
        ///   1. Start simulation
        ///   2. Record initial dump joint angle
        ///   3. Set a target angle above current via RotDumpSubscriber.DumpCmd
        ///      with control_type=Position (0)
        ///   4. Let the automatic OnPreStepForward → SetCommands() loop run
        ///   5. Wait for convergence (up to 10 seconds)
        ///   6. Verify joint angle reached target within tolerance
        ///
        /// This validates: GetDumpControlType → ApplyDumpCommand(Position) →
        /// computeAngularVelocity → Speed conversion → native controller.
        /// The target is set above the current angle, so the vessel should
        /// dump up to reach it.
        /// </summary>
        [UnityTest]
        public IEnumerator RosPositionCommand_MovesJointToTarget()
        {
            // Start simulation
            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            object input = FindDumpTruckInput();
            object joint = FindDumpTruckJoint();
            object dumpJoint = GetDumpJoint(joint);

            // Record initial position
            double initialAngle = GetJointCurrentPosition(dumpJoint);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] Initial dump angle: {initialAngle:F6}");

            // Set a target angle 0.3 rad above current
            double targetAngle = initialAngle + 0.3;

            // Get the RotDumpSubscriber and set up a position command
            object subscriber = GetRotDumpSubscriber(input);
            Assert.That(subscriber, Is.Not.Null,
                "RotDumpSubscriber not assigned on DumpTruckInput.");

            // Create JointCmdMsg with control_type=Position
            string[] jointNames = { "dump_joint" };
            double[] positions = { targetAngle };
            double[] velocities = { 0.0 };
            double[] efforts = { 0.0 };
            object dumpCmd = CreateJointCmdMsg(
                PositionControl, jointNames, positions, velocities, efforts);

            SetDumpCmd(subscriber, dumpCmd);

            Debug.Log(
                $"[DumpTruckControlPlayModeTests] Target angle: {targetAngle:F6}, " +
                $"control_type=Position");

            // Wait for the automatic SetCommands() loop to drive the joint.
            // The DumpTruckJoint.OnPreStepForward → RequestCommands →
            // DumpTruckInput.SetCommands() runs each physics step.
            // computeAngularVelocity returns w_up (0.5 rad/s) until |target - current| < _eps.
            // 0.3 rad / 0.5 rad/s = 0.6 s minimum, but allow generous timeout.
            float timeout = 10.0f;
            float elapsed = 0.0f;
            double currentAngle = initialAngle;
            const double eps = 0.01; // 0.01 rad tolerance (larger than _eps=0.0087)

            while (elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                currentAngle = GetJointCurrentPosition(dumpJoint);
                Debug.Log(
                    $"[DumpTruckControlPlayModeTests] t={elapsed:F1}s, " +
                    $"angle={currentAngle:F6}, target={targetAngle:F6}, " +
                    $"err={System.Math.Abs(currentAngle - targetAngle):F6}");

                if (System.Math.Abs(currentAngle - targetAngle) < eps)
                    break;
            }

            Assert.That(currentAngle, Is.EqualTo(targetAngle).Within(eps),
                $"Dump joint should converge to target angle {targetAngle:F6} " +
                $"within {eps} rad tolerance after receiving a ROS position command. " +
                $"Final angle: {currentAngle:F6}. " +
                "This validates the GetDumpControlType → ApplyDumpCommand(Position) → " +
                "computeAngularVelocity → Speed conversion pipeline.");

            // Stop simulation
            _actionModeField.SetValue(null, ActionModeIdle);
            yield return null;
        }

        /// <summary>
        /// Verifies the dump up control under load from particle-to-solid
        /// conversion:
        ///
        ///   1. Start simulation
        ///   2. Spawn AGX soil particles over the merge zone
        ///   3. Wait for particles to be captured (soilMass > MinLoadSoilMass)
        ///   4. Record dump joint angle
        ///   5. Command dump up via ApplyManualDumpSpeed
        ///   6. Wait MoveTime, verify joint angle increased despite the load
        ///
        /// This proves that:
        ///   - The particle-to-solid conversion creates real mass on the
        ///     soilMassBody (linked to the container via LockJoint)
        ///   - The controlMaxForce (600000) is sufficient to lift the
        ///     loaded vessel
        ///   - The control loop (ApplyDumpCommand → ReapplyControlValue)
        ///     works correctly under physical load
        /// </summary>
        [UnityTest]
        public IEnumerator DumpUp_UnderLoad_FromParticleCapture()
        {
            // Start simulation
            _actionModeField.SetValue(null, ActionModeSimulation);
            yield return new WaitForSeconds(SettleTime);

            object input = FindDumpTruckInput();
            object joint = FindDumpTruckJoint();
            object dumpJoint = GetDumpJoint(joint);
            object dumpSoil = FindDumpSoil();

            // Verify DumpSoil is initialized
            Assert.That(GetIsRuntimeReady(dumpSoil), Is.True,
                "DumpSoil must be initialized before spawning particles.");

            // Record initial soilMass (should be 0)
            double soilMassBefore = GetSoilMass(dumpSoil);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] soilMass before spawn: {soilMassBefore}");

            // Spawn particles over the merge zone
            int spawned = SpawnParticlesOverMergeZone(dumpSoil, SpawnCount);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] Spawned {spawned}/{SpawnCount} " +
                $"particles over merge zone.");
            Assert.That(spawned, Is.GreaterThan(0),
                "Failed to spawn any soil particles.");

            // Wait for particles to be captured and converted to solid mass
            float captureTimeout = 10.0f;
            float captureElapsed = 0.0f;
            double soilMassAfter = 0.0;

            while (captureElapsed < captureTimeout)
            {
                yield return new WaitForSeconds(0.5f);
                captureElapsed += 0.5f;
                soilMassAfter = GetSoilMass(dumpSoil);
                Debug.Log(
                    $"[DumpTruckControlPlayModeTests] t={captureElapsed:F1}s, " +
                    $"soilMass={soilMassAfter:F3}");

                if (soilMassAfter >= MinLoadSoilMass)
                    break;
            }

            Assert.That(soilMassAfter, Is.GreaterThanOrEqualTo(MinLoadSoilMass),
                $"soilMass should reach >= {MinLoadSoilMass} kg after particles " +
                $"are captured. Got {soilMassAfter:F3}. " +
                "The particle-to-solid conversion pipeline may be broken, " +
                "or particles may not have fallen into the merge zone.");

            Debug.Log(
                $"[DumpTruckControlPlayModeTests] Load confirmed: soilMass={soilMassAfter:F3} kg");

            // Record dump joint angle under load (before commanding up)
            double loadedAngle = GetJointCurrentPosition(dumpJoint);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] Dump angle under load: {loadedAngle:F6}");

            // --- Command Dump Up under load ---
            // Use a moderate speed — the vessel must overcome gravity × soilMass.
            // With soilMass ~50-200 kg and controlMaxForce=600000, this should
            // be achievable. Use 0.3 rad/s (same as manual test).
            ApplyManualDumpSpeed(input, 0.3);
            yield return new WaitForSeconds(MoveTime);

            double liftedAngle = GetJointCurrentPosition(dumpJoint);
            Debug.Log(
                $"[DumpTruckControlPlayModeTests] Dump angle after lift: {liftedAngle:F6}");

            Assert.That(liftedAngle, Is.GreaterThan(loadedAngle),
                "Dump joint angle should increase after a positive speed command " +
                "even when the vessel is loaded with captured soil mass. " +
                $"soilMass={soilMassAfter:F3} kg, " +
                $"controlMaxForce should be sufficient (600000). " +
                "If the angle doesn't increase, the controlMaxForce may be " +
                "insufficient for the load, or the particle-to-solid mass " +
                "is not properly coupled to the container body.");

            // Stop the dump
            ApplyManualDumpSpeed(input, 0.0);
            yield return new WaitForSeconds(0.5f);

            // Stop simulation
            _actionModeField.SetValue(null, ActionModeIdle);
            yield return null;
        }
    }
}
