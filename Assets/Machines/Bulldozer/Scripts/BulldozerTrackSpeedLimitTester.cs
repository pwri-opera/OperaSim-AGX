using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using PWRISimulator;

namespace PWRISimulator.ROS
{
    public class BulldozerTrackSpeedLimitTester : MonoBehaviour
    {
        [SerializeField] BulldozerInput target;
        [SerializeField] float trackSpeedLimitKph = 8.5f;
        [SerializeField] float forwardSpeedRadiansPerSec = 1.0f;
        [SerializeField] float reverseSpeedRadiansPerSec = -1.0f;
        [SerializeField] float holdSeconds = 2.0f;
        [SerializeField] float sampleIntervalSeconds = 0.5f;
        [SerializeField] int cycleCount = 2;

        private FieldInfo enabledDummyField;
        private FieldInfo controlTypeField;
        private FieldInfo movementControlTypeField;
        private FieldInfo leftTrackField;
        private FieldInfo rightTrackField;
        private BulldozerJoints joints;
        private ControlType originalControlType;
        private bool originalControlTypeCaptured;
        private ConstractionMovementControlType originalMovementControlType;
        private bool originalMovementControlTypeCaptured;

        private void Start()
        {
            if (target == null)
                target = FindObjectOfType<BulldozerInput>();

            if (target == null)
            {
                Debug.LogError($"[{nameof(BulldozerTrackSpeedLimitTester)}] No {nameof(BulldozerInput)} found.", this);
                enabled = false;
                return;
            }

            enabledDummyField = typeof(BulldozerInput).GetField("enabledDummy", BindingFlags.Instance | BindingFlags.NonPublic);
            controlTypeField = typeof(BulldozerInput).GetField("controlType", BindingFlags.Instance | BindingFlags.NonPublic);
            movementControlTypeField = typeof(BulldozerInput).GetField("movementControlType", BindingFlags.Instance | BindingFlags.NonPublic);
            leftTrackField = typeof(BulldozerInput).GetField("left_track", BindingFlags.Instance | BindingFlags.NonPublic);
            rightTrackField = typeof(BulldozerInput).GetField("right_track", BindingFlags.Instance | BindingFlags.NonPublic);
            joints = target.GetComponent<BulldozerJoints>();

            if (controlTypeField != null)
            {
                originalControlType = (ControlType)controlTypeField.GetValue(target);
                originalControlTypeCaptured = true;
            }

            if (movementControlTypeField != null)
            {
                originalMovementControlType = (ConstractionMovementControlType)movementControlTypeField.GetValue(target);
                originalMovementControlTypeCaptured = true;
            }

            SetPrivateFloat("trackVelocityLimitKph", trackSpeedLimitKph);
            enabledDummyField?.SetValue(target, true);
            controlTypeField?.SetValue(target, ControlType.Speed);
            movementControlTypeField?.SetValue(target, ConstractionMovementControlType.ActuatorCommand);

            StartCoroutine(RunTest());
        }

        private void OnDisable()
        {
            RestoreState();
        }

        private IEnumerator RunTest()
        {
            Debug.Log($"[{nameof(BulldozerTrackSpeedLimitTester)}] Starting bidirectional track speed test with {cycleCount} cycles.", this);

            for (int cycle = 1; cycle <= cycleCount; cycle++)
            {
                yield return HoldTrackSpeed(cycle, "FORWARD", forwardSpeedRadiansPerSec);
                yield return HoldReverseForce(cycle, reverseSpeedRadiansPerSec);
            }

            RestoreState();
            Debug.Log($"[{nameof(BulldozerTrackSpeedLimitTester)}] Bidirectional track speed test completed.", this);
        }

        private IEnumerator HoldReverseForce(int cycle, double command)
        {
            yield return HoldTrackSpeed(cycle, "REVERSE", command);
        }

