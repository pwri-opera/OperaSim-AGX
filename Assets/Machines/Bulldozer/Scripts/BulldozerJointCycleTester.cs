using System.Collections;
using System.Reflection;
using UnityEngine;
using PWRISimulator;

namespace PWRISimulator.ROS
{
    public class BulldozerJointCycleTester : MonoBehaviour
    {
        private enum TestControlMode
        {
            Position,
            Speed
        }

        [SerializeField] BulldozerInput target;
        [SerializeField] TestControlMode controlMode = TestControlMode.Position;

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

        [Header("Lift speed commands")]
        [SerializeField] float upVelocityRadiansPerSec = -0.35f;
        [SerializeField] float downVelocityRadiansPerSec = 0.35f;

        [Header("Tilt speed commands")]
        [SerializeField] float tiltNegativeVelocityRadiansPerSec = -0.25f;
        [SerializeField] float tiltPositiveVelocityRadiansPerSec = 0.25f;

        [Header("Angle speed commands")]
        [SerializeField] float angleNegativeVelocityRadiansPerSec = -0.35f;
        [SerializeField] float anglePositiveVelocityRadiansPerSec = 0.35f;

        [Header("Timing")]
        [SerializeField] float holdSeconds = 2.0f;
        [SerializeField] int cycleCount = 2;
        [SerializeField] float sampleIntervalSeconds = 0.5f;

        [Header("Limit check")]
        [SerializeField] float liftLimitToleranceMeters = 0.03f;
        [SerializeField] float tiltLimitToleranceMeters = 0.03f;
        [SerializeField] float angleLimitToleranceRadians = 0.03f;

        private FieldInfo enabledDummyField;
        private FieldInfo emergencyStopField;
        private FieldInfo controlTypeField;
        private FieldInfo liftJointField;
        private FieldInfo tiltJointField;
        private FieldInfo angleJointField;
        private FieldInfo debugLogsField;
        private FieldInfo bladeHeightField;
        private FieldInfo bladeEdgeDifferenceField;
        private BulldozerJoints joints;
        private ControlType originalControlType;
        private bool originalControlTypeCaptured;
        private float bladeHeightUpperLimitMeters;
        private float bladeHeightLowerLimitMeters;
        private float bladeEdgeDifferenceLimitMeters;
        private float bladeAngleLimitRadians;

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
            controlTypeField = typeof(BulldozerInput).GetField("controlType", BindingFlags.Instance | BindingFlags.NonPublic);
            liftJointField = typeof(BulldozerInput).GetField("lift_joint", BindingFlags.Instance | BindingFlags.NonPublic);
            tiltJointField = typeof(BulldozerInput).GetField("tilt_joint", BindingFlags.Instance | BindingFlags.NonPublic);
            angleJointField = typeof(BulldozerInput).GetField("angle_joint", BindingFlags.Instance | BindingFlags.NonPublic);
            debugLogsField = typeof(BulldozerInput).GetField("enableLiftLimitDebugLogs", BindingFlags.Instance | BindingFlags.NonPublic);
            bladeHeightField = typeof(BulldozerInput).GetField("currentBladeHeightMeters", BindingFlags.Instance | BindingFlags.NonPublic);
            bladeEdgeDifferenceField = typeof(BulldozerInput).GetField("currentBladeEdgeDifferenceMeters", BindingFlags.Instance | BindingFlags.NonPublic);
            joints = target.GetComponent<BulldozerJoints>();

            if (controlTypeField != null)
            {
                originalControlType = (ControlType)controlTypeField.GetValue(target);
                originalControlTypeCaptured = true;
            }

            bladeHeightUpperLimitMeters = GetPrivateFloat("bladeEdgeHeightUpperLimitMeters", 0.8f);
            bladeHeightLowerLimitMeters = GetPrivateFloat("bladeEdgeHeightLowerLimitMeters", -0.38f);
            bladeEdgeDifferenceLimitMeters = GetPrivateFloat("bladeEdgeEndHeightDifferenceLimitMeters", 0.435f);
            bladeAngleLimitRadians = Mathf.Deg2Rad * GetPrivateFloat("bladeAngleLimitDegrees", 24.0f);

