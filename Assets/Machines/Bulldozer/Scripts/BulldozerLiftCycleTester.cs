using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PWRISimulator.ROS
{
    public class BulldozerLiftCycleTester : MonoBehaviour
    {
        [SerializeField] BulldozerInput target;
        [SerializeField] float upCommandRadians = -0.2f;
        [SerializeField] float downCommandRadians = 1.2f;
        [SerializeField] float holdSeconds = 2.0f;
        [SerializeField] int cycleCount = 2;
        [SerializeField] float sampleIntervalSeconds = 0.5f;

        private FieldInfo enabledDummyField;
        private FieldInfo emergencyStopField;
        private FieldInfo liftJointField;
        private FieldInfo debugLogsField;
        private FieldInfo bladeHeightField;
        private BulldozerJoints joints;

        private void Start()
        {
            if (target == null)
                target = FindObjectOfType<BulldozerInput>();

            if (target == null)
            {
                Debug.LogError($"[{nameof(BulldozerLiftCycleTester)}] No {nameof(BulldozerInput)} found.", this);
                enabled = false;
                return;
            }

            enabledDummyField = typeof(BulldozerInput).GetField("enabledDummy", BindingFlags.Instance | BindingFlags.NonPublic);
            emergencyStopField = typeof(BulldozerInput).GetField("emergencyStop", BindingFlags.Instance | BindingFlags.NonPublic);
            liftJointField = typeof(BulldozerInput).GetField("lift_joint", BindingFlags.Instance | BindingFlags.NonPublic);
            debugLogsField = typeof(BulldozerInput).GetField("enableLiftLimitDebugLogs", BindingFlags.Instance | BindingFlags.NonPublic);
            bladeHeightField = typeof(BulldozerInput).GetField("currentBladeHeightMeters", BindingFlags.Instance | BindingFlags.NonPublic);
            joints = target.GetComponent<BulldozerJoints>();

            enabledDummyField?.SetValue(target, true);
            emergencyStopField?.SetValue(target, false);
            debugLogsField?.SetValue(target, true);

            StartCoroutine(RunTest());
        }

        private IEnumerator RunTest()
        {
            Debug.Log($"[{nameof(BulldozerLiftCycleTester)}] Starting lift cycle test with {cycleCount} cycles.", this);

            for (int cycle = 1; cycle <= cycleCount; cycle++)
            {
                yield return HoldCommand(cycle, "UP", upCommandRadians);
                yield return HoldCommand(cycle, "DOWN", downCommandRadians);
            }

            SetLiftCommand(0.0);
            enabledDummyField?.SetValue(target, false);
            Debug.Log($"[{nameof(BulldozerLiftCycleTester)}] Lift cycle test completed.", this);
        }

        private IEnumerator HoldCommand(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerLiftCycleTester)}] Cycle {cycle} phase {phase}: command={command:F3} rad.", this);
            SetLiftCommand(command);

            float elapsed = 0.0f;
            float nextSample = 0.0f;
            while (elapsed < holdSeconds)
            {
                if (elapsed >= nextSample)
                {
                    LogSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            LogSample(cycle, phase, command, holdSeconds);
        }

        private void SetLiftCommand(double command)
        {
            liftJointField?.SetValue(target, command);
        }

        private void LogSample(int cycle, string phase, double command, float elapsed)
        {
            float bladeHeight = bladeHeightField != null ? (float)bladeHeightField.GetValue(target) : float.NaN;
            double liftPosition = joints != null ? joints.bladeLift.CurrentPosition : double.NaN;
            Debug.Log($"[{nameof(BulldozerLiftCycleTester)}] Sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, bladeHeight={bladeHeight:F4}, liftPosition={liftPosition:F4}", this);
        }
    }
}