        private IEnumerator HoldTrackSpeed(int cycle, string phase, double command)
        {
            Debug.Log($"[{nameof(BulldozerTrackSpeedLimitTester)}] Track speed cycle {cycle} phase {phase}: command={command:F3}.", this);
            SetTrackCommands(command, command);

            float minLeftSpeed = float.PositiveInfinity;
            float maxLeftSpeed = float.NegativeInfinity;
            float minRightSpeed = float.PositiveInfinity;
            float maxRightSpeed = float.NegativeInfinity;
            float elapsed = 0.0f;
            float nextSample = 0.0f;

            while (elapsed < holdSeconds)
            {
                float leftSpeed = GetLeftSpeed();
                float rightSpeed = GetRightSpeed();
                minLeftSpeed = Mathf.Min(minLeftSpeed, leftSpeed);
                maxLeftSpeed = Mathf.Max(maxLeftSpeed, leftSpeed);
                minRightSpeed = Mathf.Min(minRightSpeed, rightSpeed);
                maxRightSpeed = Mathf.Max(maxRightSpeed, rightSpeed);

                if (elapsed >= nextSample)
                {
                    LogTrackSample(cycle, phase, command, elapsed);
                    nextSample += sampleIntervalSeconds;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            float finalLeftSpeed = GetLeftSpeed();
            float finalRightSpeed = GetRightSpeed();
            minLeftSpeed = Mathf.Min(minLeftSpeed, finalLeftSpeed);
            maxLeftSpeed = Mathf.Max(maxLeftSpeed, finalLeftSpeed);
            minRightSpeed = Mathf.Min(minRightSpeed, finalRightSpeed);
            maxRightSpeed = Mathf.Max(maxRightSpeed, finalRightSpeed);
            LogTrackSample(cycle, phase, command, holdSeconds);
            LogTrackSummary(cycle, phase, command, minLeftSpeed, maxLeftSpeed, minRightSpeed, maxRightSpeed, finalLeftSpeed, finalRightSpeed);
        }

        private void SetTrackCommands(double leftCommand, double rightCommand)
        {
            leftTrackField?.SetValue(target, leftCommand);
            rightTrackField?.SetValue(target, rightCommand);
        }

        private void RestoreState()
        {
            if (target == null)
                return;

            SetTrackCommands(0.0, 0.0);
            enabledDummyField?.SetValue(target, false);

            if (originalControlTypeCaptured)
                controlTypeField?.SetValue(target, originalControlType);

            if (originalMovementControlTypeCaptured)
                movementControlTypeField?.SetValue(target, originalMovementControlType);
        }

        private void SetPrivateFloat(string fieldName, float value)
        {
            FieldInfo field = typeof(BulldozerInput).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private float GetLeftSpeed()
        {
            return joints != null && joints.leftSprocket != null ? (float)joints.leftSprocket.CurrentSpeed : float.NaN;
        }

        private float GetRightSpeed()
        {
            return joints != null && joints.rightSprocket != null ? (float)joints.rightSprocket.CurrentSpeed : float.NaN;
        }

        private void LogTrackSample(int cycle, string phase, double command, float elapsed)
        {
            Debug.Log($"[{nameof(BulldozerTrackSpeedLimitTester)}] Track sample cycle={cycle}, phase={phase}, t={elapsed:F2}s, command={command:F3}, leftSpeed={GetLeftSpeed():F4}, rightSpeed={GetRightSpeed():F4}", this);
        }

        private void LogTrackSummary(int cycle, string phase, double command, float minLeftSpeed, float maxLeftSpeed, float minRightSpeed, float maxRightSpeed, float finalLeftSpeed, float finalRightSpeed)
        {
            float speedLimit = GetTrackSpeedLimitRadiansPerSec();
            bool leftStayedWithinSpeedLimit = Mathf.Abs(minLeftSpeed) <= speedLimit + speedLimit * 0.1f &&
                Mathf.Abs(maxLeftSpeed) <= speedLimit + speedLimit * 0.1f;
            bool rightStayedWithinSpeedLimit = Mathf.Abs(minRightSpeed) <= speedLimit + speedLimit * 0.1f &&
                Mathf.Abs(maxRightSpeed) <= speedLimit + speedLimit * 0.1f;
            Debug.Log($"[{nameof(BulldozerTrackSpeedLimitTester)}] Track summary cycle={cycle}, phase={phase}, command={command:F3}, speedLimit={speedLimit:F4}, leftMin={minLeftSpeed:F4}, leftMax={maxLeftSpeed:F4}, rightMin={minRightSpeed:F4}, rightMax={maxRightSpeed:F4}, finalLeft={finalLeftSpeed:F4}, finalRight={finalRightSpeed:F4}, leftStayedWithinSpeedLimit={leftStayedWithinSpeedLimit}, rightStayedWithinSpeedLimit={rightStayedWithinSpeedLimit}", this);
        }

        private float GetTrackSpeedLimitRadiansPerSec()
        {
            if (target?.twistCommandConvertor != null)
                return (float)target.twistCommandConvertor.GetTrackSpeedLimitRadiansPerSec(trackSpeedLimitKph);

            return 0.0f;
        }
    }
}