            enabledDummyField?.SetValue(target, true);
            emergencyStopField?.SetValue(target, false);
            debugLogsField?.SetValue(target, true);
            SetControlMode();

            StartCoroutine(RunTest());
        }

        private void OnDisable()
        {
            RestoreState();
        }

        private IEnumerator RunTest()
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Starting {controlMode} joint cycle test with {cycleCount} cycles.", this);

            if (testLift)
            {
                for (int cycle = 1; cycle <= cycleCount; cycle++)
                {
                    if (controlMode == TestControlMode.Position)
                    {
                        yield return HoldLiftCommand(cycle, "UP", upCommandRadians);
                        yield return HoldLiftCommand(cycle, "DOWN", downCommandRadians);
                    }
                    else
                    {
                        yield return HoldLiftVelocity(cycle, "UP", upVelocityRadiansPerSec);
                        yield return HoldLiftVelocity(cycle, "DOWN", downVelocityRadiansPerSec);
                    }
                }
            }

            if (testTilt)
            {
                for (int cycle = 1; cycle <= cycleCount; cycle++)
                {
                    if (controlMode == TestControlMode.Position)
                    {
                        yield return HoldTiltCommand(cycle, "NEG", tiltNegativeCommandRadians);
                        yield return HoldTiltCommand(cycle, "POS", tiltPositiveCommandRadians);
                    }
                    else
                    {
                        yield return HoldTiltVelocity(cycle, "NEG", tiltNegativeVelocityRadiansPerSec);
                        yield return HoldTiltVelocity(cycle, "POS", tiltPositiveVelocityRadiansPerSec);
                    }
                }
            }

            if (testAngle)
            {
                for (int cycle = 1; cycle <= cycleCount; cycle++)
                {
                    if (controlMode == TestControlMode.Position)
                    {
                        yield return HoldAngleCommand(cycle, "NEG", angleNegativeCommandRadians);
                        yield return HoldAngleCommand(cycle, "POS", anglePositiveCommandRadians);
                    }
                    else
                    {
                        yield return HoldAngleVelocity(cycle, "NEG", angleNegativeVelocityRadiansPerSec);
                        yield return HoldAngleVelocity(cycle, "POS", anglePositiveVelocityRadiansPerSec);
                    }
                }
            }

            RestoreState();
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

