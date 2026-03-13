using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PWRISimulator.ROS
{
    public class BulldozerJointCycleTester : MonoBehaviour
    {
        [SerializeField] BulldozerInput target;
        [Header("Enabled tests")]
        [SerializeField] bool testLift = true;
        [SerializeField] bool testTilt = true;
        [SerializeField] bool testAngle = true;

        [Header("Lift commands")]
        [SerializeField] float upCommandRadians = -0.2f;
        [SerializeField] float downCommandRadians = 1.2f;

        [Header("Tilt commands")]
        [SerializeField] float tiltNegativeCommandRadians = -0.25f;
        [SerializeField] float tiltPositiveCommandRadians = 0.25f;

        [Header("Angle commands")]
        [SerializeField] float angleNegativeCommandRadians = -0.35f;
        [SerializeField] float anglePositiveCommandRadians = 0.35f;

        [Header("Timing")]
        [SerializeField] float holdSeconds = 2.0f;
        [SerializeField] int cycleCount = 2;
        [SerializeField] float sampleIntervalSeconds = 0.5f;

        private FieldInfo enabledDummyField;
        private FieldInfo emergencyStopField;
        private FieldInfo liftJointField;
        private FieldInfo tiltJointField;
        private FieldInfo angleJointField;
        private FieldInfo debugLogsField;
        private FieldInfo bladeHeightField;
        private FieldInfo bladeEdgeDifferenceField;
        private BulldozerJoints joints;

        private void Start()
        {
            if (target == null)
                target = FindObjectOfType<BulldozerInput>();

            if (target == null)
            {
                Debug.LogError($"[{nameof(BulldozerJointCycleTester)}] No {nameof(BulldozerInput)} found.", this);
                enabled = false;
                return;
            }

            enabledDummyField = typeof(BulldozerInput).GetField("enabledDummy", BindingFlags.Instance | BindingFlags.NonPublic);
            emergencyStopField = typeof(BulldozerInput).GetField("emergencyStop", BindingFlags.Instance | BindingFlags.NonPublic);
            liftJointField = typeof(BulldozerInput).GetField("lift_joint", BindingFlags.Instance | BindingFlags.NonPublic);
            tiltJointField = typeof(BulldozerInput).GetField("tilt_joint", BindingFlags.Instance | BindingFlags.NonPublic);
            angleJointField = typeof(BulldozerInput).GetField("angle_joint", BindingFlags.Instance | BindingFlags.NonPublic);
            debugLogsField = typeof(BulldozerInput).GetField("enableLiftLimitDebugLogs", BindingFlags.Instance | BindingFlags.NonPublic);
            bladeHeightField = typeof(BulldozerInput).GetField("currentBladeHeightMeters", BindingFlags.Instance | BindingFlags.NonPublic);
            bladeEdgeDifferenceField = typeof(BulldozerInput).GetField("currentBladeEdgeDifferenceMeters", BindingFlags.Instance | BindingFlags.NonPublic);
            joints = target.GetComponent<BulldozerJoints>();

            enabledDummyField?.SetValue(target, true);
            emergencyStopField?.SetValue(target, false);
            debugLogsField?.SetValue(target, true);

            StartCoroutine(RunTest());
        }

        private IEnumerator RunTest()
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Starting joint cycle test with {cycleCount} cycles.", this);

            if (testLift)
            {
                for (int cycle = 1; cycle <= cycleCount; cycle++)
                {
                    yield return HoldLiftCommand(cycle, "UP", upCommandRadians);
                    yield return HoldLiftCommand(cycle, "DOWN", downCommandRadians);
                }
            }

            if (testTilt)
            {
                for (int cycle = 1; cycle <= cycleCount; cycle++)
                {
                    yield return HoldTiltCommand(cycle, "NEG", tiltNegativeCommandRadians);
                    yield return HoldTiltCommand(cycle, "POS", tiltPositiveCommandRadians);
                }
            }

            if (testAngle)
            {
                for (int cycle = 1; cycle <= cycleCount; cycle++)
                {
                    yield return HoldAngleCommand(cycle, "NEG", angleNegativeCommandRadians);
                    yield return HoldAngleCommand(cycle, "POS", anglePositiveCommandRadians);
                }
            }

            SetAllCommands(0.0, 0.0, 0.0);
            enabledDummyField?.SetValue(target, false);
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Joint cycle test completed.", this);
        }

        private IEnumerator HoldLiftCommand(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Lift cycle {cycle} phase {phase}: command={command:F3} rad.", this);
            SetAllCommands(command, 0.0, 0.0);

            float elapsed = 0.0f;
            float nextSample = 0.0f;
            while (elapsed < holdSeconds)
            {
                if (elapsed >= nextSample)
                {
                    LogLiftSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            LogLiftSample(cycle, phase, command, holdSeconds);
        }

        private IEnumerator HoldTiltCommand(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Tilt cycle {cycle} phase {phase}: command={command:F3} rad.", this);
            SetAllCommands(0.0, command, 0.0);

            float elapsed = 0.0f;
            float nextSample = 0.0f;
            while (elapsed < holdSeconds)
            {
                if (elapsed >= nextSample)
                {
                    LogTiltSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            LogTiltSample(cycle, phase, command, holdSeconds);
        }

        private IEnumerator HoldAngleCommand(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Angle cycle {cycle} phase {phase}: command={command:F3} rad.", this);
            SetAllCommands(0.0, 0.0, command);

            float elapsed = 0.0f;
            float nextSample = 0.0f;
            while (elapsed < holdSeconds)
            {
                if (elapsed >= nextSample)
                {
                    LogAngleSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            LogAngleSample(cycle, phase, command, holdSeconds);
        }

        private void SetAllCommands(double liftCommand, double tiltCommand, double angleCommand)
        {
            liftJointField?.SetValue(target, liftCommand);
            tiltJointField?.SetValue(target, tiltCommand);
            angleJointField?.SetValue(target, angleCommand);
        }

        private void LogLiftSample(int cycle, string phase, double command, float elapsed)
        {
            float bladeHeight = bladeHeightField != null ? (float)bladeHeightField.GetValue(target) : float.NaN;
            double liftPosition = joints != null ? joints.bladeLift.CurrentPosition : double.NaN;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Lift sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, bladeHeight={bladeHeight:F4}, liftPosition={liftPosition:F4}", this);
        }

        private void LogTiltSample(int cycle, string phase, double command, float elapsed)
        {
            float bladeEdgeDifference = bladeEdgeDifferenceField != null ? (float)bladeEdgeDifferenceField.GetValue(target) : float.NaN;
            double tiltPosition = joints != null ? joints.bladeTilt.CurrentPosition : double.NaN;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Tilt sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, bladeEdgeDifference={bladeEdgeDifference:F4}, tiltPosition={tiltPosition:F4}", this);
        }

        private void LogAngleSample(int cycle, string phase, double command, float elapsed)
        {
            float bladeAngle = target != null && target.bladeAngleCylConv != null ? target.bladeAngleCylConv.currentLinkAngle : float.NaN;
            double angleLeftPosition = joints != null ? joints.bladeAngleLeft.CurrentPosition : double.NaN;
            double angleRightPosition = joints != null ? joints.bladeAngleRight.CurrentPosition : double.NaN;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Angle sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, bladeAngle={bladeAngle:F4}, angleLeftPosition={angleLeftPosition:F4}, angleRightPosition={angleRightPosition:F4}", this);
        }
    }
}
