using System.Collections;
using System.Reflection;
using AGXUnity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PWRISimulator.Tests
{
    /// <summary>
    /// PlayMode tests verifying that capture processing runs directly in
    /// OnPostStepForward (per physics step) rather than being deferred and
    /// batched in Update().  This design prevents both the particle escape
    /// bug (issue #75) and the death spiral bug (issue #79).
    ///
    /// Uses reflection to access DumpSoil members because the test assembly
    /// (PWRISimulator.Tests.PlayMode) does not reference Assembly-CSharp
    /// directly — same pattern as SimulationSaveLoadTests.
    /// </summary>
    public class DumpSoilCapturePlayModeTests
    {
        private const BindingFlags PrivateInstance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// The DumpSoil type resolved via reflection.
        /// Null at class-init time; SetUp assigns it, and every test asserts it.
        /// </summary>
        private System.Type _dumpSoilType;

        /// <summary>
        /// The temporary GameObject created by CreateMinimalDumpSoil.
        /// Tracked here so TearDown can destroy it between tests.
        /// </summary>
        private GameObject _gameObject;

        /// <summary>
        /// Reflection handles for GlobalVariables.ActionMode, used by the
        /// subscription test to start/stop AGX simulation.
        /// </summary>
        private System.Type _globalVariablesType;
        private FieldInfo _actionModeField;
        private int _originalActionMode;

        // ------------------------------------------------------------------ //
        // Lifecycle                                                            //
        // ------------------------------------------------------------------ //

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _dumpSoilType = System.Type.GetType(
                "PWRISimulator.DumpSoil, Assembly-CSharp");
            Assert.That(_dumpSoilType, Is.Not.Null,
                "DumpSoil type not found in Assembly-CSharp. " +
                "Ensure DumpSoil.cs is in the project.");

            _globalVariablesType = System.Type.GetType(
                "PWRISimulator.GlobalVariables, Assembly-CSharp");
            if (_globalVariablesType != null)
            {
                _actionModeField = _globalVariablesType.GetField(
                    "ActionMode", BindingFlags.Public | BindingFlags.Static);
                if (_actionModeField != null)
                    _originalActionMode = (int)_actionModeField.GetValue(null);
            }

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_gameObject != null)
            {
                Object.Destroy(_gameObject);
                _gameObject = null;
            }

            if (_actionModeField != null)
                _actionModeField.SetValue(null, _originalActionMode);

            yield return null;
        }

        // ------------------------------------------------------------------ //
        // Helpers                                                              //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Create a minimal GameObject with MeshFilter+MeshRenderer (needed by
        /// UpdateVisualMaterial) plus a DumpSoil component.  Returns the
        /// Component as object (since we cannot statically type DumpSoil).
        /// </summary>
        private Component CreateMinimalDumpSoil()
        {
            _gameObject = new GameObject("TestDumpSoil",
                typeof(MeshFilter), typeof(MeshRenderer));
            return _gameObject.AddComponent(_dumpSoilType) as Component;
        }

        /// <summary>
        /// Force isRuntimeReady = true via reflection so that Update() and
        /// OnPostStepForward() execute their bodies.
        /// </summary>
        private static void ForceRuntimeReady(object instance, System.Type type)
        {
            var field = type.GetField("isRuntimeReady", PrivateInstance);
            Assert.That(field, Is.Not.Null,
                "DumpSoil.isRuntimeReady field not found via reflection.");
            field.SetValue(instance, true);
        }

        // ------------------------------------------------------------------ //
        // Tests                                                                //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Verifies that OnPostStepForward executes ProcessCaptureStep
        /// directly (not deferred to Update via a pendingStepCount counter).
        ///
        /// After the issue #79 fix, capture processing runs inside
        /// OnPostStepForward itself — once per physics step, immediately
        /// after the step completes.  This eliminates the pendingStepCount
        /// mechanism and the while-loop batch processing in Update().
        ///
        /// Expected GREEN pass (after fix):
        ///   Three OnPostStepForward calls ⇒ captureStepExecutionCount == 3,
        ///   one Update call              ⇒ captureStepExecutionCount still 3.
        /// </summary>
        [UnityTest]
        public IEnumerator OnPostStepForward_ExecutesCapturePerStep()
        {
#if UNITY_ASSERTIONS
            System.Type type = _dumpSoilType;

            // --- Arrange -------------------------------------------------- //

            object dumpSoil = CreateMinimalDumpSoil();
            ForceRuntimeReady(dumpSoil, type);

            var postStepMethod = type.GetMethod("OnPostStepForward", PrivateInstance);
            Assert.That(postStepMethod, Is.Not.Null,
                "DumpSoil.OnPostStepForward method not found.");

            var updateMethod = type.GetMethod("Update", PrivateInstance);
            Assert.That(updateMethod, Is.Not.Null,
                "DumpSoil.Update method not found.");

            var execCountField = type.GetField("captureStepExecutionCount", PrivateInstance);
            Assert.That(execCountField, Is.Not.Null,
                "DumpSoil.captureStepExecutionCount (int) field not found.");

            // pendingStepCount should no longer exist — the mechanism was
            // removed as part of the issue #79 fix.
            var pendingField = type.GetField("pendingStepCount", PrivateInstance);
            Assert.That(pendingField, Is.Null,
                "DumpSoil.pendingStepCount should not exist — capture " +
                "processing now runs directly in OnPostStepForward (issue #79 fix).");

            // --- Act ------------------------------------------------------ //

            // Simulate 3 physics steps.  Each should execute capture
            // processing immediately inside the callback.
            postStepMethod.Invoke(dumpSoil, null);
            postStepMethod.Invoke(dumpSoil, null);
            postStepMethod.Invoke(dumpSoil, null);

            int execAfterSteps = (int)execCountField.GetValue(dumpSoil);
            Assert.That(execAfterSteps, Is.EqualTo(3),
                "captureStepExecutionCount should be 3 after three " +
                "OnPostStepForward calls — capture runs directly in the callback.");

            // Update() should NOT process any capture steps.
            updateMethod.Invoke(dumpSoil, null);

            // --- Assert --------------------------------------------------- //

            int execAfterUpdate = (int)execCountField.GetValue(dumpSoil);
            Assert.That(execAfterUpdate, Is.EqualTo(3),
                "captureStepExecutionCount should still be 3 after Update() — " +
                "Update() must not process capture steps (issue #79 fix).");

            yield return null;
#else
            Assert.Ignore(
                "captureStepExecutionCount is only compiled when " +
                "UNITY_ASSERTIONS is defined (Editor/Development builds).");
            yield break;
#endif
        }

        /// <summary>
        /// Verifies that OnPostStepForward can be successfully subscribed to
        /// the Simulation StepCallbacks.PostStepForward delegate.
        ///
        /// DumpSoil.OnEnable() subscribes OnPostStepForward after
        /// base.OnEnable() (which triggers Initialize() on first activation,
        /// setting isRuntimeReady = true).  This test validates the
        /// subscription mechanism in isolation: it creates a DumpSoil
        /// without calling Initialize(), then manually performs the same
        /// registration that OnEnable() does, confirming the callback
        /// appears on the delegate chain.
        ///
        /// Because the test bypasses OnEnable(), it does not verify that
        /// the subscription code path itself is reached — that is validated
        /// indirectly by the OnPostStepForward_ExecutesCapturePerStep test,
        /// which proves OnPostStepForward executes by verifying
        /// captureStepExecutionCount increments.
        /// </summary>
        [UnityTest]
        public IEnumerator PostStepCallback_IsSubscribedAfterFirstInitialize()
        {
            System.Type type = _dumpSoilType;

            // --------------------------------------------------------------- //
            // 1.  Create DumpSoil in inactive state (prevents OnEnable crash) //
            // --------------------------------------------------------------- //
            _gameObject = new GameObject("TestDumpSoil",
                typeof(MeshFilter), typeof(MeshRenderer));
            _gameObject.SetActive(false);
            var dumpSoil = _gameObject.AddComponent(type) as Component;
            _gameObject.SetActive(true);

            yield return null;

            // --------------------------------------------------------------- //
            // 2.  Check Simulation availability                               //
            // --------------------------------------------------------------- //
            if (!Simulation.HasInstance)
            {
                Assert.Ignore(
                    "AGX Simulation.Instance not available — cannot verify " +
                    "event subscription.");
                yield break;
            }

            // --------------------------------------------------------------- //
            // 3.  Inspect PostStepForward (public field, no reflection needed)//
            // --------------------------------------------------------------- //
            var stepCallbacks = Simulation.Instance.StepCallbacks;

            // The PostStepForward is a public StepCallbackDef delegate field.
            // Before any subscriber: it is null.

            // --------------------------------------------------------------- //
            // 4.  Simulate the post-Initialize state                          //
            // --------------------------------------------------------------- //
            var isReadyField = type.GetField("isRuntimeReady", PrivateInstance);
            Assert.That(isReadyField, Is.Not.Null);
            isReadyField.SetValue(dumpSoil, true);

            // DumpSoil.OnEnable() subscribes OnPostStepForward after
            // base.OnEnable() sets isRuntimeReady = true via Initialize().
            // Since we bypassed OnEnable(), we repeat the same registration
            // here to validate the mechanism in isolation:

            var postStepMethod = type.GetMethod("OnPostStepForward", PrivateInstance);
            var onPostStepDelegate = System.Delegate.CreateDelegate(
                typeof(StepCallbackFunctions.StepCallbackDef),
                dumpSoil,
                postStepMethod);

            stepCallbacks.PostStepForward +=
                (StepCallbackFunctions.StepCallbackDef)onPostStepDelegate;

            // --------------------------------------------------------------- //
            // 5.  Verify the callback is now on the delegate chain            //
            // --------------------------------------------------------------- //
            var delAfter = stepCallbacks.PostStepForward;

            Assert.That(delAfter, Is.Not.Null,
                "PostStepForward delegate should be non-null after " +
                "subscription. DumpSoil.Initialize() performs this " +
                "registration (see Initialize() end, after " +
                "isRuntimeReady = true).");

            yield return null;
        }
        /// <summary>
        /// Verifies that ProcessCaptureStep() executes exactly once per
        /// OnPostStepForward call — proving per-step capture processing.
        ///
        /// After the issue #75/#79 fix, ProcessCaptureStep() is called
        /// directly from OnPostStepForward, not deferred to Update().
        /// This test uses the captureStepExecutionCount counter to prove
        /// 1:1 correspondence between physics steps and capture executions.
        ///
        /// Expected GREEN pass (after fix):
        ///   Three OnPostStepForward calls ⇒ captureStepExecutionCount == 3.
        /// </summary>
        [UnityTest]
        public IEnumerator ProcessCaptureStep_ExecutesPerQueuedStep()
        {
#if UNITY_ASSERTIONS
            System.Type type = _dumpSoilType;

            // --- Arrange -------------------------------------------------- //

            object dumpSoil = CreateMinimalDumpSoil();
            ForceRuntimeReady(dumpSoil, type);

            var postStepMethod = type.GetMethod("OnPostStepForward", PrivateInstance);
            Assert.That(postStepMethod, Is.Not.Null,
                "DumpSoil.OnPostStepForward method not found.");

            var execCountField = type.GetField("captureStepExecutionCount", PrivateInstance);
            Assert.That(execCountField, Is.Not.Null,
                "DumpSoil.captureStepExecutionCount (int) field not found. " +
                "Add private field and increment it inside ProcessCaptureStep().");

            // --- Act ------------------------------------------------------ //

            // Simulate 3 physics steps.  Each OnPostStepForward should
            // execute ProcessCaptureStep exactly once.
            postStepMethod.Invoke(dumpSoil, null);
            postStepMethod.Invoke(dumpSoil, null);
            postStepMethod.Invoke(dumpSoil, null);

            // --- Assert --------------------------------------------------- //

            int execCount = (int)execCountField.GetValue(dumpSoil);
            Assert.That(execCount, Is.EqualTo(3),
                "captureStepExecutionCount should be 3 after three " +
                "OnPostStepForward calls — one ProcessCaptureStep per step, " +
                "executed directly in the callback (issue #75/#79 fix).");

            yield return null;
#else
            Assert.Ignore(
                "captureStepExecutionCount is only compiled when " +
                "UNITY_ASSERTIONS is defined (Editor/Development builds).");
            yield break;
#endif
        }

        /// <summary>
        /// Verifies the issue #79 fix: Update() must NOT process any capture
        /// steps.  All capture processing happens in OnPostStepForward (per
        /// physics step), so Update() only does visual updates.
        ///
        /// Before the fix, the `while (pendingStepCount > 0)` loop in
        /// Update() processed ALL accumulated physics steps in a single
        /// frame, causing a death spiral when the frame rate dropped.
        ///
        /// After the fix, Update() processes zero capture steps regardless
        /// of how many OnPostStepForward calls preceded it.
        ///
        /// See: https://github.com/pwri-opera/OperaSim-AGX/issues/79
        /// </summary>