        private IEnumerator HoldLiftVelocity(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Lift speed cycle {cycle} phase {phase}: velocity={command:F3} rad/s.", this);
            SetAllCommands(command, 0.0, 0.0);

            float minBladeHeight = float.PositiveInfinity;
            float maxBladeHeight = float.NegativeInfinity;
            float elapsed = 0.0f;
            float nextSample = 0.0f;
            while (elapsed < holdSeconds)
            {
                float bladeHeight = GetBladeHeight();
                minBladeHeight = Mathf.Min(minBladeHeight, bladeHeight);
                maxBladeHeight = Mathf.Max(maxBladeHeight, bladeHeight);

                if (elapsed >= nextSample)
                {
                    LogLiftSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            float finalBladeHeight = GetBladeHeight();
            minBladeHeight = Mathf.Min(minBladeHeight, finalBladeHeight);
            maxBladeHeight = Mathf.Max(maxBladeHeight, finalBladeHeight);
            LogLiftSample(cycle, phase, command, holdSeconds);
            LogLiftSpeedLimitSummary(cycle, phase, command, minBladeHeight, maxBladeHeight, finalBladeHeight);
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

        private IEnumerator HoldTiltVelocity(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Tilt speed cycle {cycle} phase {phase}: velocity={command:F3} rad/s.", this);
            SetAllCommands(0.0, command, 0.0);

            float minBladeEdgeDifference = float.PositiveInfinity;
            float maxBladeEdgeDifference = float.NegativeInfinity;
            float elapsed = 0.0f;
            float nextSample = 0.0f;
            while (elapsed < holdSeconds)
            {
                float bladeEdgeDifference = GetBladeEdgeDifference();
                minBladeEdgeDifference = Mathf.Min(minBladeEdgeDifference, bladeEdgeDifference);
                maxBladeEdgeDifference = Mathf.Max(maxBladeEdgeDifference, bladeEdgeDifference);

                if (elapsed >= nextSample)
                {
                    LogTiltSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            float finalBladeEdgeDifference = GetBladeEdgeDifference();
            minBladeEdgeDifference = Mathf.Min(minBladeEdgeDifference, finalBladeEdgeDifference);
            maxBladeEdgeDifference = Mathf.Max(maxBladeEdgeDifference, finalBladeEdgeDifference);
            LogTiltSample(cycle, phase, command, holdSeconds);
            LogTiltSpeedLimitSummary(cycle, phase, command, minBladeEdgeDifference, maxBladeEdgeDifference, finalBladeEdgeDifference);
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

        private IEnumerator HoldAngleVelocity(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Angle speed cycle {cycle} phase {phase}: velocity={command:F3} rad/s.", this);
            SetAllCommands(0.0, 0.0, command);

            float minBladeAngle = float.PositiveInfinity;
            float maxBladeAngle = float.NegativeInfinity;
            float elapsed = 0.0f;
            float nextSample = 0.0f;
            while (elapsed < holdSeconds)
            {
                float bladeAngle = GetBladeAngle();
                minBladeAngle = Mathf.Min(minBladeAngle, bladeAngle);
                maxBladeAngle = Mathf.Max(maxBladeAngle, bladeAngle);

                if (elapsed >= nextSample)
                {
                    LogAngleSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            float finalBladeAngle = GetBladeAngle();
            minBladeAngle = Mathf.Min(minBladeAngle, finalBladeAngle);
            maxBladeAngle = Mathf.Max(maxBladeAngle, finalBladeAngle);
            LogAngleSample(cycle, phase, command, holdSeconds);
            LogAngleSpeedLimitSummary(cycle, phase, command, minBladeAngle, maxBladeAngle, finalBladeAngle);
        }

        private void SetAllCommands(double liftCommand, double tiltCommand, double angleCommand)
        {
            liftJointField?.SetValue(target, liftCommand);
            tiltJointField?.SetValue(target, tiltCommand);
            angleJointField?.SetValue(target, angleCommand);
        }

        private void SetControlMode()
        {
            if (controlTypeField == null)
                return;

            ControlType requestedMode = controlMode == TestControlMode.Speed ? ControlType.Speed : ControlType.Position;
            controlTypeField.SetValue(target, requestedMode);
        }

        private void RestoreState()
        {
            if (target == null)
                return;

            SetAllCommands(0.0, 0.0, 0.0);
            enabledDummyField?.SetValue(target, false);
            if (originalControlTypeCaptured)
                controlTypeField?.SetValue(target, originalControlType);
        }

        private float GetPrivateFloat(string fieldName, float fallbackValue)
        {
            FieldInfo field = typeof(BulldozerInput).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                return fallbackValue;

            return (float)field.GetValue(target);
        }

        private float GetBladeHeight()
        {
            return bladeHeightField != null ? (float)bladeHeightField.GetValue(target) : float.NaN;
        }

        private float GetBladeEdgeDifference()
        {
            return bladeEdgeDifferenceField != null ? (float)bladeEdgeDifferenceField.GetValue(target) : float.NaN;
        }

        private float GetBladeAngle()
        {
            return target != null && target.bladeAngleCylConv != null ? target.bladeAngleCylConv.currentLinkAngle : float.NaN;
        }

        private void LogLiftSpeedLimitSummary(int cycle, string phase, double command, float minBladeHeight, float maxBladeHeight, float finalBladeHeight)
        {
            bool lowerLimitReached = minBladeHeight <= bladeHeightLowerLimitMeters + liftLimitToleranceMeters;
            bool upperLimitReached = maxBladeHeight >= bladeHeightUpperLimitMeters - liftLimitToleranceMeters;
            bool stayedWithinRange = minBladeHeight >= bladeHeightLowerLimitMeters - liftLimitToleranceMeters &&
                maxBladeHeight <= bladeHeightUpperLimitMeters + liftLimitToleranceMeters;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Lift speed summary cycle={cycle}, phase={phase}, velocity={command:F3}, minHeight={minBladeHeight:F4}, maxHeight={maxBladeHeight:F4}, finalHeight={finalBladeHeight:F4}, lowerLimit={bladeHeightLowerLimitMeters:F4}, upperLimit={bladeHeightUpperLimitMeters:F4}, reachedLower={lowerLimitReached}, reachedUpper={upperLimitReached}, stayedWithinRange={stayedWithinRange}", this);
        }

        private void LogTiltSpeedLimitSummary(int cycle, string phase, double command, float minBladeEdgeDifference, float maxBladeEdgeDifference, float finalBladeEdgeDifference)
        {
            bool positiveLimitReached = maxBladeEdgeDifference >= bladeEdgeDifferenceLimitMeters - tiltLimitToleranceMeters;
            bool negativeLimitReached = minBladeEdgeDifference <= -bladeEdgeDifferenceLimitMeters + tiltLimitToleranceMeters;
            bool stayedWithinRange = minBladeEdgeDifference >= -bladeEdgeDifferenceLimitMeters - tiltLimitToleranceMeters &&
                maxBladeEdgeDifference <= bladeEdgeDifferenceLimitMeters + tiltLimitToleranceMeters;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Tilt speed summary cycle={cycle}, phase={phase}, velocity={command:F3}, minDifference={minBladeEdgeDifference:F4}, maxDifference={maxBladeEdgeDifference:F4}, finalDifference={finalBladeEdgeDifference:F4}, limit={bladeEdgeDifferenceLimitMeters:F4}, reachedNegative={negativeLimitReached}, reachedPositive={positiveLimitReached}, stayedWithinRange={stayedWithinRange}", this);
        }

        private void LogAngleSpeedLimitSummary(int cycle, string phase, double command, float minBladeAngle, float maxBladeAngle, float finalBladeAngle)
        {
            bool positiveLimitReached = maxBladeAngle >= bladeAngleLimitRadians - angleLimitToleranceRadians;
            bool negativeLimitReached = minBladeAngle <= -bladeAngleLimitRadians + angleLimitToleranceRadians;
            bool stayedWithinRange = minBladeAngle >= -bladeAngleLimitRadians - angleLimitToleranceRadians &&
                maxBladeAngle <= bladeAngleLimitRadians + angleLimitToleranceRadians;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Angle speed summary cycle={cycle}, phase={phase}, velocity={command:F3}, minAngle={minBladeAngle:F4}, maxAngle={maxBladeAngle:F4}, finalAngle={finalBladeAngle:F4}, limit={bladeAngleLimitRadians:F4}, reachedNegative={negativeLimitReached}, reachedPositive={positiveLimitReached}, stayedWithinRange={stayedWithinRange}", this);
        }

        private void LogLiftSample(int cycle, string phase, double command, float elapsed)
        {
            float bladeHeight = GetBladeHeight();
            double liftPosition = joints != null ? joints.bladeLift.CurrentPosition : double.NaN;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Lift sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, bladeHeight={bladeHeight:F4}, liftPosition={liftPosition:F4}", this);
        }

        private void LogTiltSample(int cycle, string phase, double command, float elapsed)
        {
            float bladeEdgeDifference = GetBladeEdgeDifference();
            double tiltPosition = joints != null ? joints.bladeTilt.CurrentPosition : double.NaN;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Tilt sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, bladeEdgeDifference={bladeEdgeDifference:F4}, tiltPosition={tiltPosition:F4}", this);
        }

        private void LogAngleSample(int cycle, string phase, double command, float elapsed)
        {
            float bladeAngle = GetBladeAngle();
            double angleLeftPosition = joints != null ? joints.bladeAngleLeft.CurrentPosition : double.NaN;
            double angleRightPosition = joints != null ? joints.bladeAngleRight.CurrentPosition : double.NaN;
            Debug.Log($"[{nameof(BulldozerJointCycleTester)}] Angle sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, bladeAngle={bladeAngle:F4}, angleLeftPosition={angleLeftPosition:F4}, angleRightPosition={angleRightPosition:F4}", this);
        }
    }
}