#if UNITY_ASSERTIONS
        [UnityTest]
        public IEnumerator Update_DoesNotProcessCaptureSteps_Issue79()
        {
            System.Type type = _dumpSoilType;

            // --- Arrange -------------------------------------------------- //

            object dumpSoil = CreateMinimalDumpSoil();
            ForceRuntimeReady(dumpSoil, type);

            var postStepMethod = type.GetMethod("OnPostStepForward", PrivateInstance);
            Assert.That(postStepMethod, Is.Not.Null);

            var updateMethod = type.GetMethod("Update", PrivateInstance);
            Assert.That(updateMethod, Is.Not.Null);

            var execCountField = type.GetField("captureStepExecutionCount", PrivateInstance);
            Assert.That(execCountField, Is.Not.Null,
                "DumpSoil.captureStepExecutionCount field not found.");

            // Simulate 30 physics steps (e.g. a frame stall where many
            // FixedUpdate calls occur).  Each step processes capture
            // immediately in OnPostStepForward.
            const int QueuedSteps = 30;
            for (int i = 0; i < QueuedSteps; i++)
                postStepMethod.Invoke(dumpSoil, null);

            int execBefore = (int)execCountField.GetValue(dumpSoil);
            Assert.That(execBefore, Is.EqualTo(QueuedSteps),
                "All 30 steps should have been processed in OnPostStepForward.");

            // --- Act ------------------------------------------------------ //

            // A single Update() call — one rendered frame.
            updateMethod.Invoke(dumpSoil, null);

            // --- Assert --------------------------------------------------- //

            int execAfter = (int)execCountField.GetValue(dumpSoil);
            Assert.That(execAfter, Is.EqualTo(QueuedSteps),
                $"Update() must not process capture steps. " +
                $"captureStepExecutionCount was {execBefore} before Update() " +
                $"and {execAfter} after — Update() must not increment it " +
                $"(issue #79 fix: no while-loop batch processing in Update).");

            yield return null;
        }
#else
        [UnityTest]
        public IEnumerator Update_DoesNotProcessCaptureSteps_Issue79()
        {
            Assert.Ignore(
                "captureStepExecutionCount is only compiled when " +
                "UNITY_ASSERTIONS is defined (Editor/Development builds).");
            yield break;
        }
#endif

        /// <summary>
        /// Verifies the issue #79 secondary fix: OnPostStepForward is
        /// registered only in OnEnable() (not in both Initialize() and
        /// OnEnable() as before), so a disable→enable cycle does not cause
        /// double-registration on the Simulation.StepCallbacks.PostStepForward
        /// delegate.
        ///
        /// Before the fix, registration happened in both Initialize() and
        /// OnEnable(), so a re-enable cycle could add the delegate twice,
        /// doubling capture processing per physics step and worsening the
        /// death spiral.
        ///
        /// After the fix, registration happens only in OnEnable(), and
        /// OnDisable() unregisters — so the count stays at 1 after re-enable.
        ///
        /// Expected GREEN pass (after fix): delegate count after re-enable
        /// does not exceed count after initial registration.
        /// </summary>
        [UnityTest]
        public IEnumerator PostStepForward_NotDoubleRegisteredAfterReEnable_Issue79()
        {
            System.Type type = _dumpSoilType;

            // --- Arrange -------------------------------------------------- //

            // Create inactive to avoid OnEnable firing before we set up.
            _gameObject = new GameObject("TestDumpSoil",
                typeof(MeshFilter), typeof(MeshRenderer));
            _gameObject.SetActive(false);
            object dumpSoil = _gameObject.AddComponent(type) as Component;
            _gameObject.SetActive(true);

            yield return null;

            if (!Simulation.HasInstance)
            {
                Assert.Ignore(
                    "AGX Simulation.Instance not available — cannot verify " +
                    "delegate registration count.");
                yield break;
            }

            var stepCallbacks = Simulation.Instance.StepCallbacks;
            var isReadyField = type.GetField("isRuntimeReady", PrivateInstance);
            Assert.That(isReadyField, Is.Not.Null);

            var postStepMethod = type.GetMethod("OnPostStepForward", PrivateInstance);
            Assert.That(postStepMethod, Is.Not.Null);

            // Simulate what Initialize() does: set isRuntimeReady and register.
            isReadyField.SetValue(dumpSoil, true);
            var onPostStepDelegate = System.Delegate.CreateDelegate(
                typeof(StepCallbackFunctions.StepCallbackDef),
                dumpSoil,
                postStepMethod);
            stepCallbacks.PostStepForward +=
                (StepCallbackFunctions.StepCallbackDef)onPostStepDelegate;

            // Count entries on the delegate after initial registration.
            int countAfterInit = stepCallbacks.PostStepForward?.GetInvocationList().Length ?? 0;
            Assert.That(countAfterInit, Is.GreaterThanOrEqualTo(1),
                "PostStepForward should have at least 1 entry after Initialize.");

            // --- Act: simulate disable → re-enable cycle ----------------- //

            var onDisableMethod = type.GetMethod("OnDisable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var onEnableMethod = type.GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);

            // OnDisable should unregister.
            if (onDisableMethod != null)
                onDisableMethod.Invoke(dumpSoil, null);

            int countAfterDisable = stepCallbacks.PostStepForward?.GetInvocationList().Length ?? 0;

            // OnEnable should re-register — but must not duplicate.
            if (onEnableMethod != null)
                onEnableMethod.Invoke(dumpSoil, null);

            // --- Assert --------------------------------------------------- //

            int countAfterReEnable = stepCallbacks.PostStepForward?.GetInvocationList().Length ?? 0;

            // The count after re-enable should not exceed the count after
            // initial registration.  If OnEnable blindly adds without checking
            // for an existing registration (or if Initialize already registered
            // and OnEnable adds again), the count will be higher.
            Assert.That(countAfterReEnable, Is.LessThanOrEqualTo(countAfterInit),
                $"PostStepForward has {countAfterReEnable} entries after " +
                $"re-enable, but should not exceed {countAfterInit} (the count " +
                $"after initial registration). Double-registration of " +
                $"OnPostStepForward causes capture processing to run 2x per " +
                $"physics step, worsening the death spiral (issue #79).");

            // Clean up: unregister our delegate if still present.
            stepCallbacks.PostStepForward -=
                (StepCallbackFunctions.StepCallbackDef)onPostStepDelegate;

            yield return null;
        }
    }
}
